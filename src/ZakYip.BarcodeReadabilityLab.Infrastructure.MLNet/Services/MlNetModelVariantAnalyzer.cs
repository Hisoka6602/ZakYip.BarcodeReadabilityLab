namespace ZakYip.BarcodeReadabilityLab.Infrastructure.MLNet.Services;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using ZakYip.BarcodeReadabilityLab.Core.Domain.Contracts;
using ZakYip.BarcodeReadabilityLab.Core.Domain.Models;
using ZakYip.BarcodeReadabilityLab.Core.Enums;
using ZakYip.BarcodeReadabilityLab.Infrastructure.MLNet.Models;

/// <summary>
/// 支持多模型对比的 ML.NET 预测服务
/// </summary>
public sealed class MlNetModelVariantAnalyzer : IModelVariantAnalyzer, IDisposable
{
    private readonly ILogger<MlNetModelVariantAnalyzer> _logger;
    private readonly MLContext _mlContext;
    private bool _isDisposed;

    public MlNetModelVariantAnalyzer(ILogger<MlNetModelVariantAnalyzer> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _mlContext = new MLContext(seed: 0);
    }

    /// <inheritdoc />
    public ValueTask<IReadOnlyList<ModelComparisonResult>> AnalyzeAsync(
        BarcodeSample sample,
        IEnumerable<ModelVersion> modelVersions,
        CancellationToken cancellationToken = default)
    {
        if (sample is null)
            throw new ArgumentNullException(nameof(sample));

        if (modelVersions is null)
            throw new ArgumentNullException(nameof(modelVersions));

        if (_isDisposed)
            throw new ObjectDisposedException(nameof(MlNetModelVariantAnalyzer));

        var versionList = modelVersions.ToList();
        var results = new List<ModelComparisonResult>(versionList.Count);

        foreach (var version in versionList)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var comparisonResult = AnalyzeSingle(sample, version);
            results.Add(comparisonResult);
        }

        return ValueTask.FromResult<IReadOnlyList<ModelComparisonResult>>(results);
    }

    private ModelComparisonResult AnalyzeSingle(BarcodeSample sample, ModelVersion version)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(version.ModelPath))
            {
                var message = "模型路径未配置";
                _logger.LogWarning("模型版本缺少路径 => VersionId: {VersionId}, VersionName: {VersionName}", version.VersionId, version.VersionName);
                return BuildFailureResult(sample, version, message);
            }

            if (!File.Exists(version.ModelPath))
            {
                var message = $"模型文件不存在：{version.ModelPath}";
                _logger.LogWarning("模型文件缺失 => VersionId: {VersionId}, Path: {ModelPath}", version.VersionId, version.ModelPath);
                return BuildFailureResult(sample, version, message);
            }

            var loadedModel = _mlContext.Model.Load(version.ModelPath, out _);

            using var predictionEngine = _mlContext.Model.CreatePredictionEngine<MlNetImageInput, MlNetPredictionOutput>(loadedModel);
            var input = new MlNetImageInput { ImagePath = sample.FilePath };
            var prediction = predictionEngine.Predict(input);

            // 使用统一的映射器
            var analysisResult = MlNetPredictionMapper.MapToBarcodeAnalysisResult(prediction, sample.SampleId);

            if (!analysisResult.IsAnalyzed)
            {
                _logger.LogWarning("无法映射预测标签 => VersionId: {VersionId}, SampleId: {SampleId}, Label: {Label}", 
                    version.VersionId, sample.SampleId, prediction.PredictedLabel);
            }

            return new ModelComparisonResult
            {
                Version = version,
                AnalysisResult = analysisResult
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "模型版本分析失败 => VersionId: {VersionId}, SampleId: {SampleId}", version.VersionId, sample.SampleId);
            return BuildFailureResult(sample, version, ex.Message);
        }
    }

    private static ModelComparisonResult BuildFailureResult(BarcodeSample sample, ModelVersion version, string message)
    {
        var analysisResult = MlNetPredictionMapper.CreateFailureResult(sample.SampleId, message);

        return new ModelComparisonResult
        {
            Version = version,
            AnalysisResult = analysisResult
        };
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        // MLContext does not implement IDisposable in ML.NET 5.0
        // No cleanup needed
        _isDisposed = true;
        GC.SuppressFinalize(this);
    }
}
