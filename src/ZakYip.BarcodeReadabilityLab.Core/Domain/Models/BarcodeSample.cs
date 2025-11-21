using ZakYip.BarcodeReadabilityLab.Core.Enums;

namespace ZakYip.BarcodeReadabilityLab.Core.Domain.Models;

/// <summary>
/// 条码样本数据
/// </summary>
public record class BarcodeSample
{
    /// <summary>
    /// 样本唯一标识符
    /// </summary>
    public required Guid SampleId { get; init; }

    /// <summary>
    /// 条码图片文件路径
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// 图片拍摄时间
    /// </summary>
    public required DateTimeOffset CapturedAt { get; init; }

    /// <summary>
    /// 相机标识符（可选）
    /// </summary>
    public string? CameraId { get; init; }
}
