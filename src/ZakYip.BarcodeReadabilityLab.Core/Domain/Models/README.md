# Domain/Models

## 职责说明

此目录包含领域模型（Domain Models），表示业务核心概念和实体。

### 设计原则

- 使用 `record class` 定义不可变领域模型
- 使用 `required` 关键字标记必需属性
- 所有属性使用 `init` 访问器确保不可变性
- 优先使用 `decimal` 处理精确数值计算
- 启用可空引用类型，明确标记可空属性

### 示例

```csharp
namespace ZakYip.BarcodeReadabilityLab.Core.Domain.Models;

/// <summary>
/// 条码图片信息
/// </summary>
public record class BarcodeImage
{
    public required Guid Id { get; init; }
    public required string FilePath { get; init; }
    public required DateTime CapturedAt { get; init; }
    public required long FileSize { get; init; }
    public string? BarcodeType { get; init; }
}
```
