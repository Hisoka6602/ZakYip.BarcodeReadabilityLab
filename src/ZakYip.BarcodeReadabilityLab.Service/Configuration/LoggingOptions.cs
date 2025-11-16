namespace ZakYip.BarcodeReadabilityLab.Service.Configuration;

/// <summary>
/// 日志配置选项
/// </summary>
public record class LoggingOptions
{
    /// <summary>
    /// 默认日志级别（Verbose、Debug、Information、Warning、Error、Fatal）
    /// </summary>
    public required string MinimumLevel { get; init; }

    /// <summary>
    /// 是否启用审计日志
    /// </summary>
    public bool EnableAuditLog { get; init; } = true;

    /// <summary>
    /// 是否启用性能日志
    /// </summary>
    public bool EnablePerformanceLog { get; init; } = true;

    /// <summary>
    /// 性能日志慢操作阈值（毫秒）
    /// </summary>
    public int SlowOperationThresholdMs { get; init; } = 1000;

    /// <summary>
    /// 日志文件路径
    /// </summary>
    public string LogFilePath { get; init; } = "logs/barcode-lab-.log";

    /// <summary>
    /// 日志文件轮转间隔（Minute、Hour、Day、Month、Year、Infinite）
    /// </summary>
    public string RollingInterval { get; init; } = "Day";

    /// <summary>
    /// 日志文件大小限制（字节）
    /// </summary>
    public long FileSizeLimitBytes { get; init; } = 104857600; // 100MB

    /// <summary>
    /// 保留的日志文件数量限制
    /// </summary>
    public int? RetainedFileCountLimit { get; init; } = 31;

    /// <summary>
    /// 是否在达到文件大小限制时轮转
    /// </summary>
    public bool RollOnFileSizeLimit { get; init; } = true;
}
