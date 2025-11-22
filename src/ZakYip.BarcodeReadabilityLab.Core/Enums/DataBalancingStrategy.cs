using System.ComponentModel;
using System.Text.Json.Serialization;

namespace ZakYip.BarcodeReadabilityLab.Core.Enums;

/// <summary>
/// 数据平衡策略
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DataBalancingStrategy
{
    /// <summary>
    /// 不进行数据平衡
    /// </summary>
    [Description("不进行数据平衡")]
    None = 0,

    /// <summary>
    /// 通过复制样本进行过采样
    /// </summary>
    [Description("通过复制样本进行过采样")]
    OverSample = 1,

    /// <summary>
    /// 通过裁剪样本进行欠采样
    /// </summary>
    [Description("通过裁剪样本进行欠采样")]
    UnderSample = 2
}
