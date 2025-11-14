# GitHub Copilot 编码规范

## 重要说明

**后续所有 PR 生成的代码都必须严格遵守本文档中定义的编码规范。**

本规范旨在确保代码库的一致性、可维护性和高质量。请在生成任何代码之前仔细阅读并理解这些规范。

---

## 1. 注释规范

- **所有注释必须使用简体中文**
- 包括但不限于：
  - XML 文档注释（`///`）
  - 行内注释（`//`）
  - 多行注释（`/* */`）
  - TODO、FIXME 等标记注释

**示例：**
```csharp
/// <summary>
/// 分析条码图片的可读性
/// </summary>
/// <param name="imagePath">图片文件路径</param>
/// <returns>分析结果</returns>
public AnalysisResult AnalyzeBarcode(string imagePath)
{
    // 验证图片路径是否有效
    if (string.IsNullOrEmpty(imagePath))
    {
        throw new ArgumentException("图片路径不能为空", nameof(imagePath));
    }
    
    // TODO: 实现具体的分析逻辑
    return new AnalysisResult();
}
```

---

## 2. 命名规范

### 2.1 基本原则

- **类名、接口名、字段名、变量名统一使用英文**
- **严禁使用中文或拼音命名**
- 使用清晰、有意义的英文单词或短语

### 2.2 具体规则

- **类名**：使用 PascalCase（如 `BarcodeAnalyzer`）
- **接口名**：使用 PascalCase 并以 `I` 开头（如 `IImageProcessor`）
- **方法名**：使用 PascalCase（如 `ProcessImage`）
- **属性名**：使用 PascalCase（如 `ImagePath`）
- **字段名**：
  - 私有字段使用 camelCase 并以 `_` 开头（如 `_imageProcessor`）
  - 公共字段使用 PascalCase（如 `MaxRetryCount`）
- **变量名**：使用 camelCase（如 `imagePath`、`retryCount`）
- **常量名**：使用 PascalCase（如 `MaxImageSize`）

**示例：**
```csharp
public class BarcodeAnalyzer : IImageProcessor
{
    private readonly ILogger _logger;
    private readonly string _modelPath;
    
    public const int MaxImageSize = 10485760; // 10MB
    
    public string ModelPath => _modelPath;
    
    public AnalysisResult Analyze(string imagePath)
    {
        var imageData = LoadImage(imagePath);
        return ProcessData(imageData);
    }
}
```

---

## 3. 数据模型规范

### 3.1 优先使用 record 或 record class

- 对于不可变数据模型，优先使用 `record` 或 `record class`
- `record` 提供值语义、自动实现相等性比较，适合表达数据传输对象（DTO）

### 3.2 使用 required 关键字

- 在 `record class` 中，对于不可空字段必须使用 `required` 关键字
- 确保对象初始化时必须提供所有必需的属性值

**示例：**
```csharp
// 推荐：使用 record class 表达不可变模型
public record class BarcodeImageInfo
{
    public required string FilePath { get; init; }
    public required DateTime CaptureTime { get; init; }
    public required long FileSize { get; init; }
    public string? BarcodeType { get; init; }
}

// 推荐：简单的值对象使用 record
public record AnalysisResult(string Label, decimal Confidence, DateTime AnalyzedAt);

// 避免：对于不可变模型使用传统 class
public class BarcodeImageInfo // ❌ 应使用 record class
{
    public string FilePath { get; set; }
    public DateTime CaptureTime { get; set; }
}
```

---

## 4. 枚举规范

### 4.1 Description 特性

- **所有枚举成员必须使用 `Description` 特性标记**
- Description 内容使用简体中文描述

### 4.2 添加中文注释

- 在枚举定义和每个成员上添加 XML 文档注释

**示例：**
```csharp
using System.ComponentModel;

/// <summary>
/// 条码读取失败的原因类型
/// </summary>
public enum NoReadReason
{
    /// <summary>
    /// 图片模糊
    /// </summary>
    [Description("图片模糊")]
    Blurry = 1,
    
    /// <summary>
    /// 光照不足
    /// </summary>
    [Description("光照不足")]
    InsufficientLight = 2,
    
    /// <summary>
    /// 条码损坏
    /// </summary>
    [Description("条码损坏")]
    Damaged = 3,
    
    /// <summary>
    /// 角度偏斜
    /// </summary>
    [Description("角度偏斜")]
    Skewed = 4
}
```

---

## 5. 布尔命名规范

- **布尔类型的字段和属性表示"是否状态"时，必须使用以下前缀之一：**
  - `Is`（表示状态）
  - `Has`（表示拥有）
  - `Can`（表示能力）
  - `Should`（表示建议）

