using ZakYip.BarcodeReadabilityLab.Core.Enum;

namespace ZakYip.BarcodeReadabilityLab.Core.Domain.Models;

/// <summary>
/// 超参数试验结果
/// </summary>
public record class HyperparameterTrialResult
{
    /// <summary>
    /// 试验唯一标识
    /// </summary>
    public required Guid TrialId { get; init; }

    /// <summary>
    /// 超参数配置
    /// </summary>
    public required HyperparameterConfiguration Configuration { get; init; }

    /// <summary>
    /// 模型评估指标
    /// </summary>
    public required ModelEvaluationMetrics Metrics { get; init; }

    /// <summary>
    /// 模型文件路径
    /// </summary>
    public required string ModelFilePath { get; init; }

    /// <summary>
    /// 训练开始时间
    /// </summary>
    public required DateTime StartTime { get; init; }

    /// <summary>
    /// 训练结束时间
    /// </summary>
    public required DateTime EndTime { get; init; }

    /// <summary>
    /// 训练耗时（秒）
    /// </summary>
    public decimal TrainingDurationSeconds => (decimal)(EndTime - StartTime).TotalSeconds;

    /// <summary>
    /// 是否成功
    /// </summary>
    public required bool IsSuccessful { get; init; }

    /// <summary>
    /// 错误消息（如果失败）
    /// </summary>
    public string? ErrorMessage { get; init; }
}
