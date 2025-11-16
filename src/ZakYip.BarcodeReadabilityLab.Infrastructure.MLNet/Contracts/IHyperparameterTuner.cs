namespace ZakYip.BarcodeReadabilityLab.Infrastructure.MLNet.Contracts;

using ZakYip.BarcodeReadabilityLab.Core.Domain.Models;

/// <summary>
/// 超参数调优器契约
/// </summary>
public interface IHyperparameterTuner
{
    /// <summary>
    /// 执行超参数调优
    /// </summary>
    /// <param name="trainingRootDirectory">训练数据根目录</param>
    /// <param name="outputModelDirectory">输出模型目录</param>
    /// <param name="strategy">调优策略</param>
    /// <param name="gridSearchOptions">网格搜索配置（当策略为网格搜索时必需）</param>
    /// <param name="randomSearchOptions">随机搜索配置（当策略为随机搜索时必需）</param>
    /// <param name="progressCallback">进度回调（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>超参数调优结果</returns>
    Task<HyperparameterTuningResult> TuneAsync(
        string trainingRootDirectory,
        string outputModelDirectory,
        HyperparameterTuningStrategy strategy,
        GridSearchOptions? gridSearchOptions = null,
        RandomSearchOptions? randomSearchOptions = null,
        IHyperparameterTuningProgressCallback? progressCallback = null,
        CancellationToken cancellationToken = default);
}
