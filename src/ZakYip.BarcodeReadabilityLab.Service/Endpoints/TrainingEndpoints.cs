namespace ZakYip.BarcodeReadabilityLab.Service.Endpoints;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.ComponentModel;
using ZakYip.BarcodeReadabilityLab.Application.Options;
using ZakYip.BarcodeReadabilityLab.Application.Services;
using ZakYip.BarcodeReadabilityLab.Service.Models;

/// <summary>
/// 训练任务相关 API 端点
/// </summary>
public static class TrainingEndpoints
{
    /// <summary>
    /// 注册训练端点
    /// </summary>
    public static void MapTrainingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/training")
            .WithTags("Training");

        group.MapPost("/start", StartTrainingAsync)
            .WithName("StartTraining")
            .WithSummary("启动训练任务")
            .WithDescription("触发一次基于目录的训练任务。如果请求体中未提供参数，则使用配置文件中的默认 TrainingOptions。");

        group.MapGet("/status/{jobId:guid}", GetTrainingStatusAsync)
            .WithName("GetTrainingStatus")
            .WithSummary("查询训练任务状态")
            .WithDescription("根据 jobId 查询训练任务的当前状态与进度信息。");
    }

    /// <summary>
    /// 启动训练任务
    /// </summary>
    private static async Task<IResult> StartTrainingAsync(
        [FromBody] StartTrainingRequest? request,
        [FromServices] ITrainingJobService trainingJobService,
        [FromServices] IOptions<TrainingOptions> trainingOptions,
        [FromServices] ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            // 如果请求体为空或字段为空，使用配置文件中的默认值
            var defaultOptions = trainingOptions.Value;

            var trainingRequest = new TrainingRequest
            {
                TrainingRootDirectory = request?.TrainingRootDirectory ?? defaultOptions.TrainingRootDirectory,
                OutputModelDirectory = request?.OutputModelDirectory ?? defaultOptions.OutputModelDirectory,
                ValidationSplitRatio = request?.ValidationSplitRatio ?? defaultOptions.ValidationSplitRatio,
                Remarks = request?.Remarks
            };

            logger.LogInformation("收到训练任务请求，训练目录: {TrainingRootDirectory}",
                trainingRequest.TrainingRootDirectory);

            var jobId = await trainingJobService.StartTrainingAsync(trainingRequest, cancellationToken);

            logger.LogInformation("训练任务已创建，JobId: {JobId}", jobId);

            var response = new StartTrainingResponse
            {
                JobId = jobId,
                Message = "训练任务已创建并加入队列"
            };

            return Results.Ok(response);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "训练请求参数无效");
            return Results.BadRequest(new ErrorResponse { Error = ex.Message });
        }
        catch (DirectoryNotFoundException ex)
        {
            logger.LogWarning(ex, "训练目录不存在");
            return Results.BadRequest(new ErrorResponse { Error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "启动训练任务失败");
            return Results.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError,
                title: "启动训练任务失败");
        }
    }

    /// <summary>
    /// 查询训练任务状态
    /// </summary>
    private static async Task<IResult> GetTrainingStatusAsync(
        Guid jobId,
        [FromServices] ITrainingJobService trainingJobService,
        [FromServices] ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var status = await trainingJobService.GetStatusAsync(jobId, cancellationToken);

            if (status is null)
            {
                logger.LogWarning("训练任务不存在，JobId: {JobId}", jobId);
                return Results.NotFound(new ErrorResponse { Error = "训练任务不存在" });
            }

            // 获取状态枚举的中文描述
            var stateDescription = GetEnumDescription(status.Status);

            var response = new TrainingJobResponse
            {
                JobId = status.JobId,
                State = stateDescription,
                Progress = status.Progress,
                Message = status.Status switch
                {
                    Application.Services.TrainingStatus.Queued => "训练任务排队中",
                    Application.Services.TrainingStatus.Running => "训练任务正在执行",
                    Application.Services.TrainingStatus.Completed => "训练任务已完成",
                    Application.Services.TrainingStatus.Failed => $"训练任务失败: {status.ErrorMessage}",
                    Application.Services.TrainingStatus.Cancelled => "训练任务已取消",
                    _ => "未知状态"
                },
                StartTime = status.StartTime,
                CompletedTime = status.CompletedTime,
                ErrorMessage = status.ErrorMessage,
                Remarks = status.Remarks
            };

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "查询训练任务状态失败，JobId: {JobId}", jobId);
            return Results.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError,
                title: "查询训练任务状态失败");
        }
    }

    /// <summary>
    /// 获取枚举的描述特性值
    /// </summary>
    private static string GetEnumDescription(Enum value)
    {
        var field = value.GetType().GetField(value.ToString());
        if (field is null)
            return value.ToString();

        var attribute = (DescriptionAttribute?)Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute));
        return attribute?.Description ?? value.ToString();
    }
}
