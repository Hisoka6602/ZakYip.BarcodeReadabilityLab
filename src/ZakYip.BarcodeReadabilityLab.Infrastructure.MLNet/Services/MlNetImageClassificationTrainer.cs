namespace ZakYip.BarcodeReadabilityLab.Infrastructure.MLNet.Services;

using Microsoft.Extensions.Logging;
using Microsoft.ML;
using ZakYip.BarcodeReadabilityLab.Core.Domain.Exceptions;
using ZakYip.BarcodeReadabilityLab.Infrastructure.MLNet.Contracts;

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
    public async Task<string> TrainAsync(
        string trainingRootDirectory,
        string outputModelDirectory,
        decimal? validationSplitRatio = null,
        CancellationToken cancellationToken = default)
    {
        ValidateParameters(trainingRootDirectory, outputModelDirectory);

        _logger.LogInformation("开始训练任务 => 训练根目录: {TrainingRootDirectory}, 输出目录: {OutputModelDirectory}, 验证比例: {ValidationSplitRatio}",
            trainingRootDirectory, outputModelDirectory, validationSplitRatio ?? 0.0m);

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            // 扫描训练数据
            var trainingData = ScanTrainingData(trainingRootDirectory);
            _logger.LogInformation("扫描到训练样本 => 总数: {Count}, 标签分布详情见后续日志", trainingData.Count);

            if (trainingData.Count == 0)
            {
                throw new TrainingException("训练根目录中没有找到任何训练样本", "NO_TRAINING_DATA");
            }

            cancellationToken.ThrowIfCancellationRequested();

            // 加载数据到 ML.NET
            var dataView = _mlContext.Data.LoadFromEnumerable(trainingData);

            cancellationToken.ThrowIfCancellationRequested();

            // 构建训练管道
            var pipeline = BuildTrainingPipeline();

            _logger.LogInformation("开始训练模型...");

            // 执行训练
            var trainedModel = await Task.Run(() => pipeline.Fit(dataView), cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            // 保存模型
            var modelFilePath = SaveModel(trainedModel, dataView.Schema, outputModelDirectory);

            _logger.LogInformation("模型训练完成 => 模型路径: {ModelFilePath}, 训练样本数: {SampleCount}", 
                modelFilePath, trainingData.Count);

            return modelFilePath;
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
    private IEstimator<ITransformer> BuildTrainingPipeline()
    {
        var pipeline = _mlContext.Transforms.Conversion.MapValueToKey("Label")
            .Append(_mlContext.Transforms.LoadRawImageBytes(
                outputColumnName: "ImageBytes",
                imageFolder: null,
                inputColumnName: nameof(TrainingImageData.ImagePath)))
            .Append(_mlContext.Transforms.CopyColumns(
                outputColumnName: "Features",
                inputColumnName: "ImageBytes"))
            .Append(_mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy(
                labelColumnName: "Label",
                featureColumnName: "Features"))
            .Append(_mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

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
}
