using ZakYip.BarcodeReadabilityLab.Core.Enums;

namespace ZakYip.BarcodeReadabilityLab.Core.Domain.Models;

/// <summary>
/// 迁移学习配置选项
/// </summary>
public record class TransferLearningOptions
{
    /// <summary>
    /// 是否启用迁移学习
    /// </summary>
    public bool Enable { get; init; }

    /// <summary>
    /// 预训练模型类型
    /// </summary>
    public PretrainedModelType PretrainedModelType { get; init; } = PretrainedModelType.ResNet50;

    /// <summary>
    /// 预训练模型文件路径（可选，如果未提供则自动下载）
    /// </summary>
    public string? PretrainedModelPath { get; init; }

    /// <summary>
    /// 层冻结策略
    /// </summary>
    public LayerFreezeStrategy LayerFreezeStrategy { get; init; } = LayerFreezeStrategy.FreezeAll;

    /// <summary>
    /// 部分冻结时，要解冻的层数百分比（0.0 到 1.0）
    /// </summary>
    /// <remarks>
    /// 例如：0.3 表示解冻最后 30% 的层
    /// 仅在 LayerFreezeStrategy 为 FreezePartial 时有效
    /// </remarks>
    public decimal UnfreezeLayersPercentage { get; init; } = 0.3m;

    /// <summary>
    /// 是否启用多阶段训练
    /// </summary>
    public bool EnableMultiStageTraining { get; init; }

    /// <summary>
    /// 多阶段训练配置
    /// </summary>
    public List<MultiStageTrainingPhase>? TrainingPhases { get; init; }

    /// <summary>
    /// 迁移学习的学习率（通常低于从头训练）
    /// </summary>
    /// <remarks>
    /// 如果未设置，将使用主训练配置中的学习率
    /// 建议使用较小的学习率（如 0.001）以避免破坏预训练权重
    /// </remarks>
    public decimal? TransferLearningRate { get; init; }
}
