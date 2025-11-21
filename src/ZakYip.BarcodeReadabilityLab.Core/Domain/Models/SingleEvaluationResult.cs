using ZakYip.BarcodeReadabilityLab.Core.Enums;

namespace ZakYip.BarcodeReadabilityLab.Core.Domain.Models;

/// <summary>
/// 单张图片评估结果
/// </summary>
public record class SingleEvaluationResult
{
    /// <summary>
    /// 预测的标签
    /// </summary>
    public required NoreadReason PredictedLabel { get; init; }

    /// <summary>
    /// 置信度（0.0 到 1.0）
    /// </summary>
    public required decimal Confidence { get; init; }

    /// <summary>
    /// 预期的标签（可选）
    /// </summary>
    public NoreadReason? ExpectedLabel { get; init; }

    /// <summary>
    /// 预测是否正确（仅当提供 ExpectedLabel 时有效）
    /// </summary>
    public bool? IsCorrect { get; init; }

    /// <summary>
    /// 各类别的原始概率分布（可选）
    /// </summary>
    public Dictionary<NoreadReason, decimal>? NoreadReasonScores { get; init; }
}
