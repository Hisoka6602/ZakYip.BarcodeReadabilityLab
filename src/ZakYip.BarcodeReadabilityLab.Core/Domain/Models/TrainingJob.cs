using ZakYip.BarcodeReadabilityLab.Core.Enums;

namespace ZakYip.BarcodeReadabilityLab.Core.Domain.Models;

/// <summary>
/// 训练任务领域模型
/// </summary>
public record class TrainingJob
{
    /// <summary>
    /// 训练任务唯一标识符
    /// </summary>
    public required Guid JobId { get; init; }

    /// <summary>
    /// 训练数据根目录路径
    /// </summary>
    public required string TrainingRootDirectory { get; init; }

    /// <summary>
    /// 训练输出模型文件存放目录路径
    /// </summary>
    public required string OutputModelDirectory { get; init; }

    /// <summary>
    /// 验证集分割比例（0.0 到 1.0 之间）
    /// </summary>
    public decimal? ValidationSplitRatio { get; init; }

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
    /// 训练任务类型
    /// </summary>
    public required TrainingJobType JobType { get; init; }

    /// <summary>
    /// 基础模型版本 ID（增量训练时必填，指向本次训练基于的模型版本）
    /// </summary>
    public Guid? BaseModelVersionId { get; init; }

    /// <summary>
    /// 父训练任务 ID（用于串联训练任务谱系）
    /// </summary>
    public Guid? ParentTrainingJobId { get; init; }

    /// <summary>
    /// 训练任务状态
    /// </summary>
    public required TrainingJobState Status { get; init; }

    /// <summary>
    /// 训练进度百分比（0.0 到 1.0 之间）
    /// </summary>
    public decimal Progress { get; init; }

    /// <summary>
    /// 训练开始时间
    /// </summary>
    public required DateTimeOffset StartTime { get; init; }

    /// <summary>
    /// 训练完成时间（可选）
    /// </summary>
    public DateTimeOffset? CompletedTime { get; init; }

    /// <summary>
    /// 错误信息（训练失败时可用）
    /// </summary>
    public string? ErrorMessage { get; init; }

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
    /// 模型评估指标（训练完成后可用）
    /// </summary>
    public ModelEvaluationMetrics? EvaluationMetrics { get; init; }
}
