using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using ZakYip.BarcodeReadabilityLab.Service.Configuration;

namespace ZakYip.BarcodeReadabilityLab.Service.Middleware;

/// <summary>
/// 审计日志和性能监控中间件
/// </summary>
public class AuditLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditLoggingMiddleware> _logger;
    private readonly LoggingOptions _options;

    public AuditLoggingMiddleware(
        RequestDelegate next,
        ILogger<AuditLoggingMiddleware> logger,
        IOptions<LoggingOptions> options)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestPath = context.Request.Path;
        var requestMethod = context.Request.Method;
        var requestId = context.TraceIdentifier;

        try
        {
            // 记录请求开始（仅对 API 端点）
            if (_options.EnableAuditLog && IsApiEndpoint(requestPath))
            {
                _logger.LogInformation(
                    "API 请求开始 => RequestId: {RequestId}, Method: {Method}, Path: {Path}, RemoteIp: {RemoteIp}",
                    requestId,
                    requestMethod,
                    requestPath,
                    context.Connection.RemoteIpAddress);
            }

            // 执行下一个中间件
            await _next(context);

            stopwatch.Stop();

            // 记录请求完成
            if (_options.EnableAuditLog && IsApiEndpoint(requestPath))
            {
                var statusCode = context.Response.StatusCode;
                var elapsed = stopwatch.ElapsedMilliseconds;

                // 根据性能阈值决定日志级别
                if (_options.EnablePerformanceLog && elapsed > _options.SlowOperationThresholdMs)
                {
                    _logger.LogWarning(
                        "API 请求完成（慢操作）=> RequestId: {RequestId}, Method: {Method}, Path: {Path}, StatusCode: {StatusCode}, Duration: {Duration}ms",
                        requestId,
                        requestMethod,
                        requestPath,
                        statusCode,
                        elapsed);
                }
                else
                {
                    _logger.LogInformation(
                        "API 请求完成 => RequestId: {RequestId}, Method: {Method}, Path: {Path}, StatusCode: {StatusCode}, Duration: {Duration}ms",
                        requestId,
                        requestMethod,
                        requestPath,
                        statusCode,
                        elapsed);
                }
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            // 记录请求失败
            _logger.LogError(
                ex,
                "API 请求失败 => RequestId: {RequestId}, Method: {Method}, Path: {Path}, Duration: {Duration}ms, Error: {ErrorMessage}",
                requestId,
                requestMethod,
                requestPath,
                stopwatch.ElapsedMilliseconds,
                ex.Message);

            throw;
        }
    }

    /// <summary>
    /// 判断是否为 API 端点
    /// </summary>
    private static bool IsApiEndpoint(PathString path)
    {
        return path.StartsWithSegments("/api") || path.StartsWithSegments("/hubs");
    }
}

/// <summary>
/// 审计日志中间件扩展
/// </summary>
public static class AuditLoggingMiddlewareExtensions
{
    /// <summary>
    /// 使用审计日志中间件
    /// </summary>
    public static IApplicationBuilder UseAuditLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<AuditLoggingMiddleware>();
    }
}
