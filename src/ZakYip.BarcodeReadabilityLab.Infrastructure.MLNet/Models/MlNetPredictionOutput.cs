namespace ZakYip.BarcodeReadabilityLab.Infrastructure.MLNet.Models;

using Microsoft.ML.Data;

/// <summary>
/// ML.NET 预测输出模型
/// </summary>
public record class MlNetPredictionOutput
{
    /// <summary>
    /// 预测的标签
    /// </summary>
    [ColumnName("PredictedLabel")]
    public string PredictedLabel { get; init; } = string.Empty;

    /// <summary>
    /// 各类别的置信度得分数组
    /// </summary>
    [ColumnName("Score")]
    public float[] Score { get; init; } = Array.Empty<float>();
}
