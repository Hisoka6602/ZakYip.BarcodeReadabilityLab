namespace ZakYip.BarcodeReadabilityLab.Service.Models;

/// <summary>
/// 启动训练任务的响应模型
/// </summary>
/// <example>
/// {
///   "jobId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
///   "message": "训练任务已创建并加入队列",
///   "jobType": "Full",
///   "baseModelVersionId": null
/// }
/// </example>
public record class StartTrainingResponse
{
    /// <summary>
    /// 训练任务唯一标识符
    /// </summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public required Guid JobId { get; init; }

    /// <summary>
    /// 响应消息
    /// </summary>
    /// <example>训练任务已创建并加入队列</example>
    public required string Message { get; init; }

    /// <summary>
    /// 训练任务类型（Full/Incremental/TransferLearning）
    /// </summary>
    /// <example>Full</example>
    public string? JobType { get; init; }

    /// <summary>
    /// 基础模型版本 ID（仅增量训练时有值）
    /// </summary>
    /// <example>null</example>
    public Guid? BaseModelVersionId { get; init; }
}
