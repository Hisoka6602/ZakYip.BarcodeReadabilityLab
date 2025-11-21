using ZakYip.BarcodeReadabilityLab.Core.Enums;

namespace ZakYip.BarcodeReadabilityLab.Core.Domain.Models;

/// <summary>
/// 数据增强与数据平衡对训练的影响报告
/// </summary>
public record class DataAugmentationImpact
{
    /// <summary>
    /// 是否启用了数据增强
    /// </summary>
    public bool IsAugmentationApplied { get; init; }

    /// <summary>
    /// 是否启用了数据平衡
    /// </summary>
    public bool IsBalancingApplied { get; init; }

    /// <summary>
    /// 数据增强配置
    /// </summary>
    public DataAugmentationOptions? AugmentationOptions { get; init; }

    /// <summary>
    /// 数据平衡配置
    /// </summary>
    public DataBalancingOptions? BalancingOptions { get; init; }

    /// <summary>
    /// 数据集汇总信息
    /// </summary>
    public required DataAugmentationDatasetSummary Dataset { get; init; }

    /// <summary>
    /// 评估指标对比
    /// </summary>
    public DataAugmentationEvaluationSummary? Evaluation { get; init; }
}

/// <summary>
/// 数据增强前后数据集分布情况
/// </summary>
public record class DataAugmentationDatasetSummary
{
    /// <summary>
    /// 原始样本数量
    /// </summary>
    public int OriginalSamples { get; init; }

    /// <summary>
    /// 数据平衡后的样本数量
    /// </summary>
    public int BalancedSamples { get; init; }

    /// <summary>
    /// 新增的增强样本数量
    /// </summary>
    public int AugmentedSamples { get; init; }

    /// <summary>
    /// 最终用于训练的样本总数
    /// </summary>
    public int TotalSamples { get; init; }

    /// <summary>
    /// 原始标签分布
    /// </summary>
    public IDictionary<string, int> OriginalDistribution { get; init; } = new Dictionary<string, int>();

    /// <summary>
    /// 数据平衡后的标签分布
    /// </summary>
    public IDictionary<string, int> BalancedDistribution { get; init; } = new Dictionary<string, int>();

    /// <summary>
    /// 最终训练数据的标签分布
    /// </summary>
    public IDictionary<string, int> FinalDistribution { get; init; } = new Dictionary<string, int>();

    /// <summary>
    /// 各增强操作的使用次数
    /// </summary>
    public IDictionary<string, int> OperationUsage { get; init; } = new Dictionary<string, int>();
}

/// <summary>
/// 数据增强前后的评估指标对比
/// </summary>
public record class DataAugmentationEvaluationSummary
{
    /// <summary>
    /// 原始测试集样本数量
    /// </summary>
    public int OriginalSampleCount { get; init; }

    /// <summary>
    /// 增强后的测试集样本数量
    /// </summary>
    public int AugmentedSampleCount { get; init; }

    /// <summary>
    /// 原始测试集准确率
    /// </summary>
    public decimal OriginalAccuracy { get; init; }

    /// <summary>
    /// 增强测试集准确率
    /// </summary>
    public decimal AugmentedAccuracy { get; init; }

    /// <summary>
    /// 准确率差值
    /// </summary>
    public decimal AccuracyDelta => AugmentedAccuracy - OriginalAccuracy;

    /// <summary>
    /// 原始宏平均精确率
    /// </summary>
    public decimal OriginalMacroPrecision { get; init; }

    /// <summary>
    /// 增强宏平均精确率
    /// </summary>
    public decimal AugmentedMacroPrecision { get; init; }

    /// <summary>
    /// 宏平均精确率差值
    /// </summary>
    public decimal MacroPrecisionDelta => AugmentedMacroPrecision - OriginalMacroPrecision;

    /// <summary>
    /// 原始宏平均召回率
    /// </summary>
    public decimal OriginalMacroRecall { get; init; }

    /// <summary>
    /// 增强宏平均召回率
    /// </summary>
    public decimal AugmentedMacroRecall { get; init; }

    /// <summary>
    /// 宏平均召回率差值
    /// </summary>
    public decimal MacroRecallDelta => AugmentedMacroRecall - OriginalMacroRecall;

    /// <summary>
    /// 原始宏平均 F1 值
    /// </summary>
    public decimal OriginalMacroF1 { get; init; }

    /// <summary>
    /// 增强宏平均 F1 值
    /// </summary>
    public decimal AugmentedMacroF1 { get; init; }

    /// <summary>
    /// 宏平均 F1 差值
    /// </summary>
    public decimal MacroF1Delta => AugmentedMacroF1 - OriginalMacroF1;

    /// <summary>
    /// 原始微平均精确率
    /// </summary>
    public decimal OriginalMicroPrecision { get; init; }

    /// <summary>
    /// 增强微平均精确率
    /// </summary>
    public decimal AugmentedMicroPrecision { get; init; }

    /// <summary>
    /// 微平均精确率差值
    /// </summary>
    public decimal MicroPrecisionDelta => AugmentedMicroPrecision - OriginalMicroPrecision;

    /// <summary>
    /// 原始微平均召回率
    /// </summary>
    public decimal OriginalMicroRecall { get; init; }

    /// <summary>
    /// 增强微平均召回率
    /// </summary>
    public decimal AugmentedMicroRecall { get; init; }

    /// <summary>
    /// 微平均召回率差值
    /// </summary>
    public decimal MicroRecallDelta => AugmentedMicroRecall - OriginalMicroRecall;

    /// <summary>
    /// 原始微平均 F1 值
    /// </summary>
    public decimal OriginalMicroF1 { get; init; }

    /// <summary>
    /// 增强微平均 F1 值
    /// </summary>
    public decimal AugmentedMicroF1 { get; init; }

    /// <summary>
    /// 微平均 F1 差值
    /// </summary>
    public decimal MicroF1Delta => AugmentedMicroF1 - OriginalMicroF1;
}
