namespace ZakYip.BarcodeReadabilityLab.Application.Workers;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ZakYip.BarcodeReadabilityLab.Application.Services;
using ZakYip.BarcodeReadabilityLab.Infrastructure.MLNet.Contracts;

/// <summary>
/// 训练任务后台工作器
/// </summary>
public sealed class TrainingWorker : BackgroundService
{
    private readonly ILogger<TrainingWorker> _logger;
    private readonly TrainingJobService _trainingJobService;
    private readonly IImageClassificationTrainer _trainer;

    public TrainingWorker(
        ILogger<TrainingWorker> logger,
        ITrainingJobService trainingJobService,
        IImageClassificationTrainer trainer)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        if (trainingJobService is not TrainingJobService concreteService)
            throw new ArgumentException("trainingJobService 必须是 TrainingJobService 类型", nameof(trainingJobService));
        
        _trainingJobService = concreteService;
        _trainer = trainer ?? throw new ArgumentNullException(nameof(trainer));
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("训练任务工作器已启动");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // 尝试从队列中获取任务
                var job = _trainingJobService.TryDequeueJob();

                if (job.HasValue)
                {
                    var (jobId, request) = job.Value;
                    await ProcessTrainingJobAsync(jobId, request, stoppingToken);
                }
                else
                {
                    // 队列为空，等待一段时间
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
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
        _logger.LogInformation("开始处理训练任务，JobId: {JobId}, 训练目录: {TrainingRootDirectory}",
            jobId, request.TrainingRootDirectory);

        // 更新任务状态为运行中
        _trainingJobService.UpdateJobToRunning(jobId);

        try
        {
            // 调用训练器执行训练
            var modelFilePath = await _trainer.TrainAsync(
                request.TrainingRootDirectory,
                request.OutputModelDirectory,
                request.ValidationSplitRatio,
                cancellationToken);

            _logger.LogInformation("训练任务完成，JobId: {JobId}, 模型文件: {ModelFilePath}",
                jobId, modelFilePath);

            // 更新任务状态为完成
            _trainingJobService.UpdateJobToCompleted(jobId);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("训练任务被取消，JobId: {JobId}", jobId);
            _trainingJobService.UpdateJobToFailed(jobId, "训练任务被取消");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "训练任务执行失败，JobId: {JobId}", jobId);
            _trainingJobService.UpdateJobToFailed(jobId, $"训练任务执行失败: {ex.Message}");
        }
    }
}
