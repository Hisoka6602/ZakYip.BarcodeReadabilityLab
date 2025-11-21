using ZakYip.BarcodeReadabilityLab.Core.Domain.Models;
using ZakYip.BarcodeReadabilityLab.Infrastructure.MLNet.Contracts;
using ZakYip.BarcodeReadabilityLab.Infrastructure.MLNet.Models;

namespace ZakYip.BarcodeReadabilityLab.IntegrationTests.Simulation;

/// <summary>
/// 仿真训练器，用于集成测试，不执行真实的 ML.NET 训练
/// </summary>
internal sealed class FakeImageClassificationTrainer : IImageClassificationTrainer
{
    private readonly int _simulationDelayMs;

    public FakeImageClassificationTrainer(int simulationDelayMs = 200)
    {
        _simulationDelayMs = simulationDelayMs;
    }

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
        // 验证训练目录
        if (!Directory.Exists(trainingRootDirectory))
        {
            throw new DirectoryNotFoundException($"训练目录不存在: {trainingRootDirectory}");
        }

        // 扫描训练集目录，统计图片数量
        var classDirectories = Directory.GetDirectories(trainingRootDirectory);
        var totalImages = 0;
        var classCounts = new Dictionary<string, int>();

        foreach (var classDir in classDirectories)
        {
            var className = Path.GetFileName(classDir);
            var imageFiles = Directory.GetFiles(classDir, "*.*")
                .Where(f => f.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                           f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                           f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            
            classCounts[className] = imageFiles.Length;
            totalImages += imageFiles.Length;
        }

        // 模拟训练进度
        progressCallback?.ReportProgress(0.0m, "开始初始化训练环境");
        await Task.Delay(_simulationDelayMs / 4, cancellationToken);

        progressCallback?.ReportProgress(0.15m, $"已扫描 {classDirectories.Length} 个类别，共 {totalImages} 张图片");
        await Task.Delay(_simulationDelayMs / 4, cancellationToken);

        progressCallback?.ReportProgress(0.35m, "正在执行数据预处理");
        await Task.Delay(_simulationDelayMs / 4, cancellationToken);

        progressCallback?.ReportProgress(0.55m, "正在训练模型");
        await Task.Delay(_simulationDelayMs / 4, cancellationToken);

        progressCallback?.ReportProgress(0.75m, "正在评估模型性能");
        await Task.Delay(_simulationDelayMs / 4, cancellationToken);

        progressCallback?.ReportProgress(0.95m, "正在保存模型文件");
        await Task.Delay(_simulationDelayMs / 4, cancellationToken);

        // 创建输出目录并生成仿真模型文件
        Directory.CreateDirectory(outputModelDirectory);
        var modelFileName = $"simulation-model-{DateTime.UtcNow:yyyyMMddHHmmss}.zip";
        var modelFilePath = Path.Combine(outputModelDirectory, modelFileName);
        
        var modelContent = $@"Simulation Model File
Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}
Training Directory: {trainingRootDirectory}
Total Images: {totalImages}
Classes: {string.Join(", ", classCounts.Keys)}
Class Distribution: {string.Join(", ", classCounts.Select(kvp => $"{kvp.Key}={kvp.Value}"))}
Hyperparameters:
  - Learning Rate: {learningRate}
  - Epochs: {epochs}
  - Batch Size: {batchSize}
  - Validation Split: {validationSplitRatio?.ToString() ?? "None"}
";
        await File.WriteAllTextAsync(modelFilePath, modelContent, cancellationToken);

        progressCallback?.ReportProgress(1.0m, "训练完成");

        // 构造仿真的评估指标
        var metrics = new ModelEvaluationMetrics
        {
            Accuracy = 0.92m,
            MacroPrecision = 0.91m,
            MacroRecall = 0.90m,
            MacroF1Score = 0.905m,
            MicroPrecision = 0.92m,
            MicroRecall = 0.92m,
            MicroF1Score = 0.92m,
            LogLoss = 0.08m,
            ConfusionMatrixJson = GenerateConfusionMatrix(classDirectories.Length),
            PerClassMetricsJson = GeneratePerClassMetrics(classCounts),
            DataAugmentationImpactJson = "{\"summary\":\"simulation\",\"augmentedSamples\":0}"
        };

        return new TrainingResult
        {
            ModelFilePath = modelFilePath,
            EvaluationMetrics = metrics
        };
    }

    public async Task<TrainingResult> TrainWithTransferLearningAsync(
        string trainingRootDirectory,
        string outputModelDirectory,
        decimal learningRate,
        int epochs,
        int batchSize,
        decimal? validationSplitRatio = null,
        TransferLearningOptions? transferLearningOptions = null,
        DataAugmentationOptions? dataAugmentationOptions = null,
        DataBalancingOptions? dataBalancingOptions = null,
        ITrainingProgressCallback? progressCallback = null,
        CancellationToken cancellationToken = default)
    {
        // 对于测试目的，简单地委托给常规训练方法
        return await TrainAsync(
            trainingRootDirectory,
            outputModelDirectory,
            learningRate,
            epochs,
            batchSize,
            validationSplitRatio,
            dataAugmentationOptions,
            dataBalancingOptions,
            progressCallback,
            cancellationToken);
    }

    private static string GenerateConfusionMatrix(int classCount)
    {
        // 生成一个简单的混淆矩阵 JSON (使用锯齿数组而非多维数组)
        var matrix = new int[classCount][];
        for (var i = 0; i < classCount; i++)
        {
            matrix[i] = new int[classCount];
            for (var j = 0; j < classCount; j++)
            {
                matrix[i][j] = i == j ? 10 : 1; // 对角线上的值较大
            }
        }

        return System.Text.Json.JsonSerializer.Serialize(new { matrix });
    }

    private static string GeneratePerClassMetrics(Dictionary<string, int> classCounts)
    {
        // 生成每个类别的指标
        var perClassMetrics = classCounts.Keys.ToDictionary(
            className => className,
            className => new
            {
                precision = 0.90m + (decimal)(new Random().NextDouble() * 0.05),
                recall = 0.88m + (decimal)(new Random().NextDouble() * 0.07),
                f1Score = 0.89m + (decimal)(new Random().NextDouble() * 0.06),
                support = classCounts[className]
            });

        return System.Text.Json.JsonSerializer.Serialize(perClassMetrics);
    }
}
