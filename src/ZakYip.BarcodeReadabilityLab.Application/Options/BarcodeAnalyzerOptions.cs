namespace ZakYip.BarcodeReadabilityLab.Application.Options;

/// <summary>
/// 条码分析器配置选项
/// </summary>
public record class BarcodeAnalyzerOptions
{
    /// <summary>
    /// 监控目录路径
    /// </summary>
    public required string WatchDirectory { get; init; }

    /// <summary>
    /// 无法分析图片存放目录路径
    /// </summary>
    public required string UnresolvedDirectory { get; init; }

    /// <summary>
    /// 置信度阈值（0.0 到 1.0 之间）
    /// </summary>
    public decimal ConfidenceThreshold { get; init; } = 0.90m;

    /// <summary>
    /// 是否递归监控子目录
    /// </summary>
    public bool IsRecursive { get; init; }

    /// <summary>
    /// 是否启用仿真模式（自动创建示例数据）
    /// </summary>
    public bool IsSimulationMode { get; init; }

    /// <summary>
    /// 是否自动创建不存在的目录
    /// </summary>
    public bool ShouldAutoCreateDirectories { get; init; } = true;
}
