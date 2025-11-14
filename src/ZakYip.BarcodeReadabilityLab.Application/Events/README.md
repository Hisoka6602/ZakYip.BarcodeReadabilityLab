# Application/Events

## 职责说明

此目录包含应用事件（Application Events），用于事件驱动架构。

### 设计原则

- 事件参数使用 `record struct` 或 `record class`
- 小型事件载荷优先使用 `readonly record struct`
- 事件类型名必须以 `EventArgs` 结尾
- 使用 `required` 关键字标记必需属性
- 事件应该是不可变的

### 示例

```csharp
namespace ZakYip.BarcodeReadabilityLab.Application.Events;

/// <summary>
/// 图片处理完成事件参数
/// </summary>
public readonly record struct ImageProcessedEventArgs(
    Guid ImageId,
    string ImagePath,
    bool IsSuccessful,
    DateTime ProcessedAt
);

/// <summary>
/// 训练任务完成事件参数
/// </summary>
public record class TrainingCompletedEventArgs
{
    public required Guid TaskId { get; init; }
    public required DateTime StartTime { get; init; }
    public required DateTime EndTime { get; init; }
    public required decimal Accuracy { get; init; }
    public required int TotalSamples { get; init; }
    public string? ErrorMessage { get; init; }
}
```
