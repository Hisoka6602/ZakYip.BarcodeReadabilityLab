namespace ZakYip.BarcodeReadabilityLab.Core.Domain.Models;

/// <summary>
/// 网格搜索配置选项
/// </summary>
public record class GridSearchOptions
{
    /// <summary>
    /// 超参数搜索空间
    /// </summary>
    public required HyperparameterSpace SearchSpace { get; init; }

    /// <summary>
    /// 是否启用并行搜索
    /// </summary>
    public bool EnableParallelSearch { get; init; } = true;

    /// <summary>
    /// 最大并行任务数（0 表示使用系统默认值）
    /// </summary>
    public int MaxParallelTrials { get; init; } = 0;

    /// <summary>
    /// 是否在找到更好结果时提前停止
    /// </summary>
    public bool EnableEarlyStopping { get; init; } = false;

    /// <summary>
    /// 评估指标类型（用于确定最佳模型）
    /// </summary>
    public EvaluationMetricType MetricType { get; init; } = EvaluationMetricType.Accuracy;

    /// <summary>
    /// 是否记录所有试验结果
    /// </summary>
    public bool LogAllTrials { get; init; } = true;
}
