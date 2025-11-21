namespace ZakYip.BarcodeReadabilityLab.Core.Domain.Contracts;

/// <summary>
/// 数据库连接检查器接口
/// </summary>
public interface IDatabaseConnectionChecker
{
    /// <summary>
    /// 检查数据库连接是否正常
    /// </summary>
    /// <returns>如果连接正常返回 true，否则返回 false</returns>
    Task<bool> CanConnectAsync();
}
