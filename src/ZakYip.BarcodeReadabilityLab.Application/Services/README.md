# Application/Services

## 职责说明

此目录包含应用服务（Application Services），协调领域逻辑和基础设施。

### 设计原则

- 实现应用层业务逻辑编排
- 依赖 Core 层接口，不直接依赖基础设施实现
- 方法保持专注且小巧，一个方法一个职责
- 使用依赖注入获取所需服务
- 异常消息使用中文

### 示例

```csharp
namespace ZakYip.BarcodeReadabilityLab.Application.Services;

/// <summary>
/// 图片处理服务
/// </summary>
public class ImageProcessingService
{
    private readonly IImageRepository _imageRepository;
    private readonly ILogger<ImageProcessingService> _logger;
    
    public ImageProcessingService(
        IImageRepository imageRepository,
        ILogger<ImageProcessingService> logger)
    {
        _imageRepository = imageRepository;
        _logger = logger;
    }
    
    /// <summary>
    /// 处理图片
    /// </summary>
    public async Task<ProcessingResult> ProcessAsync(
        string imagePath, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("开始处理图片：{ImagePath}", imagePath);
        
        ValidateImagePath(imagePath);
        var image = await LoadImageAsync(imagePath, cancellationToken);
        
        return await AnalyzeImageAsync(image, cancellationToken);
    }
    
    private void ValidateImagePath(string imagePath)
    {
        if (string.IsNullOrEmpty(imagePath))
            throw new ArgumentException("图片路径不能为空", nameof(imagePath));
    }
}
```

## 已实现的服务

### IUnresolvedImageRouter

**实现类**: `UnresolvedImageRouter`

**职责**: 根据分析结果决定是否将图片复制到"无法分析"目录。

**触发条件** (满足任意一条即复制):
1. `BarcodeAnalysisResult.IsAnalyzed` 为 `false`
2. `BarcodeAnalysisResult.Confidence` 为 `null`
3. `BarcodeAnalysisResult.Confidence` 小于 `BarcodeAnalyzerOptions.ConfidenceThreshold`

**特性**:
- 按日期分层存储: `UnresolvedDirectory/yyyyMMdd/`
- 自动处理文件名冲突（附加时间戳 `HHmmss_fff`）
- 异步文件复制操作
- 完整的中文日志记录（源路径、目标路径、原因）

### IDirectoryMonitoringService

**实现类**: `DirectoryMonitoringService`

**职责**: 监控目录并自动处理新图片文件。

**工作流程**:
1. 使用 `FileSystemWatcher` 监控 `WatchDirectory`
2. 检测到新图片文件（支持 jpg, jpeg, png, bmp, gif, tif, tiff）
3. 等待文件写入完成
4. 构造 `BarcodeSample` 对象
5. 调用 `IBarcodeReadabilityAnalyzer.AnalyzeAsync` 进行分析
6. 根据 `ConfidenceThreshold` 设置 `IsAboveThreshold` 字段
7. 根据条件调用 `IUnresolvedImageRouter` 路由图片

**特性**:
- 支持递归监控子目录（根据 `IsRecursive` 配置）
- 防止重复处理同一文件
- 单个文件出错不影响其他文件处理
- 完整的异常处理和日志记录
- 实现 `IDisposable` 接口，正确释放资源

### 使用示例

#### 服务注册

```csharp
using ZakYip.BarcodeReadabilityLab.Application.Extensions;

// 在 Program.cs 或 Startup.cs 中
services.AddBarcodeAnalyzerServices();

// 配置 BarcodeAnalyzerOptions
services.Configure<BarcodeAnalyzerOptions>(
    configuration.GetSection("BarcodeAnalyzer"));
```

#### 配置示例 (appsettings.json)

```json
{
  "BarcodeAnalyzer": {
    "WatchDirectory": "C:\\Images\\Watch",
    "UnresolvedDirectory": "C:\\Images\\Unresolved",
    "ConfidenceThreshold": 0.90,
    "IsRecursive": false
  }
}
```

#### 启动监控服务

```csharp
public class Worker : BackgroundService
{
    private readonly IDirectoryMonitoringService _monitoringService;

    public Worker(IDirectoryMonitoringService monitoringService)
    {
        _monitoringService = monitoringService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _monitoringService.StartAsync(stoppingToken);
        
        await Task.Delay(Timeout.Infinite, stoppingToken);
        
        await _monitoringService.StopAsync(stoppingToken);
    }
}
```
