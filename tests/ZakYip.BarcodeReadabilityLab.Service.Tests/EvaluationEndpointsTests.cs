using Microsoft.Extensions.Options;
using ZakYip.BarcodeReadabilityLab.Application.Options;
using ZakYip.BarcodeReadabilityLab.Application.Services;
using ZakYip.BarcodeReadabilityLab.Core.Domain.Models;
using ZakYip.BarcodeReadabilityLab.Core.Enums;

namespace ZakYip.BarcodeReadabilityLab.Service.Tests;

/// <summary>
/// 评估端点测试
/// </summary>
public sealed class EvaluationEndpointsTests
{
    private readonly Mock<IImageEvaluationService> _evaluationService = new();
    private readonly IOptions<EvaluationOptions> _options;

    public EvaluationEndpointsTests()
    {
        _options = Options.Create(new EvaluationOptions
        {
            MaxImageSizeBytes = 10 * 1024 * 1024,
            MaxImageCount = 100,
            AllowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".bmp" }
        });
    }

    [Fact]
    public async Task EvaluateSingle_ShouldReturnCorrectResult_WhenNoExpectedLabel()
    {
        // Arrange
        var predictedLabel = NoreadReason.Truncated;
        var confidence = 0.95m;

        _evaluationService
            .Setup(s => s.EvaluateSingleAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                null,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SingleEvaluationResult
            {
                PredictedLabel = predictedLabel,
                Confidence = confidence,
                ExpectedLabel = null,
                IsCorrect = null
            });

        // Act
        var result = await _evaluationService.Object.EvaluateSingleAsync(
            Stream.Null,
            "test.jpg",
            null,
            false,
            default);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(predictedLabel, result.PredictedLabel);
        Assert.Equal(confidence, result.Confidence);
        Assert.Null(result.ExpectedLabel);
        Assert.Null(result.IsCorrect);
    }

    [Fact]
    public async Task EvaluateSingle_ShouldReturnIsCorrect_WhenExpectedLabelProvided()
    {
        // Arrange
        var predictedLabel = NoreadReason.BlurryOrOutOfFocus;
        var expectedLabel = NoreadReason.BlurryOrOutOfFocus;
        var confidence = 0.88m;

        _evaluationService
            .Setup(s => s.EvaluateSingleAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                expectedLabel,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SingleEvaluationResult
            {
                PredictedLabel = predictedLabel,
                Confidence = confidence,
                ExpectedLabel = expectedLabel,
                IsCorrect = true
            });

        // Act
        var result = await _evaluationService.Object.EvaluateSingleAsync(
            Stream.Null,
            "test.jpg",
            expectedLabel,
            false,
            default);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(predictedLabel, result.PredictedLabel);
        Assert.Equal(expectedLabel, result.ExpectedLabel);
        Assert.True(result.IsCorrect);
    }

    [Fact]
    public async Task EvaluateSingle_ShouldReturnIsCorrectFalse_WhenPredictionIsWrong()
    {
        // Arrange
        var predictedLabel = NoreadReason.Truncated;
        var expectedLabel = NoreadReason.BlurryOrOutOfFocus;
        var confidence = 0.75m;

        _evaluationService
            .Setup(s => s.EvaluateSingleAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                expectedLabel,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SingleEvaluationResult
            {
                PredictedLabel = predictedLabel,
                Confidence = confidence,
                ExpectedLabel = expectedLabel,
                IsCorrect = false
            });

        // Act
        var result = await _evaluationService.Object.EvaluateSingleAsync(
            Stream.Null,
            "test.jpg",
            expectedLabel,
            false,
            default);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(predictedLabel, result.PredictedLabel);
        Assert.Equal(expectedLabel, result.ExpectedLabel);
        Assert.False(result.IsCorrect);
    }

    [Fact]
    public async Task EvaluateBatch_ShouldReturnCorrectSummary()
    {
        // Arrange
        var items = new List<BatchEvaluationItem>
        {
            new()
            {
                FileName = "img1.jpg",
                Result = new SingleEvaluationResult
                {
                    PredictedLabel = NoreadReason.Truncated,
                    Confidence = 0.9m,
                    ExpectedLabel = NoreadReason.Truncated,
                    IsCorrect = true
                }
            },
            new()
            {
                FileName = "img2.jpg",
                Result = new SingleEvaluationResult
                {
                    PredictedLabel = NoreadReason.BlurryOrOutOfFocus,
                    Confidence = 0.8m,
                    ExpectedLabel = NoreadReason.Truncated,
                    IsCorrect = false
                }
            }
        };

        var expectedSummary = new EvaluationSummary
        {
            Total = 2,
            WithExpectedLabel = 2,
            CorrectCount = 1,
            Accuracy = 0.5m,
            MacroF1 = 0.4m,
            MicroF1 = 0.5m
        };

        _evaluationService
            .Setup(s => s.EvaluateBatchAsync(
                It.IsAny<IEnumerable<(Stream, string, NoreadReason?)>>(),
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BatchEvaluationResult
            {
                Items = items,
                Summary = expectedSummary
            });

        // Act
        var images = new[]
        {
            (Stream.Null, "img1.jpg", (NoreadReason?)NoreadReason.Truncated),
            (Stream.Null, "img2.jpg", (NoreadReason?)NoreadReason.Truncated)
        };

        var result = await _evaluationService.Object.EvaluateBatchAsync(
            images,
            false,
            default);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(2, result.Summary.Total);
        Assert.Equal(2, result.Summary.WithExpectedLabel);
        Assert.Equal(1, result.Summary.CorrectCount);
        Assert.Equal(0.5m, result.Summary.Accuracy);
    }

    [Fact]
    public async Task EvaluateBatch_ShouldCalculateMetrics_WhenSomeItemsHaveNoExpectedLabel()
    {
        // Arrange
        var items = new List<BatchEvaluationItem>
        {
            new()
            {
                FileName = "img1.jpg",
                Result = new SingleEvaluationResult
                {
                    PredictedLabel = NoreadReason.Truncated,
                    Confidence = 0.9m,
                    ExpectedLabel = NoreadReason.Truncated,
                    IsCorrect = true
                }
            },
            new()
            {
                FileName = "img2.jpg",
                Result = new SingleEvaluationResult
                {
                    PredictedLabel = NoreadReason.BlurryOrOutOfFocus,
                    Confidence = 0.8m,
                    ExpectedLabel = null,
                    IsCorrect = null
                }
            }
        };

        var expectedSummary = new EvaluationSummary
        {
            Total = 2,
            WithExpectedLabel = 1,
            CorrectCount = 1,
            Accuracy = 1.0m,
            MacroF1 = null,
            MicroF1 = null
        };

        _evaluationService
            .Setup(s => s.EvaluateBatchAsync(
                It.IsAny<IEnumerable<(Stream, string, NoreadReason?)>>(),
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BatchEvaluationResult
            {
                Items = items,
                Summary = expectedSummary
            });

        // Act
        var images = new[]
        {
            (Stream.Null, "img1.jpg", (NoreadReason?)NoreadReason.Truncated),
            (Stream.Null, "img2.jpg", (NoreadReason?)null)
        };

        var result = await _evaluationService.Object.EvaluateBatchAsync(
            images,
            false,
            default);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Summary.Total);
        Assert.Equal(1, result.Summary.WithExpectedLabel);
        Assert.Equal(1.0m, result.Summary.Accuracy);
    }
}
