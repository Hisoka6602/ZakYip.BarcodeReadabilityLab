using ZakYip.BarcodeReadabilityLab.Core.Domain.Models;
using ZakYip.BarcodeReadabilityLab.Infrastructure.MLNet.Contracts;
using ZakYip.BarcodeReadabilityLab.Infrastructure.MLNet.Models;

namespace ZakYip.BarcodeReadabilityLab.IntegrationTests;

internal sealed class FakeImageClassificationTrainer : IImageClassificationTrainer
{
    public async Task<TrainingResult> TrainAsync(
        string trainingRootDirectory,
        string outputModelDirectory,
        decimal learningRate,
        int epochs,
        int batchSize,
        decimal? validationSplitRatio = null,
        DataAugmentationOptions? dataAugmentationOptions = null,
        DataBalancingOptions? dataBalancingOptions = null,
        ITrainingProgressCallback? progressCallback = null,
        CancellationToken cancellationToken = default)
    {
        progressCallback?.ReportProgress(0.15m, "synthetic-training-started");
        await Task.Delay(TimeSpan.FromMilliseconds(50), cancellationToken);
        progressCallback?.ReportProgress(0.55m, "synthetic-training-running");

        Directory.CreateDirectory(outputModelDirectory);
        var modelFilePath = Path.Combine(outputModelDirectory, $"fake-model-{Guid.NewGuid():N}.zip");
        await File.WriteAllTextAsync(modelFilePath, "fake-model", cancellationToken);

        progressCallback?.ReportProgress(1.0m, "synthetic-training-completed");

        var metrics = new ModelEvaluationMetrics
        {
            Accuracy = 0.95m,
            MacroPrecision = 0.94m,
            MacroRecall = 0.93m,
            MacroF1Score = 0.92m,
            MicroPrecision = 0.95m,
            MicroRecall = 0.95m,
            MicroF1Score = 0.95m,
            LogLoss = 0.05m,
            ConfusionMatrixJson = "{\"matrix\":[[5,0],[0,5]]}",
            PerClassMetricsJson = "{\"readable\":{}}",
            DataAugmentationImpactJson = "{\"summary\":\"synthetic\"}"
        };

        return new TrainingResult
        {
            ModelFilePath = modelFilePath,
            EvaluationMetrics = metrics
        };
    }
}
