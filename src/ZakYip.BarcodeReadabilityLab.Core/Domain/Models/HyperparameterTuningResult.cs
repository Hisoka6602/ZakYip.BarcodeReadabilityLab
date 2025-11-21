using ZakYip.BarcodeReadabilityLab.Core.Enum;

namespace ZakYip.BarcodeReadabilityLab.Core.Domain.Models;

/// <summary>
/// 超参数调优结果
/// </summary>
public record class HyperparameterTuningResult
{
    /// <summary>
    /// 调优任务唯一标识
    /// </summary>
    public required Guid TuningJobId { get; init; }

    /// <summary>
    /// 调优策略
    /// </summary>
    public required HyperparameterTuningStrategy Strategy { get; init; }

    /// <summary>
    /// 所有试验结果
    /// </summary>
    public required List<HyperparameterTrialResult> Trials { get; init; }

    /// <summary>
    /// 最佳试验结果
    /// </summary>
    public HyperparameterTrialResult? BestTrial { get; init; }

    /// <summary>
    /// 总试验次数
    /// </summary>
    public int TotalTrials => Trials.Count;

    /// <summary>
    /// 成功试验次数
    /// </summary>
    public int SuccessfulTrials => Trials.Count(t => t.IsSuccessful);

    /// <summary>
    /// 失败试验次数
    /// </summary>
    public int FailedTrials => Trials.Count(t => !t.IsSuccessful);

    /// <summary>
    /// 调优开始时间
    /// </summary>
    public required DateTime StartTime { get; init; }

    /// <summary>
    /// 调优结束时间
    /// </summary>
    public required DateTime EndTime { get; init; }

    /// <summary>
    /// 总耗时（秒）
    /// </summary>
    public decimal TotalDurationSeconds => (decimal)(EndTime - StartTime).TotalSeconds;

    /// <summary>
    /// 训练数据根目录
    /// </summary>
    public required string TrainingRootDirectory { get; init; }

    /// <summary>
    /// 输出模型目录
    /// </summary>
    public required string OutputModelDirectory { get; init; }
}
