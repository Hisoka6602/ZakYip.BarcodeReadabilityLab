namespace ZakYip.BarcodeReadabilityLab.Application.Workers;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZakYip.BarcodeReadabilityLab.Application.Options;
using ZakYip.BarcodeReadabilityLab.Application.Services;
using ZakYip.BarcodeReadabilityLab.Core.Domain.Models;
using ZakYip.BarcodeReadabilityLab.Infrastructure.MLNet.Contracts;

/// <summary>
/// 训练任务后台工作器
/// </summary>
public sealed class TrainingWorker : BackgroundService
{
    private readonly ILogger<TrainingWorker> _logger;
    private readonly TrainingJobService _trainingJobService;
    private readonly IImageClassificationTrainer _trainer;
    private readonly ITrainingProgressNotifier? _progressNotifier;
    private readonly IResourceMonitor? _resourceMonitor;
    private readonly TrainingOptions _trainingOptions;
    private readonly List<Task> _runningTasks = new();

    public TrainingWorker(
        ILogger<TrainingWorker> logger,
        ITrainingJobService trainingJobService,
        IImageClassificationTrainer trainer,
        IOptions<TrainingOptions> trainingOptions,
        ITrainingProgressNotifier? progressNotifier = null,
        IResourceMonitor? resourceMonitor = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        if (trainingJobService is not TrainingJobService concreteService)
            throw new ArgumentException("trainingJobService 必须是 TrainingJobService 类型", nameof(trainingJobService));
        
        _trainingJobService = concreteService;
        _trainer = trainer ?? throw new ArgumentNullException(nameof(trainer));
        _trainingOptions = trainingOptions?.Value ?? throw new ArgumentNullException(nameof(trainingOptions));
        _progressNotifier = progressNotifier;
        _resourceMonitor = resourceMonitor;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var maxConcurrency = _trainingJobService.GetMaxConcurrentTrainingJobs();
        _logger.LogInformation("训练任务工作器已启动，最大并发数: {MaxConcurrency}，资源监控: {ResourceMonitoringEnabled}",
            maxConcurrency, _trainingOptions.EnableResourceMonitoring);

        // 启动资源监控任务（如果启用）
        Task? monitoringTask = null;
        if (_trainingOptions.EnableResourceMonitoring && _resourceMonitor is not null)
        {
            monitoringTask = StartResourceMonitoringAsync(stoppingToken);
        }

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // 清理已完成的任务
                _runningTasks.RemoveAll(t => t.IsCompleted);

                // 尝试从队列中获取任务
                var job = _trainingJobService.TryDequeueJob();

                if (job.HasValue)
                {
                    var (jobId, request) = job.Value;
                    
                    // 启动新的训练任务（异步，不等待完成）
                    var trainingTask = Task.Run(async () =>
                    {
                        await ProcessTrainingJobAsync(jobId, request, stoppingToken);
                    }, stoppingToken);

                    _runningTasks.Add(trainingTask);

                    _logger.LogInformation("训练任务已启动，JobId: {JobId}，当前运行任务数: {RunningTaskCount}/{MaxConcurrency}",
                        jobId, _runningTasks.Count, maxConcurrency);
                }
                else
                {
                    // 队列为空，等待一段时间
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }

            // 等待所有运行中的任务完成
            if (_runningTasks.Count > 0)
            {
                _logger.LogInformation("等待 {Count} 个训练任务完成...", _runningTasks.Count);
                await Task.WhenAll(_runningTasks);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("训练任务工作器正在停止");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "训练任务工作器发生未处理的异常");
        }
        finally
        {
            // 等待资源监控任务完成
            if (monitoringTask is not null)
            {
                try
                {
                    await monitoringTask;
                }
                catch (OperationCanceledException)
                {
                    // 正常取消，忽略
                }
            }
        }

