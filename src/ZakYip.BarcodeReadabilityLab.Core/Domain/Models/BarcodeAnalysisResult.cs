using ZakYip.BarcodeReadabilityLab.Core.Enum;

namespace ZakYip.BarcodeReadabilityLab.Core.Domain.Models;

/// <summary>
/// 条码分析结果
/// </summary>
public record class BarcodeAnalysisResult
{
    /// <summary>
    /// 样本唯一标识符
    /// </summary>
    public required Guid SampleId { get; init; }

    /// <summary>
    /// 是否已完成分析
    /// </summary>
    public required bool IsAnalyzed { get; init; }

    /// <summary>
    /// 分析失败的原因（当条码无法读取时）
    /// </summary>
    public NoreadReason? Reason { get; init; }

    /// <summary>
    /// 分析结果的置信度（0.0 到 1.0 之间）
    /// </summary>
    public decimal? Confidence { get; init; }

    /// <summary>
    /// 置信度是否达到阈值
    /// </summary>
    public required bool IsAboveThreshold { get; init; }

    /// <summary>
    /// 分析结果的补充说明或异常原因（可选）
    /// </summary>
    public string? Message { get; init; }
}
