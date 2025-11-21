using ZakYip.BarcodeReadabilityLab.Core.Enum;

namespace ZakYip.BarcodeReadabilityLab.Core.Domain.Models;

/// <summary>
/// 模型版本之间的预测对比结果
/// </summary>
public sealed record class ModelComparisonResult
{
    /// <summary>
    /// 产出本结果的模型版本
    /// </summary>
    public required ModelVersion Version { get; init; }

    /// <summary>
    /// 模型版本对样本的预测结果
    /// </summary>
    public required BarcodeAnalysisResult AnalysisResult { get; init; }
}