        _logger.LogInformation("训练任务工作器已停止");
    }

    /// <summary>
    /// 处理单个训练任务
    /// </summary>
    private async Task ProcessTrainingJobAsync(
        Guid jobId,
        TrainingRequest request,
        CancellationToken cancellationToken)
    {
        // 等待获取训练槽位
        await _trainingJobService.WaitForTrainingSlotAsync(cancellationToken);

        try
        {
            _logger.LogInformation("开始处理训练任务，JobId: {JobId}, 训练目录: {TrainingRootDirectory}，可用槽位: {AvailableSlots}/{MaxSlots}",
                jobId, request.TrainingRootDirectory,
                _trainingJobService.GetAvailableTrainingSlots(),
                _trainingJobService.GetMaxConcurrentTrainingJobs());

            // 更新任务状态为运行中
            await _trainingJobService.UpdateJobToRunning(jobId);

            // 创建进度回调
            var progressCallback = new TrainingProgressCallback(jobId, _trainingJobService, _progressNotifier, _logger);

            // 调用训练器执行训练
            _logger.LogInformation(
                "任务超参数 => 学习率: {LearningRate}, Epochs: {Epochs}, BatchSize: {BatchSize}, 验证集比例: {ValidationSplitRatio}",
                request.LearningRate,
                request.Epochs,
                request.BatchSize,
                request.ValidationSplitRatio ?? 0.0m);

            if (request.DataAugmentation.Enable)
            {
                _logger.LogInformation(
                    "执行训练前的数据增强配置 => 副本数: {Copies}, 旋转概率: {RotationProbability:P0}, 水平翻转概率: {HorizontalProbability:P0}, 垂直翻转概率: {VerticalProbability:P0}, 亮度概率: {BrightnessProbability:P0}",
                    request.DataAugmentation.AugmentedImagesPerSample,
                    request.DataAugmentation.RotationProbability,
                    request.DataAugmentation.HorizontalFlipProbability,
                    request.DataAugmentation.VerticalFlipProbability,
                    request.DataAugmentation.BrightnessProbability);
            }

            if (request.DataBalancing.Strategy != DataBalancingStrategy.None)
            {
                _logger.LogInformation(
                    "执行训练前的数据平衡配置 => 策略: {Strategy}, 目标样本数: {Target}, 是否乱序: {Shuffle}",
                    request.DataBalancing.Strategy,
                    request.DataBalancing.TargetSampleCountPerClass,
                    request.DataBalancing.ShuffleAfterBalancing);
            }

            // 根据是否启用迁移学习选择不同的训练方法
            var trainingResult = request.TransferLearningOptions?.Enable == true
                ? await _trainer.TrainWithTransferLearningAsync(
                    request.TrainingRootDirectory,
                    request.OutputModelDirectory,
                    request.LearningRate,
                    request.Epochs,
                    request.BatchSize,
                    request.ValidationSplitRatio,
                    request.TransferLearningOptions,
                    request.DataAugmentation,
                    request.DataBalancing,
                    progressCallback,
                    cancellationToken)
                : await _trainer.TrainAsync(
                    request.TrainingRootDirectory,
                    request.OutputModelDirectory,
                    request.LearningRate,
                    request.Epochs,
                    request.BatchSize,
                    request.ValidationSplitRatio,
                    request.DataAugmentation,
                    request.DataBalancing,
                    progressCallback,
                    cancellationToken);

            _logger.LogInformation("训练任务完成，JobId: {JobId}, 模型文件: {ModelFilePath}, 准确率: {Accuracy:P2}",
                jobId, trainingResult.ModelFilePath, trainingResult.EvaluationMetrics.Accuracy);

            // 更新任务状态为完成，并保存评估指标
            await _trainingJobService.UpdateJobToCompleted(jobId, trainingResult.EvaluationMetrics);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("训练任务被取消，JobId: {JobId}", jobId);
            await _trainingJobService.UpdateJobToFailed(jobId, "训练任务被取消");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "训练任务执行失败，JobId: {JobId}", jobId);
            await _trainingJobService.UpdateJobToFailed(jobId, $"训练任务执行失败: {ex.Message}");
        }
        finally
        {
            // 释放训练槽位
            _trainingJobService.ReleaseTrainingSlot();
            
            _logger.LogInformation("训练任务已结束，JobId: {JobId}，释放槽位，当前可用槽位: {AvailableSlots}/{MaxSlots}",
                jobId,
                _trainingJobService.GetAvailableTrainingSlots(),
                _trainingJobService.GetMaxConcurrentTrainingJobs());
        }
    }

    /// <summary>
    /// 启动资源监控任务
    /// </summary>
    private async Task StartResourceMonitoringAsync(CancellationToken cancellationToken)
    {
        if (_resourceMonitor is null)
            return;

        var intervalSeconds = Math.Max(1, _trainingOptions.ResourceMonitoringIntervalSeconds);
        var interval = TimeSpan.FromSeconds(intervalSeconds);

        _logger.LogInformation("资源监控已启动，监控间隔: {IntervalSeconds} 秒", intervalSeconds);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var usage = await _resourceMonitor.GetCurrentUsageAsync(cancellationToken);

                    _logger.LogInformation(
                        "系统资源使用情况 => CPU: {CpuUsage:F2}%, 内存: {MemoryUsage:F2}% ({UsedMemoryMB:F0} MB / {TotalMemoryMB:F0} MB), 运行中任务: {RunningTasks}/{MaxTasks}",
                        usage.CpuUsagePercent,
                        usage.MemoryUsagePercent,
                        usage.UsedMemoryBytes / 1024m / 1024m,
                        usage.TotalMemoryBytes / 1024m / 1024m,
                        _trainingJobService.GetMaxConcurrentTrainingJobs() - _trainingJobService.GetAvailableTrainingSlots(),
                        _trainingJobService.GetMaxConcurrentTrainingJobs());
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "资源监控出现错误");
                }

                await Task.Delay(interval, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("资源监控已停止");
        }
    }

    /// <summary>
    /// 训练进度回调实现
    /// </summary>
    private sealed class TrainingProgressCallback : ITrainingProgressCallback
    {
        private readonly Guid _jobId;
        private readonly TrainingJobService _trainingJobService;
        private readonly ITrainingProgressNotifier? _progressNotifier;
        private readonly ILogger<TrainingWorker> _logger;

        public TrainingProgressCallback(
            Guid jobId,
            TrainingJobService trainingJobService,
            ITrainingProgressNotifier? progressNotifier,
            ILogger<TrainingWorker> logger)
        {
            _jobId = jobId;
            _trainingJobService = trainingJobService;
            _progressNotifier = progressNotifier;
            _logger = logger;
        }

        public void ReportProgress(decimal progress, string? message = null)
        {
            // 异步更新进度，不阻塞训练过程
            Task.Run(async () =>
            {
                try
                {
                    // 更新数据库中的进度
                    await _trainingJobService.UpdateJobProgress(_jobId, progress);
                    
                    // 通过 SignalR 推送进度更新
                    if (_progressNotifier is not null)
                    {
                        await _progressNotifier.NotifyProgressAsync(_jobId, progress, message);
                    }
                    
                    if (!string.IsNullOrEmpty(message))
                    {
                        _logger.LogInformation("训练进度更新 => JobId: {JobId}, 进度: {Progress:P0}, 消息: {Message}",
                            _jobId, progress, message);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "更新训练进度失败，JobId: {JobId}", _jobId);
                }
            });
        }

        public void ReportDetailedProgress(TrainingProgressInfo progressInfo)
        {
            // 异步更新进度，不阻塞训练过程
            Task.Run(async () =>
            {
                try
                {
                    // 更新数据库中的进度
                    await _trainingJobService.UpdateJobProgress(_jobId, progressInfo.Progress);
                    
                    // 通过 SignalR 推送详细进度更新
                    if (_progressNotifier is not null)
                    {
                        await _progressNotifier.NotifyDetailedProgressAsync(progressInfo);
                    }
                    
                    var logMessage = progressInfo.Message ?? progressInfo.Stage.ToString();
                    _logger.LogInformation(
                        "详细进度更新 => JobId: {JobId}, 阶段: {Stage}, 进度: {Progress:P0}, ETA: {ETA}秒, 消息: {Message}",
                        _jobId, 
                        progressInfo.Stage,
                        progressInfo.Progress,
                        progressInfo.EstimatedRemainingSeconds,
                        logMessage);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "更新详细训练进度失败，JobId: {JobId}", _jobId);
                }
            });
        }
    }
}
