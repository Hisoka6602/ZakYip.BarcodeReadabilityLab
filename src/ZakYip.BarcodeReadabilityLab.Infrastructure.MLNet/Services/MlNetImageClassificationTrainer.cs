namespace ZakYip.BarcodeReadabilityLab.Infrastructure.MLNet.Services;

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Vision;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using ZakYip.BarcodeReadabilityLab.Core.Domain.Exceptions;
using ZakYip.BarcodeReadabilityLab.Core.Domain.Models;
using ZakYip.BarcodeReadabilityLab.Infrastructure.MLNet.Contracts;
using ZakYip.BarcodeReadabilityLab.Infrastructure.MLNet.Models;

/// <summary>
/// 训练用图像数据模型
/// </summary>
internal sealed class TrainingImageData
{
    /// <summary>
    /// 图片文件路径
    /// </summary>
    public string ImagePath { get; init; } = string.Empty;

    /// <summary>
    /// 分类标签
    /// </summary>
    public string Label { get; init; } = string.Empty;
}

/// <summary>
/// 基于 ML.NET 的图像分类训练器
/// </summary>
public sealed class MlNetImageClassificationTrainer : IImageClassificationTrainer
{
    private readonly ILogger<MlNetImageClassificationTrainer> _logger;
    private readonly MLContext _mlContext;
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public MlNetImageClassificationTrainer(ILogger<MlNetImageClassificationTrainer> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _mlContext = new MLContext(seed: 0);
    }

    /// <inheritdoc />
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
        ValidateParameters(trainingRootDirectory, outputModelDirectory, learningRate, epochs, batchSize);

        var augmentationOptions = dataAugmentationOptions ?? new DataAugmentationOptions();
        var balancingOptions = dataBalancingOptions ?? new DataBalancingOptions();
        var cleanupDirectories = new List<string>();

        _logger.LogInformation(
            "开始训练任务 => 训练根目录: {TrainingRootDirectory}, 输出目录: {OutputModelDirectory}, 验证比例: {ValidationSplitRatio}, 学习率: {LearningRate}, Epochs: {Epochs}, BatchSize: {BatchSize}, 数据增强: {AugmentationEnabled}, 数据平衡: {BalancingStrategy}",
            trainingRootDirectory,
            outputModelDirectory,
            validationSplitRatio ?? 0.0m,
            learningRate,
            epochs,
            batchSize,
            augmentationOptions.Enable,
            balancingOptions.Strategy);

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            progressCallback?.ReportProgress(0.05m, "开始扫描训练数据");

            var trainingData = ScanTrainingData(trainingRootDirectory);
            _logger.LogInformation("扫描到训练样本 => 总数: {Count}", trainingData.Count);

            if (trainingData.Count == 0)
            {
                throw new TrainingException("训练根目录中没有找到任何训练样本", "NO_TRAINING_DATA");
            }

            var originalDistribution = CountByLabel(trainingData);
            _logger.LogInformation("原始标签分布 => {Distribution}", FormatDistribution(originalDistribution));

            cancellationToken.ThrowIfCancellationRequested();

            progressCallback?.ReportProgress(0.12m, "应用数据平衡策略");
            var balancedTrainingData = ApplyDataBalancing(trainingData, balancingOptions, out var balancedDistribution);
            _logger.LogInformation(
                "数据平衡完成 => 策略: {Strategy}, 样本数: {Count}",
                balancingOptions.Strategy,
                balancedTrainingData.Count);
            _logger.LogInformation("平衡后标签分布 => {Distribution}", FormatDistribution(balancedDistribution));

            cancellationToken.ThrowIfCancellationRequested();

            var augmentationWorkspace = Path.Combine(outputModelDirectory, "ml-workspace", $"augment-train-{Guid.NewGuid():N}");
            var augmentationResult = ApplyDataAugmentation(balancedTrainingData, augmentationOptions, augmentationWorkspace, cancellationToken);

            if (!string.IsNullOrWhiteSpace(augmentationResult.WorkspaceDirectory))
            {
                cleanupDirectories.Add(augmentationResult.WorkspaceDirectory);
            }

            var finalTrainingData = balancedTrainingData.Concat(augmentationResult.Samples).ToList();

