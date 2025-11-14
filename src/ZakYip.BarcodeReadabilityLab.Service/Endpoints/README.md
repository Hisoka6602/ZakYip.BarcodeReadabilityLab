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
        [FromBody] StartTrainingRequest request,
        [FromServices] ITrainingService trainingService,
        CancellationToken cancellationToken)
    {
        try
        {
            var taskId = await trainingService.StartAsync(request, cancellationToken);
            return Results.Ok(new { TaskId = taskId });
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { Error = ex.Message });
        }
    }
    
    /// <summary>
    /// 获取训练状态
    /// </summary>
    private static async Task<IResult> GetTrainingStatusAsync(
        Guid taskId,
        [FromServices] ITrainingService trainingService,
        CancellationToken cancellationToken)
    {
        var status = await trainingService.GetStatusAsync(taskId, cancellationToken);
        return status is not null 
            ? Results.Ok(status) 
            : Results.NotFound();
    }
}
```
