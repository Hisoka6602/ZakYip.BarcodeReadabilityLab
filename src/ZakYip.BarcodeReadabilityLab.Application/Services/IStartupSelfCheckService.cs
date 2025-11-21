namespace ZakYip.BarcodeReadabilityLab.Application.Services;

/// <summary>
/// 启动配置自检服务接口
/// </summary>
public interface IStartupSelfCheckService
{
    /// <summary>
    /// 执行启动自检
    /// </summary>
    /// <returns>自检结果</returns>
    Task<StartupSelfCheckResult> PerformSelfCheckAsync();

    /// <summary>
    /// 获取最后一次自检结果
    /// </summary>
    /// <returns>最后一次自检结果，如果未执行过则返回 null</returns>
    StartupSelfCheckResult? GetLastCheckResult();
}
