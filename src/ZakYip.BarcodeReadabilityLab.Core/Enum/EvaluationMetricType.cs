using System.ComponentModel;

namespace ZakYip.BarcodeReadabilityLab.Core.Enum;

/// <summary>
/// 评估指标类型
/// </summary>
public enum EvaluationMetricType
{
    /// <summary>
    /// 准确率
    /// </summary>
    [Description("准确率")]
    Accuracy = 1,

    /// <summary>
    /// 宏平均 F1 分数
    /// </summary>
    [Description("宏平均F1分数")]
    MacroF1Score = 2,

    /// <summary>
    /// 微平均 F1 分数
    /// </summary>
    [Description("微平均F1分数")]
    MicroF1Score = 3,

    /// <summary>
    /// 对数损失（越小越好）
    /// </summary>
    [Description("对数损失")]
    LogLoss = 4
}
