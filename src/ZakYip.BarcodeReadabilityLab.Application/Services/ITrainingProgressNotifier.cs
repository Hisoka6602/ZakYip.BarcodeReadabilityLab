namespace ZakYip.BarcodeReadabilityLab.Application.Services;

/// <summary>
/// 训练进度通知服务契约
/// </summary>
public interface ITrainingProgressNotifier
{
    /// <summary>
    /// 通知训练进度更新
    /// </summary>
    /// <param name="jobId">训练任务 ID</param>
    /// <param name="progress">进度（0.0 到 1.0）</param>
    /// <param name="message">进度消息（可选）</param>
    Task NotifyProgressAsync(Guid jobId, decimal progress, string? message = null);
}
