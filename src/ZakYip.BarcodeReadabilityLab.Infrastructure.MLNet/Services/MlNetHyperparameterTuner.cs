namespace ZakYip.BarcodeReadabilityLab.Infrastructure.MLNet.Services;

using Microsoft.Extensions.Logging;
using ZakYip.BarcodeReadabilityLab.Core.Domain.Exceptions;
using ZakYip.BarcodeReadabilityLab.Core.Domain.Models;
using ZakYip.BarcodeReadabilityLab.Core.Enum;
using ZakYip.BarcodeReadabilityLab.Infrastructure.MLNet.Contracts;

/// <summary>
/// 超参数调优器实现
/// </summary>
public sealed class MlNetHyperparameterTuner : IHyperparameterTuner
{
    private readonly ILogger<MlNetHyperparameterTuner> _logger;
    private readonly IImageClassificationTrainer _trainer;

    public MlNetHyperparameterTuner(
        ILogger<MlNetHyperparameterTuner> logger,
        IImageClassificationTrainer trainer)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _trainer = trainer ?? throw new ArgumentNullException(nameof(trainer));
    }

    /// <inheritdoc />
    public async Task<HyperparameterTuningResult> TuneAsync(
        string trainingRootDirectory,
        string outputModelDirectory,
        HyperparameterTuningStrategy strategy,
        GridSearchOptions? gridSearchOptions = null,
        RandomSearchOptions? randomSearchOptions = null,
        IHyperparameterTuningProgressCallback? progressCallback = null,
        CancellationToken cancellationToken = default)
    {
        ValidateParameters(trainingRootDirectory, outputModelDirectory);

        var tuningJobId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation(
            "开始超参数调优 => 任务ID: {TuningJobId}, 策略: {Strategy}, 训练目录: {TrainingRootDirectory}",
            tuningJobId,
            strategy,
            trainingRootDirectory);

        try
        {
            var result = strategy switch
            {
                HyperparameterTuningStrategy.GridSearch => await ExecuteGridSearchAsync(
                    tuningJobId,
                    trainingRootDirectory,
                    outputModelDirectory,
                    gridSearchOptions ?? throw new TrainingException("网格搜索配置不能为空", "MISSING_GRID_SEARCH_OPTIONS"),
                    progressCallback,
                    cancellationToken),

                HyperparameterTuningStrategy.RandomSearch => await ExecuteRandomSearchAsync(
                    tuningJobId,
                    trainingRootDirectory,
                    outputModelDirectory,
                    randomSearchOptions ?? throw new TrainingException("随机搜索配置不能为空", "MISSING_RANDOM_SEARCH_OPTIONS"),
                    progressCallback,
                    cancellationToken),

                HyperparameterTuningStrategy.BayesianOptimization => throw new TrainingException(
                    "贝叶斯优化尚未实现",
                    "BAYESIAN_OPTIMIZATION_NOT_IMPLEMENTED"),

                _ => throw new TrainingException($"未知的调优策略: {strategy}", "UNKNOWN_TUNING_STRATEGY")
            };

            progressCallback?.OnTuningCompleted(result);

            _logger.LogInformation(
                "超参数调优完成 => 任务ID: {TuningJobId}, 总试验: {TotalTrials}, 成功: {SuccessfulTrials}, 最佳准确率: {BestAccuracy:P2}",
                tuningJobId,
                result.TotalTrials,
                result.SuccessfulTrials,
                result.BestTrial?.Metrics.Accuracy ?? 0m);

            return result;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "超参数调优失败 => 任务ID: {TuningJobId}", tuningJobId);
            progressCallback?.OnTuningFailed(tuningJobId, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// 执行网格搜索
    /// </summary>
    private async Task<HyperparameterTuningResult> ExecuteGridSearchAsync(
        Guid tuningJobId,
        string trainingRootDirectory,
        string outputModelDirectory,
        GridSearchOptions options,
        IHyperparameterTuningProgressCallback? progressCallback,
        CancellationToken cancellationToken)
    {
        ValidateGridSearchOptions(options);

        var configurations = GenerateGridSearchConfigurations(options.SearchSpace);
        var totalTrials = configurations.Count;

        _logger.LogInformation(
            "开始网格搜索 => 总配置数: {TotalTrials}, 并行: {EnableParallel}, 最大并行数: {MaxParallel}",
            totalTrials,
            options.EnableParallelSearch,
            options.MaxParallelTrials);

        progressCallback?.OnTuningStarted(tuningJobId, totalTrials);

        var startTime = DateTime.UtcNow;
        var trials = new List<HyperparameterTrialResult>();

        if (options.EnableParallelSearch)
        {
            var parallelOptions = new ParallelOptions
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = options.MaxParallelTrials > 0 
                    ? options.MaxParallelTrials 
                    : Environment.ProcessorCount
            };

            var trialResults = new List<HyperparameterTrialResult>();
            var lockObject = new object();
            var trialNumber = 0;

            await Parallel.ForEachAsync(
                configurations,
                parallelOptions,
                async (config, ct) =>
                {
                    var currentTrialNumber = Interlocked.Increment(ref trialNumber);
                    var result = await ExecuteTrialAsync(
                        config,
                        currentTrialNumber,
                        totalTrials,
                        trainingRootDirectory,
                        outputModelDirectory,
                        progressCallback,
                        ct);

                    lock (lockObject)
                    {
                        trialResults.Add(result);
                    }
                });

            trials = trialResults;
        }
        else
        {
            for (var i = 0; i < configurations.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var config = configurations[i];
                var trialNumber = i + 1;

                var result = await ExecuteTrialAsync(
                    config,
                    trialNumber,
                    totalTrials,
                    trainingRootDirectory,
                    outputModelDirectory,
                    progressCallback,
                    cancellationToken);

                trials.Add(result);

                if (options.EnableEarlyStopping && ShouldStopEarly(trials, options.MetricType))
                {
                    _logger.LogInformation("提前停止网格搜索 => 已找到最佳配置");
                    break;
                }
            }
        }

        var endTime = DateTime.UtcNow;
        var bestTrial = SelectBestTrial(trials, options.MetricType);

        return new HyperparameterTuningResult
        {
            TuningJobId = tuningJobId,
            Strategy = HyperparameterTuningStrategy.GridSearch,
            Trials = trials,
            BestTrial = bestTrial,
            StartTime = startTime,
            EndTime = endTime,
            TrainingRootDirectory = trainingRootDirectory,
            OutputModelDirectory = outputModelDirectory
        };
    }

    /// <summary>
    /// 执行随机搜索
    /// </summary>
    private async Task<HyperparameterTuningResult> ExecuteRandomSearchAsync(
        Guid tuningJobId,
        string trainingRootDirectory,
        string outputModelDirectory,
        RandomSearchOptions options,
        IHyperparameterTuningProgressCallback? progressCallback,
        CancellationToken cancellationToken)
    {
        ValidateRandomSearchOptions(options);

        var configurations = GenerateRandomSearchConfigurations(
            options.SearchSpace,
            options.NumberOfTrials,
            options.RandomSeed);

        var totalTrials = configurations.Count;

        _logger.LogInformation(
            "开始随机搜索 => 总试验数: {TotalTrials}, 并行: {EnableParallel}, 最大并行数: {MaxParallel}",
            totalTrials,
            options.EnableParallelSearch,
            options.MaxParallelTrials);

        progressCallback?.OnTuningStarted(tuningJobId, totalTrials);

        var startTime = DateTime.UtcNow;
        var trials = new List<HyperparameterTrialResult>();

        if (options.EnableParallelSearch)
        {
            var parallelOptions = new ParallelOptions
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = options.MaxParallelTrials > 0 
                    ? options.MaxParallelTrials 
                    : Environment.ProcessorCount
            };

            var trialResults = new List<HyperparameterTrialResult>();
            var lockObject = new object();
            var trialNumber = 0;

            await Parallel.ForEachAsync(
                configurations,
                parallelOptions,
                async (config, ct) =>
                {
                    var currentTrialNumber = Interlocked.Increment(ref trialNumber);
                    var result = await ExecuteTrialAsync(
                        config,
                        currentTrialNumber,
                        totalTrials,
                        trainingRootDirectory,
                        outputModelDirectory,
                        progressCallback,
                        ct);

                    lock (lockObject)
                    {
                        trialResults.Add(result);
                    }
                });

            trials = trialResults;
        }
        else
        {
            for (var i = 0; i < configurations.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var config = configurations[i];
                var trialNumber = i + 1;

                var result = await ExecuteTrialAsync(
                    config,
                    trialNumber,
                    totalTrials,
                    trainingRootDirectory,
                    outputModelDirectory,
                    progressCallback,
                    cancellationToken);

                trials.Add(result);

                if (options.EnableEarlyStopping && ShouldStopEarly(trials, options.MetricType))
                {
                    _logger.LogInformation("提前停止随机搜索 => 已找到最佳配置");
                    break;
                }
            }
        }

        var endTime = DateTime.UtcNow;
        var bestTrial = SelectBestTrial(trials, options.MetricType);

        return new HyperparameterTuningResult
        {
            TuningJobId = tuningJobId,
            Strategy = HyperparameterTuningStrategy.RandomSearch,
            Trials = trials,
            BestTrial = bestTrial,
            StartTime = startTime,
            EndTime = endTime,
            TrainingRootDirectory = trainingRootDirectory,
            OutputModelDirectory = outputModelDirectory
        };
    }

    /// <summary>
    /// 执行单次试验
    /// </summary>
    private async Task<HyperparameterTrialResult> ExecuteTrialAsync(
        HyperparameterConfiguration config,
        int trialNumber,
        int totalTrials,
        string trainingRootDirectory,
        string outputModelDirectory,
        IHyperparameterTuningProgressCallback? progressCallback,
        CancellationToken cancellationToken)
    {
        var trialId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation(
            "开始试验 {TrialNumber}/{TotalTrials} => 学习率: {LearningRate}, Epochs: {Epochs}, BatchSize: {BatchSize}",
            trialNumber,
            totalTrials,
            config.LearningRate,
            config.Epochs,
            config.BatchSize);

        progressCallback?.OnTrialStarted(trialId, trialNumber, totalTrials, config);

        try
        {
            var trialOutputDirectory = Path.Combine(outputModelDirectory, $"trial-{config.Id:N}");
            Directory.CreateDirectory(trialOutputDirectory);

            var trainingResult = await _trainer.TrainAsync(
                trainingRootDirectory,
                trialOutputDirectory,
                config.LearningRate,
                config.Epochs,
                config.BatchSize,
                config.ValidationSplitRatio,
                config.DataAugmentation,
                config.DataBalancing,
                progressCallback: null,
                cancellationToken);

            var endTime = DateTime.UtcNow;

            var result = new HyperparameterTrialResult
            {
                TrialId = trialId,
                Configuration = config,
                Metrics = trainingResult.EvaluationMetrics,
                ModelFilePath = trainingResult.ModelFilePath,
                StartTime = startTime,
                EndTime = endTime,
                IsSuccessful = true,
                ErrorMessage = null
            };

            progressCallback?.OnTrialCompleted(result);

            _logger.LogInformation(
                "试验完成 {TrialNumber}/{TotalTrials} => 准确率: {Accuracy:P2}, 耗时: {Duration:F2}秒",
                trialNumber,
                totalTrials,
                result.Metrics.Accuracy,
                result.TrainingDurationSeconds);

            return result;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            var endTime = DateTime.UtcNow;

            _logger.LogWarning(
                ex,
                "试验失败 {TrialNumber}/{TotalTrials} => 错误: {Error}",
                trialNumber,
                totalTrials,
                ex.Message);

            var result = new HyperparameterTrialResult
            {
                TrialId = trialId,
                Configuration = config,
                Metrics = new ModelEvaluationMetrics
                {
                    Accuracy = 0m,
                    MacroPrecision = 0m,
                    MacroRecall = 0m,
                    MacroF1Score = 0m,
                    MicroPrecision = 0m,
                    MicroRecall = 0m,
                    MicroF1Score = 0m,
                    LogLoss = decimal.MaxValue,
                    ConfusionMatrixJson = "{}",
                    PerClassMetricsJson = "[]"
                },
                ModelFilePath = string.Empty,
                StartTime = startTime,
                EndTime = endTime,
                IsSuccessful = false,
                ErrorMessage = ex.Message
            };

            progressCallback?.OnTrialCompleted(result);

            return result;
        }
    }

    /// <summary>
    /// 生成网格搜索的所有配置组合
    /// </summary>
    private List<HyperparameterConfiguration> GenerateGridSearchConfigurations(HyperparameterSpace space)
    {
        var configurations = new List<HyperparameterConfiguration>();

        var validationSplitRatios = space.ValidationSplitRatios ?? new[] { 0.2m };
        var augmentationOptions = space.DataAugmentationOptionsSet ?? new[] { new DataAugmentationOptions() };
        var balancingOptions = space.DataBalancingOptionsSet ?? new[] { new DataBalancingOptions() };

        foreach (var lr in space.LearningRates)
        {
            foreach (var epochs in space.EpochsOptions)
            {
                foreach (var batchSize in space.BatchSizeOptions)
                {
                    foreach (var validationRatio in validationSplitRatios)
                    {
                        foreach (var augmentation in augmentationOptions)
                        {
                            foreach (var balancing in balancingOptions)
                            {
                                configurations.Add(new HyperparameterConfiguration
                                {
                                    Id = Guid.NewGuid(),
                                    LearningRate = lr,
                                    Epochs = epochs,
                                    BatchSize = batchSize,
                                    ValidationSplitRatio = validationRatio,
                                    DataAugmentation = augmentation,
                                    DataBalancing = balancing
                                });
                            }
                        }
                    }
                }
            }
        }

        return configurations;
    }

    /// <summary>
    /// 生成随机搜索的配置
    /// </summary>
    private List<HyperparameterConfiguration> GenerateRandomSearchConfigurations(
        HyperparameterSpace space,
        int numberOfTrials,
        int randomSeed)
    {
        var random = new Random(randomSeed);
        var configurations = new List<HyperparameterConfiguration>();

        var validationSplitRatios = space.ValidationSplitRatios ?? new[] { 0.2m };
        var augmentationOptions = space.DataAugmentationOptionsSet ?? new[] { new DataAugmentationOptions() };
        var balancingOptions = space.DataBalancingOptionsSet ?? new[] { new DataBalancingOptions() };

        for (var i = 0; i < numberOfTrials; i++)
        {
            var lr = space.LearningRates[random.Next(space.LearningRates.Length)];
            var epochs = space.EpochsOptions[random.Next(space.EpochsOptions.Length)];
            var batchSize = space.BatchSizeOptions[random.Next(space.BatchSizeOptions.Length)];
            var validationRatio = validationSplitRatios[random.Next(validationSplitRatios.Length)];
            var augmentation = augmentationOptions[random.Next(augmentationOptions.Length)];
            var balancing = balancingOptions[random.Next(balancingOptions.Length)];

            configurations.Add(new HyperparameterConfiguration
            {
                Id = Guid.NewGuid(),
                LearningRate = lr,
                Epochs = epochs,
                BatchSize = batchSize,
                ValidationSplitRatio = validationRatio,
                DataAugmentation = augmentation,
                DataBalancing = balancing
            });
        }

        return configurations;
    }

    /// <summary>
    /// 选择最佳试验结果
    /// </summary>
    private HyperparameterTrialResult? SelectBestTrial(
        List<HyperparameterTrialResult> trials,
        EvaluationMetricType metricType)
    {
        var successfulTrials = trials.Where(t => t.IsSuccessful).ToList();

        if (successfulTrials.Count == 0)
            return null;

        return metricType switch
        {
            EvaluationMetricType.Accuracy => successfulTrials.OrderByDescending(t => t.Metrics.Accuracy).First(),
            EvaluationMetricType.MacroF1Score => successfulTrials.OrderByDescending(t => t.Metrics.MacroF1Score).First(),
            EvaluationMetricType.MicroF1Score => successfulTrials.OrderByDescending(t => t.Metrics.MicroF1Score).First(),
            EvaluationMetricType.LogLoss => successfulTrials.OrderBy(t => t.Metrics.LogLoss).First(),
            _ => successfulTrials.OrderByDescending(t => t.Metrics.Accuracy).First()
        };
    }

    /// <summary>
    /// 判断是否应该提前停止
    /// </summary>
    private bool ShouldStopEarly(List<HyperparameterTrialResult> trials, EvaluationMetricType metricType)
    {
        // 简单实现：如果连续 3 次试验没有改进，则停止
        if (trials.Count < 4)
            return false;

        var recentTrials = trials.TakeLast(4).ToList();
        var successfulTrials = recentTrials.Where(t => t.IsSuccessful).ToList();

        if (successfulTrials.Count < 4)
            return false;

        var best = SelectBestTrial(successfulTrials.Take(1).ToList(), metricType);
        if (best is null)
            return false;

        var bestMetricValue = GetMetricValue(best, metricType);

        foreach (var trial in successfulTrials.Skip(1))
        {
            var metricValue = GetMetricValue(trial, metricType);
            
            if (metricType == EvaluationMetricType.LogLoss)
            {
                if (metricValue < bestMetricValue)
                    return false;
            }
            else
            {
                if (metricValue > bestMetricValue)
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 获取指定指标的值
    /// </summary>
    private decimal GetMetricValue(HyperparameterTrialResult trial, EvaluationMetricType metricType)
    {
        return metricType switch
        {
            EvaluationMetricType.Accuracy => trial.Metrics.Accuracy,
            EvaluationMetricType.MacroF1Score => trial.Metrics.MacroF1Score,
            EvaluationMetricType.MicroF1Score => trial.Metrics.MicroF1Score,
            EvaluationMetricType.LogLoss => trial.Metrics.LogLoss ?? decimal.MaxValue,
            _ => trial.Metrics.Accuracy
        };
    }

    /// <summary>
    /// 验证输入参数
    /// </summary>
    private void ValidateParameters(string trainingRootDirectory, string outputModelDirectory)
    {
        if (string.IsNullOrWhiteSpace(trainingRootDirectory))
            throw new TrainingException("训练根目录路径不能为空", "TRAIN_DIR_EMPTY");

        if (string.IsNullOrWhiteSpace(outputModelDirectory))
            throw new TrainingException("输出模型目录路径不能为空", "OUTPUT_DIR_EMPTY");

        if (!Directory.Exists(trainingRootDirectory))
            throw new TrainingException($"训练根目录不存在: {trainingRootDirectory}", "TRAIN_DIR_NOT_FOUND");
    }

    /// <summary>
    /// 验证超参数空间
    /// </summary>
    private static void ValidateHyperparameterSpace(HyperparameterSpace space)
    {
        if (space.LearningRates.Length == 0)
            throw new TrainingException("学习率候选值列表不能为空", "EMPTY_LEARNING_RATES");

        if (space.EpochsOptions.Length == 0)
            throw new TrainingException("Epoch 候选值列表不能为空", "EMPTY_EPOCHS");

        if (space.BatchSizeOptions.Length == 0)
            throw new TrainingException("批大小候选值列表不能为空", "EMPTY_BATCH_SIZES");

        foreach (var lr in space.LearningRates)
        {
            if (lr <= 0m || lr > 1m)
                throw new TrainingException($"学习率必须在 0 到 1 之间（不含 0）: {lr}", "INVALID_LEARNING_RATE");
        }

        foreach (var epoch in space.EpochsOptions)
        {
            if (epoch < 1 || epoch > 500)
                throw new TrainingException($"Epoch 数必须在 1 到 500 之间: {epoch}", "INVALID_EPOCHS");
        }

        foreach (var batchSize in space.BatchSizeOptions)
        {
            if (batchSize < 1 || batchSize > 512)
                throw new TrainingException($"批大小必须在 1 到 512 之间: {batchSize}", "INVALID_BATCH_SIZE");
        }

        if (space.ValidationSplitRatios is not null)
        {
            foreach (var ratio in space.ValidationSplitRatios)
            {
                if (ratio < 0m || ratio > 1m)
                    throw new TrainingException($"验证集分割比例必须在 0 到 1 之间: {ratio}", "INVALID_VALIDATION_SPLIT");
            }
        }
    }

    /// <summary>
    /// 验证网格搜索配置
    /// </summary>
    private static void ValidateGridSearchOptions(GridSearchOptions options)
    {
        ValidateHyperparameterSpace(options.SearchSpace);

        if (options.MaxParallelTrials < 0)
            throw new TrainingException("最大并行任务数不能为负数", "INVALID_MAX_PARALLEL_TRIALS");
    }

    /// <summary>
    /// 验证随机搜索配置
    /// </summary>
    private static void ValidateRandomSearchOptions(RandomSearchOptions options)
    {
        ValidateHyperparameterSpace(options.SearchSpace);

        if (options.NumberOfTrials <= 0)
            throw new TrainingException("试验次数必须大于 0", "INVALID_NUMBER_OF_TRIALS");

        if (options.MaxParallelTrials < 0)
            throw new TrainingException("最大并行任务数不能为负数", "INVALID_MAX_PARALLEL_TRIALS");
    }
}
