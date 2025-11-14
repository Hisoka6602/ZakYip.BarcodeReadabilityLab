namespace ZakYip.BarcodeReadabilityLab.Application.Services;

/// <summary>
/// 系统资源监控服务契约
/// </summary>
public interface IResourceMonitor
{
    /// <summary>
    /// 获取当前系统资源使用情况快照
    /// </summary>
    /// <returns>资源使用快照</returns>
    ResourceUsageSnapshot GetCurrentUsage();

    /// <summary>
    /// 异步获取当前系统资源使用情况快照
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>资源使用快照</returns>
    Task<ResourceUsageSnapshot> GetCurrentUsageAsync(CancellationToken cancellationToken = default);
}
