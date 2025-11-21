namespace ZakYip.BarcodeReadabilityLab.Application.Tests.TestData.Builders;

using ZakYip.BarcodeReadabilityLab.Core.Domain.Models;

/// <summary>
/// 条码样本测试数据构造器（Builder 模式）
/// </summary>
public sealed class BarcodeSampleBuilder
{
    private Guid? _sampleId = null;
    private string _filePath = Path.Combine(Path.GetTempPath(), "sample.jpg");
    private DateTimeOffset _capturedAt = DateTimeOffset.UtcNow;
    private string? _cameraId = null;

    /// <summary>
    /// 设置样本 ID
    /// </summary>
    public BarcodeSampleBuilder WithSampleId(Guid sampleId)
    {
        _sampleId = sampleId;
        return this;
    }

    /// <summary>
    /// 设置文件路径
    /// </summary>
    public BarcodeSampleBuilder WithFilePath(string filePath)
    {
        _filePath = filePath;
        return this;
    }

    /// <summary>
    /// 设置拍摄时间
    /// </summary>
    public BarcodeSampleBuilder WithCapturedAt(DateTimeOffset capturedAt)
    {
        _capturedAt = capturedAt;
        return this;
    }

    /// <summary>
    /// 设置相机 ID
    /// </summary>
    public BarcodeSampleBuilder WithCameraId(string? cameraId)
    {
        _cameraId = cameraId;
        return this;
    }

    /// <summary>
    /// 构建条码样本对象
    /// </summary>
    public BarcodeSample Build()
    {
        return new BarcodeSample
        {
            SampleId = _sampleId ?? Guid.NewGuid(),
            FilePath = _filePath,
            CapturedAt = _capturedAt,
            CameraId = _cameraId
        };
    }
}
