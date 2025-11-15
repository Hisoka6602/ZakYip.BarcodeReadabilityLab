namespace ZakYip.BarcodeReadabilityLab.Application.Services;

using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZakYip.BarcodeReadabilityLab.Application.Options;
using ZakYip.BarcodeReadabilityLab.Core.Domain.Contracts;
using ZakYip.BarcodeReadabilityLab.Core.Domain.Exceptions;
using ZakYip.BarcodeReadabilityLab.Core.Domain.Models;

/// <summary>
/// 训练任务服务实现
/// </summary>
public sealed class TrainingJobService : ITrainingJobService, IDisposable
{
    private readonly ILogger<TrainingJobService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConcurrentQueue<(Guid jobId, TrainingRequest request)> _jobQueue;
    private readonly SemaphoreSlim _concurrencySemaphore;
    private readonly TrainingOptions _trainingOptions;
    private bool _disposed;

    public TrainingJobService(
        ILogger<TrainingJobService> logger,
        IServiceScopeFactory scopeFactory,
        IOptions<TrainingOptions> trainingOptions)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _trainingOptions = trainingOptions?.Value ?? throw new ArgumentNullException(nameof(trainingOptions));
        _jobQueue = new ConcurrentQueue<(Guid, TrainingRequest)>();

        // 初始化并发控制信号量
        var maxConcurrency = Math.Max(1, _trainingOptions.MaxConcurrentTrainingJobs);
        _concurrencySemaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);

        _logger.LogInformation("训练任务服务已初始化，最大并发数: {MaxConcurrency}", maxConcurrency);
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
            LearningRate = request.LearningRate,
            Epochs = request.Epochs,
            BatchSize = request.BatchSize,
            Status = TrainingJobState.Queued,
            Progress = 0.0m,
            StartTime = DateTimeOffset.UtcNow,
            Remarks = request.Remarks,
            DataAugmentation = request.DataAugmentation,
            DataBalancing = request.DataBalancing
        };

        // 持久化到数据库
        using (var scope = _scopeFactory.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<ITrainingJobRepository>();
            await repository.AddAsync(trainingJob, cancellationToken);
        }

        // 加入队列
        _jobQueue.Enqueue((jobId, request));

        _logger.LogInformation(
            "训练任务已加入队列 => JobId: {JobId}, 训练目录: {TrainingRootDirectory}, 输出目录: {OutputModelDirectory}, 验证比例: {ValidationSplitRatio}, 学习率: {LearningRate}, Epochs: {Epochs}, BatchSize: {BatchSize}",
            jobId,
            request.TrainingRootDirectory,
            request.OutputModelDirectory,
            request.ValidationSplitRatio ?? 0.2m,
            request.LearningRate,
            request.Epochs,
            request.BatchSize);

        if (request.DataAugmentation.Enable)
        {
            _logger.LogInformation(
                "数据增强配置 => 副本数: {Copies}, 旋转: {RotationEnabled}/{RotationProbability:P0}, 水平翻转: {HorizontalEnabled}/{HorizontalProbability:P0}, 垂直翻转: {VerticalEnabled}/{VerticalProbability:P0}, 亮度: {BrightnessEnabled}/{BrightnessProbability:P0}",
                request.DataAugmentation.AugmentedImagesPerSample,
                request.DataAugmentation.EnableRotation,
                request.DataAugmentation.RotationProbability,
                request.DataAugmentation.EnableHorizontalFlip,
                request.DataAugmentation.HorizontalFlipProbability,
                request.DataAugmentation.EnableVerticalFlip,
                request.DataAugmentation.VerticalFlipProbability,
                request.DataAugmentation.EnableBrightnessAdjustment,
                request.DataAugmentation.BrightnessProbability);
        }

        if (request.DataBalancing.Strategy != DataBalancingStrategy.None)
        {
            _logger.LogInformation(
                "数据平衡配置 => 策略: {Strategy}, 目标样本数: {Target}, 是否乱序: {Shuffle}",
                request.DataBalancing.Strategy,
                request.DataBalancing.TargetSampleCountPerClass,
                request.DataBalancing.ShuffleAfterBalancing);
        }

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
            LearningRate = trainingJob.LearningRate,
            Epochs = trainingJob.Epochs,
            BatchSize = trainingJob.BatchSize,
            StartTime = trainingJob.StartTime,
            CompletedTime = trainingJob.CompletedTime,
            ErrorMessage = trainingJob.ErrorMessage,
            Remarks = trainingJob.Remarks,
            DataAugmentation = trainingJob.DataAugmentation,
            DataBalancing = trainingJob.DataBalancing,
            EvaluationMetrics = trainingJob.EvaluationMetrics
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
                LearningRate = job.LearningRate,
                Epochs = job.Epochs,
                BatchSize = job.BatchSize,
                StartTime = job.StartTime,
                CompletedTime = job.CompletedTime,
                ErrorMessage = job.ErrorMessage,
                Remarks = job.Remarks,
                DataAugmentation = job.DataAugmentation,
                DataBalancing = job.DataBalancing,
                EvaluationMetrics = job.EvaluationMetrics
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
    /// 等待获取并发训练槽位
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    internal async Task WaitForTrainingSlotAsync(CancellationToken cancellationToken = default)
    {
        await _concurrencySemaphore.WaitAsync(cancellationToken);
        _logger.LogDebug("获取到训练槽位，当前可用槽位数: {AvailableSlots}", _concurrencySemaphore.CurrentCount);
    }

    /// <summary>
    /// 释放并发训练槽位
    /// </summary>
    internal void ReleaseTrainingSlot()
    {
        _concurrencySemaphore.Release();
        _logger.LogDebug("释放训练槽位，当前可用槽位数: {AvailableSlots}", _concurrencySemaphore.CurrentCount);
    }

    /// <summary>
    /// 获取当前可用的训练槽位数
    /// </summary>
    internal int GetAvailableTrainingSlots()
    {
        return _concurrencySemaphore.CurrentCount;
    }

    /// <summary>
    /// 获取最大并发训练任务数
    /// </summary>
    internal int GetMaxConcurrentTrainingJobs()
    {
        return _trainingOptions.MaxConcurrentTrainingJobs;
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
    /// 更新任务进度
    /// </summary>
    internal async Task UpdateJobProgress(Guid jobId, decimal progress)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ITrainingJobRepository>();
        var trainingJob = await repository.GetByIdAsync(jobId);
        
        if (trainingJob is null)
            return;

        var updatedJob = trainingJob with
        {
            Progress = progress
        };

        await repository.UpdateAsync(updatedJob);

        _logger.LogDebug("训练任务进度更新 => JobId: {JobId}, 进度: {Progress:P0}", jobId, progress);
    }

    /// <summary>
    /// 更新任务状态为完成
    /// </summary>
    internal async Task UpdateJobToCompleted(Guid jobId, ModelEvaluationMetrics? evaluationMetrics = null)
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
            CompletedTime = DateTimeOffset.UtcNow,
            EvaluationMetrics = evaluationMetrics
        };

        await repository.UpdateAsync(updatedJob);

        _logger.LogInformation("训练任务已完成 => JobId: {JobId}, 状态: {Status}, 进度: {Progress:P0}, 准确率: {Accuracy:P2}", 
            jobId, TrainingJobState.Completed, 1.0m, evaluationMetrics?.Accuracy ?? 0m);
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

        if (request.LearningRate <= 0m || request.LearningRate > 1m)
            throw new TrainingException("学习率必须在 0 到 1 之间（不含 0）", "INVALID_LEARNING_RATE");

        if (request.Epochs < 1 || request.Epochs > 500)
            throw new TrainingException("Epoch 数必须在 1 到 500 之间", "INVALID_EPOCHS");

        if (request.BatchSize < 1 || request.BatchSize > 512)
            throw new TrainingException("Batch Size 必须在 1 到 512 之间", "INVALID_BATCH_SIZE");

        var augmentation = request.DataAugmentation;
        if (augmentation is null)
            throw new TrainingException("数据增强配置不能为空", "AUGMENTATION_NULL");

        if (augmentation.AugmentedImagesPerSample < 0)
            throw new TrainingException("数据增强副本数量不能为负数", "INVALID_AUGMENTATION_COPIES");

        if (augmentation.EvaluationAugmentedImagesPerSample < 1)
            throw new TrainingException("评估增强副本数量至少为 1", "INVALID_EVAL_AUGMENTATION_COPIES");

        if (augmentation.RotationAngles is null)
            throw new TrainingException("旋转角度集合不能为空", "INVALID_ROTATION_ANGLES");

        ValidateProbability(augmentation.RotationProbability, "旋转概率", "INVALID_ROTATION_PROBABILITY");
        ValidateProbability(augmentation.HorizontalFlipProbability, "水平翻转概率", "INVALID_HFLIP_PROBABILITY");
        ValidateProbability(augmentation.VerticalFlipProbability, "垂直翻转概率", "INVALID_VFLIP_PROBABILITY");
        ValidateProbability(augmentation.BrightnessProbability, "亮度调整概率", "INVALID_BRIGHTNESS_PROBABILITY");

        if (augmentation.BrightnessLower <= 0 || augmentation.BrightnessUpper <= 0)
            throw new TrainingException("亮度调整范围必须大于 0", "INVALID_BRIGHTNESS_RANGE");

        if (augmentation.BrightnessLower > augmentation.BrightnessUpper)
            throw new TrainingException("亮度调整下限不能大于上限", "INVALID_BRIGHTNESS_RANGE");

        var balancing = request.DataBalancing;
        if (balancing is null)
            throw new TrainingException("数据平衡配置不能为空", "BALANCING_NULL");

        if (balancing.TargetSampleCountPerClass.HasValue && balancing.TargetSampleCountPerClass <= 0)
            throw new TrainingException("数据平衡目标样本数必须大于 0", "INVALID_BALANCING_TARGET");
    }

    private static void ValidateProbability(double value, string displayName, string errorCode)
    {
        if (value < 0d || value > 1d)
            throw new TrainingException($"{displayName} 必须在 0 到 1 之间", errorCode);
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _concurrencySemaphore?.Dispose();
        _disposed = true;

        _logger.LogInformation("训练任务服务资源已释放");
    }
}
