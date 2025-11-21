using ZakYip.BarcodeReadabilityLab.Core.Enum;

namespace ZakYip.BarcodeReadabilityLab.Core.Domain.Models;

/// <summary>
/// 超参数配置
/// </summary>
public record class HyperparameterConfiguration
{
    /// <summary>
    /// 配置唯一标识
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// 学习率
    /// </summary>
    public required decimal LearningRate { get; init; }

    /// <summary>
    /// 训练轮数（Epoch）
    /// </summary>
    public required int Epochs { get; init; }

    /// <summary>
    /// 批大小（Batch Size）
    /// </summary>
    public required int BatchSize { get; init; }

    /// <summary>
    /// 验证集分割比例
    /// </summary>
    public decimal? ValidationSplitRatio { get; init; }

    /// <summary>
    /// 数据增强配置
    /// </summary>
    public DataAugmentationOptions? DataAugmentation { get; init; }

    /// <summary>
    /// 数据平衡配置
    /// </summary>
    public DataBalancingOptions? DataBalancing { get; init; }

    /// <summary>
    /// 配置创建时间
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
