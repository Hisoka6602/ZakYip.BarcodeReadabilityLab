using ZakYip.BarcodeReadabilityLab.Core.Domain.Models;

namespace ZakYip.BarcodeReadabilityLab.Application.Services;

/// <summary>
/// 无法分析图片路由服务契约
/// </summary>
/// <remarks>
/// 根据分析结果决定是否将图片复制到"无法分析"目录，并负责实际的文件复制逻辑。
/// </remarks>
public interface IUnresolvedImageRouter
{
    /// <summary>
    /// 路由无法分析的图片
    /// </summary>
    /// <param name="sample">条码样本</param>
    /// <param name="result">条码分析结果</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>路由任务</returns>
    ValueTask RouteAsync(BarcodeSample sample, BarcodeAnalysisResult result, CancellationToken cancellationToken = default);
}
