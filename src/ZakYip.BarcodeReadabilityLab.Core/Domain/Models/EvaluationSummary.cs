namespace ZakYip.BarcodeReadabilityLab.Core.Domain.Models;

/// <summary>
/// 在线推理评估汇总统计
/// </summary>
public record class EvaluationSummary
{
    /// <summary>
    /// 总样本数
    /// </summary>
    public required int Total { get; init; }

    /// <summary>
    /// 包含预期标签的样本数
    /// </summary>
    public required int WithExpectedLabel { get; init; }

    /// <summary>
    /// 预测正确的样本数
    /// </summary>
    public required int CorrectCount { get; init; }

    /// <summary>
    /// 准确率（仅针对有预期标签的样本）
    /// </summary>
    public decimal? Accuracy { get; init; }

    /// <summary>
    /// 宏平均 F1 分数
    /// </summary>
    public decimal? MacroF1 { get; init; }

    /// <summary>
    /// 微平均 F1 分数
    /// </summary>
    public decimal? MicroF1 { get; init; }
}
