using ZakYip.BarcodeReadabilityLab.Core.Domain.Models;
using ZakYip.BarcodeReadabilityLab.Core.Enums;

namespace ZakYip.BarcodeReadabilityLab.Core.Tests;

public class TrainingJobTests
{
    [Fact]
    public void CreatingJob_ShouldPopulateDefaults()
    {
        var job = new TrainingJob
        {
            JobId = Guid.NewGuid(),
            TrainingRootDirectory = "/tmp/train",
            OutputModelDirectory = "/tmp/output",
            LearningRate = 0.1m,
            Epochs = 10,
            BatchSize = 8,
            Status = TrainingJobState.Queued,
            StartTime = DateTimeOffset.UtcNow
        };

        Assert.NotNull(job.DataAugmentation);
        Assert.NotNull(job.DataBalancing);
        Assert.Equal(TrainingJobState.Queued, job.Status);
        Assert.Equal(0m, job.Progress);
        Assert.Null(job.CompletedTime);
        Assert.Null(job.ErrorMessage);
        Assert.Null(job.EvaluationMetrics);
    }
}
