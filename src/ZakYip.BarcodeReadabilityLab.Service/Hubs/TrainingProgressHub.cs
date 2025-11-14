namespace ZakYip.BarcodeReadabilityLab.Service.Hubs;

using Microsoft.AspNetCore.SignalR;

/// <summary>
/// 训练进度推送 Hub
/// </summary>
public class TrainingProgressHub : Hub
{
    private readonly ILogger<TrainingProgressHub> _logger;

    public TrainingProgressHub(ILogger<TrainingProgressHub> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 客户端订阅训练任务进度更新
    /// </summary>
    /// <param name="jobId">训练任务 ID</param>
    public async Task SubscribeToJob(string jobId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"training-job-{jobId}");
        _logger.LogInformation("客户端 {ConnectionId} 订阅了训练任务 {JobId} 的进度更新", Context.ConnectionId, jobId);
    }

    /// <summary>
    /// 客户端取消订阅训练任务进度更新
    /// </summary>
    /// <param name="jobId">训练任务 ID</param>
    public async Task UnsubscribeFromJob(string jobId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"training-job-{jobId}");
        _logger.LogInformation("客户端 {ConnectionId} 取消订阅了训练任务 {JobId} 的进度更新", Context.ConnectionId, jobId);
    }

    public override Task OnConnectedAsync()
    {
        _logger.LogInformation("客户端连接到训练进度推送 Hub，ConnectionId: {ConnectionId}", Context.ConnectionId);
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception is not null)
        {
            _logger.LogWarning(exception, "客户端断开连接，ConnectionId: {ConnectionId}", Context.ConnectionId);
        }
        else
        {
            _logger.LogInformation("客户端正常断开连接，ConnectionId: {ConnectionId}", Context.ConnectionId);
        }

        return base.OnDisconnectedAsync(exception);
    }
}
