namespace ZakYip.BarcodeReadabilityLab.Application.Services;

/// <summary>
/// 目录监控服务契约
/// </summary>
/// <remarks>
/// 负责监控 WatchDirectory，发现新图片后构造 BarcodeSample，调用 IBarcodeReadabilityAnalyzer，并根据结果决定是否调用 IUnresolvedImageRouter。
/// </remarks>
public interface IDirectoryMonitoringService
{
    /// <summary>
    /// 启动目录监控
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>启动任务</returns>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 停止目录监控
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>停止任务</returns>
    Task StopAsync(CancellationToken cancellationToken = default);
}
