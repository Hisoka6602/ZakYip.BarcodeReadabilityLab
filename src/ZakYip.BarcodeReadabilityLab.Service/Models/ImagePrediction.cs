using Microsoft.ML.Data;

namespace ZakYip.BarcodeReadabilityLab.Service.Models;

public class ImagePrediction
{
    [ColumnName("Score")]
    public float[] Score { get; set; } = Array.Empty<float>();

    [ColumnName("PredictedLabel")]
    public string PredictedLabel { get; set; } = string.Empty;
}
