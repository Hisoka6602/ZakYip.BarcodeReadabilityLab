namespace ZakYip.BarcodeReadabilityLab.Infrastructure.MLNet.Services;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.ML;
using ZakYip.BarcodeReadabilityLab.Core.Domain.Contracts;
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
            _logger.LogInformation("开始分析条码样本，SampleId: {SampleId}, FilePath: {FilePath}",
                sample.SampleId, sample.FilePath);

            ValidateImageFile(sample.FilePath);
            EnsureModelLoaded();

            var result = PerformPrediction(sample);

            _logger.LogInformation(
                "条码样本分析完成，SampleId: {SampleId}, IsAnalyzed: {IsAnalyzed}, Reason: {Reason}, Confidence: {Confidence}",
                sample.SampleId, result.IsAnalyzed, result.Reason, result.Confidence);

            return ValueTask.FromResult(result);
        }
        catch (Exception ex) when (ex is not ArgumentNullException)
        {
            _logger.LogError(ex, "分析条码样本时发生异常，SampleId: {SampleId}, FilePath: {FilePath}",
                sample.SampleId, sample.FilePath);

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
            throw new ArgumentException("图片文件路径不能为空", nameof(filePath));

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"图片文件不存在：{filePath}", filePath);
    }

    /// <summary>
    /// 确保模型已加载
    /// </summary>
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
            _logger.LogWarning("模型路径未配置，分析功能将不可用");
            return;
        }

        if (!File.Exists(modelPath))
        {
            _logger.LogError("模型文件不存在：{ModelPath}", modelPath);
            throw new FileNotFoundException($"模型文件不存在：{modelPath}", modelPath);
        }

        lock (_lock)
        {
            try
            {
                _logger.LogInformation("开始加载 ML.NET 模型：{ModelPath}", modelPath);

                var loadedModel = _mlContext.Model.Load(modelPath, out var modelInputSchema);
                _predictionEngine = _mlContext.Model.CreatePredictionEngine<MlNetImageInput, MlNetPredictionOutput>(loadedModel);
                _currentModelPath = modelPath;

                _logger.LogInformation("ML.NET 模型加载成功：{ModelPath}", modelPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载 ML.NET 模型失败：{ModelPath}", modelPath);
                throw new InvalidOperationException($"加载模型失败：{modelPath}", ex);
            }
        }
    }

    /// <summary>
    /// 执行预测
    /// </summary>
    private BarcodeAnalysisResult PerformPrediction(BarcodeSample sample)
    {
        if (_predictionEngine is null)
            throw new InvalidOperationException("预测引擎未初始化");

        var input = new MlNetImageInput { ImagePath = sample.FilePath };
        var prediction = _predictionEngine.Predict(input);

        var (reason, isSuccess) = MapLabelToNoreadReason(prediction.PredictedLabel);

        if (!isSuccess)
        {
            _logger.LogWarning(
                "无法将预测标签映射为 NoreadReason，SampleId: {SampleId}, Label: {Label}",
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

        // 尝试按枚举名称匹配
        if (Enum.TryParse<NoreadReason>(label, ignoreCase: true, out var reasonByName))
            return (reasonByName, true);

        // 尝试按数值匹配
        if (int.TryParse(label, out var numericValue) && Enum.IsDefined(typeof(NoreadReason), numericValue))
            return ((NoreadReason)numericValue, true);

        return (null, false);
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
