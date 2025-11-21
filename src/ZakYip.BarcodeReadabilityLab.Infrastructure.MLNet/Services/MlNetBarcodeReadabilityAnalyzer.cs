namespace ZakYip.BarcodeReadabilityLab.Infrastructure.MLNet.Services;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.ML;
using ZakYip.BarcodeReadabilityLab.Core.Domain.Contracts;
using ZakYip.BarcodeReadabilityLab.Core.Domain.Exceptions;
using ZakYip.BarcodeReadabilityLab.Core.Domain.Models;
using ZakYip.BarcodeReadabilityLab.Infrastructure.MLNet.Models;

/// <summary>
/// 基于 ML.NET 的条码可读性分析器
/// </summary>
public sealed class MlNetBarcodeReadabilityAnalyzer : IBarcodeReadabilityAnalyzer, IDisposable
{
    private readonly ILogger<MlNetBarcodeReadabilityAnalyzer> _logger;
    private readonly IOptionsMonitor<BarcodeMlModelOptions> _optionsMonitor;
    private readonly MLContext _mlContext;
    private PredictionEngine<MlNetImageInput, MlNetPredictionOutput>? _predictionEngine;
    private string? _currentModelPath;
    private readonly object _lock = new();
    private bool _isDisposed;

    public MlNetBarcodeReadabilityAnalyzer(
        ILogger<MlNetBarcodeReadabilityAnalyzer> logger,
        IOptionsMonitor<BarcodeMlModelOptions> optionsMonitor)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
        _mlContext = new MLContext(seed: 0);

        // 监听配置变更
        _optionsMonitor.OnChange(OnOptionsChanged);

