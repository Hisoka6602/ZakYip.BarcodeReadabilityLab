namespace ZakYip.BarcodeReadabilityLab.Service.Models;

/// <summary>
/// 启动训练任务的响应模型
/// </summary>
public record class StartTrainingResponse
{
    /// <summary>
    /// 训练任务唯一标识符
    /// </summary>
    public required Guid JobId { get; init; }

    /// <summary>
    /// 响应消息
    /// </summary>
    public required string Message { get; init; }
}