**示例：**
```csharp
public record class ImageValidationResult
{
    public required bool IsValid { get; init; }
    public required bool HasBarcode { get; init; }
    public bool CanBeProcessed { get; init; }
    public bool ShouldRetry { get; init; }
}

public class ImageProcessor
{
    private bool _isInitialized;
    private bool _hasLoadedModel;
    
    public bool CanProcessImage(string path) => _isInitialized && _hasLoadedModel;
}
```

---

## 6. 数值类型规范

### 6.1 优先使用 decimal

- **涉及金额、百分比、精确计算时，优先使用 `decimal` 替代 `double`**
- 避免浮点数精度问题

### 6.2 例外情况

- 仅在以下情况使用 `double` 或 `float`：
  - 需要极端性能优化（如密集型科学计算）
  - 与外部 API 或库的接口要求必须使用浮点数
  - ML.NET 等框架明确要求使用 float

**示例：**
```csharp
public record class PricingInfo
{
    // 推荐：价格使用 decimal
    public required decimal UnitPrice { get; init; }
    public required decimal TotalAmount { get; init; }
    
    // 推荐：百分比使用 decimal
    public decimal DiscountRate { get; init; } = 0.0m;
}

public record class ImageQualityScore
{
    // 推荐：评分使用 decimal
    public required decimal Sharpness { get; init; }
    public required decimal Brightness { get; init; }
    
    // 例外：ML.NET 模型输出可能需要 float
    public float ModelConfidence { get; init; }
}
```

---

## 7. LINQ 使用规范

### 7.1 优先使用 LINQ

- 查询和数据操作逻辑优先使用 LINQ 表达式
- 提高代码可读性和简洁性

### 7.2 关注性能

- **避免不必要的枚举和 `ToList()` 调用**
- 理解延迟执行（Deferred Execution）和立即执行（Immediate Execution）的区别
- 避免在循环中重复枚举同一个集合

**示例：**
```csharp
// ✅ 推荐：使用 LINQ，避免不必要的 ToList()
public IEnumerable<string> GetValidImagePaths(IEnumerable<string> paths)
{
    return paths
        .Where(p => File.Exists(p))
        .Where(p => IsValidImageExtension(p));
}

// ❌ 避免：不必要的 ToList() 调用
public IEnumerable<string> GetValidImagePaths(IEnumerable<string> paths)
{
    return paths
        .Where(p => File.Exists(p))
        .ToList() // 不必要的物化
        .Where(p => IsValidImageExtension(p))
        .ToList(); // 第二次不必要的物化
}

// ✅ 推荐：需要多次枚举时才物化
public List<ProcessedImage> ProcessImages(IEnumerable<string> paths)
{
    // 第一次过滤，保持延迟执行
    var validPaths = paths.Where(p => File.Exists(p));
    
    // 需要多次访问，物化为 List
    var imagesToProcess = validPaths
        .Select(p => new ImageInfo(p))
        .ToList();
    
    // 多次使用已物化的集合
    LogImageCount(imagesToProcess.Count);
    return ProcessImageBatch(imagesToProcess);
}
```

---

## 8. 架构层次规范

### 8.1 严格划分结构层级边界

- **Core 层（领域核心）不依赖任何基础设施**
- Core 层不得引用：
  - Entity Framework Core
  - HttpClient
  - UI 框架（WPF、WinForms 等）
  - 任何特定的 I/O 或外部依赖库

### 8.2 依赖方向

- 依赖方向应始终从外层指向内层：
  - `Infrastructure` → `Core`
  - `Application` → `Core`
  - `Presentation` → `Application` → `Core`

**示例：**
```csharp
// ✅ Core 层：纯粹的领域模型和接口
namespace ZakYip.BarcodeReadabilityLab.Core.Domain
{
    public record class BarcodeImage
    {
        public required string Id { get; init; }
        public required byte[] ImageData { get; init; }
        public DateTime CreatedAt { get; init; }
    }
    
    // 定义接口，不依赖具体实现
    public interface IImageRepository
    {
        Task<BarcodeImage?> GetByIdAsync(string id);
        Task SaveAsync(BarcodeImage image);
    }
}

// ✅ Infrastructure 层：依赖 Core 层接口
namespace ZakYip.BarcodeReadabilityLab.Infrastructure.Persistence
{
    using ZakYip.BarcodeReadabilityLab.Core.Domain;
    using Microsoft.EntityFrameworkCore;
    
    // 实现 Core 层定义的接口
    public class ImageRepository : IImageRepository
    {
        private readonly DbContext _context;
        
        public async Task<BarcodeImage?> GetByIdAsync(string id)
        {
            // EF Core 实现
        }
        
        public async Task SaveAsync(BarcodeImage image)
        {
            // EF Core 实现
        }
    }
}
```

---

## 9. 事件载荷规范

### 9.1 使用 record struct 或 record class

- 事件参数（EventArgs）使用 `record struct` 或 `record class`
- 小型事件载荷优先使用 `record struct` 以提高性能
- 复杂事件载荷使用 `record class`

### 9.2 命名规范

