namespace ZakYip.BarcodeReadabilityLab.Infrastructure.MLNet.Models;

/// <summary>
/// ML.NET 条码分类模型配置选项
/// </summary>
public record class BarcodeMlModelOptions
{
    /// <summary>
    /// 当前在线推理使用的模型文件路径
    /// </summary>
    /// <remarks>
    /// 例如：models/noread-classifier-current.zip
    /// </remarks>
    public required string CurrentModelPath { get; init; }
}
