using ZakYip.BarcodeReadabilityLab.Core.Enums;

namespace ZakYip.BarcodeReadabilityLab.Core.Domain.Models;

/// <summary>
/// 超参数空间定义
/// </summary>
public record class HyperparameterSpace
{
    /// <summary>
    /// 学习率候选值列表
    /// </summary>
    public required decimal[] LearningRates { get; init; }

    /// <summary>
    /// Epoch 候选值列表
    /// </summary>
    public required int[] EpochsOptions { get; init; }

    /// <summary>
    /// 批大小候选值列表
    /// </summary>
    public required int[] BatchSizeOptions { get; init; }

    /// <summary>
    /// 验证集分割比例候选值列表（可选）
    /// </summary>
    public decimal[]? ValidationSplitRatios { get; init; }

    /// <summary>
    /// 数据增强配置选项列表（可选）
    /// </summary>
    public DataAugmentationOptions[]? DataAugmentationOptionsSet { get; init; }

    /// <summary>
    /// 数据平衡策略选项列表（可选）
    /// </summary>
    public DataBalancingOptions[]? DataBalancingOptionsSet { get; init; }
}
