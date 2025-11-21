namespace ZakYip.BarcodeReadabilityLab.Service.Models;

using ZakYip.BarcodeReadabilityLab.Core.Domain.Models;
using ZakYip.BarcodeReadabilityLab.Core.Enum;

/// <summary>
/// 训练任务响应模型（字段名使用小驼峰风格）
/// </summary>
/// <remarks>
/// 提供训练任务的完整状态信息，包括进度、时间、错误信息和评估指标。
/// </remarks>
/// <example>
/// {
///   "jobId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
///   "state": "运行中",
///   "progress": 0.65,
///   "learningRate": 0.01,
///   "epochs": 50,
///   "batchSize": 20,
///   "message": "训练任务正在执行",
///   "startTime": "2024-01-01T10:00:00Z",
///   "completedTime": null,
///   "errorMessage": null,
///   "remarks": "第一次训练测试"
/// }
/// </example>
public record class TrainingJobResponse
{
    /// <summary>
    /// 训练任务唯一标识符
    /// </summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public required Guid JobId { get; init; }

    /// <summary>
    /// 训练任务状态描述
    /// </summary>
    /// <example>运行中</example>
    public required string State { get; init; }

    /// <summary>
    /// 训练进度百分比（0.0 到 1.0 之间，可选）
    /// </summary>
    /// <example>0.65</example>
    public decimal? Progress { get; init; }

    /// <summary>
    /// 学习率
    /// </summary>
    /// <example>0.01</example>
    public decimal LearningRate { get; init; }

    /// <summary>
    /// 训练轮数（Epoch）
    /// </summary>
    /// <example>50</example>
    public int Epochs { get; init; }

    /// <summary>
    /// 批大小（Batch Size）
    /// </summary>
    /// <example>20</example>
    public int BatchSize { get; init; }

    /// <summary>
    /// 响应消息
    /// </summary>
    /// <example>训练任务正在执行</example>
    public string? Message { get; init; }

    /// <summary>
    /// 训练开始时间（可选）
    /// </summary>
    /// <example>2024-01-01T10:00:00Z</example>
    public DateTimeOffset? StartTime { get; init; }

    /// <summary>
    /// 训练完成时间（可选）
    /// </summary>
    /// <example>2024-01-01T11:30:00Z</example>
    public DateTimeOffset? CompletedTime { get; init; }

    /// <summary>
    /// 错误信息（训练失败时可用）
    /// </summary>
    /// <example>训练数据不足</example>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// 训练任务备注说明（可选）
    /// </summary>
    /// <example>第一次训练测试</example>
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
