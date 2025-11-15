namespace ZakYip.BarcodeReadabilityLab.Core.Domain.Models;

/// <summary>
/// 数据增强配置选项
/// </summary>
public record class DataAugmentationOptions
{
    /// <summary>
    /// 是否启用数据增强
    /// </summary>
    public bool Enable { get; init; } = false;

    /// <summary>
    /// 每个样本生成的增强副本数量
    /// </summary>
    public int AugmentedImagesPerSample { get; init; } = 1;

    /// <summary>
    /// 验证增强评估时每个样本生成的副本数量
    /// </summary>
    public int EvaluationAugmentedImagesPerSample { get; init; } = 1;

    /// <summary>
    /// 是否启用旋转操作
    /// </summary>
    public bool EnableRotation { get; init; } = true;

    /// <summary>
    /// 旋转角度集合（单位：度）
    /// </summary>
    public float[] RotationAngles { get; init; } = new[] { -15f, -10f, -5f, 5f, 10f, 15f };

    /// <summary>
    /// 应用旋转操作的概率（0-1）
    /// </summary>
    public double RotationProbability { get; init; } = 0.7;

    /// <summary>
    /// 是否启用水平翻转
    /// </summary>
    public bool EnableHorizontalFlip { get; init; } = true;

    /// <summary>
    /// 应用水平翻转的概率（0-1）
    /// </summary>
    public double HorizontalFlipProbability { get; init; } = 0.5;

    /// <summary>
    /// 是否启用垂直翻转
    /// </summary>
    public bool EnableVerticalFlip { get; init; } = false;

    /// <summary>
    /// 应用垂直翻转的概率（0-1）
    /// </summary>
    public double VerticalFlipProbability { get; init; } = 0.2;

    /// <summary>
    /// 是否启用亮度调整
    /// </summary>
    public bool EnableBrightnessAdjustment { get; init; } = true;

    /// <summary>
    /// 应用亮度调整的概率（0-1）
    /// </summary>
    public double BrightnessProbability { get; init; } = 0.6;

    /// <summary>
    /// 亮度调整下限（0-3）
    /// </summary>
    public float BrightnessLower { get; init; } = 0.85f;

    /// <summary>
    /// 亮度调整上限（0-3）
    /// </summary>
    public float BrightnessUpper { get; init; } = 1.15f;

    /// <summary>
    /// 是否在增强后打乱数据
    /// </summary>
    public bool ShuffleAugmentedData { get; init; } = true;

    /// <summary>
    /// 随机数种子，保证增强过程可重复
    /// </summary>
    public int RandomSeed { get; init; } = 42;
}
