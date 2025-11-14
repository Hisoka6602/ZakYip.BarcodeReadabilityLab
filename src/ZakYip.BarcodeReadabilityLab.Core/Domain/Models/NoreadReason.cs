using System.ComponentModel;

namespace ZakYip.BarcodeReadabilityLab.Core.Domain.Models;

/// <summary>
/// 条码读取失败的原因类型
/// </summary>
public enum NoreadReason
{
    /// <summary>
    /// 条码被截断
    /// </summary>
    [Description("条码被截断")]
    Truncated = 1,

    /// <summary>
    /// 条码模糊或失焦
    /// </summary>
    [Description("条码模糊或失焦")]
    BlurryOrOutOfFocus = 2,

    /// <summary>
    /// 反光或高亮过曝
    /// </summary>
    [Description("反光或高亮过曝")]
    ReflectionOrOverexposure = 3,

    /// <summary>
    /// 条码褶皱或形变严重
    /// </summary>
    [Description("条码褶皱或形变严重")]
    WrinkledOrDeformed = 4,

    /// <summary>
    /// 画面内无条码
    /// </summary>
    [Description("画面内无条码")]
    NoBarcodeInImage = 5,

    /// <summary>
    /// 条码有污渍或遮挡
    /// </summary>
    [Description("条码有污渍或遮挡")]
    StainedOrObstructed = 6,

    /// <summary>
    /// 条码清晰但未被识别
    /// </summary>
    [Description("条码清晰但未被识别")]
    ClearButNotRecognized = 7
}