            if (augmentationOptions.ShuffleAugmentedData && finalTrainingData.Count > 1)
            {
                var shuffleRandom = new Random(augmentationOptions.RandomSeed);
                finalTrainingData = finalTrainingData.OrderBy(_ => shuffleRandom.Next()).ToList();
            }

            var finalDistribution = CountByLabel(finalTrainingData);

            var datasetSummary = new DataAugmentationDatasetSummary
            {
                OriginalSamples = trainingData.Count,
                BalancedSamples = balancedTrainingData.Count,
                AugmentedSamples = augmentationResult.Samples.Count,
                TotalSamples = finalTrainingData.Count,
                OriginalDistribution = new Dictionary<string, int>(originalDistribution, StringComparer.OrdinalIgnoreCase),
                BalancedDistribution = new Dictionary<string, int>(balancedDistribution, StringComparer.OrdinalIgnoreCase),
                FinalDistribution = new Dictionary<string, int>(finalDistribution, StringComparer.OrdinalIgnoreCase),
                OperationUsage = new Dictionary<string, int>(augmentationResult.OperationUsage, StringComparer.OrdinalIgnoreCase)
            };

            if (augmentationResult.Samples.Count > 0)
            {
                _logger.LogInformation("数据增强完成 => 新增样本数: {Count}", augmentationResult.Samples.Count);
                _logger.LogInformation("增强后标签分布 => {Distribution}", FormatDistribution(finalDistribution));

                if (augmentationResult.OperationUsage.Count > 0)
                {
                    var usageDetails = string.Join(", ", augmentationResult.OperationUsage.Select(pair => $"{pair.Key}:{pair.Value}"));
                    _logger.LogInformation("增强操作使用统计 => {Usage}", usageDetails);
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            progressCallback?.ReportProgress(0.18m, $"准备训练数据，共 {finalTrainingData.Count} 条样本");

            var dataView = _mlContext.Data.LoadFromEnumerable(finalTrainingData);

            var splitRatio = validationSplitRatio ?? 0.2m;
            var trainTestSplit = _mlContext.Data.TrainTestSplit(dataView, testFraction: (double)splitRatio);

            _logger.LogInformation("数据集分割 => 训练集比例: {TrainRatio:P0}, 测试集比例: {TestRatio:P0}", 1.0m - splitRatio, splitRatio);

            cancellationToken.ThrowIfCancellationRequested();

            progressCallback?.ReportProgress(0.25m, "构建训练管道");

            var trainerOptions = BuildTrainerOptions(learningRate, epochs, batchSize, outputModelDirectory, progressCallback);
            var pipeline = BuildTrainingPipeline(trainerOptions);

            var uniqueLabels = finalTrainingData
                .Select(d => d.Label)
                .Distinct()
                .OrderBy(label => label)
                .ToList();

            _logger.LogInformation("开始训练模型...");

            progressCallback?.ReportProgress(0.30m, "开始训练模型");

            var trainedModel = await Task.Run(() => pipeline.Fit(trainTestSplit.TrainSet), cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            progressCallback?.ReportProgress(0.80m, "评估模型性能");

            var evaluationMetrics = EvaluateModel(trainedModel, trainTestSplit.TestSet, uniqueLabels);

            cancellationToken.ThrowIfCancellationRequested();

            var testSamples = _mlContext.Data.CreateEnumerable<TrainingImageData>(trainTestSplit.TestSet, reuseRowObject: false).ToList();

            var augmentationImpact = BuildAugmentationImpact(
                augmentationOptions,
                balancingOptions,
                datasetSummary,
                evaluationMetrics,
                uniqueLabels,
                testSamples,
                trainedModel,
                outputModelDirectory,
                cleanupDirectories,
                cancellationToken);

            if (augmentationImpact is not null)
            {
                var impactJson = JsonSerializer.Serialize(augmentationImpact, _jsonSerializerOptions);
                evaluationMetrics = evaluationMetrics with { DataAugmentationImpactJson = impactJson };
                _logger.LogInformation("数据增强影响评估 => {Report}", impactJson);
            }

            progressCallback?.ReportProgress(0.90m, "训练完成，保存模型");

            var modelFilePath = SaveModel(trainedModel, trainTestSplit.TrainSet.Schema, outputModelDirectory);

            progressCallback?.ReportProgress(1.0m, "训练任务完成");

            _logger.LogInformation(
                "模型训练完成 => 模型路径: {ModelFilePath}, 最终样本数: {SampleCount}, 准确率: {Accuracy:P2}",
                modelFilePath,
                finalTrainingData.Count,
                evaluationMetrics.Accuracy);

            return new TrainingResult
            {
                ModelFilePath = modelFilePath,
                EvaluationMetrics = evaluationMetrics
            };
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("训练任务被取消 => 目录: {TrainingRootDirectory}", trainingRootDirectory);
            throw;
        }
        catch (TrainingException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "训练任务失败 => 错误类型: {ExceptionType}, 目录: {TrainingRootDirectory}", ex.GetType().Name, trainingRootDirectory);
            throw new TrainingException($"训练任务失败: {ex.Message}", "TRAINING_FAILED", ex);
        }
        finally
        {
            CleanupTemporaryDirectories(cleanupDirectories);
        }
    }

