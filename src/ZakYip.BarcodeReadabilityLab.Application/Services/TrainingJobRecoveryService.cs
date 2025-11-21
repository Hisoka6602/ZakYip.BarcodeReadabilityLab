namespace ZakYip.BarcodeReadabilityLab.Application.Services;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ZakYip.BarcodeReadabilityLab.Core.Domain.Contracts;
using ZakYip.BarcodeReadabilityLab.Core.Domain.Models;
using ZakYip.BarcodeReadabilityLab.Core.Enum;

/// <summary>
/// 训练任务恢复服务
/// </summary>
/// <remarks>
/// 在服务启动时检查是否有未完成的训练任务，并将状态为"运行中"或"排队中"的任务标记为失败
/// </remarks>
public class TrainingJobRecoveryService : IHostedService
{
    private readonly ILogger<TrainingJobRecoveryService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public TrainingJobRecoveryService(
        ILogger<TrainingJobRecoveryService> logger,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("开始恢复训练任务状态");

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<ITrainingJobRepository>();

            // 获取所有正在运行或排队中的任务
            var runningJobs = await repository.GetByStatusAsync(TrainingJobState.Running, cancellationToken);
            var queuedJobs = await repository.GetByStatusAsync(TrainingJobState.Queued, cancellationToken);

            var incompleteJobs = runningJobs.Concat(queuedJobs).ToList();

            if (incompleteJobs.Count == 0)
            {
                _logger.LogInformation("没有需要恢复的训练任务");
                return;
            }

            _logger.LogWarning("发现 {Count} 个未完成的训练任务，将标记为失败", incompleteJobs.Count);

            foreach (var job in incompleteJobs)
            {
                var updatedJob = job with
                {
                    Status = TrainingJobState.Failed,
                    CompletedTime = DateTimeOffset.UtcNow,
                    ErrorMessage = "服务重启，训练任务被中断"
                };

                await repository.UpdateAsync(updatedJob, cancellationToken);

                _logger.LogInformation("训练任务 {JobId} 已标记为失败", job.JobId);
            }

            _logger.LogInformation("训练任务状态恢复完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "恢复训练任务状态时发生错误");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
