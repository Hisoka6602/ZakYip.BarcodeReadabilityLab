namespace ZakYip.BarcodeReadabilityLab.Application.Services;

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

/// <summary>
/// 训练任务服务实现
/// </summary>
public sealed class TrainingJobService : ITrainingJobService
{
    private readonly ILogger<TrainingJobService> _logger;
    private readonly ConcurrentDictionary<Guid, TrainingJobStatus> _jobs;
    private readonly ConcurrentQueue<(Guid jobId, TrainingRequest request)> _jobQueue;

    public TrainingJobService(ILogger<TrainingJobService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _jobs = new ConcurrentDictionary<Guid, TrainingJobStatus>();
        _jobQueue = new ConcurrentQueue<(Guid, TrainingRequest)>();
    }

    /// <inheritdoc />
    public ValueTask<Guid> StartTrainingAsync(TrainingRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        ValidateRequest(request);

        var jobId = Guid.NewGuid();

        // 创建初始任务状态
        var jobStatus = new TrainingJobStatus
        {
            JobId = jobId,
            Status = TrainingStatus.Queued,
            Progress = 0.0m,
            StartTime = DateTimeOffset.UtcNow,
            Remarks = request.Remarks
        };

        // 添加到任务字典
        _jobs[jobId] = jobStatus;

        // 加入队列
        _jobQueue.Enqueue((jobId, request));

        _logger.LogInformation("训练任务已加入队列，JobId: {JobId}, 训练目录: {TrainingRootDirectory}",
            jobId, request.TrainingRootDirectory);

        return ValueTask.FromResult(jobId);
    }

    /// <inheritdoc />
    public ValueTask<TrainingJobStatus?> GetStatusAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var status = _jobs.TryGetValue(jobId, out var jobStatus) ? jobStatus : null;
        return ValueTask.FromResult(status);
    }

    /// <summary>
    /// 尝试从队列中取出一个训练任务
    /// </summary>
    /// <returns>如果队列中有任务则返回 (jobId, request)，否则返回 null</returns>
    internal (Guid jobId, TrainingRequest request)? TryDequeueJob()
    {
        if (_jobQueue.TryDequeue(out var job))
        {
            return job;
        }

        return null;
    }

    /// <summary>
    /// 更新任务状态
    /// </summary>
    internal void UpdateJobStatus(Guid jobId, Action<TrainingJobStatus> updateAction)
    {
        if (_jobs.TryGetValue(jobId, out var currentStatus))
        {
            // 创建新的状态对象（record 是不可变的）
            var newStatus = currentStatus with { };
            updateAction(newStatus);

            // 更新字典中的状态
            _jobs[jobId] = newStatus;
        }
    }

    /// <summary>
    /// 更新任务状态为运行中
    /// </summary>
    internal void UpdateJobToRunning(Guid jobId)
    {
        if (_jobs.TryGetValue(jobId, out var currentStatus))
        {
            var newStatus = currentStatus with
            {
                Status = TrainingStatus.Running,
                Progress = 0.0m
            };

            _jobs[jobId] = newStatus;

            _logger.LogInformation("训练任务开始执行，JobId: {JobId}", jobId);
        }
    }

    /// <summary>
    /// 更新任务状态为完成
    /// </summary>
    internal void UpdateJobToCompleted(Guid jobId)
    {
        if (_jobs.TryGetValue(jobId, out var currentStatus))
        {
            var newStatus = currentStatus with
            {
                Status = TrainingStatus.Completed,
                Progress = 1.0m,
                CompletedTime = DateTimeOffset.UtcNow
            };

            _jobs[jobId] = newStatus;

            _logger.LogInformation("训练任务已完成，JobId: {JobId}", jobId);
        }
    }

    /// <summary>
    /// 更新任务状态为失败
    /// </summary>
    internal void UpdateJobToFailed(Guid jobId, string errorMessage)
    {
        if (_jobs.TryGetValue(jobId, out var currentStatus))
        {
            var newStatus = currentStatus with
            {
                Status = TrainingStatus.Failed,
                CompletedTime = DateTimeOffset.UtcNow,
                ErrorMessage = errorMessage
            };

            _jobs[jobId] = newStatus;

            _logger.LogError("训练任务失败，JobId: {JobId}, 错误: {ErrorMessage}", jobId, errorMessage);
        }
    }

    /// <summary>
    /// 验证训练请求
    /// </summary>
    private void ValidateRequest(TrainingRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TrainingRootDirectory))
            throw new ArgumentException("训练根目录路径不能为空", nameof(request.TrainingRootDirectory));

        if (string.IsNullOrWhiteSpace(request.OutputModelDirectory))
            throw new ArgumentException("输出模型目录路径不能为空", nameof(request.OutputModelDirectory));

        if (!Directory.Exists(request.TrainingRootDirectory))
            throw new DirectoryNotFoundException($"训练根目录不存在: {request.TrainingRootDirectory}");

        if (request.ValidationSplitRatio.HasValue)
        {
            var ratio = request.ValidationSplitRatio.Value;
            if (ratio < 0.0m || ratio > 1.0m)
                throw new ArgumentException("验证集分割比例必须在 0.0 到 1.0 之间", nameof(request.ValidationSplitRatio));
        }
    }
}