    /// <summary>
    /// 验证输入参数
    /// </summary>
    private void ValidateParameters(
        string trainingRootDirectory,
        string outputModelDirectory,
        decimal learningRate,
        int epochs,
        int batchSize)
    {
        if (string.IsNullOrWhiteSpace(trainingRootDirectory))
            throw new TrainingException("训练根目录路径不能为空", "TRAIN_DIR_EMPTY");

        if (string.IsNullOrWhiteSpace(outputModelDirectory))
            throw new TrainingException("输出模型目录路径不能为空", "OUTPUT_DIR_EMPTY");

        if (!Directory.Exists(trainingRootDirectory))
            throw new TrainingException($"训练根目录不存在: {trainingRootDirectory}", "TRAIN_DIR_NOT_FOUND");

        if (learningRate <= 0m || learningRate > 1m)
            throw new TrainingException("学习率必须在 0 到 1 之间（不含 0）", "INVALID_LEARNING_RATE");

        if (epochs < 1 || epochs > 500)
            throw new TrainingException("Epoch 数必须在 1 到 500 之间", "INVALID_EPOCHS");

        if (batchSize < 1 || batchSize > 512)
            throw new TrainingException("Batch Size 必须在 1 到 512 之间", "INVALID_BATCH_SIZE");
    }

