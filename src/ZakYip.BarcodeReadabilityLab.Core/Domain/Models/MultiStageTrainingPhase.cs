namespace ZakYip.BarcodeReadabilityLab.Core.Domain.Models;

/// <summary>
/// 多阶段训练的单个阶段配置
/// </summary>
public record class MultiStageTrainingPhase
{
    /// <summary>
    /// 阶段名称
    /// </summary>
    public required string PhaseName { get; init; }

    /// <summary>
    /// 阶段序号（从 1 开始）
    /// </summary>
    public required int PhaseNumber { get; init; }

    /// <summary>
    /// 该阶段的 Epoch 数
    /// </summary>
    public required int Epochs { get; init; }

    /// <summary>
    /// 该阶段的学习率
    /// </summary>
    public required decimal LearningRate { get; init; }

    /// <summary>
    /// 该阶段的层冻结策略
    /// </summary>
    public required LayerFreezeStrategy LayerFreezeStrategy { get; init; }

    /// <summary>
    /// 部分冻结时，要解冻的层数百分比（0.0 到 1.0）
    /// </summary>
    public decimal UnfreezeLayersPercentage { get; init; } = 0.3m;

    /// <summary>
    /// 阶段描述
    /// </summary>
    public string? Description { get; init; }
}
