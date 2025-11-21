using System.ComponentModel;

namespace ZakYip.BarcodeReadabilityLab.Core.Enums;

/// <summary>
/// 超参数调优策略
/// </summary>
public enum HyperparameterTuningStrategy
{
    /// <summary>
    /// 网格搜索
    /// </summary>
    [Description("网格搜索")]
    GridSearch = 1,

    /// <summary>
    /// 随机搜索
    /// </summary>
    [Description("随机搜索")]
    RandomSearch = 2,

    /// <summary>
    /// 贝叶斯优化
    /// </summary>
    [Description("贝叶斯优化")]
    BayesianOptimization = 3
}
