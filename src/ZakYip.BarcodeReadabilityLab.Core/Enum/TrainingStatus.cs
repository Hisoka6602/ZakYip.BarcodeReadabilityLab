using System.ComponentModel;

namespace ZakYip.BarcodeReadabilityLab.Core.Enum;

/// <summary>
/// 训练任务状态枚举
/// </summary>
public enum TrainingStatus
{
    /// <summary>
    /// 排队中
    /// </summary>
    [Description("排队中")]
    Queued = 1,

    /// <summary>
    /// 运行中
    /// </summary>
    [Description("运行中")]
    Running = 2,

    /// <summary>
    /// 已完成
    /// </summary>
    [Description("已完成")]
    Completed = 3,

    /// <summary>
    /// 失败
    /// </summary>
    [Description("失败")]
    Failed = 4,

    /// <summary>
    /// 已取消
    /// </summary>
    [Description("已取消")]
    Cancelled = 5
}
