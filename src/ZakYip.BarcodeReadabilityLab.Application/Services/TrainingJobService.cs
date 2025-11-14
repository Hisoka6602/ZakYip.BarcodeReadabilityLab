namespace ZakYip.BarcodeReadabilityLab.Application.Services;

using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ZakYip.BarcodeReadabilityLab.Core.Domain.Contracts;
using ZakYip.BarcodeReadabilityLab.Core.Domain.Exceptions;
using ZakYip.BarcodeReadabilityLab.Core.Domain.Models;

/// <summary>
/// 训练任务服务实现
/// </summary>
public sealed class TrainingJobService : ITrainingJobService
{
    private readonly ILogger<TrainingJobService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConcurrentQueue<(Guid jobId, TrainingRequest request)> _jobQueue;

    public TrainingJobService(
        ILogger<TrainingJobService> logger,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _jobQueue = new ConcurrentQueue<(Guid, TrainingRequest)>();
    }

    /// <inheritdoc />
    public async ValueTask<Guid> StartTrainingAsync(TrainingRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        ValidateRequest(request);

        var jobId = Guid.NewGuid();

        // 创建训练任务领域模型
        var trainingJob = new TrainingJob
        {
            JobId = jobId,
            TrainingRootDirectory = request.TrainingRootDirectory,
            OutputModelDirectory = request.OutputModelDirectory,
            ValidationSplitRatio = request.ValidationSplitRatio,
            Status = TrainingJobState.Queued,
            Progress = 0.0m,
            StartTime = DateTimeOffset.UtcNow,
            Remarks = request.Remarks
        };

        // 持久化到数据库
        using (var scope = _scopeFactory.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<ITrainingJobRepository>();
            await repository.AddAsync(trainingJob, cancellationToken);
        }

        // 加入队列
        _jobQueue.Enqueue((jobId, request));

        _logger.LogInformation("训练任务已加入队列 => JobId: {JobId}, 训练目录: {TrainingRootDirectory}, 输出目录: {OutputModelDirectory}, 验证比例: {ValidationSplitRatio}",
            jobId, request.TrainingRootDirectory, request.OutputModelDirectory, request.ValidationSplitRatio ?? 0.2m);

        return jobId;
    }

    /// <inheritdoc />
    public async ValueTask<TrainingJobStatus?> GetStatusAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ITrainingJobRepository>();
        var trainingJob = await repository.GetByIdAsync(jobId, cancellationToken);
        
        if (trainingJob is null)
            return null;

        // 转换为应用层状态对象
        return new TrainingJobStatus
        {
            JobId = trainingJob.JobId,
            Status = MapToTrainingStatus(trainingJob.Status),
            Progress = trainingJob.Progress,
            StartTime = trainingJob.StartTime,
            CompletedTime = trainingJob.CompletedTime,
            ErrorMessage = trainingJob.ErrorMessage,
            Remarks = trainingJob.Remarks
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TrainingJobStatus>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ITrainingJobRepository>();
        var trainingJobs = await repository.GetAllAsync(cancellationToken);
        
        return trainingJobs
            .Select(job => new TrainingJobStatus
            {
                JobId = job.JobId,
                Status = MapToTrainingStatus(job.Status),
                Progress = job.Progress,
                StartTime = job.StartTime,
                CompletedTime = job.CompletedTime,
                ErrorMessage = job.ErrorMessage,
                Remarks = job.Remarks
            })
            .ToList();
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
    internal async Task UpdateJobStatus(Guid jobId, Action<TrainingJob> updateAction)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ITrainingJobRepository>();
        var trainingJob = await repository.GetByIdAsync(jobId);
        
        if (trainingJob is null)
            return;

        // 应用更新操作
        updateAction(trainingJob);

        // 保存更新后的任务
        await repository.UpdateAsync(trainingJob);
    }

    /// <summary>
    /// 更新任务状态为运行中
    /// </summary>
    internal async Task UpdateJobToRunning(Guid jobId)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ITrainingJobRepository>();
        var trainingJob = await repository.GetByIdAsync(jobId);
        
        if (trainingJob is null)
            return;

        var updatedJob = trainingJob with
        {
            Status = TrainingJobState.Running,
            Progress = 0.0m
        };

        await repository.UpdateAsync(updatedJob);

        _logger.LogInformation("训练任务开始执行 => JobId: {JobId}, 状态: {Status}", jobId, TrainingJobState.Running);
    }

    /// <summary>
    /// 更新任务状态为完成
    /// </summary>
    internal async Task UpdateJobToCompleted(Guid jobId)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ITrainingJobRepository>();
        var trainingJob = await repository.GetByIdAsync(jobId);
        
        if (trainingJob is null)
            return;

        var updatedJob = trainingJob with
        {
            Status = TrainingJobState.Completed,
            Progress = 1.0m,
            CompletedTime = DateTimeOffset.UtcNow
        };

        await repository.UpdateAsync(updatedJob);

        _logger.LogInformation("训练任务已完成 => JobId: {JobId}, 状态: {Status}, 进度: {Progress:P0}", 
            jobId, TrainingJobState.Completed, 1.0m);
    }

    /// <summary>
    /// 更新任务状态为失败
    /// </summary>
    internal async Task UpdateJobToFailed(Guid jobId, string errorMessage)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ITrainingJobRepository>();
        var trainingJob = await repository.GetByIdAsync(jobId);
        
        if (trainingJob is null)
            return;

        var updatedJob = trainingJob with
        {
            Status = TrainingJobState.Failed,
            CompletedTime = DateTimeOffset.UtcNow,
            ErrorMessage = errorMessage
        };

        await repository.UpdateAsync(updatedJob);

        _logger.LogError("训练任务失败 => JobId: {JobId}, 状态: {Status}, 错误: {ErrorMessage}", 
            jobId, TrainingJobState.Failed, errorMessage);
    }

    /// <summary>
    /// 映射领域状态到应用层状态
    /// </summary>
    private static TrainingStatus MapToTrainingStatus(TrainingJobState state)
    {
        return state switch
        {
            TrainingJobState.Queued => TrainingStatus.Queued,
            TrainingJobState.Running => TrainingStatus.Running,
            TrainingJobState.Completed => TrainingStatus.Completed,
            TrainingJobState.Failed => TrainingStatus.Failed,
            TrainingJobState.Cancelled => TrainingStatus.Cancelled,
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, "未知的训练任务状态")
        };
    }

    /// <summary>
    /// 验证训练请求
    /// </summary>
    private void ValidateRequest(TrainingRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TrainingRootDirectory))
            throw new TrainingException("训练根目录路径不能为空", "TRAIN_DIR_EMPTY");

        if (string.IsNullOrWhiteSpace(request.OutputModelDirectory))
            throw new TrainingException("输出模型目录路径不能为空", "OUTPUT_DIR_EMPTY");

        if (!Directory.Exists(request.TrainingRootDirectory))
            throw new TrainingException($"训练根目录不存在: {request.TrainingRootDirectory}", "TRAIN_DIR_NOT_FOUND");

        if (request.ValidationSplitRatio.HasValue)
        {
            var ratio = request.ValidationSplitRatio.Value;
            if (ratio < 0.0m || ratio > 1.0m)
                throw new TrainingException("验证集分割比例必须在 0.0 到 1.0 之间", "INVALID_SPLIT_RATIO");
        }
    }
}
