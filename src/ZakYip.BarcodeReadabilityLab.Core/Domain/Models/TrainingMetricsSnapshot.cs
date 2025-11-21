using ZakYip.BarcodeReadabilityLab.Core.Enums;

namespace ZakYip.BarcodeReadabilityLab.Core.Domain.Models;

/// <summary>
/// 训练指标快照（用于进度报告）
/// </summary>
public record class TrainingMetricsSnapshot
{
    /// <summary>
    /// 当前 Epoch 编号（从 1 开始）
    /// </summary>
    public int? CurrentEpoch { get; init; }

    /// <summary>
    /// 总 Epoch 数
    /// </summary>
    public int? TotalEpochs { get; init; }

    /// <summary>
    /// 当前准确率（0.0 到 1.0）
    /// </summary>
    public decimal? Accuracy { get; init; }

    /// <summary>
    /// 当前损失值
    /// </summary>
    public decimal? Loss { get; init; }

    /// <summary>
    /// 学习率
    /// </summary>
    public decimal? LearningRate { get; init; }
}
