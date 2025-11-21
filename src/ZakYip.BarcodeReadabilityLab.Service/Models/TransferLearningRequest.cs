namespace ZakYip.BarcodeReadabilityLab.Service.Models;

using ZakYip.BarcodeReadabilityLab.Core.Domain.Models;
using ZakYip.BarcodeReadabilityLab.Core.Enums;

/// <summary>
/// 迁移学习训练请求
/// </summary>
public record class TransferLearningRequest
{
    /// <summary>
    /// 训练数据根目录
    /// </summary>
    public string? TrainingRootDirectory { get; init; }

    /// <summary>
    /// 输出模型目录
    /// </summary>
    public string? OutputModelDirectory { get; init; }

    /// <summary>
    /// 验证集分割比例（0.0 到 1.0）
    /// </summary>
    public decimal? ValidationSplitRatio { get; init; }

    /// <summary>
    /// 学习率
    /// </summary>
    public decimal? LearningRate { get; init; }

    /// <summary>
    /// 训练轮数（Epoch）
    /// </summary>
    public int? Epochs { get; init; }

    /// <summary>
    /// 批大小（Batch Size）
    /// </summary>
    public int? BatchSize { get; init; }

    /// <summary>
    /// 备注说明
    /// </summary>
    public string? Remarks { get; init; }

    /// <summary>
    /// 预训练模型类型
    /// </summary>
    public PretrainedModelType PretrainedModelType { get; init; } = PretrainedModelType.ResNet50;

    /// <summary>
    /// 层冻结策略
    /// </summary>
    public LayerFreezeStrategy LayerFreezeStrategy { get; init; } = LayerFreezeStrategy.FreezeAll;

    /// <summary>
    /// 部分冻结时，要解冻的层数百分比
    /// </summary>
    public decimal UnfreezeLayersPercentage { get; init; } = 0.3m;

    /// <summary>
    /// 是否启用多阶段训练
    /// </summary>
    public bool EnableMultiStageTraining { get; init; }

    /// <summary>
    /// 多阶段训练配置
    /// </summary>
    public List<MultiStageTrainingPhaseDto>? TrainingPhases { get; init; }

    /// <summary>
    /// 数据增强配置
    /// </summary>
    public DataAugmentationOptions? DataAugmentation { get; init; }

    /// <summary>
    /// 数据平衡配置
    /// </summary>
    public DataBalancingOptions? DataBalancing { get; init; }
}

/// <summary>
/// 多阶段训练阶段 DTO
/// </summary>
public record class MultiStageTrainingPhaseDto
{
    /// <summary>
    /// 阶段名称
    /// </summary>
    public required string PhaseName { get; init; }

    /// <summary>
    /// 阶段序号
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
    /// 部分冻结时，要解冻的层数百分比
    /// </summary>
    public decimal UnfreezeLayersPercentage { get; init; } = 0.3m;

    /// <summary>
    /// 阶段描述
    /// </summary>
    public string? Description { get; init; }
}
