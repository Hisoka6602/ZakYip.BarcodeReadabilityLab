namespace ZakYip.BarcodeReadabilityLab.Infrastructure.MLNet.Services;

using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Vision;
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
        ITrainingProgressCallback? progressCallback = null,
        CancellationToken cancellationToken = default)
    {
        ValidateParameters(trainingRootDirectory, outputModelDirectory, learningRate, epochs, batchSize);

        _logger.LogInformation(
            "开始训练任务 => 训练根目录: {TrainingRootDirectory}, 输出目录: {OutputModelDirectory}, 验证比例: {ValidationSplitRatio}, 学习率: {LearningRate}, Epochs: {Epochs}, BatchSize: {BatchSize}",
            trainingRootDirectory,
            outputModelDirectory,
            validationSplitRatio ?? 0.0m,
            learningRate,
            epochs,
            batchSize);

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            // 报告进度：开始扫描数据
            progressCallback?.ReportProgress(0.05m, "开始扫描训练数据");

            // 扫描训练数据
            var trainingData = ScanTrainingData(trainingRootDirectory);
            _logger.LogInformation("扫描到训练样本 => 总数: {Count}, 标签分布详情见后续日志", trainingData.Count);

            if (trainingData.Count == 0)
            {
                throw new TrainingException("训练根目录中没有找到任何训练样本", "NO_TRAINING_DATA");
            }

            cancellationToken.ThrowIfCancellationRequested();

            // 报告进度：加载数据
            progressCallback?.ReportProgress(0.15m, "加载训练数据到内存");

            // 加载数据到 ML.NET
            var dataView = _mlContext.Data.LoadFromEnumerable(trainingData);

            // 分割数据为训练集和测试集
            var splitRatio = validationSplitRatio ?? 0.2m;
            var trainTestSplit = _mlContext.Data.TrainTestSplit(dataView, testFraction: (double)splitRatio);

            _logger.LogInformation("数据集分割 => 训练集比例: {TrainRatio:P0}, 测试集比例: {TestRatio:P0}", 
                1.0m - splitRatio, splitRatio);

            cancellationToken.ThrowIfCancellationRequested();

            // 报告进度：构建训练管道
            progressCallback?.ReportProgress(0.25m, "构建训练管道");

            // 构建训练管道
            var trainerOptions = BuildTrainerOptions(learningRate, epochs, batchSize, outputModelDirectory, progressCallback);
            var pipeline = BuildTrainingPipeline(trainerOptions);

            _logger.LogInformation("开始训练模型...");

            // 报告进度：开始训练
            progressCallback?.ReportProgress(0.30m, "开始训练模型");

            // 执行训练（使用训练集）
            var trainedModel = await Task.Run(() => pipeline.Fit(trainTestSplit.TrainSet), cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            // 报告进度：评估模型
            progressCallback?.ReportProgress(0.80m, "评估模型性能");

            // 在测试集上评估模型
            var evaluationMetrics = EvaluateModel(trainedModel, trainTestSplit.TestSet, trainingData);

            cancellationToken.ThrowIfCancellationRequested();

            // 报告进度：训练完成，保存模型
            progressCallback?.ReportProgress(0.90m, "训练完成，保存模型");

            // 保存模型
            var modelFilePath = SaveModel(trainedModel, trainTestSplit.TrainSet.Schema, outputModelDirectory);

            // 报告进度：完成
            progressCallback?.ReportProgress(1.0m, "训练任务完成");

            _logger.LogInformation("模型训练完成 => 模型路径: {ModelFilePath}, 训练样本数: {SampleCount}, 准确率: {Accuracy:P2}", 
                modelFilePath, trainingData.Count, evaluationMetrics.Accuracy);

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
            // 重新抛出自定义训练异常
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "训练任务失败 => 错误类型: {ExceptionType}, 目录: {TrainingRootDirectory}", 
                ex.GetType().Name, trainingRootDirectory);
            throw new TrainingException($"训练任务失败: {ex.Message}", "TRAINING_FAILED", ex);
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
        List<TrainingImageData> allTrainingData)
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

        // 获取所有唯一标签
        var uniqueLabels = allTrainingData
            .Select(d => d.Label)
            .Distinct()
            .OrderBy(l => l)
            .ToList();

        // 计算混淆矩阵
        var confusionMatrix = metrics.ConfusionMatrix;
        var confusionMatrixData = BuildConfusionMatrixData(confusionMatrix, uniqueLabels);
        var confusionMatrixJson = JsonSerializer.Serialize(confusionMatrixData);

        // 计算每个类别的指标
        var perClassMetrics = CalculatePerClassMetrics(confusionMatrix, uniqueLabels);
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
}
