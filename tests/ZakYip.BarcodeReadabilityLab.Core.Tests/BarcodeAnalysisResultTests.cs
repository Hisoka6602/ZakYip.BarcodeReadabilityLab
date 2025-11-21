using ZakYip.BarcodeReadabilityLab.Core.Domain.Models;
using ZakYip.BarcodeReadabilityLab.Core.Enum;

namespace ZakYip.BarcodeReadabilityLab.Core.Tests;

/// <summary>
/// 条码分析结果测试
/// </summary>
public sealed class BarcodeAnalysisResultTests
{
    [Fact]
    public void Constructor_ShouldCreateInstance_WithRequiredProperties()
    {
        // Arrange & Act
        var result = new BarcodeAnalysisResult
        {
            SampleId = Guid.NewGuid(),
            IsAnalyzed = true,
            IsAboveThreshold = true
        };

        // Assert
        Assert.NotEqual(Guid.Empty, result.SampleId);
        Assert.True(result.IsAnalyzed);
        Assert.True(result.IsAboveThreshold);
        Assert.Null(result.Reason);
        Assert.Null(result.Confidence);
        Assert.Null(result.Message);
    }

    [Fact]
    public void Constructor_ShouldCreateInstance_WithAllProperties()
    {
        // Arrange
        var sampleId = Guid.NewGuid();
        var confidence = 0.95m;
        var message = "分析成功";

        // Act
        var result = new BarcodeAnalysisResult
        {
            SampleId = sampleId,
            IsAnalyzed = true,
            Reason = NoreadReason.BlurryOrOutOfFocus,
            Confidence = confidence,
            IsAboveThreshold = true,
            Message = message
        };

        // Assert
        Assert.Equal(sampleId, result.SampleId);
        Assert.True(result.IsAnalyzed);
        Assert.Equal(NoreadReason.BlurryOrOutOfFocus, result.Reason);
        Assert.Equal(confidence, result.Confidence);
        Assert.True(result.IsAboveThreshold);
        Assert.Equal(message, result.Message);
    }

    [Fact]
    public void Constructor_ShouldCreateInstance_ForFailedAnalysis()
    {
        // Arrange & Act
        var result = new BarcodeAnalysisResult
        {
            SampleId = Guid.NewGuid(),
            IsAnalyzed = true,
            Reason = NoreadReason.ReflectionOrOverexposure,
            Confidence = 0.3m,
            IsAboveThreshold = false,
            Message = "反光严重，无法识别"
        };

        // Assert
        Assert.True(result.IsAnalyzed);
        Assert.Equal(NoreadReason.ReflectionOrOverexposure, result.Reason);
        Assert.Equal(0.3m, result.Confidence);
        Assert.False(result.IsAboveThreshold);
        Assert.NotNull(result.Message);
    }

    [Fact]
    public void RecordEquality_ShouldWork_ForSameValues()
    {
        // Arrange
        var sampleId = Guid.NewGuid();
        var result1 = new BarcodeAnalysisResult
        {
            SampleId = sampleId,
            IsAnalyzed = true,
            IsAboveThreshold = true,
            Confidence = 0.9m
        };

        var result2 = new BarcodeAnalysisResult
        {
            SampleId = sampleId,
            IsAnalyzed = true,
            IsAboveThreshold = true,
            Confidence = 0.9m
        };

        // Act & Assert
        Assert.Equal(result1, result2);
        Assert.True(result1 == result2);
    }

    [Fact]
    public void RecordEquality_ShouldNotWork_ForDifferentValues()
    {
        // Arrange
        var result1 = new BarcodeAnalysisResult
        {
            SampleId = Guid.NewGuid(),
            IsAnalyzed = true,
            IsAboveThreshold = true
        };

        var result2 = new BarcodeAnalysisResult
        {
            SampleId = Guid.NewGuid(),
            IsAnalyzed = true,
            IsAboveThreshold = false
        };

        // Act & Assert
        Assert.NotEqual(result1, result2);
        Assert.False(result1 == result2);
    }
}
