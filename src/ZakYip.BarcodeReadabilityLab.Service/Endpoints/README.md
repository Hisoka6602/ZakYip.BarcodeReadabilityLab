# Service/Endpoints

## 职责说明

此目录包含 Minimal API 端点定义，提供 HTTP API 接口。

### 设计原则

- 使用 Minimal API 风格定义端点
- 端点方法保持简洁，业务逻辑委托给应用服务
- 使用 `async`/`await` 模式
- 返回结果使用 `Results` 辅助类
- API 文档注释使用中文

### 示例

```csharp
namespace ZakYip.BarcodeReadabilityLab.Service.Endpoints;

/// <summary>
/// 训练相关 API 端点
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
            .WithOpenApi();
        
        group.MapGet("/status/{taskId:guid}", GetTrainingStatusAsync)
            .WithName("GetTrainingStatus")
            .WithOpenApi();
    }
    
    /// <summary>
    /// 启动训练任务
    /// </summary>
    private static async Task<IResult> StartTrainingAsync(
        [FromBody] StartTrainingRequest? request,
        [FromServices] ITrainingJobService trainingJobService,
        [FromServices] IOptions<TrainingOptions> trainingOptions,
        CancellationToken cancellationToken)
    {
        var defaults = trainingOptions.Value;
        var trainingRequest = new TrainingRequest
        {
            TrainingRootDirectory = request?.TrainingRootDirectory ?? defaults.TrainingRootDirectory,
            OutputModelDirectory = request?.OutputModelDirectory ?? defaults.OutputModelDirectory,
            ValidationSplitRatio = request?.ValidationSplitRatio ?? defaults.ValidationSplitRatio,
            LearningRate = request?.LearningRate ?? defaults.LearningRate,
            Epochs = request?.Epochs ?? defaults.Epochs,
            BatchSize = request?.BatchSize ?? defaults.BatchSize,
            Remarks = request?.Remarks,
            DataAugmentation = request?.DataAugmentation ?? (defaults.DataAugmentation with { }),
            DataBalancing = request?.DataBalancing ?? (defaults.DataBalancing with { })
        };

        var jobId = await trainingJobService.StartTrainingAsync(trainingRequest, cancellationToken);
        return Results.Ok(new { JobId = jobId });
    }

    /// <summary>
    /// 获取训练状态
    /// </summary>
    private static async Task<IResult> GetTrainingStatusAsync(
        Guid jobId,
        [FromServices] ITrainingJobService trainingJobService,
        CancellationToken cancellationToken)
    {
        var status = await trainingJobService.GetStatusAsync(jobId, cancellationToken);
        return status is not null
            ? Results.Ok(status)
            : Results.NotFound();
    }
}
```
