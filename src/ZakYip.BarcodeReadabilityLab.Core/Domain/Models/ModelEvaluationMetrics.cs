using ZakYip.BarcodeReadabilityLab.Core.Enums;

namespace ZakYip.BarcodeReadabilityLab.Core.Domain.Models;

/// <summary>
/// 模型评估指标
/// </summary>
public record class ModelEvaluationMetrics
{
    /// <summary>
    /// 准确率（Accuracy）：正确分类的样本数量占总样本数的比例
    /// </summary>
    public required decimal Accuracy { get; init; }

    /// <summary>
    /// 宏平均精确率（Macro-Average Precision）：所有类别精确率的算术平均值
    /// </summary>
    public required decimal MacroPrecision { get; init; }

    /// <summary>
    /// 宏平均召回率（Macro-Average Recall）：所有类别召回率的算术平均值
    /// </summary>
    public required decimal MacroRecall { get; init; }

    /// <summary>
    /// 宏平均 F1 分数（Macro-Average F1 Score）：所有类别 F1 分数的算术平均值
    /// </summary>
    public required decimal MacroF1Score { get; init; }

    /// <summary>
    /// 微平均精确率（Micro-Average Precision）：全局统计的精确率
    /// </summary>
    public required decimal MicroPrecision { get; init; }

    /// <summary>
    /// 微平均召回率（Micro-Average Recall）：全局统计的召回率
    /// </summary>
    public required decimal MicroRecall { get; init; }

    /// <summary>
    /// 微平均 F1 分数（Micro-Average F1 Score）：全局统计的 F1 分数
    /// </summary>
    public required decimal MicroF1Score { get; init; }

    /// <summary>
    /// 对数损失（Log Loss）：评估模型预测概率的质量
    /// </summary>
    public decimal? LogLoss { get; init; }

    /// <summary>
    /// 混淆矩阵（Confusion Matrix）：JSON 序列化的混淆矩阵数据
    /// </summary>
    public required string ConfusionMatrixJson { get; init; }

    /// <summary>
    /// 每个类别的评估指标（JSON 序列化）
    /// </summary>
    public string? PerClassMetricsJson { get; init; }

    /// <summary>
    /// 数据增强与数据平衡影响报告（JSON 序列化）
    /// </summary>
    public string? DataAugmentationImpactJson { get; init; }
}
