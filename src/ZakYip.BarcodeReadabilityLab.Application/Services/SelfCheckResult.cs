namespace ZakYip.BarcodeReadabilityLab.Application.Services;

/// <summary>
/// 自检结果
/// </summary>
public record class SelfCheckResult
{
    /// <summary>
    /// 检查项名称
    /// </summary>
    public required string CheckName { get; init; }

    /// <summary>
    /// 是否通过检查
    /// </summary>
    public required bool IsHealthy { get; init; }

    /// <summary>
    /// 检查描述信息
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// 错误消息（如果检查失败）
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// 是否已自动修复
    /// </summary>
    public bool IsAutoFixed { get; init; }
}
