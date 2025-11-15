using ZakYip.BarcodeReadabilityLab.Core.Domain.Models;

namespace ZakYip.BarcodeReadabilityLab.Core.Tests;

public class DataBalancingOptionsTests
{
    [Fact]
    public void Defaults_ShouldPreferNoBalancing()
    {
        var options = new DataBalancingOptions();

        Assert.Equal(DataBalancingStrategy.None, options.Strategy);
        Assert.Null(options.TargetSampleCountPerClass);
        Assert.True(options.ShuffleAfterBalancing);
        Assert.Equal(42, options.RandomSeed);
    }
}
