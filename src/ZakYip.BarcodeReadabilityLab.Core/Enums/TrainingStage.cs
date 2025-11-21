using System.ComponentModel;

namespace ZakYip.BarcodeReadabilityLab.Core.Enums;

/// <summary>
/// 训练阶段枚举
/// </summary>
public enum TrainingStage
{
    /// <summary>
    /// 初始化
    /// </summary>
    [Description("初始化")]
    Initializing = 0,

    /// <summary>
    /// 扫描数据
    /// </summary>
    [Description("扫描数据")]
    ScanningData = 1,

    /// <summary>
    /// 数据平衡
    /// </summary>
    [Description("数据平衡")]
    BalancingData = 2,

    /// <summary>
    /// 数据增强
    /// </summary>
    [Description("数据增强")]
    AugmentingData = 3,

    /// <summary>
    /// 准备训练数据
    /// </summary>
    [Description("准备训练数据")]
    PreparingData = 4,

    /// <summary>
    /// 构建训练管道
    /// </summary>
    [Description("构建训练管道")]
    BuildingPipeline = 5,

    /// <summary>
    /// 训练模型
    /// </summary>
    [Description("训练模型")]
    Training = 6,

    /// <summary>
    /// 评估模型
    /// </summary>
    [Description("评估模型")]
    Evaluating = 7,

    /// <summary>
    /// 保存模型
    /// </summary>
    [Description("保存模型")]
    SavingModel = 8,

    /// <summary>
    /// 完成
    /// </summary>
    [Description("完成")]
    Completed = 9
}
