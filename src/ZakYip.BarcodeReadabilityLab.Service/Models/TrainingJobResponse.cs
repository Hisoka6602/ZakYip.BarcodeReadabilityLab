namespace ZakYip.BarcodeReadabilityLab.Service.Models;

using ZakYip.BarcodeReadabilityLab.Core.Domain.Models;

/// <summary>
/// 训练任务响应模型（字段名使用小驼峰风格）
/// </summary>
public record class TrainingJobResponse
{
    /// <summary>
    /// 训练任务唯一标识符
    /// </summary>
    public required Guid JobId { get; init; }

    /// <summary>
    /// 训练任务状态描述
    /// </summary>
    public required string State { get; init; }

    /// <summary>
    /// 训练进度百分比（0.0 到 1.0 之间，可选）
    /// </summary>
    public decimal? Progress { get; init; }

    /// <summary>
    /// 响应消息
    /// </summary>
    public string? Message { get; init; }

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