- **事件载荷类型名必须以 `EventArgs` 结尾**
- 清晰表达事件的含义

**示例：**
```csharp
// ✅ 小型事件载荷：使用 record struct
public readonly record struct ImageProcessedEventArgs(
    string ImagePath,
    DateTime ProcessedAt,
    bool IsSuccessful
);

// ✅ 复杂事件载荷：使用 record class
public record class TrainingCompletedEventArgs
{
    public required Guid TaskId { get; init; }
    public required DateTime StartTime { get; init; }
    public required DateTime EndTime { get; init; }
    public required decimal Accuracy { get; init; }
    public required int TotalSamples { get; init; }
    public string? ErrorMessage { get; init; }
}

// 事件定义
public class ImageProcessor
{
    public event EventHandler<ImageProcessedEventArgs>? ImageProcessed;
    public event EventHandler<TrainingCompletedEventArgs>? TrainingCompleted;
    
    protected virtual void OnImageProcessed(ImageProcessedEventArgs e)
    {
        ImageProcessed?.Invoke(this, e);
    }
}
```

---

## 10. 异常与日志规范

### 10.1 异常消息使用中文

- 所有异常（Exception）的消息文本必须使用简体中文
- 便于用户理解和故障排查

### 10.2 日志消息使用中文

- 所有日志（Logger）的输出内容必须使用简体中文
- 包括调试信息、警告、错误等

**示例：**
```csharp
public class ImageValidator
{
    private readonly ILogger<ImageValidator> _logger;
    
    public void ValidateImage(string path)
    {
        _logger.LogInformation("开始验证图片：{Path}", path);
        
        if (!File.Exists(path))
        {
            var message = $"图片文件不存在：{path}";
            _logger.LogError(message);
            throw new FileNotFoundException(message, path);
        }
        
        if (new FileInfo(path).Length > MaxImageSize)
        {
            var message = $"图片文件大小超过限制：{path}";
            _logger.LogWarning(message);
            throw new InvalidOperationException(message);
        }
        
        _logger.LogInformation("图片验证通过：{Path}", path);
    }
}

public class TrainingService
{
    private readonly ILogger<TrainingService> _logger;
    
    public async Task StartTrainingAsync(string dataPath)
    {
        try
        {
            _logger.LogInformation("开始训练任务，数据路径：{DataPath}", dataPath);
            
            // 训练逻辑
            await PerformTrainingAsync(dataPath);
            
            _logger.LogInformation("训练任务完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "训练任务失败：{Message}", ex.Message);
            throw new InvalidOperationException("训练任务执行失败，请检查日志获取详细信息", ex);
        }
    }
}
```

---

## 11. 代码风格补充

### 11.1 使用现代 C# 特性

- 优先使用 C# 最新特性提高代码简洁性和可读性：
  - File-scoped namespaces（文件范围命名空间）
  - Global using（全局 using）
  - Pattern matching（模式匹配）
  - Target-typed new expressions（目标类型 new 表达式）

**示例：**
```csharp
// ✅ 使用 file-scoped namespace
namespace ZakYip.BarcodeReadabilityLab.Core.Services;

// ✅ 使用 target-typed new
List<string> paths = new();
Dictionary<string, int> counts = new();

// ✅ 使用模式匹配
public decimal CalculateDiscount(Customer customer) => customer switch
{
    { IsVip: true, TotalOrders: > 100 } => 0.2m,
    { IsVip: true } => 0.1m,
    { TotalOrders: > 50 } => 0.05m,
    _ => 0.0m
};
```

### 11.2 Null 处理

- 合理使用可空引用类型（Nullable Reference Types）
- 使用 `?` 标记可空类型，避免空引用异常

**示例：**
```csharp
public record class ValidationResult
{
    public required bool IsValid { get; init; }
    
    // 可空属性明确标记
    public string? ErrorMessage { get; init; }
    public List<string>? Warnings { get; init; }
}

public class Validator
{
    // 返回值可能为 null 时明确标记
    public ValidationResult? Validate(string? input)
    {
        if (input is null)
        {
            return null;
        }
        
        // 验证逻辑
        return new ValidationResult { IsValid = true };
    }
}
```

---

## 12. 总结

以上规范涵盖了本项目代码生成的核心要求。请在编写或生成代码时严格遵守这些规范，以确保代码库的高质量和一致性。

**关键要点：**
- ✅ 注释用中文
- ✅ 命名用英文
- ✅ 优先 record/record class + required
- ✅ 枚举必须有 Description 特性
- ✅ 布尔命名用 Is/Has/Can/Should 前缀
- ✅ 优先 decimal，谨慎用 double
- ✅ LINQ 优先，关注性能
- ✅ Core 层纯净，无基础设施依赖
- ✅ 事件载荷类型名以 EventArgs 结尾
- ✅ 异常和日志消息用中文

如有疑问或需要调整规范，请及时沟通。
