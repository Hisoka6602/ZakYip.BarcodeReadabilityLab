namespace ZakYip.BarcodeReadabilityLab.Infrastructure.MLNet.Contracts;

using ZakYip.BarcodeReadabilityLab.Core.Domain.Models;
using ZakYip.BarcodeReadabilityLab.Core.Enums;

/// <summary>
/// 训练进度回调接口
/// </summary>
public interface ITrainingProgressCallback
{
    /// <summary>
    /// 报告训练进度（简化版）
    /// </summary>
    /// <param name="progress">当前进度（0.0 到 1.0 之间）</param>
    /// <param name="message">进度消息（可选）</param>
    void ReportProgress(decimal progress, string? message = null);

    /// <summary>
    /// 报告详细训练进度信息
    /// </summary>
    /// <param name="progressInfo">详细进度信息</param>
    void ReportDetailedProgress(TrainingProgressInfo progressInfo);
}
