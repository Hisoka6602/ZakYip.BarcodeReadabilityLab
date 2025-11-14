namespace ZakYip.BarcodeReadabilityLab.Infrastructure.MLNet.Models;

using Microsoft.ML.Data;

/// <summary>
/// ML.NET 图片输入模型
/// </summary>
public record class MlNetImageInput
{
    /// <summary>
    /// 图片文件路径
    /// </summary>
    [LoadColumn(0)]
    public required string ImagePath { get; init; }
}
