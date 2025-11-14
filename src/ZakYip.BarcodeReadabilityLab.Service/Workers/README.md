# Service/Workers

## 职责说明

此目录包含后台工作服务（Worker Services），执行长时间运行的后台任务。

### 设计原则

- 继承 `BackgroundService` 或实现 `IHostedService`
- 使用依赖注入获取所需服务
- 支持优雅关闭（CancellationToken）
- 日志消息使用中文
- 异常处理要完善

### 示例

```csharp
namespace ZakYip.BarcodeReadabilityLab.Service.Workers;

/// <summary>
/// 图片监控后台服务
/// </summary>
public class ImageMonitoringWorker : BackgroundService
{
    private readonly ILogger<ImageMonitoringWorker> _logger;
    private readonly IImageProcessingService _processingService;
    
    public ImageMonitoringWorker(
        ILogger<ImageMonitoringWorker> logger,
        IImageProcessingService processingService)
    {
        _logger = logger;
        _processingService = processingService;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("图片监控服务启动");
        
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await ProcessImagesAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("图片监控服务正在停止");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "图片监控服务发生错误：{Message}", ex.Message);
            throw;
        }
    }
    
    private async Task ProcessImagesAsync(CancellationToken cancellationToken)
    {
        // 处理逻辑
        await Task.CompletedTask;
    }
}
```
