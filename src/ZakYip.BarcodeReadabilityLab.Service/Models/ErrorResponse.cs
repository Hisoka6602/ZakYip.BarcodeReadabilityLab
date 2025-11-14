namespace ZakYip.BarcodeReadabilityLab.Service.Models;

/// <summary>
/// API 错误响应模型
/// </summary>
public record class ErrorResponse
{
    /// <summary>
    /// 错误消息
    /// </summary>
    public required string Error { get; init; }
}
