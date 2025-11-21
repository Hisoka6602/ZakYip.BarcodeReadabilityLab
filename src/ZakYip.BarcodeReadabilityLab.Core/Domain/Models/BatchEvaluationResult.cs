namespace ZakYip.BarcodeReadabilityLab.Core.Domain.Models;

/// <summary>
/// 批量评估结果
/// </summary>
public record class BatchEvaluationResult
{
    /// <summary>
    /// 单个图片评估结果列表
    /// </summary>
    public required List<BatchEvaluationItem> Items { get; init; }

    /// <summary>
    /// 聚合统计信息
    /// </summary>
    public required EvaluationSummary Summary { get; init; }
}

/// <summary>
/// 批量评估中的单个条目结果
/// </summary>
public record class BatchEvaluationItem
{
    /// <summary>
    /// 文件名
    /// </summary>
    public required string FileName { get; init; }

    /// <summary>
    /// 评估结果
    /// </summary>
    public required SingleEvaluationResult Result { get; init; }
}
