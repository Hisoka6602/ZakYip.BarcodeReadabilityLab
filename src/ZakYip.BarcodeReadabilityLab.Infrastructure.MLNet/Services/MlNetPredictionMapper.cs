namespace ZakYip.BarcodeReadabilityLab.Infrastructure.MLNet.Services;

using System;
using System.Linq;
using ZakYip.BarcodeReadabilityLab.Core.Domain.Models;
using ZakYip.BarcodeReadabilityLab.Core.Enums;
using ZakYip.BarcodeReadabilityLab.Infrastructure.MLNet.Models;

/// <summary>
/// ML.NET 预测结果映射器（统一的预测结果转换逻辑）
/// </summary>
internal static class MlNetPredictionMapper
{
    /// <summary>
    /// 将模型输出标签映射为 NoreadReason
    /// </summary>
    public static (NoreadReason? reason, bool isSuccess) MapLabelToNoreadReason(string label)
    {
        if (string.IsNullOrWhiteSpace(label))
            return (null, false);

        // 尝试直接按枚举名称解析
        if (Enum.TryParse<NoreadReason>(label, ignoreCase: true, out var reasonByName))
            return (reasonByName, true);

        // 尝试解析为数值表示
        if (int.TryParse(label, out var numericValue) && Enum.IsDefined(typeof(NoreadReason), numericValue))
            return ((NoreadReason)numericValue, true);

        return (null, false);
    }

    /// <summary>
    /// 从 ML.NET 预测输出提取置信度（最大得分）
    /// </summary>
    public static decimal ExtractConfidence(MlNetPredictionOutput prediction)
    {
        if (prediction?.Score is null || prediction.Score.Length == 0)
            return 0m;

        var maxScore = prediction.Score.Max();
        return Convert.ToDecimal(maxScore);
    }

    /// <summary>
    /// 将 ML.NET 预测输出映射为条码分析结果
    /// </summary>
    /// <param name="prediction">ML.NET 预测输出</param>
    /// <param name="sampleId">样本 ID</param>
    /// <returns>条码分析结果</returns>
    public static BarcodeAnalysisResult MapToBarcodeAnalysisResult(
        MlNetPredictionOutput prediction,
        Guid sampleId)
    {
        var (reason, isSuccess) = MapLabelToNoreadReason(prediction.PredictedLabel);

        if (!isSuccess)
        {
            return new BarcodeAnalysisResult
            {
                SampleId = sampleId,
                IsAnalyzed = false,
                IsAboveThreshold = false,
                Message = $"无法识别的标签：{prediction.PredictedLabel}"
            };
        }

        var confidence = ExtractConfidence(prediction);

        return new BarcodeAnalysisResult
        {
            SampleId = sampleId,
            IsAnalyzed = true,
            Reason = reason,
            Confidence = confidence,
            IsAboveThreshold = false
        };
    }

    /// <summary>
    /// 创建失败的条码分析结果
    /// </summary>
    /// <param name="sampleId">样本 ID</param>
    /// <param name="message">错误消息</param>
    /// <returns>表示失败的条码分析结果</returns>
    public static BarcodeAnalysisResult CreateFailureResult(Guid sampleId, string message)
    {
        return new BarcodeAnalysisResult
        {
            SampleId = sampleId,
            IsAnalyzed = false,
            IsAboveThreshold = false,
            Message = message
        };
    }
}
