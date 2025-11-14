namespace ZakYip.BarcodeReadabilityLab.Infrastructure.MLNet.Contracts;

/// <summary>
/// 训练进度回调接口
/// </summary>
public interface ITrainingProgressCallback
{
    /// <summary>
    /// 报告训练进度
    /// </summary>
    /// <param name="progress">当前进度（0.0 到 1.0 之间）</param>
    /// <param name="message">进度消息（可选）</param>
    void ReportProgress(decimal progress, string? message = null);
}
