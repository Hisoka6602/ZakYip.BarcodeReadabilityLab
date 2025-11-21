using ZakYip.BarcodeReadabilityLab.Core.Domain.Models;
using ZakYip.BarcodeReadabilityLab.Core.Enums;

namespace ZakYip.BarcodeReadabilityLab.Application.Services;

/// <summary>
/// 图片评估服务接口
/// </summary>
public interface IImageEvaluationService
{
    /// <summary>
    /// 评估单张图片
    /// </summary>
    /// <param name="imageStream">图片流</param>
    /// <param name="fileName">文件名</param>
    /// <param name="expectedLabel">预期标签（可选）</param>
    /// <param name="returnRawScores">是否返回原始分数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>评估结果</returns>
    Task<SingleEvaluationResult> EvaluateSingleAsync(
        Stream imageStream,
        string fileName,
        NoreadReason? expectedLabel = null,
        bool returnRawScores = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量评估图片
    /// </summary>
    /// <param name="images">图片列表（流、文件名、预期标签）</param>
    /// <param name="returnRawScores">是否返回原始分数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>批量评估结果</returns>
    Task<BatchEvaluationResult> EvaluateBatchAsync(
        IEnumerable<(Stream Stream, string FileName, NoreadReason? ExpectedLabel)> images,
        bool returnRawScores = false,
        CancellationToken cancellationToken = default);
}
