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
