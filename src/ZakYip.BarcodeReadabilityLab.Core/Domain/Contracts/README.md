# Domain/Contracts

## 职责说明

此目录包含领域契约（Domain Contracts），定义核心业务接口和抽象。

### 设计原则

- 定义服务接口（如仓储、领域服务等）
- **Core 层不依赖任何基础设施实现**
- 接口名使用 `I` 前缀，采用 PascalCase 命名
- 方法返回值明确标记可空性
- 使用异步方法时返回 `Task` 或 `Task<T>`

### 示例

```csharp
namespace ZakYip.BarcodeReadabilityLab.Core.Domain.Contracts;

/// <summary>
/// 图片仓储接口
/// </summary>
public interface IImageRepository
{
    /// <summary>
    /// 根据 ID 获取图片信息
    /// </summary>
    Task<BarcodeImage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 保存图片信息
    /// </summary>
    Task SaveAsync(BarcodeImage image, CancellationToken cancellationToken = default);
}
```
