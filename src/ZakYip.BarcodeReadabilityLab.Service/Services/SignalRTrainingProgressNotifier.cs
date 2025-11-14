namespace ZakYip.BarcodeReadabilityLab.Service.Services;

using Microsoft.AspNetCore.SignalR;
using ZakYip.BarcodeReadabilityLab.Application.Services;
using ZakYip.BarcodeReadabilityLab.Service.Hubs;

/// <summary>
/// SignalR 训练进度通知服务实现
/// </summary>
public sealed class SignalRTrainingProgressNotifier : ITrainingProgressNotifier
{
    private readonly IHubContext<TrainingProgressHub> _hubContext;
    private readonly ILogger<SignalRTrainingProgressNotifier> _logger;

    public SignalRTrainingProgressNotifier(
        IHubContext<TrainingProgressHub> hubContext,
        ILogger<SignalRTrainingProgressNotifier> logger)
    {
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task NotifyProgressAsync(Guid jobId, decimal progress, string? message = null)
    {
        try
        {
            var groupName = $"training-job-{jobId}";
            
            await _hubContext.Clients.Group(groupName).SendAsync(
                "ProgressUpdated",
                new
                {
                    jobId,
                    progress,
                    message,
                    timestamp = DateTimeOffset.UtcNow
                });

            _logger.LogDebug("通过 SignalR 推送训练进度 => JobId: {JobId}, 进度: {Progress:P0}, 消息: {Message}",
                jobId, progress, message ?? "无");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "通过 SignalR 推送训练进度失败 => JobId: {JobId}", jobId);
        }
    }
}
