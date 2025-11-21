namespace ZakYip.BarcodeReadabilityLab.Infrastructure.MLNet.Services;

using ZakYip.BarcodeReadabilityLab.Core.Domain.Models;
using ZakYip.BarcodeReadabilityLab.Core.Enums;

/// <summary>
/// 超参数推荐配置提供者
/// </summary>
public static class HyperparameterRecommendations
{
    /// <summary>
    /// 获取快速调试配置（适用于快速验证）
    /// </summary>
    public static HyperparameterSpace GetQuickDebugSpace()
    {
        return new HyperparameterSpace
        {
            LearningRates = new[] { 0.01m, 0.05m },
            EpochsOptions = new[] { 10, 20 },
            BatchSizeOptions = new[] { 10, 20 },
            ValidationSplitRatios = new[] { 0.2m },
            DataAugmentationOptionsSet = new[]
            {
                new DataAugmentationOptions { Enable = false }
            },
            DataBalancingOptionsSet = new[]
            {
                new DataBalancingOptions { Strategy = DataBalancingStrategy.None }
            }
        };
    }

    /// <summary>
    /// 获取标准配置（适用于一般场景）
    /// </summary>
    public static HyperparameterSpace GetStandardSpace()
    {
        return new HyperparameterSpace
        {
            LearningRates = new[] { 0.001m, 0.005m, 0.01m, 0.05m },
            EpochsOptions = new[] { 30, 50, 70 },
            BatchSizeOptions = new[] { 10, 20, 32 },
            ValidationSplitRatios = new[] { 0.2m, 0.3m },
            DataAugmentationOptionsSet = new[]
            {
                new DataAugmentationOptions { Enable = false },
                new DataAugmentationOptions
                {
                    Enable = true,
                    AugmentedImagesPerSample = 1,
                    EnableRotation = true,
                    RotationProbability = 0.6,
                    EnableHorizontalFlip = true,
                    HorizontalFlipProbability = 0.5,
                    EnableBrightnessAdjustment = true,
                    BrightnessProbability = 0.6
                }
            },
            DataBalancingOptionsSet = new[]
            {
                new DataBalancingOptions { Strategy = DataBalancingStrategy.None },
                new DataBalancingOptions { Strategy = DataBalancingStrategy.OverSample }
            }
        };
    }

    /// <summary>
    /// 获取精细配置（适用于追求最佳性能）
    /// </summary>
    public static HyperparameterSpace GetFineGrainedSpace()
    {
        return new HyperparameterSpace
        {
            LearningRates = new[] { 0.0001m, 0.0005m, 0.001m, 0.005m, 0.01m, 0.02m, 0.05m },
            EpochsOptions = new[] { 20, 30, 50, 70, 100 },
            BatchSizeOptions = new[] { 8, 10, 16, 20, 32, 64 },
            ValidationSplitRatios = new[] { 0.15m, 0.2m, 0.25m, 0.3m },
            DataAugmentationOptionsSet = new[]
            {
                new DataAugmentationOptions { Enable = false },
                new DataAugmentationOptions
                {
                    Enable = true,
                    AugmentedImagesPerSample = 1,
                    EnableRotation = true,
                    RotationProbability = 0.5,
                    EnableHorizontalFlip = true,
                    HorizontalFlipProbability = 0.5,
                    EnableBrightnessAdjustment = true,
                    BrightnessProbability = 0.5
                },
                new DataAugmentationOptions
                {
                    Enable = true,
                    AugmentedImagesPerSample = 2,
                    EnableRotation = true,
                    RotationProbability = 0.7,
                    EnableHorizontalFlip = true,
                    HorizontalFlipProbability = 0.5,
                    EnableVerticalFlip = true,
                    VerticalFlipProbability = 0.3,
                    EnableBrightnessAdjustment = true,
                    BrightnessProbability = 0.7
                }
            },
            DataBalancingOptionsSet = new[]
            {
                new DataBalancingOptions { Strategy = DataBalancingStrategy.None },
                new DataBalancingOptions { Strategy = DataBalancingStrategy.OverSample },
                new DataBalancingOptions { Strategy = DataBalancingStrategy.UnderSample }
            }
        };
    }

