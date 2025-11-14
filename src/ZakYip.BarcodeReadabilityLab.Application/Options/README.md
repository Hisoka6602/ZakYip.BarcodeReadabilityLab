# Application/Options

## 职责说明

此目录包含应用配置选项（Options），用于存储配置信息。

### 设计原则

- 使用 `record class` 定义配置选项
- 使用 `required` 关键字标记必需配置
- 配置类名使用 `Options` 或 `Settings` 后缀
- 启用可空引用类型，明确标记可选配置
- 配合 `IOptions<T>` 模式使用

### 示例

```csharp
namespace ZakYip.BarcodeReadabilityLab.Application.Options;

/// <summary>
/// 图片处理配置选项
/// </summary>
public record class ImageProcessingOptions
{
    /// <summary>
    /// 最大图片大小（字节）
    /// </summary>
    public required long MaxFileSize { get; init; }
    
    /// <summary>
    /// 支持的图片格式
    /// </summary>
    public required string[] SupportedFormats { get; init; }
    
    /// <summary>
    /// 处理超时时间（秒）
    /// </summary>
    public int TimeoutSeconds { get; init; } = 30;
    
    /// <summary>
    /// 是否启用并行处理
    /// </summary>
    public bool IsParallelProcessingEnabled { get; init; } = true;
}
```