    /// <summary>
    /// 扫描训练数据
    /// </summary>
    private List<TrainingImageData> ScanTrainingData(string trainingRootDirectory)
    {
        var trainingData = new List<TrainingImageData>();
        var supportedExtensions = new[] { ".jpg", ".jpeg", ".png", ".bmp" };

        var labelDirectories = Directory.GetDirectories(trainingRootDirectory);

        foreach (var labelDir in labelDirectories)
        {
            var label = Path.GetFileName(labelDir);

            if (string.IsNullOrWhiteSpace(label))
                continue;

            var imageFiles = Directory.GetFiles(labelDir, "*.*", SearchOption.TopDirectoryOnly)
                .Where(f => supportedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .ToList();

            _logger.LogInformation("标签分布 => 标签: {Label}, 样本数: {Count}", label, imageFiles.Count);

            foreach (var imageFile in imageFiles)
            {
                trainingData.Add(new TrainingImageData
                {
                    ImagePath = imageFile,
                    Label = label
                });
            }
        }

        return trainingData;
    }

    /// <summary>
    /// 构建训练管道
    /// </summary>
    private ImageClassificationTrainer.Options BuildTrainerOptions(
        decimal learningRate,
        int epochs,
        int batchSize,
        string outputModelDirectory,
        ITrainingProgressCallback? progressCallback)
    {
        var options = new ImageClassificationTrainer.Options
        {
            FeatureColumnName = "Image",
            LabelColumnName = "Label",
            Arch = ImageClassificationTrainer.Architecture.Resnet50,
            Epoch = epochs,
            BatchSize = batchSize,
            LearningRate = (float)learningRate,
            ReuseTrainSetBottleneckCachedValues = true,
            ReuseValidationSetBottleneckCachedValues = true,
            WorkspacePath = Path.Combine(outputModelDirectory, "ml-workspace")
        };

        options.MetricsCallback = metrics =>
        {
            if (metrics is null)
                return;

            var accuracy = Math.Clamp(metrics.Accuracy, 0f, 1f);
            var progress = 0.30m + (decimal)accuracy * 0.5m;
            progressCallback?.ReportProgress(progress, $"Epoch {metrics.Epoch} 准确率 {accuracy:P2}, 损失 {metrics.CrossEntropy:F4}");
            _logger.LogInformation(
                "训练中 => Epoch: {Epoch}, 准确率: {Accuracy:P2}, 损失: {CrossEntropy:F4}",
                metrics.Epoch,
                accuracy,
                metrics.CrossEntropy);
        };

        Directory.CreateDirectory(options.WorkspacePath!);

        return options;
    }

    private IEstimator<ITransformer> BuildTrainingPipeline(ImageClassificationTrainer.Options trainerOptions)
    {
        var pipeline = _mlContext.Transforms.Conversion.MapValueToKey(
                outputColumnName: "Label",
                inputColumnName: nameof(TrainingImageData.Label))
            .Append(_mlContext.Transforms.LoadRawImageBytes(
                outputColumnName: "Image",
                imageFolder: null,
                inputColumnName: nameof(TrainingImageData.ImagePath)))
            .Append(_mlContext.MulticlassClassification.Trainers.ImageClassification(trainerOptions))
            .Append(_mlContext.Transforms.Conversion.MapKeyToValue(
                outputColumnName: "PredictedLabel",
                inputColumnName: "PredictedLabel"));

        return pipeline;
    }

    /// <summary>
    /// 保存模型到文件
    /// </summary>
    private string SaveModel(ITransformer model, DataViewSchema schema, string outputModelDirectory)
    {
        // 确保输出目录存在
        Directory.CreateDirectory(outputModelDirectory);

        // 生成带时间戳的模型文件名
        var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var modelFileName = $"noread-classifier-{timestamp}.zip";
        var modelFilePath = Path.Combine(outputModelDirectory, modelFileName);

        // 保存模型
        _mlContext.Model.Save(model, schema, modelFilePath);

        _logger.LogInformation("模型已保存到: {ModelFilePath}", modelFilePath);

        // 同时保存一个 "current" 模型副本，用于热切换
        var currentModelPath = Path.Combine(outputModelDirectory, "noread-classifier-current.zip");

        try
        {
            // 如果存在旧的 current 模型，先删除
            if (File.Exists(currentModelPath))
            {
                File.Delete(currentModelPath);
            }

            // 复制新模型为 current
            File.Copy(modelFilePath, currentModelPath);

            _logger.LogInformation("当前在线模型已更新: {CurrentModelPath}", currentModelPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "更新当前在线模型失败，但训练模型已保存");
        }

        return modelFilePath;
    }

    /// <summary>
    /// 评估模型性能
    /// </summary>
    private ModelEvaluationMetrics EvaluateModel(
        ITransformer model,
        IDataView testSet,
        IReadOnlyList<string> uniqueLabels)
    {
        // 在测试集上进行预测
        var predictions = model.Transform(testSet);

        // 计算多分类评估指标
        var metrics = _mlContext.MulticlassClassification.Evaluate(predictions, labelColumnName: "Label");

        _logger.LogInformation("模型评估指标 => 准确率: {Accuracy:P2}, 宏平均F1: {MacroF1:P2}, 微平均F1: {MicroF1:P2}, 对数损失: {LogLoss:F4}",
            metrics.MacroAccuracy,
            metrics.MacroAccuracy > 0 ? 2 * metrics.MacroAccuracy / (1 + metrics.MacroAccuracy) : 0,
            metrics.MicroAccuracy,
            metrics.LogLoss);

        var labelList = uniqueLabels
            .Distinct()
            .OrderBy(label => label)
            .ToList();

        // 计算混淆矩阵
        var confusionMatrix = metrics.ConfusionMatrix;
        var confusionMatrixData = BuildConfusionMatrixData(confusionMatrix, labelList);
        var confusionMatrixJson = JsonSerializer.Serialize(confusionMatrixData);

        // 计算每个类别的指标
        var perClassMetrics = CalculatePerClassMetrics(confusionMatrix, labelList);
        var perClassMetricsJson = JsonSerializer.Serialize(perClassMetrics);

        // 计算宏平均和微平均指标
        var (macroPrecision, macroRecall, macroF1) = CalculateMacroAverageMetrics(perClassMetrics);
        var (microPrecision, microRecall, microF1) = CalculateMicroAverageMetrics(confusionMatrix);

        _logger.LogInformation("详细评估指标 => 宏平均: 精确率={MacroPrecision:P2}, 召回率={MacroRecall:P2}, F1={MacroF1:P2} | 微平均: 精确率={MicroPrecision:P2}, 召回率={MicroRecall:P2}, F1={MicroF1:P2}",
            macroPrecision, macroRecall, macroF1, microPrecision, microRecall, microF1);

        return new ModelEvaluationMetrics
        {
            Accuracy = (decimal)metrics.MacroAccuracy,
            MacroPrecision = macroPrecision,
            MacroRecall = macroRecall,
            MacroF1Score = macroF1,
            MicroPrecision = microPrecision,
            MicroRecall = microRecall,
            MicroF1Score = microF1,
            LogLoss = (decimal)metrics.LogLoss,
            ConfusionMatrixJson = confusionMatrixJson,
            PerClassMetricsJson = perClassMetricsJson
        };
    }

    /// <summary>
    /// 构建混淆矩阵数据结构
    /// </summary>
    private Dictionary<string, object> BuildConfusionMatrixData(
        ConfusionMatrix confusionMatrix,
        List<string> labels)
    {
        var matrix = new List<List<int>>();
        
        for (var i = 0; i < confusionMatrix.NumberOfClasses; i++)
        {
            var row = new List<int>();
            for (var j = 0; j < confusionMatrix.NumberOfClasses; j++)
            {
                row.Add((int)confusionMatrix.Counts[i][j]);
            }
            matrix.Add(row);
        }

        return new Dictionary<string, object>
        {
            ["labels"] = labels,
            ["matrix"] = matrix
        };
    }

    /// <summary>
    /// 计算每个类别的精确率、召回率和 F1 分数
    /// </summary>
    private List<Dictionary<string, object>> CalculatePerClassMetrics(
        ConfusionMatrix confusionMatrix,
        List<string> labels)
    {
        var perClassMetrics = new List<Dictionary<string, object>>();

        for (var i = 0; i < confusionMatrix.NumberOfClasses; i++)
        {
            var truePositives = confusionMatrix.Counts[i][i];
            var falsePositives = 0.0;
            var falseNegatives = 0.0;

            // 计算假正例（该列其他行的和）
            for (var j = 0; j < confusionMatrix.NumberOfClasses; j++)
            {
                if (j != i)
                {
                    falsePositives += confusionMatrix.Counts[j][i];
                }
            }

            // 计算假负例（该行其他列的和）
            for (var j = 0; j < confusionMatrix.NumberOfClasses; j++)
            {
                if (j != i)
                {
                    falseNegatives += confusionMatrix.Counts[i][j];
                }
            }

            var precision = truePositives + falsePositives > 0 
                ? truePositives / (truePositives + falsePositives) 
                : 0.0;
            
            var recall = truePositives + falseNegatives > 0 
                ? truePositives / (truePositives + falseNegatives) 
                : 0.0;
            
            var f1Score = precision + recall > 0 
                ? 2 * precision * recall / (precision + recall) 
                : 0.0;

            perClassMetrics.Add(new Dictionary<string, object>
            {
                ["label"] = labels[i],
                ["precision"] = Math.Round(precision, 4),
                ["recall"] = Math.Round(recall, 4),
                ["f1Score"] = Math.Round(f1Score, 4),
                ["support"] = (int)(truePositives + falseNegatives)
            });
        }

        return perClassMetrics;
    }

    /// <summary>
    /// 计算宏平均指标（每个类别指标的算术平均）
    /// </summary>
    private (decimal precision, decimal recall, decimal f1Score) CalculateMacroAverageMetrics(
        List<Dictionary<string, object>> perClassMetrics)
    {
        if (perClassMetrics.Count == 0)
            return (0m, 0m, 0m);

        var avgPrecision = perClassMetrics.Average(m => Convert.ToDouble(m["precision"]));
        var avgRecall = perClassMetrics.Average(m => Convert.ToDouble(m["recall"]));
        var avgF1 = perClassMetrics.Average(m => Convert.ToDouble(m["f1Score"]));

        return ((decimal)avgPrecision, (decimal)avgRecall, (decimal)avgF1);
    }

    /// <summary>
    /// 计算微平均指标（全局统计）
    /// </summary>
    private (decimal precision, decimal recall, decimal f1Score) CalculateMicroAverageMetrics(
        ConfusionMatrix confusionMatrix)
    {
        var totalTruePositives = 0.0;
        var totalFalsePositives = 0.0;
        var totalFalseNegatives = 0.0;

        for (var i = 0; i < confusionMatrix.NumberOfClasses; i++)
        {
            var truePositives = confusionMatrix.Counts[i][i];
            totalTruePositives += truePositives;

            for (var j = 0; j < confusionMatrix.NumberOfClasses; j++)
            {
                if (j != i)
                {
                    totalFalsePositives += confusionMatrix.Counts[j][i];
                    totalFalseNegatives += confusionMatrix.Counts[i][j];
                }
            }
        }

        var microPrecision = totalTruePositives + totalFalsePositives > 0
            ? totalTruePositives / (totalTruePositives + totalFalsePositives)
            : 0.0;

        var microRecall = totalTruePositives + totalFalseNegatives > 0
            ? totalTruePositives / (totalTruePositives + totalFalseNegatives)
            : 0.0;

        var microF1 = microPrecision + microRecall > 0
            ? 2 * microPrecision * microRecall / (microPrecision + microRecall)
            : 0.0;

        return ((decimal)microPrecision, (decimal)microRecall, (decimal)microF1);
    }

    private List<TrainingImageData> ApplyDataBalancing(
        IReadOnlyList<TrainingImageData> sourceData,
        DataBalancingOptions options,
        out Dictionary<string, int> balancedDistribution)
    {
        if (options.Strategy == DataBalancingStrategy.None || sourceData.Count == 0)
        {
            var originalDistribution = CountByLabel(sourceData);
            balancedDistribution = new Dictionary<string, int>(originalDistribution, StringComparer.OrdinalIgnoreCase);
            return sourceData.ToList();
        }

        var grouped = sourceData
            .GroupBy(sample => sample.Label)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);

        if (grouped.Count == 0)
        {
            balancedDistribution = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            return new List<TrainingImageData>();
        }

        var random = new Random(options.RandomSeed);
        var defaultTarget = options.Strategy == DataBalancingStrategy.OverSample
            ? grouped.Max(pair => pair.Value.Count)
            : grouped.Min(pair => pair.Value.Count);

        var target = options.TargetSampleCountPerClass ?? defaultTarget;
        target = Math.Max(target, 0);

        var balanced = new List<TrainingImageData>();

        foreach (var pair in grouped)
        {
            var samples = pair.Value;

            if (samples.Count == 0)
            {
                continue;
            }

            if (options.Strategy == DataBalancingStrategy.OverSample)
            {
                var desiredCount = Math.Max(target, samples.Count);
                var oversampled = new List<TrainingImageData>(samples);

                while (oversampled.Count < desiredCount)
                {
                    var duplicate = samples[random.Next(samples.Count)];
                    oversampled.Add(duplicate);
                }

                balanced.AddRange(oversampled);
            }
            else
            {
                var desiredCount = Math.Min(target, samples.Count);

                if (desiredCount <= 0)
                {
                    continue;
                }

                var selected = samples
                    .OrderBy(_ => random.Next())
                    .Take(desiredCount);

                balanced.AddRange(selected);
            }
        }

        if (options.ShuffleAfterBalancing && balanced.Count > 1)
        {
            balanced = balanced.OrderBy(_ => random.Next()).ToList();
        }

        balancedDistribution = CountByLabel(balanced);
        return balanced;
    }

