using ZakYip.BarcodeReadabilityLab.Core.Domain.Models;
using ZakYip.BarcodeReadabilityLab.Core.Enum;

namespace ZakYip.BarcodeReadabilityLab.Core.Tests;

/// <summary>
/// 条码样本数据测试
/// </summary>
public sealed class BarcodeSampleTests
{
    [Fact]
    public void Constructor_ShouldCreateInstance_WithRequiredProperties()
    {
        // Arrange
        var sampleId = Guid.NewGuid();
        var filePath = "/path/to/barcode.jpg";
        var capturedAt = DateTimeOffset.UtcNow;

        // Act
        var sample = new BarcodeSample
        {
            SampleId = sampleId,
            FilePath = filePath,
            CapturedAt = capturedAt
        };

        // Assert
        Assert.Equal(sampleId, sample.SampleId);
        Assert.Equal(filePath, sample.FilePath);
        Assert.Equal(capturedAt, sample.CapturedAt);
        Assert.Null(sample.CameraId);
    }

    [Fact]
    public void Constructor_ShouldCreateInstance_WithAllProperties()
    {
        // Arrange
        var sampleId = Guid.NewGuid();
        var filePath = "/path/to/barcode.jpg";
        var capturedAt = DateTimeOffset.UtcNow;
        var cameraId = "CAM-001";

        // Act
        var sample = new BarcodeSample
        {
            SampleId = sampleId,
            FilePath = filePath,
            CapturedAt = capturedAt,
            CameraId = cameraId
        };

        // Assert
        Assert.Equal(sampleId, sample.SampleId);
        Assert.Equal(filePath, sample.FilePath);
        Assert.Equal(capturedAt, sample.CapturedAt);
        Assert.Equal(cameraId, sample.CameraId);
    }

    [Fact]
    public void RecordEquality_ShouldWork_ForSameValues()
    {
        // Arrange
        var sampleId = Guid.NewGuid();
        var filePath = "/path/to/barcode.jpg";
        var capturedAt = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);

        var sample1 = new BarcodeSample
        {
            SampleId = sampleId,
            FilePath = filePath,
            CapturedAt = capturedAt,
            CameraId = "CAM-001"
        };

        var sample2 = new BarcodeSample
        {
            SampleId = sampleId,
            FilePath = filePath,
            CapturedAt = capturedAt,
            CameraId = "CAM-001"
        };

        // Act & Assert
        Assert.Equal(sample1, sample2);
        Assert.True(sample1 == sample2);
    }

    [Fact]
    public void RecordEquality_ShouldNotWork_ForDifferentSampleIds()
    {
        // Arrange
        var capturedAt = DateTimeOffset.UtcNow;

        var sample1 = new BarcodeSample
        {
            SampleId = Guid.NewGuid(),
            FilePath = "/path/to/barcode.jpg",
            CapturedAt = capturedAt
        };

        var sample2 = new BarcodeSample
        {
            SampleId = Guid.NewGuid(),
            FilePath = "/path/to/barcode.jpg",
            CapturedAt = capturedAt
        };

        // Act & Assert
        Assert.NotEqual(sample1, sample2);
        Assert.False(sample1 == sample2);
    }

    [Fact]
    public void RecordEquality_ShouldNotWork_ForDifferentFilePaths()
    {
        // Arrange
        var sampleId = Guid.NewGuid();
        var capturedAt = DateTimeOffset.UtcNow;

        var sample1 = new BarcodeSample
        {
            SampleId = sampleId,
            FilePath = "/path/to/barcode1.jpg",
            CapturedAt = capturedAt
        };

        var sample2 = new BarcodeSample
        {
            SampleId = sampleId,
            FilePath = "/path/to/barcode2.jpg",
            CapturedAt = capturedAt
        };

        // Act & Assert
        Assert.NotEqual(sample1, sample2);
    }
}
