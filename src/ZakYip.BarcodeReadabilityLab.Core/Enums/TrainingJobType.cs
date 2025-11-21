using System.ComponentModel;

namespace ZakYip.BarcodeReadabilityLab.Core.Enums;

/// <summary>
/// 训练任务类型
/// </summary>
public enum TrainingJobType
{
    /// <summary>
    /// 全量训练
    /// </summary>
    [Description("全量训练")]
    Full = 0,

    /// <summary>
    /// 增量训练
    /// </summary>
    [Description("增量训练")]
    Incremental = 1,

    /// <summary>
    /// 迁移学习训练
    /// </summary>
    [Description("迁移学习训练")]
    TransferLearning = 2
}
