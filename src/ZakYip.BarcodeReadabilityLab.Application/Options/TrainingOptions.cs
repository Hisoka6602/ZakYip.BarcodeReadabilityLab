namespace ZakYip.BarcodeReadabilityLab.Application.Options;

using ZakYip.BarcodeReadabilityLab.Core.Domain.Models;

/// <summary>
/// 训练任务配置选项
/// </summary>
public record class TrainingOptions
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
    /// 学习率（默认值：0.01）
    /// </summary>
    public decimal LearningRate { get; init; } = 0.01m;

    /// <summary>
    /// 训练轮数（Epoch，默认值：50）
    /// </summary>
    public int Epochs { get; init; } = 50;

    /// <summary>
    /// 批大小（Batch Size，默认值：20）
    /// </summary>
    public int BatchSize { get; init; } = 20;

    /// <summary>
    /// 最大并发训练任务数量（默认值：1）
    /// </summary>
    public int MaxConcurrentTrainingJobs { get; init; } = 1;

    /// <summary>
    /// 是否启用资源监控（默认值：false）
    /// </summary>
    public bool EnableResourceMonitoring { get; init; } = false;

    /// <summary>
    /// 资源监控间隔（秒）（默认值：5）
    /// </summary>
    public int ResourceMonitoringIntervalSeconds { get; init; } = 5;

    /// <summary>
    /// 数据增强配置（默认禁用）
    /// </summary>
    public DataAugmentationOptions DataAugmentation { get; init; } = new();

    /// <summary>
    /// 数据平衡配置（默认不处理）
    /// </summary>
    public DataBalancingOptions DataBalancing { get; init; } = new();
}
