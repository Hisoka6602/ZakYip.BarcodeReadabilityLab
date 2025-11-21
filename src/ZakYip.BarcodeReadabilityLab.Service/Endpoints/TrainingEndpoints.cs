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
using ZakYip.BarcodeReadabilityLab.Core.Domain.Exceptions;
using ZakYip.BarcodeReadabilityLab.Core.Domain.Models;
using ZakYip.BarcodeReadabilityLab.Core.Enums;
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
            .WithDescription(@"触发一次基于目录的训练任务。

**功能说明：**
- 如果请求体中未提供参数，则使用配置文件中的默认 TrainingOptions
- 训练数据目录应包含按类别组织的子目录（如 readable、unreadable）
- 支持自定义验证集分割比例
- 可添加备注说明便于管理历史任务

**返回值：**
- 成功时返回训练任务 ID，可用于后续查询任务状态")
            .Produces<StartTrainingResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet("/status/{jobId:guid}", GetTrainingStatusAsync)
            .WithName("GetTrainingStatus")
            .WithSummary("查询训练任务状态")
            .WithDescription(@"根据 jobId 查询训练任务的当前状态与进度信息。

**功能说明：**
- 返回任务的实时状态（排队中、运行中、已完成、失败、已取消）
- 包含训练进度百分比（0.0 到 1.0）
- 完成后提供模型评估指标

**状态说明：**
- 排队中：任务已创建，等待执行
- 运行中：任务正在训练模型
- 已完成：训练成功完成
- 失败：训练过程中发生错误
- 已取消：任务被用户取消")
            .Produces<TrainingJobResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet("/history", GetTrainingHistoryAsync)
            .WithName("GetTrainingHistory")
            .WithSummary("获取训练任务历史")
            .WithDescription(@"获取所有训练任务的历史记录，按开始时间降序排列。

**功能说明：**
- 返回所有历史训练任务列表
- 包含每个任务的完整状态信息
- 可用于分析和跟踪训练历史")
            .Produces<List<TrainingJobResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapPost("/incremental-start", StartIncrementalTrainingAsync)
            .WithName("StartIncrementalTraining")
            .WithSummary("启动增量训练任务")
            .WithDescription(@"基于指定模型版本与新增样本目录执行增量训练。

**功能说明：**
- 基于已有模型版本进行增量训练
- 支持合并历史训练数据或仅使用新数据
- 支持数据增强和数据平衡
- 自动建立模型版本谱系关系

**返回值：**
- 成功时返回训练任务 ID，可用于后续查询任务状态")
            .Produces<StartTrainingResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapPost("/transfer-learning/start", StartTransferLearningTrainingAsync)
            .WithName("StartTransferLearningTraining")
            .WithSummary("启动迁移学习训练任务")
            .WithDescription(@"使用预训练模型启动迁移学习训练任务。

**功能说明：**
- 支持使用预训练模型（ResNet、InceptionV3、EfficientNet等）进行迁移学习
- 支持层冻结策略：全部冻结、部分冻结、全部解冻
- 支持多阶段训练策略
- 可配置数据增强和数据平衡

**返回值：**
- 成功时返回训练任务 ID，可用于后续查询任务状态")
            .Produces<StartTrainingResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
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
                LearningRate = request?.LearningRate ?? defaultOptions.LearningRate,
                Epochs = request?.Epochs ?? defaultOptions.Epochs,
                BatchSize = request?.BatchSize ?? defaultOptions.BatchSize,
                Remarks = request?.Remarks,
                DataAugmentation = request?.DataAugmentation ?? (defaultOptions.DataAugmentation with { }),
                DataBalancing = request?.DataBalancing ?? (defaultOptions.DataBalancing with { })
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
                LearningRate = status.LearningRate,
                Epochs = status.Epochs,
                BatchSize = status.BatchSize,
                Message = status.Status switch
                {
                    Core.Enums.TrainingJobState.Queued => "训练任务排队中",
                    Core.Enums.TrainingJobState.Running => "训练任务正在执行",
                    Core.Enums.TrainingJobState.Completed => "训练任务已完成",
                    Core.Enums.TrainingJobState.Failed => $"训练任务失败: {status.ErrorMessage}",
                    Core.Enums.TrainingJobState.Cancelled => "训练任务已取消",
                    _ => "未知状态"
                },
                StartTime = status.StartTime,
                CompletedTime = status.CompletedTime,
                ErrorMessage = status.ErrorMessage,
                Remarks = status.Remarks,
                DataAugmentation = status.DataAugmentation,
                DataBalancing = status.DataBalancing,
                EvaluationMetrics = status.EvaluationMetrics
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
    /// 获取训练任务历史
    /// </summary>
    private static async Task<IResult> GetTrainingHistoryAsync(
        [FromServices] ITrainingJobService trainingJobService,
        [FromServices] ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var allJobs = await trainingJobService.GetAllAsync(cancellationToken);

            var response = allJobs.Select(status => new TrainingJobResponse
            {
                JobId = status.JobId,
                State = GetEnumDescription(status.Status),
                Progress = status.Progress,
                LearningRate = status.LearningRate,
                Epochs = status.Epochs,
                BatchSize = status.BatchSize,
                Message = status.Status switch
                {
                    Core.Enums.TrainingJobState.Queued => "训练任务排队中",
                    Core.Enums.TrainingJobState.Running => "训练任务正在执行",
                    Core.Enums.TrainingJobState.Completed => "训练任务已完成",
                    Core.Enums.TrainingJobState.Failed => $"训练任务失败: {status.ErrorMessage}",
                    Core.Enums.TrainingJobState.Cancelled => "训练任务已取消",
                    _ => "未知状态"
                },
                StartTime = status.StartTime,
                CompletedTime = status.CompletedTime,
                ErrorMessage = status.ErrorMessage,
                Remarks = status.Remarks,
                DataAugmentation = status.DataAugmentation,
                DataBalancing = status.DataBalancing,
                EvaluationMetrics = status.EvaluationMetrics
            }).ToList();

            logger.LogInformation("返回 {Count} 条训练任务历史记录", response.Count);

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "获取训练任务历史失败");
            return Results.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError,
                title: "获取训练任务历史失败");
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

    /// <summary>
    /// 启动增量训练任务
    /// </summary>
    private static async Task<IResult> StartIncrementalTrainingAsync(
        [FromBody] IncrementalTrainingStartRequest? request,
        [FromServices] ITrainingJobService trainingJobService,
        [FromServices] IOptions<TrainingOptions> trainingOptions,
        [FromServices] ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            if (request is null)
            {
                logger.LogWarning("增量训练请求体为空");
                return Results.BadRequest(new ErrorResponse { Error = "请求体不能为空" });
            }

            var defaultOptions = trainingOptions.Value;

            var incrementalRequest = new IncrementalTrainingRequest
            {
                BaseModelVersionId = request.BaseModelVersionId,
                TrainingRootDirectory = request.TrainingRootDirectory,
                OutputModelDirectory = request.OutputModelDirectory,
                MergeWithHistoricalData = request.MergeWithHistoricalData,
                ValidationSplitRatio = request.ValidationSplitRatio ?? defaultOptions.ValidationSplitRatio,
                LearningRate = request.LearningRate ?? defaultOptions.LearningRate,
                Epochs = request.Epochs ?? defaultOptions.Epochs,
                BatchSize = request.BatchSize ?? defaultOptions.BatchSize,
                Remarks = request.Remarks,
                DataAugmentation = request.DataAugmentation ?? (defaultOptions.DataAugmentation with { }),
                DataBalancing = request.DataBalancing ?? (defaultOptions.DataBalancing with { })
            };

            logger.LogInformation(
                "收到增量训练任务请求，基础模型版本: {BaseModelVersionId}, 训练目录: {TrainingRootDirectory}, 合并历史数据: {MergeWithHistoricalData}",
                incrementalRequest.BaseModelVersionId,
                incrementalRequest.TrainingRootDirectory,
                incrementalRequest.MergeWithHistoricalData);

            var jobId = await trainingJobService.StartIncrementalTrainingAsync(incrementalRequest, cancellationToken);

            logger.LogInformation("增量训练任务已创建，JobId: {JobId}", jobId);

            var response = new StartTrainingResponse
            {
                JobId = jobId,
                Message = "增量训练任务已创建并加入队列",
                JobType = "Incremental",
                BaseModelVersionId = request.BaseModelVersionId
            };

            return Results.Ok(response);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "增量训练请求参数无效");
            return Results.BadRequest(new ErrorResponse { Error = ex.Message });
        }
        catch (DirectoryNotFoundException ex)
        {
            logger.LogWarning(ex, "训练目录不存在");
            return Results.BadRequest(new ErrorResponse { Error = ex.Message });
        }
        catch (TrainingException ex) when (ex.ErrorCode == "BASE_MODEL_NOT_FOUND")
        {
            logger.LogWarning(ex, "基础模型版本不存在");
            return Results.NotFound(new ErrorResponse { Error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "启动增量训练任务失败");
            return Results.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError,
                title: "启动增量训练任务失败");
        }
    }

    /// <summary>
    /// 启动迁移学习训练任务
    /// </summary>
    private static async Task<IResult> StartTransferLearningTrainingAsync(
        [FromBody] TransferLearningRequest? request,
        [FromServices] ITrainingJobService trainingJobService,
        [FromServices] IOptions<TrainingOptions> trainingOptions,
        [FromServices] ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var defaultOptions = trainingOptions.Value;

            // 构建迁移学习选项
            var transferLearningOptions = new TransferLearningOptions
            {
                Enable = true,
                PretrainedModelType = request?.PretrainedModelType ?? PretrainedModelType.ResNet50,
                LayerFreezeStrategy = request?.LayerFreezeStrategy ?? LayerFreezeStrategy.FreezeAll,
                UnfreezeLayersPercentage = request?.UnfreezeLayersPercentage ?? 0.3m,
                EnableMultiStageTraining = request?.EnableMultiStageTraining ?? false,
                TrainingPhases = request?.TrainingPhases?.Select(p => new MultiStageTrainingPhase
                {
                    PhaseName = p.PhaseName,
                    PhaseNumber = p.PhaseNumber,
                    Epochs = p.Epochs,
                    LearningRate = p.LearningRate,
                    LayerFreezeStrategy = p.LayerFreezeStrategy,
                    UnfreezeLayersPercentage = p.UnfreezeLayersPercentage,
                    Description = p.Description
                }).ToList()
            };

            var trainingRequest = new TrainingRequest
            {
                TrainingRootDirectory = request?.TrainingRootDirectory ?? defaultOptions.TrainingRootDirectory,
                OutputModelDirectory = request?.OutputModelDirectory ?? defaultOptions.OutputModelDirectory,
                ValidationSplitRatio = request?.ValidationSplitRatio ?? defaultOptions.ValidationSplitRatio,
                LearningRate = request?.LearningRate ?? defaultOptions.LearningRate,
                Epochs = request?.Epochs ?? defaultOptions.Epochs,
                BatchSize = request?.BatchSize ?? defaultOptions.BatchSize,
                Remarks = request?.Remarks,
                DataAugmentation = request?.DataAugmentation,
                DataBalancing = request?.DataBalancing,
                TransferLearningOptions = transferLearningOptions
            };

            logger.LogInformation(
                "接收到迁移学习训练请求 => 预训练模型: {PretrainedModel}, 冻结策略: {FreezeStrategy}",
                transferLearningOptions.PretrainedModelType,
                transferLearningOptions.LayerFreezeStrategy);

            var jobId = await trainingJobService.StartTrainingAsync(trainingRequest, cancellationToken);

            return Results.Ok(new StartTrainingResponse
            {
                JobId = jobId,
                Message = "迁移学习训练任务已创建并加入队列"
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "启动迁移学习训练任务失败");
            return Results.Problem(
                detail: $"启动迁移学习训练任务失败: {ex.Message}",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}
