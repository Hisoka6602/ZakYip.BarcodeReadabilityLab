using System.Text.Json.Serialization;

namespace ZakYip.BarcodeReadabilityLab.Service.Models.Evaluation;

/// <summary>
/// 单张图片评估响应
/// </summary>
public record class EvaluateSingleResponse
{
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
    /// 是否预测正确（仅当提供 expectedLabel 时有值）
    /// </summary>
    [JsonPropertyName("isCorrect")]
    public bool? IsCorrect { get; init; }

    /// <summary>
    /// 预期的标签名称（可选）
    /// </summary>
    [JsonPropertyName("expectedLabel")]
    public string? ExpectedLabel { get; init; }

    /// <summary>
    /// 各类别的原始概率分布（可选）
    /// </summary>
    [JsonPropertyName("noreadReasonScores")]
    public Dictionary<string, decimal>? NoreadReasonScores { get; init; }
}