    private AugmentationResult ApplyDataAugmentation(
        IReadOnlyCollection<TrainingImageData> sourceData,
        DataAugmentationOptions options,
        string workspaceDirectory,
        CancellationToken cancellationToken,
        int? overrideCopiesPerSample = null,
        int? randomSeedOverride = null)
    {
        var result = new AugmentationResult();

        if (!options.Enable || sourceData.Count == 0)
        {
            return result;
        }

        var copiesPerSample = overrideCopiesPerSample ?? options.AugmentedImagesPerSample;
        if (copiesPerSample <= 0)
        {
            return result;
        }

        Directory.CreateDirectory(workspaceDirectory);
        result.WorkspaceDirectory = workspaceDirectory;

        var randomSeed = randomSeedOverride ?? options.RandomSeed;
        var random = new Random(randomSeed);

        foreach (var sample in sourceData)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(sample.ImagePath) || !File.Exists(sample.ImagePath))
            {
                _logger.LogWarning("数据增强跳过 => 原始文件不存在: {ImagePath}", sample.ImagePath);
                continue;
            }

            try
            {
                using var image = Image.Load(sample.ImagePath);

                for (var index = 0; index < copiesPerSample; index++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        using var mutated = image.Clone(ctx => ApplyAugmentationOperations(ctx, options, random, result.OperationUsage));
                        var fileName = $"{Path.GetFileNameWithoutExtension(sample.ImagePath)}_aug_{index}_{Guid.NewGuid():N}.png";
                        var outputPath = Path.Combine(workspaceDirectory, fileName);
                        mutated.Save(outputPath);

                        result.Samples.Add(new TrainingImageData
                        {
                            ImagePath = outputPath,
                            Label = sample.Label
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "生成增强样本失败 => 文件: {ImagePath}", sample.ImagePath);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "加载原始图像失败，跳过增强 => 文件: {ImagePath}", sample.ImagePath);
            }
        }

