using System.Text.Json.Serialization;

namespace ZakYip.BarcodeReadabilityLab.Service.Models.Evaluation;

/// <summary>
/// 批量图片评估响应
/// </summary>
public record class EvaluateBatchResponse
{
    /// <summary>
    /// 单个图片评估结果列表
    /// </summary>
    [JsonPropertyName("items")]
    public required List<EvaluationItemResponse> Items { get; init; }

    /// <summary>
    /// 聚合统计信息
    /// </summary>
    [JsonPropertyName("summary")]
    public required EvaluationSummaryResponse Summary { get; init; }
}

/// <summary>
/// 单个评估项响应
/// </summary>
public record class EvaluationItemResponse
{
    /// <summary>
    /// 文件名
    /// </summary>
    [JsonPropertyName("fileName")]
    public required string FileName { get; init; }

    /// <summary>
    /// 预测的标签名称
    /// </summary>
    [JsonPropertyName("predictedLabel")]
    public required string PredictedLabel { get; init; }

    /// <summary>
    /// 预测标签的显示名称（中文描述）
    /// </summary>
    [JsonPropertyName("predictedLabelDisplayName")]
    public required string PredictedLabelDisplayName { get; init; }

    /// <summary>
    /// 置信度（0.0 到 1.0）
    /// </summary>
    [JsonPropertyName("confidence")]
    public required decimal Confidence { get; init; }

    /// <summary>
    /// 预期的标签名称（可选）
    /// </summary>
    [JsonPropertyName("expectedLabel")]
    public string? ExpectedLabel { get; init; }

    /// <summary>
    /// 是否预测正确（仅当提供 expectedLabel 时有值）
    /// </summary>
    [JsonPropertyName("isCorrect")]
    public bool? IsCorrect { get; init; }
}

/// <summary>
/// 评估汇总统计响应
/// </summary>
public record class EvaluationSummaryResponse
{
    /// <summary>
    /// 总样本数
    /// </summary>
    [JsonPropertyName("total")]
    public required int Total { get; init; }

    /// <summary>
    /// 包含预期标签的样本数
    /// </summary>
    [JsonPropertyName("withExpectedLabel")]
    public required int WithExpectedLabel { get; init; }

    /// <summary>
    /// 预测正确的样本数
    /// </summary>
    [JsonPropertyName("correctCount")]
    public required int CorrectCount { get; init; }

    /// <summary>
    /// 准确率（仅针对有预期标签的样本）
    /// </summary>
    [JsonPropertyName("accuracy")]
    public decimal? Accuracy { get; init; }

    /// <summary>
    /// 宏平均 F1 分数
    /// </summary>
    [JsonPropertyName("macroF1")]
    public decimal? MacroF1 { get; init; }

    /// <summary>
    /// 微平均 F1 分数
    /// </summary>
    [JsonPropertyName("microF1")]
    public decimal? MicroF1 { get; init; }
}
