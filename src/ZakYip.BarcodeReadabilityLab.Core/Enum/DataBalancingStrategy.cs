using System.Text.Json.Serialization;

namespace ZakYip.BarcodeReadabilityLab.Core.Enum;

/// <summary>
/// 数据平衡策略
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DataBalancingStrategy
{
    /// <summary>
    /// 不进行数据平衡
    /// </summary>
    None = 0,

    /// <summary>
    /// 通过复制样本进行过采样
    /// </summary>
    OverSample = 1,

    /// <summary>
    /// 通过裁剪样本进行欠采样
    /// </summary>
    UnderSample = 2
}
