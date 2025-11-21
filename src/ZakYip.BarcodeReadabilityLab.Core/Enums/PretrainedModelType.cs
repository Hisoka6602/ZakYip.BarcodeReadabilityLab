using System.ComponentModel;

namespace ZakYip.BarcodeReadabilityLab.Core.Enums;

/// <summary>
/// 预训练模型类型
/// </summary>
public enum PretrainedModelType
{
    /// <summary>
    /// ResNet50 - 50层深度残差网络
    /// </summary>
    [Description("ResNet50 - 50层深度残差网络")]
    ResNet50 = 1,

    /// <summary>
    /// ResNet101 - 101层深度残差网络
    /// </summary>
    [Description("ResNet101 - 101层深度残差网络")]
    ResNet101 = 2,

    /// <summary>
    /// InceptionV3 - Inception V3 网络
    /// </summary>
    [Description("InceptionV3 - Inception V3 网络")]
    InceptionV3 = 3,

    /// <summary>
    /// EfficientNetB0 - EfficientNet B0 网络
    /// </summary>
    [Description("EfficientNetB0 - EfficientNet B0 网络")]
    EfficientNetB0 = 4,

    /// <summary>
    /// MobileNetV2 - MobileNet V2 轻量级网络
    /// </summary>
    [Description("MobileNetV2 - MobileNet V2 轻量级网络")]
    MobileNetV2 = 5
}
