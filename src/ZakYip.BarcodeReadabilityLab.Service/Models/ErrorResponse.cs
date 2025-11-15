namespace ZakYip.BarcodeReadabilityLab.Service.Models;

/// <summary>
/// API 错误响应模型
/// </summary>
/// <example>
/// {
///   "error": "训练目录不存在"
/// }
/// </example>
public record class ErrorResponse
{
    /// <summary>
    /// 错误消息
    /// </summary>
    /// <example>训练目录不存在</example>
    public required string Error { get; init; }
}
