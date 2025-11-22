namespace ZakYip.BarcodeReadabilityLab.Application.Services;

using ZakYip.BarcodeReadabilityLab.Core.Domain.Models;
using ZakYip.BarcodeReadabilityLab.Core.Enums;

/// <summary>
/// 增量训练请求
/// </summary>
public record class IncrementalTrainingRequest
{
    /// <summary>
    /// 基础模型版本 ID（本次增量训练基于的模型版本）
    /// </summary>
    public required Guid BaseModelVersionId { get; init; }

    /// <summary>
    /// 训练数据根目录路径（通常只包含最新收集/标注的样本）
    /// </summary>
    public required string TrainingRootDirectory { get; init; }

    /// <summary>
    /// 训练输出模型文件存放目录路径
    /// </summary>
    public required string OutputModelDirectory { get; init; }

    /// <summary>
    /// 使用的训练档位类型（可选，不指定则使用默认档位或显式参数）
    /// </summary>
    public TrainingProfileType? ProfileType { get; init; }

    /// <summary>
    /// 是否合并历史训练数据
    /// true: 自动将历史训练数据索引与新数据合并
    /// false: 只使用新数据训练（更偏向"微调新 domain"）
    /// </summary>
    public bool MergeWithHistoricalData { get; init; } = true;

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
}
