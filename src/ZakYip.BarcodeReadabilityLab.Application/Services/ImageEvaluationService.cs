using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZakYip.BarcodeReadabilityLab.Application.Options;
using ZakYip.BarcodeReadabilityLab.Core.Domain.Contracts;
using ZakYip.BarcodeReadabilityLab.Core.Domain.Models;
using ZakYip.BarcodeReadabilityLab.Core.Enums;

namespace ZakYip.BarcodeReadabilityLab.Application.Services;

/// <summary>
/// 图片评估服务实现
/// </summary>
public class ImageEvaluationService : IImageEvaluationService
{
    private readonly IBarcodeReadabilityAnalyzer _analyzer;
    private readonly ILogger<ImageEvaluationService> _logger;
    private readonly EvaluationOptions _options;

    public ImageEvaluationService(
        IBarcodeReadabilityAnalyzer analyzer,
        ILogger<ImageEvaluationService> logger,
        IOptions<EvaluationOptions> options)
    {
        _analyzer = analyzer ?? throw new ArgumentNullException(nameof(analyzer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public async Task<SingleEvaluationResult> EvaluateSingleAsync(
        Stream imageStream,
        string fileName,
        NoreadReason? expectedLabel = null,
        bool returnRawScores = false,
        CancellationToken cancellationToken = default)
    {
        if (imageStream is null)
            throw new ArgumentNullException(nameof(imageStream));

        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("文件名不能为空", nameof(fileName));

        _logger.LogInformation("开始评估单张图片 => 文件名: {FileName}, 预期标签: {ExpectedLabel}", 
            fileName, expectedLabel);

        // 创建临时文件
        var tempFilePath = await SaveToTempFileAsync(imageStream, fileName, cancellationToken);

        try
        {
            // 创建样本并分析
            var sample = new BarcodeSample
            {
                SampleId = Guid.NewGuid(),
                FilePath = tempFilePath,
                CapturedAt = DateTimeOffset.UtcNow
            };

            var analysisResult = await _analyzer.AnalyzeAsync(sample, cancellationToken);

            if (!analysisResult.IsAnalyzed || analysisResult.Reason is null)
            {
                throw new InvalidOperationException($"分析失败: {analysisResult.Message}");
            }

            // 构建评估结果
            var result = new SingleEvaluationResult
            {
                PredictedLabel = analysisResult.Reason.Value,
                Confidence = analysisResult.Confidence ?? 0m,
                ExpectedLabel = expectedLabel,
                IsCorrect = expectedLabel.HasValue 
                    ? analysisResult.Reason.Value == expectedLabel.Value
                    : null,
                NoreadReasonScores = returnRawScores 
                    ? GetRawScores(analysisResult)
                    : null
            };

            _logger.LogInformation(
                "单张图片评估完成 => 文件名: {FileName}, 预测: {Predicted}, 置信度: {Confidence:P2}, 正确: {IsCorrect}",
                fileName, result.PredictedLabel, result.Confidence, result.IsCorrect);

            return result;
        }
        finally
        {
            // 清理临时文件
            CleanupTempFile(tempFilePath);
        }
    }

    /// <inheritdoc />
    public async Task<BatchEvaluationResult> EvaluateBatchAsync(
        IEnumerable<(Stream Stream, string FileName, NoreadReason? ExpectedLabel)> images,
        bool returnRawScores = false,
        CancellationToken cancellationToken = default)
    {
        if (images is null)
            throw new ArgumentNullException(nameof(images));

        var imageList = images.ToList();

        _logger.LogInformation("开始批量评估 => 图片数量: {Count}", imageList.Count);

        var items = new List<BatchEvaluationItem>();

        foreach (var (stream, fileName, expectedLabel) in imageList)
        {
            try
            {
                var result = await EvaluateSingleAsync(stream, fileName, expectedLabel, returnRawScores, cancellationToken);
                items.Add(new BatchEvaluationItem
                {
                    FileName = fileName,
                    Result = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "评估图片失败 => 文件名: {FileName}, 错误: {Error}", fileName, ex.Message);
                // 继续处理其他图片，返回失败结果
                // 注意：这里使用默认值是为了保持批量处理的继续性，实际错误已记录在日志中
                items.Add(new BatchEvaluationItem
                {
                    FileName = fileName,
                    Result = new SingleEvaluationResult
                    {
                        PredictedLabel = NoreadReason.ClearButNotRecognized, // 使用默认值表示分析失败
                        Confidence = 0m,
                        ExpectedLabel = expectedLabel,
                        IsCorrect = false
                    }
                });
            }
        }

        // 计算汇总统计
        var summary = CalculateSummary(items);

        _logger.LogInformation(
            "批量评估完成 => 总数: {Total}, 正确数: {Correct}, 准确率: {Accuracy:P2}",
            summary.Total, summary.CorrectCount, summary.Accuracy);

        return new BatchEvaluationResult
        {
            Items = items,
            Summary = summary
        };
    }

    /// <summary>
    /// 保存流到临时文件
    /// </summary>
    private async Task<string> SaveToTempFileAsync(Stream stream, string fileName, CancellationToken cancellationToken)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "BarcodeEvaluation");
        Directory.CreateDirectory(tempDir);

        var extension = Path.GetExtension(fileName);
        var tempFileName = $"{Guid.NewGuid()}{extension}";
        var tempFilePath = Path.Combine(tempDir, tempFileName);

        await using var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write);
        await stream.CopyToAsync(fileStream, cancellationToken);

        return tempFilePath;
    }

    /// <summary>
    /// 清理临时文件
    /// </summary>
    private void CleanupTempFile(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogDebug("临时文件已清理 => 路径: {FilePath}", filePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "清理临时文件失败 => 路径: {FilePath}", filePath);
        }
    }

