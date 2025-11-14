namespace ZakYip.BarcodeReadabilityLab.Service.Configuration;

public class BarcodeReadabilityServiceSettings
{
    public string MonitorPath { get; set; } = string.Empty;
    public string UnableToAnalyzePath { get; set; } = string.Empty;
    public string TrainingDataPath { get; set; } = string.Empty;
    public string ModelPath { get; set; } = string.Empty;
    public double ConfidenceThreshold { get; set; } = 0.9;
    public string[] SupportedImageExtensions { get; set; } = Array.Empty<string>();
}
