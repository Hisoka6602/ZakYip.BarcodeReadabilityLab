using Microsoft.Extensions.Options;

namespace ZakYip.BarcodeReadabilityLab.Application.Options;

/// <summary>
/// 训练档位配置验证器
/// </summary>
public sealed class TrainingProfileOptionsValidator : IValidateOptions<TrainingProfileOptions>
{
    public ValidateOptionsResult Validate(string? name, TrainingProfileOptions options)
    {
        var errors = new List<string>();

        // 验证 Debug 档位配置
        ValidateConfiguration(options.Debug, "Debug", errors);

        // 验证 Standard 档位配置
        ValidateConfiguration(options.Standard, "Standard", errors);

        // 验证 HighQuality 档位配置
        ValidateConfiguration(options.HighQuality, "HighQuality", errors);

        if (errors.Count > 0)
        {
            return ValidateOptionsResult.Fail(errors);
        }

        return ValidateOptionsResult.Success;
    }

    private static void ValidateConfiguration(
        TrainingProfileConfiguration config,
        string profileName,
        List<string> errors)
    {
        if (config.Epochs < 1)
        {
            errors.Add($"训练档位 {profileName} 的 Epochs 必须大于 0，当前值: {config.Epochs}");
        }

        if (config.Epochs > 1000)
        {
            errors.Add($"训练档位 {profileName} 的 Epochs 不应超过 1000，当前值: {config.Epochs}");
        }

        if (config.BatchSize < 1)
        {
            errors.Add($"训练档位 {profileName} 的 BatchSize 必须大于 0，当前值: {config.BatchSize}");
        }

        if (config.BatchSize > 512)
        {
            errors.Add($"训练档位 {profileName} 的 BatchSize 不应超过 512，当前值: {config.BatchSize}");
        }

        if (config.LearningRate <= 0m)
        {
            errors.Add($"训练档位 {profileName} 的 LearningRate 必须大于 0，当前值: {config.LearningRate}");
        }

        if (config.LearningRate > 1m)
        {
            errors.Add($"训练档位 {profileName} 的 LearningRate 不应超过 1，当前值: {config.LearningRate}");
        }

        if (config.L2Regularization.HasValue)
        {
            if (config.L2Regularization.Value < 0m)
            {
                errors.Add($"训练档位 {profileName} 的 L2Regularization 不能为负数，当前值: {config.L2Regularization.Value}");
            }
        }

        if (config.EnableEarlyStopping)
        {
            if (config.EarlyStoppingPatience < 1)
            {
                errors.Add($"训练档位 {profileName} 的 EarlyStoppingPatience 必须大于 0，当前值: {config.EarlyStoppingPatience}");
            }

            if (config.EarlyStoppingMinDelta < 0m)
            {
                errors.Add($"训练档位 {profileName} 的 EarlyStoppingMinDelta 不能为负数，当前值: {config.EarlyStoppingMinDelta}");
            }
        }

        if (config.ImageWidth < 16 || config.ImageWidth > 2048)
        {
            errors.Add($"训练档位 {profileName} 的 ImageWidth 必须在 16 到 2048 之间，当前值: {config.ImageWidth}");
        }

        if (config.ImageHeight < 16 || config.ImageHeight > 2048)
        {
            errors.Add($"训练档位 {profileName} 的 ImageHeight 必须在 16 到 2048 之间，当前值: {config.ImageHeight}");
        }

        if (config.ValidationSplitRatio < 0.0m || config.ValidationSplitRatio > 1.0m)
        {
            errors.Add($"训练档位 {profileName} 的 ValidationSplitRatio 必须在 0.0 到 1.0 之间，当前值: {config.ValidationSplitRatio}");
        }

        if (config.EvaluationFrequency < 1)
        {
            errors.Add($"训练档位 {profileName} 的 EvaluationFrequency 必须大于 0，当前值: {config.EvaluationFrequency}");
        }

        // 验证数据增强配置
        if (config.EnableDataAugmentation && config.DataAugmentation is not null)
        {
            var aug = config.DataAugmentation;

            if (aug.AugmentedImagesPerSample < 0)
            {
                errors.Add($"训练档位 {profileName} 的数据增强副本数不能为负数，当前值: {aug.AugmentedImagesPerSample}");
            }

            if (aug.RotationProbability < 0.0 || aug.RotationProbability > 1.0)
            {
                errors.Add($"训练档位 {profileName} 的旋转概率必须在 0.0 到 1.0 之间，当前值: {aug.RotationProbability}");
            }

            if (aug.HorizontalFlipProbability < 0.0 || aug.HorizontalFlipProbability > 1.0)
            {
                errors.Add($"训练档位 {profileName} 的水平翻转概率必须在 0.0 到 1.0 之间，当前值: {aug.HorizontalFlipProbability}");
            }

            if (aug.VerticalFlipProbability < 0.0 || aug.VerticalFlipProbability > 1.0)
            {
                errors.Add($"训练档位 {profileName} 的垂直翻转概率必须在 0.0 到 1.0 之间，当前值: {aug.VerticalFlipProbability}");
            }

            if (aug.BrightnessProbability < 0.0 || aug.BrightnessProbability > 1.0)
            {
                errors.Add($"训练档位 {profileName} 的亮度调整概率必须在 0.0 到 1.0 之间，当前值: {aug.BrightnessProbability}");
            }
        }
    }
}
