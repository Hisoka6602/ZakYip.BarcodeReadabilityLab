namespace ZakYip.BarcodeReadabilityLab.Service.Endpoints;

using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZakYip.BarcodeReadabilityLab.Application.Services;
using ZakYip.BarcodeReadabilityLab.Core.Enum;
using ZakYip.BarcodeReadabilityLab.Infrastructure.MLNet.Models;
using ZakYip.BarcodeReadabilityLab.Service.Configuration;
using ZakYip.BarcodeReadabilityLab.Service.Models;

/// <summary>
/// 模型管理相关 API 端点
/// </summary>
public static class ModelEndpoints
{
    /// <summary>
    /// 注册模型管理端点
    /// </summary>
    public static void MapModelEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/models")
            .WithTags("Models");

        group.MapGet("/current/download", DownloadCurrentModelAsync)
            .WithName("DownloadCurrentModel")
            .WithSummary("下载当前激活模型文件")
            .WithDescription("将当前在线推理使用的模型文件以二进制流形式下载")
            .Produces(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet("/{versionId:guid}/download", DownloadModelByVersionAsync)
            .WithName("DownloadModelByVersion")
            .WithSummary("根据版本下载模型文件")
            .WithDescription("根据模型版本标识下载对应的模型文件")
            .Produces(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapPost("/import", ImportModelAsync)
            .WithName("ImportModel")
            .WithSummary("导入模型文件并注册版本")
            .WithDescription(@"支持通过 multipart/form-data 上传模型文件，并同步注册模型版本元数据。

**功能说明：**
- 上传文件通常为 ML.NET 导出的 .zip
- 支持自定义版本名称、部署槽位与流量占比
- 可选择是否立即激活导入的模型")
            .Accepts<ModelImportRequest>("multipart/form-data")
            .Produces<ModelImportResponse>(StatusCodes.Status201Created)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .DisableAntiforgery();
    }

    private static IResult DownloadFile(string modelPath, ILogger logger)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(modelPath))
            {
                return Results.NotFound(new ErrorResponse { Error = "模型文件路径未配置" });
            }

            if (!File.Exists(modelPath))
            {
                logger.LogWarning("模型文件不存在 => Path: {ModelPath}", modelPath);
                return Results.NotFound(new ErrorResponse { Error = $"模型文件不存在：{modelPath}" });
            }

            var stream = new FileStream(modelPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var fileName = Path.GetFileName(modelPath);
            return Results.File(stream, "application/octet-stream", fileName, enableRangeProcessing: true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "下载模型文件失败 => Path: {ModelPath}", modelPath);
            return Results.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError,
                title: "下载模型文件失败");
        }
    }

    private static IResult DownloadCurrentModelAsync(
        [FromServices] IOptionsMonitor<BarcodeMlModelOptions> optionsMonitor,
        [FromServices] ILogger<Program> logger)
    {
        var modelPath = optionsMonitor.CurrentValue.CurrentModelPath;
        return DownloadFile(modelPath, logger);
    }

    private static async Task<IResult> DownloadModelByVersionAsync(
        Guid versionId,
        [FromServices] IModelVersionService modelVersionService,
        [FromServices] ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var version = await modelVersionService.GetByIdAsync(versionId, cancellationToken);

            if (version is null)
            {
                return Results.NotFound(new ErrorResponse { Error = "指定的模型版本不存在" });
            }

            return DownloadFile(version.ModelPath, logger);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "下载模型文件时参数无效 => VersionId: {VersionId}", versionId);
            return Results.BadRequest(new ErrorResponse { Error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "根据版本下载模型文件失败 => VersionId: {VersionId}", versionId);
            return Results.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError,
                title: "下载模型文件失败");
        }
    }

    private static async Task<IResult> ImportModelAsync(
        [FromForm] ModelImportRequest request,
        [FromServices] IOptions<BarcodeReadabilityServiceSettings> serviceSettings,
        [FromServices] IModelVersionService modelVersionService,
        [FromServices] ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.BadRequest(new ErrorResponse { Error = "请求体不能为空" });
        }

        if (request.ModelFile is null || request.ModelFile.Length == 0)
        {
            return Results.BadRequest(new ErrorResponse { Error = "必须上传有效的模型文件" });
        }

        var baseDirectory = serviceSettings.Value.ModelPath;
        if (string.IsNullOrWhiteSpace(baseDirectory))
        {
            return Results.BadRequest(new ErrorResponse { Error = "服务未配置模型存储目录" });
        }

        try
        {
            Directory.CreateDirectory(baseDirectory);

            var sanitizedVersionName = SanitizeName(
                string.IsNullOrWhiteSpace(request.VersionName)
                    ? Path.GetFileNameWithoutExtension(request.ModelFile.FileName)
                    : request.VersionName);

            if (string.IsNullOrWhiteSpace(sanitizedVersionName))
            {
                sanitizedVersionName = $"imported-{DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)}";
            }

            var fileExtension = Path.GetExtension(request.ModelFile.FileName);
            if (string.IsNullOrWhiteSpace(fileExtension))
            {
                fileExtension = ".zip";
            }

            var targetFileName = $"{sanitizedVersionName}-{DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture)}{fileExtension}";
            var targetFilePath = Path.Combine(baseDirectory, targetFileName);

            await using var fileStream = new FileStream(targetFilePath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);
            await request.ModelFile.CopyToAsync(fileStream, cancellationToken);

            var registration = new ModelVersionRegistration
            {
                VersionName = sanitizedVersionName,
                ModelPath = targetFilePath,
                DeploymentSlot = string.IsNullOrWhiteSpace(request.DeploymentSlot)
                    ? "Production"
                    : request.DeploymentSlot!,
                TrafficPercentage = request.TrafficPercentage,
                Notes = request.Notes,
                SetAsActive = request.SetAsActive
            };

            var version = await modelVersionService.RegisterAsync(registration, cancellationToken);

            logger.LogInformation("成功导入模型文件 => VersionId: {VersionId}, Path: {ModelPath}", version.VersionId, version.ModelPath);

            var response = new ModelImportResponse
            {
                VersionId = version.VersionId,
                VersionName = version.VersionName,
                ModelPath = version.ModelPath,
                IsActive = version.IsActive
            };

            return Results.Created($"/api/models/{version.VersionId}", response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "导入模型文件失败");
            return Results.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError,
                title: "导入模型文件失败");
        }
    }

    private static string SanitizeName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        var invalidChars = Path.GetInvalidFileNameChars();
        var buffer = name.Trim().ToCharArray();
        for (var i = 0; i < buffer.Length; i++)
        {
            var current = buffer[i];
            if (invalidChars.Contains(current))
            {
                buffer[i] = '-';
            }
        }

        return new string(buffer).Trim('-');
    }
}
