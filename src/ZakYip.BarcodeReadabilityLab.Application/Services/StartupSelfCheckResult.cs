namespace ZakYip.BarcodeReadabilityLab.Application.Services;

/// <summary>
/// 启动自检总体结果
/// </summary>
public record class StartupSelfCheckResult
{
    /// <summary>
    /// 是否所有关键检查都通过
    /// </summary>
    public required bool IsHealthy { get; init; }

    /// <summary>
    /// 是否可以运行（包括降级模式）
    /// </summary>
    public required bool CanRun { get; init; }

    /// <summary>
    /// 各项检查结果
    /// </summary>
    public required List<SelfCheckResult> CheckResults { get; init; }

    /// <summary>
    /// 总体描述
    /// </summary>
    public string? Description { get; init; }
}
