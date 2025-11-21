using ZakYip.BarcodeReadabilityLab.Core.Enum;

namespace ZakYip.BarcodeReadabilityLab.Core.Domain.Models;

/// <summary>
/// 数据平衡配置选项
/// </summary>
public record class DataBalancingOptions
{
    /// <summary>
    /// 数据平衡策略
    /// </summary>
    public DataBalancingStrategy Strategy { get; init; } = DataBalancingStrategy.None;

    /// <summary>
    /// 目标的每类样本数量（为空时根据策略自动推断）
    /// </summary>
    public int? TargetSampleCountPerClass { get; init; }

    /// <summary>
    /// 是否在平衡后打乱数据集
    /// </summary>
    public bool ShuffleAfterBalancing { get; init; } = true;

    /// <summary>
    /// 随机数种子，保证平衡过程可重复
    /// </summary>
    public int RandomSeed { get; init; } = 42;
}
