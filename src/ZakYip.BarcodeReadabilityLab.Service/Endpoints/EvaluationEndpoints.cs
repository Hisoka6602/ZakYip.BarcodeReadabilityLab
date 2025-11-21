using System.ComponentModel;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZakYip.BarcodeReadabilityLab.Application.Options;
using ZakYip.BarcodeReadabilityLab.Application.Services;
using ZakYip.BarcodeReadabilityLab.Core.Enums;
using ZakYip.BarcodeReadabilityLab.Service.Models;
using ZakYip.BarcodeReadabilityLab.Service.Models.Evaluation;

namespace ZakYip.BarcodeReadabilityLab.Service.Endpoints;

/// <summary>
/// 在线推理与评估相关 API 端点
/// </summary>
public static class EvaluationEndpoints
{
    /// <summary>
    /// 注册评估端点
    /// </summary>
    public static void MapEvaluationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/evaluation")
            .WithTags("Evaluation");

        group.MapPost("/analyze-single", AnalyzeSingleAsync)
            .WithName("AnalyzeSingle")
            .WithSummary("单张图片在线推理")
            .WithDescription(@"上传单张图片进行在线推理分析，可选传入预期标签验证准确性。

**请求参数：**
- imageFile (必填): 单张图片文件 (jpg/jpeg/png/bmp)
- expectedLabel (可选): 预期的分类标签，如 'Truncated', 'BlurryOrOutOfFocus' 等
- returnRawScores (可选): 是否返回各类别的原始概率分布，默认 false

**响应说明：**
- predictedLabel: 预测的标签枚举名称
- predictedLabelDisplayName: 标签的中文描述
- confidence: 置信度 (0.0 到 1.0)
- isCorrect: 预测是否正确（仅当提供 expectedLabel 时有值）
- noreadReasonScores: 各类别的原始概率分布（仅当 returnRawScores=true 时返回）")
            .Accepts<IFormFile>("multipart/form-data")
            .Produces<EvaluateSingleResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .DisableAntiforgery();

        group.MapPost("/analyze-batch", AnalyzeBatchAsync)
            .WithName("AnalyzeBatch")
            .WithSummary("批量图片在线推理")
            .WithDescription(@"批量上传图片进行在线推理分析，可选传入标签映射进行批量验证。

**请求参数：**
- imageFiles (必填): 多张图片文件数组 (jpg/jpeg/png/bmp)
- labelsJson (可选): JSON 格式的文件名到标签的映射，如: {""img1.jpg"": ""Truncated"", ""img2.jpg"": ""BlurryOrOutOfFocus""}
- returnRawScores (可选): 是否返回各类别的原始概率分布，默认 false

**响应说明：**
- items: 单个图片的评估结果列表
- summary: 聚合统计信息
  - total: 总样本数
  - withExpectedLabel: 包含预期标签的样本数
  - correctCount: 预测正确的样本数
  - accuracy: 准确率（仅针对有预期标签的样本）
  - macroF1: 宏平均 F1 分数
  - microF1: 微平均 F1 分数")
            .Accepts<IFormFileCollection>("multipart/form-data")
            .Produces<EvaluateBatchResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .DisableAntiforgery();
    }

    /// <summary>
    /// 单张图片分析
    /// </summary>
    private static async Task<IResult> AnalyzeSingleAsync(
        [FromServices] IImageEvaluationService evaluationService,
        [FromServices] ILogger<Program> logger,
        [FromServices] IOptions<EvaluationOptions> options,
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // 验证请求
            if (!request.HasFormContentType)
            {
                return Results.BadRequest(new ErrorResponse
                {
                    Error = "请求必须使用 multipart/form-data 格式"
                });
            }

            var form = await request.ReadFormAsync(cancellationToken);

            // 获取图片文件
            var imageFile = form.Files.GetFile("imageFile");
            if (imageFile is null || imageFile.Length == 0)
            {
                return Results.BadRequest(new ErrorResponse
                {
                    Error = "请提供有效的图片文件（参数名: imageFile）"
                });
            }

            // 验证文件大小
            if (imageFile.Length > options.Value.MaxImageSizeBytes)
            {
                return Results.BadRequest(new ErrorResponse
                {
                    Error = $"图片文件大小超过限制（最大 {options.Value.MaxImageSizeBytes / 1024 / 1024} MB）"
                });
            }

            // 验证文件扩展名
            var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
            if (!options.Value.AllowedExtensions.Contains(extension))
            {
                return Results.BadRequest(new ErrorResponse
                {
                    Error = $"不支持的文件类型: {extension}。支持的类型: {string.Join(", ", options.Value.AllowedExtensions)}"
                });
            }

            // 获取可选参数
            var expectedLabelStr = form["expectedLabel"].ToString();
            NoreadReason? expectedLabel = null;
            if (!string.IsNullOrWhiteSpace(expectedLabelStr))
            {
                if (!Enum.TryParse<NoreadReason>(expectedLabelStr, ignoreCase: true, out var parsedLabel))
                {
                    return Results.BadRequest(new ErrorResponse
                    {
                        Error = $"无效的 expectedLabel 值: {expectedLabelStr}。有效值: {string.Join(", ", Enum.GetNames<NoreadReason>())}"
                    });
                }
                expectedLabel = parsedLabel;
            }

            var returnRawScoresStr = form["returnRawScores"].ToString();
            var returnRawScores = bool.TryParse(returnRawScoresStr, out var parsed) && parsed;

            // 执行评估
            await using var stream = imageFile.OpenReadStream();
            var result = await evaluationService.EvaluateSingleAsync(
                stream,
                imageFile.FileName,
                expectedLabel,
                returnRawScores,
                cancellationToken);

            // 构建响应
            var response = new EvaluateSingleResponse
            {
                PredictedLabel = result.PredictedLabel.ToString(),
                PredictedLabelDisplayName = GetEnumDescription(result.PredictedLabel),
                Confidence = result.Confidence,
                IsCorrect = result.IsCorrect,
                ExpectedLabel = result.ExpectedLabel?.ToString(),
                NoreadReasonScores = result.NoreadReasonScores?
                    .ToDictionary(
                        kvp => kvp.Key.ToString(),
                        kvp => kvp.Value)
            };

            return Results.Ok(response);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "单张图片分析参数验证失败");
            return Results.BadRequest(new ErrorResponse { Error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "单张图片分析失败");
            return Results.Problem(
                detail: $"分析失败: {ex.Message}",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// 批量图片分析
    /// </summary>
    private static async Task<IResult> AnalyzeBatchAsync(
        [FromServices] IImageEvaluationService evaluationService,
        [FromServices] ILogger<Program> logger,
        [FromServices] IOptions<EvaluationOptions> options,
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // 验证请求
            if (!request.HasFormContentType)
            {
                return Results.BadRequest(new ErrorResponse
                {
                    Error = "请求必须使用 multipart/form-data 格式"
                });
            }

            var form = await request.ReadFormAsync(cancellationToken);

            // 获取图片文件列表
            var imageFiles = form.Files.GetFiles("imageFiles");
            if (imageFiles is null || imageFiles.Count == 0)
            {
                return Results.BadRequest(new ErrorResponse
                {
                    Error = "请提供至少一张图片文件（参数名: imageFiles）"
                });
            }

            // 验证文件数量
            if (imageFiles.Count > options.Value.MaxImageCount)
            {
                return Results.BadRequest(new ErrorResponse
                {
                    Error = $"图片数量超过限制（最大 {options.Value.MaxImageCount} 张）"
                });
            }

            // 解析标签映射（可选）
            var labelsJsonStr = form["labelsJson"].ToString();
            Dictionary<string, NoreadReason>? labelsMap = null;
            if (!string.IsNullOrWhiteSpace(labelsJsonStr))
            {
                try
                {
                    var rawMap = JsonSerializer.Deserialize<Dictionary<string, string>>(labelsJsonStr);
                    if (rawMap is not null)
                    {
                        labelsMap = new Dictionary<string, NoreadReason>();
                        foreach (var (fileName, labelStr) in rawMap)
                        {
                            if (Enum.TryParse<NoreadReason>(labelStr, ignoreCase: true, out var label))
                            {
                                labelsMap[fileName] = label;
                            }
                            else
                            {
                                return Results.BadRequest(new ErrorResponse
                                {
                                    Error = $"无效的标签值: {labelStr}（文件: {fileName}）。有效值: {string.Join(", ", Enum.GetNames<NoreadReason>())}"
                                });
                            }
                        }
                    }
                }
                catch (JsonException ex)
                {
                    return Results.BadRequest(new ErrorResponse
                    {
                        Error = $"labelsJson 格式无效: {ex.Message}"
                    });
                }
            }

            var returnRawScoresStr = form["returnRawScores"].ToString();
            var returnRawScores = bool.TryParse(returnRawScoresStr, out var parsed) && parsed;

            // 准备图片流列表
            var imageList = new List<(Stream Stream, string FileName, NoreadReason? ExpectedLabel)>();

            foreach (var imageFile in imageFiles)
            {
                // 验证文件大小
                if (imageFile.Length > options.Value.MaxImageSizeBytes)
                {
                    return Results.BadRequest(new ErrorResponse
                    {
                        Error = $"图片文件 {imageFile.FileName} 大小超过限制（最大 {options.Value.MaxImageSizeBytes / 1024 / 1024} MB）"
                    });
                }

                // 验证文件扩展名
                var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                if (!options.Value.AllowedExtensions.Contains(extension))
                {
                    return Results.BadRequest(new ErrorResponse
                    {
                        Error = $"不支持的文件类型: {extension}（文件: {imageFile.FileName}）。支持的类型: {string.Join(", ", options.Value.AllowedExtensions)}"
                    });
                }

                // 获取预期标签
                var expectedLabel = labelsMap?.GetValueOrDefault(imageFile.FileName);

                // 打开流
                var stream = imageFile.OpenReadStream();
                imageList.Add((stream, imageFile.FileName, expectedLabel));
            }

            try
            {
                // 执行批量评估
                var batchResult = await evaluationService.EvaluateBatchAsync(
                    imageList,
                    returnRawScores,
                    cancellationToken);

                // 构建响应
                var response = new EvaluateBatchResponse
                {
                    Items = batchResult.Items.Select(item => new EvaluationItemResponse
                    {
                        FileName = item.FileName,
                        PredictedLabel = item.Result.PredictedLabel.ToString(),
                        PredictedLabelDisplayName = GetEnumDescription(item.Result.PredictedLabel),
                        Confidence = item.Result.Confidence,
                        ExpectedLabel = item.Result.ExpectedLabel?.ToString(),
                        IsCorrect = item.Result.IsCorrect
                    }).ToList(),
                    Summary = new EvaluationSummaryResponse
                    {
                        Total = batchResult.Summary.Total,
                        WithExpectedLabel = batchResult.Summary.WithExpectedLabel,
                        CorrectCount = batchResult.Summary.CorrectCount,
                        Accuracy = batchResult.Summary.Accuracy,
                        MacroF1 = batchResult.Summary.MacroF1,
                        MicroF1 = batchResult.Summary.MicroF1
                    }
                };

                return Results.Ok(response);
            }
            finally
            {
                // 清理流
                foreach (var (stream, _, _) in imageList)
                {
                    await stream.DisposeAsync();
                }
            }
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "批量图片分析参数验证失败");
            return Results.BadRequest(new ErrorResponse { Error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "批量图片分析失败");
            return Results.Problem(
                detail: $"分析失败: {ex.Message}",
                statusCode: StatusCodes.Status500InternalServerError);
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
