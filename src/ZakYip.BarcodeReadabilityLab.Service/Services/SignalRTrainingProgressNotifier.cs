namespace ZakYip.BarcodeReadabilityLab.Service.Services;

using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.AspNetCore.SignalR;
using ZakYip.BarcodeReadabilityLab.Application.Services;
using ZakYip.BarcodeReadabilityLab.Core.Domain.Models;
using ZakYip.BarcodeReadabilityLab.Core.Enum;
using ZakYip.BarcodeReadabilityLab.Service.Hubs;

/// <summary>
/// SignalR 训练进度通知服务实现（支持批量推送和节流）
/// </summary>
public sealed class SignalRTrainingProgressNotifier : ITrainingProgressNotifier, IAsyncDisposable
{
    private readonly IHubContext<TrainingProgressHub> _hubContext;
    private readonly ILogger<SignalRTrainingProgressNotifier> _logger;
    private readonly Channel<TrainingProgressInfo> _progressChannel;
    private readonly Task _processingTask;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly ConcurrentDictionary<Guid, DateTime> _lastUpdateTimes;
    
    // 配置参数
    private readonly TimeSpan _throttleInterval = TimeSpan.FromMilliseconds(500); // 每个任务最小更新间隔
    private readonly int _maxBatchSize = 10; // 批量推送最大数量
    private readonly TimeSpan _batchTimeout = TimeSpan.FromMilliseconds(100); // 批量等待超时

    public SignalRTrainingProgressNotifier(
        IHubContext<TrainingProgressHub> hubContext,
        ILogger<SignalRTrainingProgressNotifier> logger)
    {
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _progressChannel = Channel.CreateUnbounded<TrainingProgressInfo>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
        
        _lastUpdateTimes = new ConcurrentDictionary<Guid, DateTime>();
        _cancellationTokenSource = new CancellationTokenSource();
        
        // 启动后台处理任务
        _processingTask = ProcessProgressUpdatesAsync(_cancellationTokenSource.Token);
        
        _logger.LogInformation("SignalR 训练进度通知服务已启动，节流间隔: {ThrottleMs}ms，批量大小: {BatchSize}",
            _throttleInterval.TotalMilliseconds, _maxBatchSize);
    }

    /// <inheritdoc />
    public async Task NotifyProgressAsync(Guid jobId, decimal progress, string? message = null)
    {
        var progressInfo = new TrainingProgressInfo
        {
            JobId = jobId,
            Progress = progress,
            Stage = TrainingStage.Training,
            Message = message,
            Timestamp = DateTime.UtcNow
        };

        await NotifyDetailedProgressAsync(progressInfo);
    }

    /// <inheritdoc />
    public async Task NotifyDetailedProgressAsync(TrainingProgressInfo progressInfo)
    {
        try
        {
            // 检查是否需要节流
            if (ShouldThrottle(progressInfo.JobId))
            {
                _logger.LogTrace("进度更新被节流 => JobId: {JobId}, 进度: {Progress:P0}",
                    progressInfo.JobId, progressInfo.Progress);
                return;
            }

            // 将进度信息放入通道
            await _progressChannel.Writer.WriteAsync(progressInfo);
            
            // 更新最后更新时间
            _lastUpdateTimes[progressInfo.JobId] = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "入队训练进度失败 => JobId: {JobId}", progressInfo.JobId);
        }
    }

    /// <summary>
    /// 检查是否需要节流
    /// </summary>
    private bool ShouldThrottle(Guid jobId)
    {
        if (!_lastUpdateTimes.TryGetValue(jobId, out var lastUpdateTime))
        {
            return false;
        }

        var elapsed = DateTime.UtcNow - lastUpdateTime;
        return elapsed < _throttleInterval;
    }

    /// <summary>
    /// 后台处理进度更新（批量推送）
    /// </summary>
    private async Task ProcessProgressUpdatesAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("进度更新处理任务已启动");

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var batch = new List<TrainingProgressInfo>(_maxBatchSize);

                try
                {
                    // 读取第一个进度更新
                    var firstProgress = await _progressChannel.Reader.ReadAsync(cancellationToken);
                    batch.Add(firstProgress);

                    // 尝试读取更多进度更新以组成批次
                    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    timeoutCts.CancelAfter(_batchTimeout);

                    while (batch.Count < _maxBatchSize)
                    {
                        if (_progressChannel.Reader.TryRead(out var nextProgress))
                        {
                            batch.Add(nextProgress);
                        }
                        else
                        {
                            break;
                        }
                    }

                    // 批量推送
                    await PushBatchAsync(batch, cancellationToken);
                }
                catch (ChannelClosedException)
                {
                    // 通道已关闭，正常退出
                    break;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    // 正常取消，退出循环
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "处理进度更新批次失败");
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("进度更新处理任务已取消");
        }
        finally
        {
            _logger.LogInformation("进度更新处理任务已停止");
        }
    }

    /// <summary>
    /// 批量推送进度更新
    /// </summary>
    private async Task PushBatchAsync(List<TrainingProgressInfo> batch, CancellationToken cancellationToken)
    {
        if (batch.Count == 0)
        {
            return;
        }

        try
        {
            // 按任务分组
            var groupedByJob = batch.GroupBy(p => p.JobId);

            foreach (var group in groupedByJob)
            {
                var jobId = group.Key;
                var updates = group.ToList();
                
                // 对于同一任务，只推送最新的更新
                var latestUpdate = updates.OrderByDescending(u => u.Timestamp).First();
                
                var groupName = $"training-job-{jobId}";
                
                await _hubContext.Clients.Group(groupName).SendAsync(
                    "DetailedProgressUpdated",
                    new
                    {
                        jobId = latestUpdate.JobId,
                        progress = latestUpdate.Progress,
                        stage = latestUpdate.Stage.ToString(),
                        message = latestUpdate.Message,
                        startTime = latestUpdate.StartTime,
                        estimatedRemainingSeconds = latestUpdate.EstimatedRemainingSeconds,
                        estimatedCompletionTime = latestUpdate.EstimatedCompletionTime,
                        metrics = latestUpdate.Metrics is not null ? new
                        {
                            currentEpoch = latestUpdate.Metrics.CurrentEpoch,
                            totalEpochs = latestUpdate.Metrics.TotalEpochs,
                            accuracy = latestUpdate.Metrics.Accuracy,
                            loss = latestUpdate.Metrics.Loss,
                            learningRate = latestUpdate.Metrics.LearningRate
                        } : null,
                        timestamp = latestUpdate.Timestamp
                    },
                    cancellationToken);

                _logger.LogDebug(
                    "通过 SignalR 推送训练进度 => JobId: {JobId}, 阶段: {Stage}, 进度: {Progress:P0}, ETA: {ETA}秒",
                    jobId, 
                    latestUpdate.Stage, 
                    latestUpdate.Progress,
                    latestUpdate.EstimatedRemainingSeconds);
            }

            if (batch.Count > 1)
            {
                _logger.LogTrace("批量推送了 {Count} 个进度更新", batch.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量推送训练进度失败");
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        _logger.LogInformation("正在停止 SignalR 训练进度通知服务...");
        
        // 停止接收新的更新
        _progressChannel.Writer.Complete();
        
        // 取消后台任务
        _cancellationTokenSource.Cancel();
        
        try
        {
            // 等待处理任务完成
            await _processingTask;
        }
        catch (OperationCanceledException)
        {
            // 预期的取消，忽略
        }
        
        _cancellationTokenSource.Dispose();
        _logger.LogInformation("SignalR 训练进度通知服务已停止");
    }
}