    /// <summary>
    /// 获取原始分数
    /// </summary>
    /// <remarks>
    /// 注意：当前实现仅返回最高置信度的类别分数。
    /// 要获取所有类别的完整概率分布，需要扩展 IBarcodeReadabilityAnalyzer 接口
    /// 以从 ML.NET 预测输出中提取完整的 Score 数组。
    /// </remarks>
    private Dictionary<NoreadReason, decimal>? GetRawScores(BarcodeAnalysisResult analysisResult)
    {
        // TODO: 需要扩展 IBarcodeReadabilityAnalyzer 以返回原始分数
        // 当前只返回置信度最高的类别
        if (analysisResult.Reason.HasValue && analysisResult.Confidence.HasValue)
        {
            return new Dictionary<NoreadReason, decimal>
            {
                { analysisResult.Reason.Value, analysisResult.Confidence.Value }
            };
        }

        return null;
    }

    /// <summary>
    /// 计算汇总统计
    /// </summary>
    private EvaluationSummary CalculateSummary(List<BatchEvaluationItem> items)
    {
        var total = items.Count;
        var withExpectedLabel = items.Count(i => i.Result.ExpectedLabel.HasValue);
        var correctCount = items.Count(i => i.Result.IsCorrect == true);

        // 准确率（仅针对有预期标签的样本）
        decimal? accuracy = withExpectedLabel > 0
            ? (decimal)correctCount / withExpectedLabel
            : null;

        // 计算 F1 分数（需要混淆矩阵）
        var (macroF1, microF1) = CalculateF1Scores(items);

        return new EvaluationSummary
        {
            Total = total,
            WithExpectedLabel = withExpectedLabel,
            CorrectCount = correctCount,
            Accuracy = accuracy,
            MacroF1 = macroF1,
            MicroF1 = microF1
        };
    }

    /// <summary>
    /// 计算 F1 分数
    /// </summary>
    private (decimal? macroF1, decimal? microF1) CalculateF1Scores(List<BatchEvaluationItem> items)
    {
        // 仅针对有预期标签的样本
        var itemsWithLabels = items
            .Where(i => i.Result.ExpectedLabel.HasValue)
            .ToList();

        if (itemsWithLabels.Count == 0)
            return (null, null);

        // 获取所有类别
        var allLabels = Enum.GetValues<NoreadReason>();
        var perClassMetrics = new Dictionary<NoreadReason, (int tp, int fp, int fn)>();

        foreach (var label in allLabels)
        {
            var tp = itemsWithLabels.Count(i => 
                i.Result.ExpectedLabel == label && i.Result.PredictedLabel == label);
            var fp = itemsWithLabels.Count(i => 
                i.Result.ExpectedLabel != label && i.Result.PredictedLabel == label);
            var fn = itemsWithLabels.Count(i => 
                i.Result.ExpectedLabel == label && i.Result.PredictedLabel != label);

            perClassMetrics[label] = (tp, fp, fn);
        }

        // 计算宏平均 F1
        var f1Scores = new List<decimal>();
        foreach (var (label, (tp, fp, fn)) in perClassMetrics)
        {
            if (tp + fp + fn == 0)
                continue;

            var precision = tp + fp > 0 ? (decimal)tp / (tp + fp) : 0m;
            var recall = tp + fn > 0 ? (decimal)tp / (tp + fn) : 0m;
            var f1 = precision + recall > 0 ? 2 * precision * recall / (precision + recall) : 0m;

            f1Scores.Add(f1);
        }

        var macroF1 = f1Scores.Count > 0 ? f1Scores.Average() : 0m;

        // 计算微平均 F1
        var totalTp = perClassMetrics.Values.Sum(v => v.tp);
        var totalFp = perClassMetrics.Values.Sum(v => v.fp);
        var totalFn = perClassMetrics.Values.Sum(v => v.fn);

        var microPrecision = totalTp + totalFp > 0 ? (decimal)totalTp / (totalTp + totalFp) : 0m;
        var microRecall = totalTp + totalFn > 0 ? (decimal)totalTp / (totalTp + totalFn) : 0m;
        var microF1 = microPrecision + microRecall > 0 
            ? 2 * microPrecision * microRecall / (microPrecision + microRecall) 
            : 0m;

        return (macroF1, microF1);
    }
}
