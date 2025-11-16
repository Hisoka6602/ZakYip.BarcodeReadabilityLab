using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Serilog.Events;
using ZakYip.BarcodeReadabilityLab.Service.Services;

namespace ZakYip.BarcodeReadabilityLab.Service.Endpoints;

/// <summary>
/// 日志管理 API 端点
/// </summary>
public static class LoggingEndpoints
{
    /// <summary>
    /// 注册日志管理端点
    /// </summary>
    public static void MapLoggingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/logging")
            .WithTags("日志管理");

        // 获取当前日志级别
        group.MapGet("/level", GetLogLevel)
            .WithName("GetLogLevel")
            .WithSummary("获取当前日志级别")
            .Produces<LogLevelResponse>(StatusCodes.Status200OK);

        // 设置日志级别
        group.MapPut("/level", SetLogLevel)
            .WithName("SetLogLevel")
            .WithSummary("设置日志级别")
            .Produces<LogLevelResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);
    }

    /// <summary>
    /// 获取当前日志级别
    /// </summary>
    private static IResult GetLogLevel(ILogLevelManager logLevelManager, ILogger<Program> logger)
    {
        var currentLevel = logLevelManager.GetMinimumLevelString();
        logger.LogInformation("查询当前日志级别 => 当前级别: {LogLevel}", currentLevel);

        return Results.Ok(new LogLevelResponse
        {
            Level = currentLevel,
            Message = "当前日志级别获取成功"
        });
    }

    /// <summary>
    /// 设置日志级别
    /// </summary>
    private static IResult SetLogLevel(
        [FromBody] SetLogLevelRequest request,
        ILogLevelManager logLevelManager,
        ILogger<Program> logger)
    {
        // 验证日志级别
        if (!Enum.TryParse<LogEventLevel>(request.Level, true, out var logLevel))
        {
            logger.LogWarning("设置日志级别失败 => 无效的日志级别: {RequestedLevel}", request.Level);
            return Results.BadRequest(new
            {
                error = $"无效的日志级别: {request.Level}",
                validLevels = Enum.GetNames<LogEventLevel>()
            });
        }

        var oldLevel = logLevelManager.GetMinimumLevelString();
        logLevelManager.SetMinimumLevel(logLevel);

        logger.LogWarning(
            "日志级别已更改 => 原级别: {OldLevel}, 新级别: {NewLevel}, 操作者: {Operator}",
            oldLevel,
            logLevel,
            request.Operator ?? "未知");

        return Results.Ok(new LogLevelResponse
        {
            Level = logLevel.ToString(),
            Message = $"日志级别已从 {oldLevel} 更改为 {logLevel}"
        });
    }
}

/// <summary>
/// 设置日志级别请求
/// </summary>
public record class SetLogLevelRequest
{
    /// <summary>
    /// 日志级别（Verbose、Debug、Information、Warning、Error、Fatal）
    /// </summary>
    public required string Level { get; init; }

    /// <summary>
    /// 操作者标识（可选）
    /// </summary>
    public string? Operator { get; init; }
}

/// <summary>
/// 日志级别响应
/// </summary>
public record class LogLevelResponse
{
    /// <summary>
    /// 当前日志级别
    /// </summary>
    public required string Level { get; init; }

    /// <summary>
    /// 响应消息
    /// </summary>
    public required string Message { get; init; }
}
