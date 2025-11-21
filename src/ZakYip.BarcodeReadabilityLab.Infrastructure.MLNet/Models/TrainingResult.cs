namespace ZakYip.BarcodeReadabilityLab.Infrastructure.MLNet.Models;

using ZakYip.BarcodeReadabilityLab.Core.Domain.Models;
using ZakYip.BarcodeReadabilityLab.Core.Enum;

/// <summary>
/// 训练结果
/// </summary>
public record class TrainingResult
{
    /// <summary>
    /// 训练完成后的模型文件路径
    /// </summary>
    public required string ModelFilePath { get; init; }

    /// <summary>
    /// 模型评估指标
    /// </summary>
    public required ModelEvaluationMetrics EvaluationMetrics { get; init; }
}
