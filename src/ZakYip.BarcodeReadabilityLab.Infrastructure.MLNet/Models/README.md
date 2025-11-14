# Infrastructure.MLNet/Models

## 职责说明

此目录包含 ML.NET 相关的数据模型，用于机器学习训练和预测。

### 设计原则

- 使用 `record class` 定义数据模型
- 使用 `required` 关键字标记必需属性
- ML.NET 模型输入/输出可能需要使用 `float` 类型
- 添加 ML.NET 特性标注（如 `[LoadColumn]`、`[ColumnName]`）
- 遵循 ML.NET 框架的命名约定

### 示例

```csharp
namespace ZakYip.BarcodeReadabilityLab.Infrastructure.MLNet.Models;

using Microsoft.ML.Data;

/// <summary>
/// 图片数据输入模型
/// </summary>
public record class ImageInputData
{
    /// <summary>
    /// 图片路径
    /// </summary>
    [LoadColumn(0)]
    public required string ImagePath { get; init; }
    
    /// <summary>
    /// 标签（可读/不可读）
    /// </summary>
    [LoadColumn(1)]
    public required string Label { get; init; }
}

/// <summary>
/// 预测结果模型
/// </summary>
public record class ImagePredictionResult
{
    /// <summary>
    /// 预测标签
    /// </summary>
    [ColumnName("PredictedLabel")]
    public required string PredictedLabel { get; init; }
    
    /// <summary>
    /// 置信度分数
    /// </summary>
    [ColumnName("Score")]
    public float[] Scores { get; init; } = Array.Empty<float>();
}
```
