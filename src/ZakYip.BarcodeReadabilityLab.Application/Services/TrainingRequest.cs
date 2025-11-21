namespace ZakYip.BarcodeReadabilityLab.Application.Services;

using ZakYip.BarcodeReadabilityLab.Core.Domain.Models;
using ZakYip.BarcodeReadabilityLab.Core.Enum;

/// <summary>
/// 训练请求
/// </summary>
public record class TrainingRequest
{
    /// <summary>
    /// 训练数据根目录路径
    /// </summary>
    public required string TrainingRootDirectory { get; init; }

    /// <summary>
    /// 训练输出模型文件存放目录路径
    /// </summary>
    public required string OutputModelDirectory { get; init; }

    /// <summary>
    /// 验证集分割比例（0.0 到 1.0 之间，可选）
    /// </summary>
    public decimal? ValidationSplitRatio { get; init; }

    /// <summary>
    /// 学习率（大于 0 的小数）
    /// </summary>
    public required decimal LearningRate { get; init; }

    /// <summary>
    /// 训练轮数（Epoch，正整数）
    /// </summary>
    public required int Epochs { get; init; }

    /// <summary>
    /// 批大小（Batch Size，正整数）
    /// </summary>
    public required int BatchSize { get; init; }

    /// <summary>
    /// 训练任务备注说明（可选）
    /// </summary>
    public string? Remarks { get; init; }

    /// <summary>
    /// 数据增强配置
    /// </summary>
    public DataAugmentationOptions DataAugmentation { get; init; } = new();

    /// <summary>
    /// 数据平衡配置
    /// </summary>
    public DataBalancingOptions DataBalancing { get; init; } = new();

    /// <summary>
    /// 迁移学习配置（可选）
    /// </summary>
    public TransferLearningOptions? TransferLearningOptions { get; init; }
}
