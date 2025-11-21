using ZakYip.BarcodeReadabilityLab.Core.Domain.Models;
using ZakYip.BarcodeReadabilityLab.Core.Enum;

namespace ZakYip.BarcodeReadabilityLab.Core.Tests;

public class DataAugmentationOptionsTests
{
    [Fact]
    public void Defaults_ShouldMatchDomainExpectations()
    {
        var options = new DataAugmentationOptions();

        Assert.False(options.Enable);
        Assert.Equal(1, options.AugmentedImagesPerSample);
        Assert.Equal(1, options.EvaluationAugmentedImagesPerSample);
        Assert.True(options.EnableRotation);
        Assert.NotNull(options.RotationAngles);
        Assert.Contains(15f, options.RotationAngles);
        Assert.Equal(0.7, options.RotationProbability);
        Assert.True(options.EnableHorizontalFlip);
        Assert.Equal(0.5, options.HorizontalFlipProbability);
        Assert.False(options.EnableVerticalFlip);
        Assert.Equal(0.2, options.VerticalFlipProbability);
        Assert.True(options.EnableBrightnessAdjustment);
        Assert.Equal(0.6, options.BrightnessProbability);
        Assert.Equal(0.85f, options.BrightnessLower);
        Assert.Equal(1.15f, options.BrightnessUpper);
        Assert.True(options.ShuffleAugmentedData);
        Assert.Equal(42, options.RandomSeed);
    }
}
