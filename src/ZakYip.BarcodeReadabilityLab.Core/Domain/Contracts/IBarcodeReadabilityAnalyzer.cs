using ZakYip.BarcodeReadabilityLab.Core.Domain.Models;

namespace ZakYip.BarcodeReadabilityLab.Core.Domain.Contracts;

/// <summary>
/// 条码可读性分析器契约
/// </summary>
/// <remarks>
/// 此接口只负责单张条码图片的推理分析，不负责目录监控与文件复制等操作。
/// </remarks>
public interface IBarcodeReadabilityAnalyzer
{
    /// <summary>
    /// 异步分析条码样本的可读性
    /// </summary>
    /// <param name="sample">待分析的条码样本</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>条码分析结果</returns>
    ValueTask<BarcodeAnalysisResult> AnalyzeAsync(BarcodeSample sample, CancellationToken cancellationToken = default);
}
