using ZakYip.BarcodeReadabilityLab.Core.Domain.Models;
using ZakYip.BarcodeReadabilityLab.Core.Enums;

namespace ZakYip.BarcodeReadabilityLab.Application.Options;

/// <summary>
/// 训练档位配置选项（用于从 appsettings.json 加载）
/// </summary>
public record class TrainingProfileOptions
{
    /// <summary>
    /// 默认使用的训练档位
    /// </summary>
    public TrainingProfileType DefaultProfileType { get; init; } = TrainingProfileType.Standard;

    /// <summary>
    /// Debug 档位配置
    /// </summary>
    public required TrainingProfileConfiguration Debug { get; init; }

    /// <summary>
    /// Standard 档位配置
    /// </summary>
    public required TrainingProfileConfiguration Standard { get; init; }

    /// <summary>
    /// HighQuality 档位配置
    /// </summary>
    public required TrainingProfileConfiguration HighQuality { get; init; }

    /// <summary>
    /// 获取指定档位的配置
    /// </summary>
    public TrainingProfileConfiguration GetConfiguration(TrainingProfileType profileType)
    {
        return profileType switch
        {
            TrainingProfileType.Debug => Debug,
            TrainingProfileType.Standard => Standard,
            TrainingProfileType.HighQuality => HighQuality,
            _ => Standard
        };
    }

    /// <summary>
    /// 转换为领域模型
    /// </summary>
    public TrainingProfile ToProfile(TrainingProfileType profileType)
    {
        var config = GetConfiguration(profileType);
        
        return new TrainingProfile
        {
            ProfileType = profileType,
            Epochs = config.Epochs,
            BatchSize = config.BatchSize,
            LearningRate = config.LearningRate,
            L2Regularization = config.L2Regularization,
            EnableEarlyStopping = config.EnableEarlyStopping,
            EarlyStoppingPatience = config.EarlyStoppingPatience,
            EarlyStoppingMinDelta = config.EarlyStoppingMinDelta,
            EnableDataAugmentation = config.EnableDataAugmentation,
            DataAugmentation = config.DataAugmentation,
            DataBalancingStrategy = config.DataBalancingStrategy,
            ImageWidth = config.ImageWidth,
            ImageHeight = config.ImageHeight,
            ConvertToGrayscale = config.ConvertToGrayscale,
            EnablePreprocessingCache = config.EnablePreprocessingCache,
            ValidationSplitRatio = config.ValidationSplitRatio,
            EvaluationFrequency = config.EvaluationFrequency
        };
    }
}

/// <summary>
/// 单个档位的配置详情
/// </summary>
public record class TrainingProfileConfiguration
{
    /// <summary>
    /// 训练轮数（Epoch）
    /// </summary>
    public int Epochs { get; init; } = 50;

    /// <summary>
    /// 批大小（Batch Size）
    /// </summary>
    public int BatchSize { get; init; } = 20;

    /// <summary>
    /// 学习率
    /// </summary>
    public decimal LearningRate { get; init; } = 0.01m;

    /// <summary>
    /// L2 正则化系数（可选）
    /// </summary>
    public decimal? L2Regularization { get; init; }

    /// <summary>
    /// 是否启用早停
    /// </summary>
    public bool EnableEarlyStopping { get; init; }

    /// <summary>
    /// 早停耐心值
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
    /// 数据增强配置
    /// </summary>
    public DataAugmentationOptions? DataAugmentation { get; init; }

    /// <summary>
    /// 数据平衡策略
    /// </summary>
    public DataBalancingStrategy DataBalancingStrategy { get; init; } = DataBalancingStrategy.None;

    /// <summary>
    /// 目标图像宽度
    /// </summary>
    public int ImageWidth { get; init; } = 224;

    /// <summary>
    /// 目标图像高度
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
    /// 验证集分割比例
    /// </summary>
    public decimal ValidationSplitRatio { get; init; } = 0.2m;

    /// <summary>
    /// 每隔多少个 Epoch 进行一次评估
    /// </summary>
    public int EvaluationFrequency { get; init; } = 1;
}