    /// <summary>
    /// 获取基于数据集大小的推荐空间
    /// </summary>
    /// <param name="sampleCount">数据集样本总数</param>
    public static HyperparameterSpace GetRecommendedSpace(int sampleCount)
    {
        // 小数据集（< 500 样本）
        if (sampleCount < 500)
        {
            return new HyperparameterSpace
            {
                LearningRates = new[] { 0.001m, 0.005m, 0.01m },
                EpochsOptions = new[] { 50, 70, 100 },
                BatchSizeOptions = new[] { 8, 10, 16 },
                ValidationSplitRatios = new[] { 0.2m },
                DataAugmentationOptionsSet = new[]
                {
                    new DataAugmentationOptions
                    {
                        Enable = true,
                        AugmentedImagesPerSample = 2,
                        EnableRotation = true,
                        RotationProbability = 0.7,
                        EnableHorizontalFlip = true,
                        HorizontalFlipProbability = 0.5,
                        EnableBrightnessAdjustment = true,
                        BrightnessProbability = 0.6
                    }
                },
                DataBalancingOptionsSet = new[]
                {
                    new DataBalancingOptions { Strategy = DataBalancingStrategy.OverSample }
                }
            };
        }

        // 中等数据集（500-2000 样本）
        if (sampleCount < 2000)
        {
            return new HyperparameterSpace
            {
                LearningRates = new[] { 0.001m, 0.005m, 0.01m, 0.02m },
                EpochsOptions = new[] { 30, 50, 70 },
                BatchSizeOptions = new[] { 16, 20, 32 },
                ValidationSplitRatios = new[] { 0.2m, 0.25m },
                DataAugmentationOptionsSet = new[]
                {
                    new DataAugmentationOptions { Enable = false },
                    new DataAugmentationOptions
                    {
                        Enable = true,
                        AugmentedImagesPerSample = 1,
                        EnableRotation = true,
                        RotationProbability = 0.6,
                        EnableHorizontalFlip = true,
                        HorizontalFlipProbability = 0.5,
                        EnableBrightnessAdjustment = true,
                        BrightnessProbability = 0.6
                    }
                },
                DataBalancingOptionsSet = new[]
                {
                    new DataBalancingOptions { Strategy = DataBalancingStrategy.None },
                    new DataBalancingOptions { Strategy = DataBalancingStrategy.OverSample }
                }
            };
        }

        // 大数据集（>= 2000 样本）
        return new HyperparameterSpace
        {
            LearningRates = new[] { 0.001m, 0.005m, 0.01m, 0.02m, 0.05m },
            EpochsOptions = new[] { 20, 30, 50 },
            BatchSizeOptions = new[] { 20, 32, 64 },
            ValidationSplitRatios = new[] { 0.2m, 0.3m },
            DataAugmentationOptionsSet = new[]
            {
                new DataAugmentationOptions { Enable = false }
            },
            DataBalancingOptionsSet = new[]
            {
                new DataBalancingOptions { Strategy = DataBalancingStrategy.None }
            }
        };
    }

    /// <summary>
    /// 获取默认网格搜索配置
    /// </summary>
    public static GridSearchOptions GetDefaultGridSearchOptions()
    {
        return new GridSearchOptions
        {
            SearchSpace = GetStandardSpace(),
            EnableParallelSearch = true,
            MaxParallelTrials = 2,
            EnableEarlyStopping = false,
            MetricType = EvaluationMetricType.Accuracy,
            LogAllTrials = true
        };
    }

    /// <summary>
    /// 获取默认随机搜索配置
    /// </summary>
    public static RandomSearchOptions GetDefaultRandomSearchOptions()
    {
        return new RandomSearchOptions
        {
            SearchSpace = GetStandardSpace(),
            NumberOfTrials = 10,
            RandomSeed = 42,
            EnableParallelSearch = true,
            MaxParallelTrials = 2,
            EnableEarlyStopping = false,
            MetricType = EvaluationMetricType.Accuracy,
            LogAllTrials = true
        };
    }
}
