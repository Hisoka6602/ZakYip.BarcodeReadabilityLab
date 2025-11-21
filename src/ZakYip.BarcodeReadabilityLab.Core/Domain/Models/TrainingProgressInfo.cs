using ZakYip.BarcodeReadabilityLab.Core.Enums;

namespace ZakYip.BarcodeReadabilityLab.Core.Domain.Models;

/// <summary>
/// 详细训练进度信息
/// </summary>
public record class TrainingProgressInfo
{
    /// <summary>
    /// 任务 ID
    /// </summary>
    public required Guid JobId { get; init; }

    /// <summary>
    /// 当前进度（0.0 到 1.0）
    /// </summary>
    public required decimal Progress { get; init; }

    /// <summary>
    /// 当前训练阶段
    /// </summary>
    public required TrainingStage Stage { get; init; }

    /// <summary>
    /// 阶段描述消息
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// 训练开始时间（UTC）
    /// </summary>
    public DateTime? StartTime { get; init; }

    /// <summary>
    /// 预估剩余时间（秒）
    /// </summary>
    public decimal? EstimatedRemainingSeconds { get; init; }

    /// <summary>
    /// 预估完成时间（UTC）
    /// </summary>
    public DateTime? EstimatedCompletionTime { get; init; }

    /// <summary>
    /// 当前训练指标快照
    /// </summary>
    public TrainingMetricsSnapshot? Metrics { get; init; }

    /// <summary>
    /// 更新时间戳（UTC）
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
