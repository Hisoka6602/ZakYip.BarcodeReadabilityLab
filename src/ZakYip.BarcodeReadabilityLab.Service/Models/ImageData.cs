using Microsoft.ML.Data;

namespace ZakYip.BarcodeReadabilityLab.Service.Models;

public class ImageData
{
    [LoadColumn(0)]
    public string ImagePath { get; set; } = string.Empty;

    [LoadColumn(1)]
    public string Label { get; set; } = string.Empty;
}
