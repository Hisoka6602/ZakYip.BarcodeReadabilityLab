namespace ZakYip.BarcodeReadabilityLab.Application.Options;

/// <summary>
/// 在线推理评估配置选项
/// </summary>
public record class EvaluationOptions
{
    /// <summary>
    /// 单个图片文件的最大字节数（默认 10MB）
    /// </summary>
    public long MaxImageSizeBytes { get; init; } = 10 * 1024 * 1024;

    /// <summary>
    /// 批量评估时的最大文件数量（默认 100）
    /// </summary>
    public int MaxImageCount { get; init; } = 100;

    /// <summary>
    /// 允许的图片文件扩展名
    /// </summary>
    public string[] AllowedExtensions { get; init; } = new[] { ".jpg", ".jpeg", ".png", ".bmp" };
}