        // 初始化模型
        InitializeModel();
    }

    /// <inheritdoc />
    public ValueTask<BarcodeAnalysisResult> AnalyzeAsync(
        BarcodeSample sample,
        CancellationToken cancellationToken = default)
    {
        if (sample is null)
            throw new ArgumentNullException(nameof(sample));

        try
        {
            _logger.LogInformation("开始分析条码样本 => SampleId: {SampleId}, FilePath: {FilePath}",
                sample.SampleId, sample.FilePath);

            ValidateImageFile(sample.FilePath);
            EnsureModelLoaded();

            var result = PerformPrediction(sample);

            _logger.LogInformation(
                "条码样本分析完成 => SampleId: {SampleId}, 已分析: {IsAnalyzed}, 原因: {Reason}, 置信度: {Confidence:P2}",
                sample.SampleId, result.IsAnalyzed, result.Reason, result.Confidence);

            return ValueTask.FromResult(result);
        }
        catch (AnalysisException)
        {
            // 重新抛出分析异常
            throw;
        }
        catch (Exception ex) when (ex is not ArgumentNullException)
        {
            _logger.LogError(ex, "分析条码样本时发生未预期异常 => SampleId: {SampleId}, FilePath: {FilePath}, 错误类型: {ExceptionType}",
                sample.SampleId, sample.FilePath, ex.GetType().Name);

            return ValueTask.FromResult(new BarcodeAnalysisResult
            {
                SampleId = sample.SampleId,
                IsAnalyzed = false,
                IsAboveThreshold = false,
                Message = $"分析失败：{ex.Message}"
            });
        }
    }

    /// <summary>
    /// 验证图片文件
    /// </summary>
    private void ValidateImageFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new AnalysisException("图片文件路径不能为空", "IMAGE_PATH_EMPTY");

        if (!File.Exists(filePath))
            throw new AnalysisException($"图片文件不存在：{filePath}", "IMAGE_FILE_NOT_FOUND");
    }

    /// <summary>
    /// 确保模型已加载（如果可能）
    /// </summary>
    /// <remarks>
    /// 此方法尝试加载模型（如果尚未加载）。
    /// 如果加载失败（例如模型文件不存在），方法不会抛出异常，
    /// 而是记录警告日志。调用者应检查预测引擎是否可用。
    /// </remarks>
    private void EnsureModelLoaded()
    {
        if (_predictionEngine is null)
        {
            lock (_lock)
            {
                if (_predictionEngine is null)
                {
                    InitializeModel();
                }
            }
        }
    }

    /// <summary>
    /// 初始化模型
    /// </summary>
    private void InitializeModel()
    {
        var options = _optionsMonitor.CurrentValue;
        var modelPath = options.CurrentModelPath;

        if (string.IsNullOrWhiteSpace(modelPath))
        {
            _logger.LogWarning("模型路径未配置，分析功能将不可用。请配置模型路径或训练新模型后再使用分析功能");
            return;
        }

        if (!File.Exists(modelPath))
        {
            _logger.LogWarning("模型文件不存在 => 模型路径: {ModelPath}。分析功能将不可用，请先训练模型或导入已有模型", modelPath);
            return;
        }

        lock (_lock)
        {
            try
            {
                _logger.LogInformation("开始加载 ML.NET 模型 => 模型路径: {ModelPath}", modelPath);

                var loadedModel = _mlContext.Model.Load(modelPath, out var modelInputSchema);
                _predictionEngine = _mlContext.Model.CreatePredictionEngine<MlNetImageInput, MlNetPredictionOutput>(loadedModel);
                _currentModelPath = modelPath;

                _logger.LogInformation("ML.NET 模型加载成功 => 模型路径: {ModelPath}", modelPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载 ML.NET 模型失败 => 模型路径: {ModelPath}, 错误类型: {ExceptionType}, 错误详情: {ErrorMessage}", 
                    modelPath, ex.GetType().Name, ex.Message);
                _logger.LogWarning("模型加载失败，分析功能将不可用。请检查模型文件是否损坏或重新训练模型。模型路径: {ModelPath}", modelPath);
            }
        }
    }

    /// <summary>
    /// 执行预测
    /// </summary>
    private BarcodeAnalysisResult PerformPrediction(BarcodeSample sample)
    {
        if (_predictionEngine is null)
        {
            _logger.LogWarning("预测引擎未初始化，无法执行分析 => SampleId: {SampleId}。请确保模型文件存在或先训练模型", sample.SampleId);
            return new BarcodeAnalysisResult
            {
                SampleId = sample.SampleId,
                IsAnalyzed = false,
                IsAboveThreshold = false,
                Message = "模型未加载，分析功能不可用。请先训练模型或确保模型文件存在"
            };
        }

        var input = new MlNetImageInput { ImagePath = sample.FilePath };
        var prediction = _predictionEngine.Predict(input);

        var (reason, isSuccess) = MapLabelToNoreadReason(prediction.PredictedLabel);

        if (!isSuccess)
        {
            _logger.LogWarning(
                "无法将预测标签映射为 NoreadReason => SampleId: {SampleId}, 标签: {Label}",
                sample.SampleId, prediction.PredictedLabel);

            return new BarcodeAnalysisResult
            {
                SampleId = sample.SampleId,
                IsAnalyzed = false,
                IsAboveThreshold = false,
                Message = $"无法识别的标签：{prediction.PredictedLabel}"
            };
        }

        var maxScore = prediction.Score.Length > 0 ? prediction.Score.Max() : 0f;
        var confidence = Convert.ToDecimal(maxScore);

        return new BarcodeAnalysisResult
        {
            SampleId = sample.SampleId,
            IsAnalyzed = true,
            Reason = reason,
            Confidence = confidence,
            IsAboveThreshold = false
        };
    }

    /// <summary>
    /// 将预测标签映射为 NoreadReason
    /// </summary>
    private (NoreadReason? reason, bool isSuccess) MapLabelToNoreadReason(string label)
    {
        if (string.IsNullOrWhiteSpace(label))
            return (null, false);

        return MlNetPredictionMapper.MapLabelToNoreadReason(label);
    }

    /// <summary>
    /// 配置变更处理
    /// </summary>
    private void OnOptionsChanged(BarcodeMlModelOptions options)
    {
        if (_currentModelPath == options.CurrentModelPath)
            return;

        _logger.LogInformation("检测到模型配置变更，旧路径: {OldPath}, 新路径: {NewPath}",
            _currentModelPath, options.CurrentModelPath);

        try
        {
            DisposeCurrentEngine();
            InitializeModel();
            _logger.LogInformation("模型重新加载完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "重新加载模型失败");
        }
    }

    /// <summary>
    /// 释放当前预测引擎
    /// </summary>
    private void DisposeCurrentEngine()
    {
        lock (_lock)
        {
            _predictionEngine?.Dispose();
            _predictionEngine = null;
            _currentModelPath = null;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed)
            return;

        DisposeCurrentEngine();
        _isDisposed = true;

        _logger.LogInformation("MlNetBarcodeReadabilityAnalyzer 已释放");
    }
}
