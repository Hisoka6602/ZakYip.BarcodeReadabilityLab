using ZakYip.BarcodeReadabilityLab.Core.Enums;

namespace ZakYip.BarcodeReadabilityLab.Core.Domain.Models;

/// <summary>
/// 训练配置档位
/// </summary>
public record class TrainingProfile
{
    /// <summary>
    /// 档位类型
    /// </summary>
    public required TrainingProfileType ProfileType { get; init; }

    /// <summary>
    /// 训练轮数（Epoch）
    /// </summary>
    public required int Epochs { get; init; }

    /// <summary>
    /// 批大小（Batch Size）
    /// </summary>
    public required int BatchSize { get; init; }

    /// <summary>
    /// 学习率
    /// </summary>
    public required decimal LearningRate { get; init; }

    /// <summary>
    /// L2 正则化系数（可选）
    /// </summary>
    public decimal? L2Regularization { get; init; }

    /// <summary>
    /// 是否启用早停
    /// </summary>
    public bool EnableEarlyStopping { get; init; }

    /// <summary>
    /// 早停耐心值（连续多少个评估周期指标无提升则停止）
    /// </summary>
    public int EarlyStoppingPatience { get; init; } = 5;

    /// <summary>
    /// 早停最小改进阈值
    /// </summary>
    public decimal EarlyStoppingMinDelta { get; init; } = 0.001m;

    /// <summary>
    /// 是否启用数据增强
    /// </summary>
    public bool EnableDataAugmentation { get; init; }

    /// <summary>
    /// 数据增强配置（当 EnableDataAugmentation 为 true 时使用）
    /// </summary>
    public DataAugmentationOptions? DataAugmentation { get; init; }

    /// <summary>
    /// 数据平衡策略
    /// </summary>
    public DataBalancingStrategy DataBalancingStrategy { get; init; } = DataBalancingStrategy.None;

    /// <summary>
    /// 目标图像宽度（像素）
    /// </summary>
    public int ImageWidth { get; init; } = 224;

    /// <summary>
    /// 目标图像高度（像素）
    /// </summary>
    public int ImageHeight { get; init; } = 224;

    /// <summary>
    /// 是否转换为灰度图
    /// </summary>
    public bool ConvertToGrayscale { get; init; }

    /// <summary>
    /// 是否启用预处理缓存
    /// </summary>
    public bool EnablePreprocessingCache { get; init; }

    /// <summary>
    /// 验证集分割比例（0.0 到 1.0 之间）
    /// </summary>
    public decimal ValidationSplitRatio { get; init; } = 0.2m;

    /// <summary>
    /// 每隔多少个 Epoch 进行一次评估（用于早停判断）
    /// </summary>
    public int EvaluationFrequency { get; init; } = 1;
}