        if (options.ShuffleAugmentedData && result.Samples.Count > 1)
        {
            var shuffleRandom = new Random(random.Next());
            result.Samples = result.Samples.OrderBy(_ => shuffleRandom.Next()).ToList();
        }

        return result;
    }

    private void ApplyAugmentationOperations(
        IImageProcessingContext context,
        DataAugmentationOptions options,
        Random random,
        Dictionary<string, int> operationUsage)
    {
        var rotationApplied = false;

        if (options.EnableRotation && options.RotationAngles.Length > 0 && random.NextDouble() <= options.RotationProbability)
        {
            var angleIndex = random.Next(options.RotationAngles.Length);
            var angle = options.RotationAngles[angleIndex];

            if (Math.Abs(angle) > float.Epsilon)
            {
                context.Rotate(angle);
                rotationApplied = true;
            }
        }

        if (rotationApplied)
        {
            IncrementOperationUsage(operationUsage, "rotation");
        }

        var horizontalApplied = false;

        if (options.EnableHorizontalFlip && random.NextDouble() <= options.HorizontalFlipProbability)
        {
            context.Flip(FlipMode.Horizontal);
            horizontalApplied = true;
        }

        if (horizontalApplied)
        {
            IncrementOperationUsage(operationUsage, "horizontalFlip");
        }

        var verticalApplied = false;

        if (options.EnableVerticalFlip && random.NextDouble() <= options.VerticalFlipProbability)
        {
            context.Flip(FlipMode.Vertical);
            verticalApplied = true;
        }

        if (verticalApplied)
        {
            IncrementOperationUsage(operationUsage, "verticalFlip");
        }

        if (options.EnableBrightnessAdjustment && random.NextDouble() <= options.BrightnessProbability)
        {
            var min = Math.Min(options.BrightnessLower, options.BrightnessUpper);
            var max = Math.Max(options.BrightnessLower, options.BrightnessUpper);
            var factor = min + (float)(random.NextDouble() * (max - min));

            context.AdjustBrightness(factor);
            IncrementOperationUsage(operationUsage, "brightness");
        }
    }

    private static void IncrementOperationUsage(Dictionary<string, int> usage, string key)
    {
        if (usage.TryGetValue(key, out var count))
        {
            usage[key] = count + 1;
        }
        else
        {
            usage[key] = 1;
        }
    }

    private DataAugmentationImpact BuildAugmentationImpact(
        DataAugmentationOptions augmentationOptions,
        DataBalancingOptions balancingOptions,
        DataAugmentationDatasetSummary datasetSummary,
        ModelEvaluationMetrics baseMetrics,
        IReadOnlyList<string> uniqueLabels,
        IReadOnlyList<TrainingImageData> testSamples,
        ITransformer trainedModel,
        string outputModelDirectory,
        List<string> cleanupDirectories,
        CancellationToken cancellationToken)
    {
        var isAugmentationApplied = augmentationOptions.Enable && datasetSummary.AugmentedSamples > 0;
        var isBalancingApplied = balancingOptions.Strategy != DataBalancingStrategy.None;

        DataAugmentationEvaluationSummary? evaluationSummary = null;

        if (isAugmentationApplied && testSamples.Count > 0)
        {
            var evaluationWorkspace = Path.Combine(outputModelDirectory, "ml-workspace", $"augment-eval-{Guid.NewGuid():N}");
            var evaluationCopies = Math.Max(1, augmentationOptions.EvaluationAugmentedImagesPerSample);

            var augmentedTestResult = ApplyDataAugmentation(
                testSamples,
                augmentationOptions,
                evaluationWorkspace,
                cancellationToken,
                evaluationCopies,
                augmentationOptions.RandomSeed + 1);

            if (!string.IsNullOrWhiteSpace(augmentedTestResult.WorkspaceDirectory))
            {
                cleanupDirectories.Add(augmentedTestResult.WorkspaceDirectory);
            }

            if (augmentedTestResult.Samples.Count > 0)
            {
                var augmentedDataView = _mlContext.Data.LoadFromEnumerable(augmentedTestResult.Samples);
                var predictions = trainedModel.Transform(augmentedDataView);
                var augmentedMetrics = _mlContext.MulticlassClassification.Evaluate(predictions, labelColumnName: "Label");

                var labelList = uniqueLabels
                    .Distinct()
                    .OrderBy(label => label)
                    .ToList();

                var perClassMetrics = CalculatePerClassMetrics(augmentedMetrics.ConfusionMatrix, labelList);
                var (macroPrecision, macroRecall, macroF1) = CalculateMacroAverageMetrics(perClassMetrics);
                var (microPrecision, microRecall, microF1) = CalculateMicroAverageMetrics(augmentedMetrics.ConfusionMatrix);

                evaluationSummary = new DataAugmentationEvaluationSummary
                {
                    OriginalSampleCount = testSamples.Count,
                    AugmentedSampleCount = augmentedTestResult.Samples.Count,
                    OriginalAccuracy = baseMetrics.Accuracy,
                    AugmentedAccuracy = (decimal)augmentedMetrics.MacroAccuracy,
                    OriginalMacroPrecision = baseMetrics.MacroPrecision,
                    AugmentedMacroPrecision = macroPrecision,
                    OriginalMacroRecall = baseMetrics.MacroRecall,
                    AugmentedMacroRecall = macroRecall,
                    OriginalMacroF1 = baseMetrics.MacroF1Score,
                    AugmentedMacroF1 = macroF1,
                    OriginalMicroPrecision = baseMetrics.MicroPrecision,
                    AugmentedMicroPrecision = microPrecision,
                    OriginalMicroRecall = baseMetrics.MicroRecall,
                    AugmentedMicroRecall = microRecall,
                    OriginalMicroF1 = baseMetrics.MicroF1Score,
                    AugmentedMicroF1 = microF1
                };
            }
        }

        return new DataAugmentationImpact
        {
            IsAugmentationApplied = isAugmentationApplied,
            IsBalancingApplied = isBalancingApplied,
            AugmentationOptions = augmentationOptions,
            BalancingOptions = balancingOptions,
            Dataset = datasetSummary,
            Evaluation = evaluationSummary
        };
    }

    private static Dictionary<string, int> CountByLabel(IEnumerable<TrainingImageData> data)
    {
        var distribution = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var sample in data)
        {
            if (string.IsNullOrWhiteSpace(sample.Label))
            {
                continue;
            }

            if (distribution.TryGetValue(sample.Label, out var count))
            {
                distribution[sample.Label] = count + 1;
            }
            else
            {
                distribution[sample.Label] = 1;
            }
        }

        return distribution;
    }

    private static string FormatDistribution(IReadOnlyDictionary<string, int> distribution)
    {
        if (distribution.Count == 0)
        {
            return "空";
        }

        var parts = distribution
            .OrderBy(pair => pair.Key)
            .Select(pair => $"{pair.Key}:{pair.Value}");

        return string.Join(", ", parts);
    }

    private void CleanupTemporaryDirectories(IEnumerable<string> directories)
    {
        foreach (var directory in directories)
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                continue;
            }

            try
            {
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, recursive: true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "清理临时目录失败 => {Directory}", directory);
            }
        }
    }

    private sealed class AugmentationResult
    {
        public List<TrainingImageData> Samples { get; set; } = new();
        public Dictionary<string, int> OperationUsage { get; } = new(StringComparer.OrdinalIgnoreCase);
        public string? WorkspaceDirectory { get; set; }
    }
}
