using Serilog.Core;
using Serilog.Events;

namespace ZakYip.BarcodeReadabilityLab.Service.Services;

/// <summary>
/// 动态日志级别管理服务
/// </summary>
public interface ILogLevelManager
{
    /// <summary>
    /// 获取当前最小日志级别
    /// </summary>
    LogEventLevel GetMinimumLevel();

    /// <summary>
    /// 设置最小日志级别
    /// </summary>
    /// <param name="level">日志级别</param>
    void SetMinimumLevel(LogEventLevel level);

    /// <summary>
    /// 获取当前最小日志级别的字符串表示
    /// </summary>
    string GetMinimumLevelString();
}

/// <summary>
/// 动态日志级别管理服务实现
/// </summary>
public class LogLevelManager : ILogLevelManager
{
    private readonly LoggingLevelSwitch _levelSwitch;

    public LogLevelManager(LoggingLevelSwitch levelSwitch)
    {
        _levelSwitch = levelSwitch ?? throw new ArgumentNullException(nameof(levelSwitch));
    }

    /// <summary>
    /// 获取当前最小日志级别
    /// </summary>
    public LogEventLevel GetMinimumLevel()
    {
        return _levelSwitch.MinimumLevel;
    }

    /// <summary>
    /// 设置最小日志级别
    /// </summary>
    public void SetMinimumLevel(LogEventLevel level)
    {
        _levelSwitch.MinimumLevel = level;
    }

    /// <summary>
    /// 获取当前最小日志级别的字符串表示
    /// </summary>
    public string GetMinimumLevelString()
    {
        return _levelSwitch.MinimumLevel.ToString();
    }
}
