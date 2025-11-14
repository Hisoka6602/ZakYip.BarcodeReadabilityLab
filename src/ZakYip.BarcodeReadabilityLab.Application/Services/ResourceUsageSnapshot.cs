namespace ZakYip.BarcodeReadabilityLab.Application.Services;

/// <summary>
/// 资源使用快照
/// </summary>
public readonly record struct ResourceUsageSnapshot
{
    /// <summary>
    /// CPU 使用率（百分比，0.0 到 100.0 之间）
    /// </summary>
    public required decimal CpuUsagePercent { get; init; }

    /// <summary>
    /// 已用内存（字节）
    /// </summary>
    public required long UsedMemoryBytes { get; init; }

    /// <summary>
    /// 总内存（字节）
    /// </summary>
    public required long TotalMemoryBytes { get; init; }

    /// <summary>
    /// 内存使用率（百分比，0.0 到 100.0 之间）
    /// </summary>
    public decimal MemoryUsagePercent => TotalMemoryBytes > 0
        ? (decimal)UsedMemoryBytes / TotalMemoryBytes * 100
        : 0m;

    /// <summary>
    /// 快照时间戳
    /// </summary>
    public required DateTimeOffset Timestamp { get; init; }
}
