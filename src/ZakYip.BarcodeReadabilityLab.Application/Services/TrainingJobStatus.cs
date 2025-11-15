namespace ZakYip.BarcodeReadabilityLab.Application.Services;

using ZakYip.BarcodeReadabilityLab.Core.Domain.Models;

/// <summary>
/// 训练任务状态
/// </summary>
public record class TrainingJobStatus
{
    /// <summary>
    /// 训练任务唯一标识符
    /// </summary>
    public required Guid JobId { get; init; }

    /// <summary>
    /// 训练任务状态
    /// </summary>
    public required TrainingStatus Status { get; init; }

    /// <summary>
    /// 训练进度百分比（0.0 到 1.0 之间）
    /// </summary>
    public decimal Progress { get; init; }

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
    /// 训练开始时间（可选）
    /// </summary>
    public DateTimeOffset? StartTime { get; init; }

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
    /// 模型评估指标（训练完成后可用）
    /// </summary>
    public ModelEvaluationMetrics? EvaluationMetrics { get; init; }
}
