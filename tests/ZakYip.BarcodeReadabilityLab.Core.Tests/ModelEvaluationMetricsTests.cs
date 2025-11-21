using ZakYip.BarcodeReadabilityLab.Core.Domain.Models;
using ZakYip.BarcodeReadabilityLab.Core.Enums;

namespace ZakYip.BarcodeReadabilityLab.Core.Tests;

/// <summary>
/// 模型评估指标测试
/// </summary>
public sealed class ModelEvaluationMetricsTests
{
    [Fact]
    public void Constructor_ShouldCreateInstance_WithRequiredProperties()
    {
        // Arrange
        var confusionMatrixJson = "[[10, 2], [1, 12]]";

        // Act
        var metrics = new ModelEvaluationMetrics
        {
            Accuracy = 0.88m,
            MacroPrecision = 0.85m,
            MacroRecall = 0.87m,
            MacroF1Score = 0.86m,
            MicroPrecision = 0.88m,
            MicroRecall = 0.88m,
            MicroF1Score = 0.88m,
            ConfusionMatrixJson = confusionMatrixJson
        };

        // Assert
        Assert.Equal(0.88m, metrics.Accuracy);
        Assert.Equal(0.85m, metrics.MacroPrecision);
        Assert.Equal(0.87m, metrics.MacroRecall);
        Assert.Equal(0.86m, metrics.MacroF1Score);
        Assert.Equal(0.88m, metrics.MicroPrecision);
        Assert.Equal(0.88m, metrics.MicroRecall);
        Assert.Equal(0.88m, metrics.MicroF1Score);
        Assert.Equal(confusionMatrixJson, metrics.ConfusionMatrixJson);
        Assert.Null(metrics.LogLoss);
        Assert.Null(metrics.PerClassMetricsJson);
        Assert.Null(metrics.DataAugmentationImpactJson);
    }

    [Fact]
    public void Constructor_ShouldCreateInstance_WithAllProperties()
    {
        // Arrange
        var confusionMatrixJson = "[[10, 2], [1, 12]]";
        var perClassMetricsJson = "{\"class1\": {\"precision\": 0.9}, \"class2\": {\"precision\": 0.8}}";
        var dataAugmentationImpactJson = "{\"improvement\": 0.05}";

        // Act
        var metrics = new ModelEvaluationMetrics
        {
            Accuracy = 0.88m,
            MacroPrecision = 0.85m,
            MacroRecall = 0.87m,
            MacroF1Score = 0.86m,
            MicroPrecision = 0.88m,
            MicroRecall = 0.88m,
            MicroF1Score = 0.88m,
            LogLoss = 0.25m,
            ConfusionMatrixJson = confusionMatrixJson,
            PerClassMetricsJson = perClassMetricsJson,
            DataAugmentationImpactJson = dataAugmentationImpactJson
        };

        // Assert
        Assert.Equal(0.88m, metrics.Accuracy);
        Assert.Equal(0.25m, metrics.LogLoss);
        Assert.Equal(confusionMatrixJson, metrics.ConfusionMatrixJson);
        Assert.Equal(perClassMetricsJson, metrics.PerClassMetricsJson);
        Assert.Equal(dataAugmentationImpactJson, metrics.DataAugmentationImpactJson);
    }

    [Fact]
    public void RecordEquality_ShouldWork_ForSameValues()
    {
        // Arrange
        var confusionMatrixJson = "[[10, 2], [1, 12]]";

        var metrics1 = new ModelEvaluationMetrics
        {
            Accuracy = 0.88m,
            MacroPrecision = 0.85m,
            MacroRecall = 0.87m,
            MacroF1Score = 0.86m,
            MicroPrecision = 0.88m,
            MicroRecall = 0.88m,
            MicroF1Score = 0.88m,
            ConfusionMatrixJson = confusionMatrixJson
        };

        var metrics2 = new ModelEvaluationMetrics
        {
            Accuracy = 0.88m,
            MacroPrecision = 0.85m,
            MacroRecall = 0.87m,
            MacroF1Score = 0.86m,
            MicroPrecision = 0.88m,
            MicroRecall = 0.88m,
            MicroF1Score = 0.88m,
            ConfusionMatrixJson = confusionMatrixJson
        };

        // Act & Assert
        Assert.Equal(metrics1, metrics2);
    }

    [Fact]
    public void RecordEquality_ShouldNotWork_ForDifferentAccuracy()
    {
        // Arrange
        var confusionMatrixJson = "[[10, 2], [1, 12]]";

        var metrics1 = new ModelEvaluationMetrics
        {
            Accuracy = 0.88m,
            MacroPrecision = 0.85m,
            MacroRecall = 0.87m,
            MacroF1Score = 0.86m,
            MicroPrecision = 0.88m,
            MicroRecall = 0.88m,
            MicroF1Score = 0.88m,
            ConfusionMatrixJson = confusionMatrixJson
        };

        var metrics2 = new ModelEvaluationMetrics
        {
            Accuracy = 0.90m,
            MacroPrecision = 0.85m,
            MacroRecall = 0.87m,
            MacroF1Score = 0.86m,
            MicroPrecision = 0.88m,
            MicroRecall = 0.88m,
            MicroF1Score = 0.88m,
            ConfusionMatrixJson = confusionMatrixJson
        };

        // Act & Assert
        Assert.NotEqual(metrics1, metrics2);
    }

    [Fact]
    public void Constructor_ShouldAcceptHighAccuracyValues()
    {
        // Arrange & Act
        var metrics = new ModelEvaluationMetrics
        {
            Accuracy = 0.99m,
            MacroPrecision = 0.98m,
            MacroRecall = 0.99m,
            MacroF1Score = 0.985m,
            MicroPrecision = 0.99m,
            MicroRecall = 0.99m,
            MicroF1Score = 0.99m,
            LogLoss = 0.01m,
            ConfusionMatrixJson = "[[100, 1], [0, 99]]"
        };

        // Assert
        Assert.True(metrics.Accuracy >= 0.9m);
        Assert.True(metrics.MacroF1Score >= 0.9m);
        Assert.True(metrics.MicroF1Score >= 0.9m);
    }
}
