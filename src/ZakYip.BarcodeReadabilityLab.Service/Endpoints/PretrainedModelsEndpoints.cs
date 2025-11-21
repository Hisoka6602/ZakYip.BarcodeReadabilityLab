namespace ZakYip.BarcodeReadabilityLab.Service.Endpoints;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using ZakYip.BarcodeReadabilityLab.Core.Domain.Contracts;
using ZakYip.BarcodeReadabilityLab.Core.Domain.Models;
using ZakYip.BarcodeReadabilityLab.Core.Enums;
using ZakYip.BarcodeReadabilityLab.Service.Models;

/// <summary>
/// 预训练模型相关 API 端点
/// </summary>
public static class PretrainedModelsEndpoints
{
    /// <summary>
    /// 注册预训练模型端点
    /// </summary>
    public static void MapPretrainedModelsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/pretrained-models")
            .WithTags("PretrainedModels");

        group.MapGet("/list", ListPretrainedModelsAsync)
            .WithName("ListPretrainedModels")
            .WithSummary("获取所有可用的预训练模型")
            .WithDescription(@"获取所有支持的预训练模型列表及其状态信息。

**功能说明：**
- 返回所有可用的预训练模型
- 包含模型大小、参数数量、下载状态等信息
- 可用于选择合适的模型进行迁移学习")
            .Produces<List<PretrainedModelResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet("/{modelType}", GetPretrainedModelInfoAsync)
            .WithName("GetPretrainedModelInfo")
            .WithSummary("获取指定预训练模型的详细信息")
            .WithDescription(@"根据模型类型获取预训练模型的详细信息。

**功能说明：**
- 返回特定模型的详细信息
- 包含下载状态和本地路径")
            .Produces<PretrainedModelResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapPost("/{modelType}/download", DownloadPretrainedModelAsync)
            .WithName("DownloadPretrainedModel")
            .WithSummary("下载指定的预训练模型")
            .WithDescription(@"下载指定类型的预训练模型到本地。

**功能说明：**
- 下载预训练模型文件
- 如果已下载，返回现有路径
- 支持下载进度追踪")
            .Produces<DownloadPretrainedModelResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }

    /// <summary>
    /// 获取所有可用的预训练模型
    /// </summary>
    private static async Task<IResult> ListPretrainedModelsAsync(
        [FromServices] IPretrainedModelManager pretrainedModelManager,
        [FromServices] ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("获取预训练模型列表");

            var models = await pretrainedModelManager.GetAvailableModelsAsync(cancellationToken);

            var response = models.Select(m => new PretrainedModelResponse
            {
                ModelType = m.ModelType,
                ModelName = m.ModelName,
                Description = m.Description,
                ModelSizeMB = m.ModelSizeBytes / 1024m / 1024m,
                IsDownloaded = m.IsDownloaded,
                LocalPath = m.LocalPath,
                RecommendedUseCase = m.RecommendedUseCase,
                ParameterCountMillions = m.ParameterCountMillions,
                TrainedOn = m.TrainedOn
            }).ToList();

            logger.LogInformation("成功获取预训练模型列表 => 总数: {Count}", response.Count);

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "获取预训练模型列表失败");
            return Results.Problem(
                detail: $"获取预训练模型列表失败: {ex.Message}",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// 获取指定预训练模型的详细信息
    /// </summary>
    private static async Task<IResult> GetPretrainedModelInfoAsync(
        PretrainedModelType modelType,
        [FromServices] IPretrainedModelManager pretrainedModelManager,
        [FromServices] ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("获取预训练模型信息 => 类型: {ModelType}", modelType);

            var model = await pretrainedModelManager.GetModelInfoAsync(modelType, cancellationToken);

            var response = new PretrainedModelResponse
            {
                ModelType = model.ModelType,
                ModelName = model.ModelName,
                Description = model.Description,
                ModelSizeMB = model.ModelSizeBytes / 1024m / 1024m,
                IsDownloaded = model.IsDownloaded,
                LocalPath = model.LocalPath,
                RecommendedUseCase = model.RecommendedUseCase,
                ParameterCountMillions = model.ParameterCountMillions,
                TrainedOn = model.TrainedOn
            };

            logger.LogInformation("成功获取预训练模型信息 => 模型: {ModelName}", response.ModelName);

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "获取预训练模型信息失败 => 类型: {ModelType}", modelType);
            return Results.Problem(
                detail: $"获取预训练模型信息失败: {ex.Message}",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// 下载预训练模型
    /// </summary>
    private static async Task<IResult> DownloadPretrainedModelAsync(
        PretrainedModelType modelType,
        [FromServices] IPretrainedModelManager pretrainedModelManager,
        [FromServices] ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("开始下载预训练模型 => 类型: {ModelType}", modelType);

            var targetDirectory = Path.Combine(AppContext.BaseDirectory, "pretrained-models");
            Directory.CreateDirectory(targetDirectory);

            var localPath = await pretrainedModelManager.DownloadModelAsync(
                modelType,
                targetDirectory,
                progress => logger.LogDebug("下载进度 => {Progress:P0}", progress),
                cancellationToken);

            logger.LogInformation("预训练模型下载完成 => 路径: {Path}", localPath);

            return Results.Ok(new DownloadPretrainedModelResponse
            {
                ModelType = modelType,
                LocalPath = localPath,
                Message = "预训练模型下载完成"
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "下载预训练模型失败 => 类型: {ModelType}", modelType);
            return Results.Problem(
                detail: $"下载预训练模型失败: {ex.Message}",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}

/// <summary>
/// 下载预训练模型响应
/// </summary>
public record class DownloadPretrainedModelResponse
{
    /// <summary>
    /// 模型类型
    /// </summary>
    public required PretrainedModelType ModelType { get; init; }

    /// <summary>
    /// 本地文件路径
    /// </summary>
    public required string LocalPath { get; init; }

    /// <summary>
    /// 响应消息
    /// </summary>
    public required string Message { get; init; }
}
