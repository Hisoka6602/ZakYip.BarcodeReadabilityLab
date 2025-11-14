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

## 12. 使用 required + init 实现更安全的对象创建

### 12.1 基本原则

- **对于不可空的必需属性，使用 `required` 关键字**
- **对于不应在创建后修改的属性，使用 `init` 访问器**
- 确保某些属性在对象创建时必须被设置，避免部分初始化的对象

### 12.2 适用场景

- DTO（数据传输对象）
- 配置对象
- 事件参数
- 所有不可变数据模型

**示例：**
```csharp
// ✅ 推荐：使用 required + init 确保安全初始化
public record class BarcodeAnalysisRequest
{
    public required string ImagePath { get; init; }
    public required Guid RequestId { get; init; }
    public required DateTime RequestTime { get; init; }
    public string? ModelVersion { get; init; }
}

// ✅ 推荐：记录类型的简洁形式也是 required + init
public record AnalysisResult(string Label, decimal Confidence);

// ❌ 避免：可变的 set 访问器导致对象状态不可控
public class BarcodeAnalysisRequest
{
    public string ImagePath { get; set; } = string.Empty;
    public Guid RequestId { get; set; }
}
```

---

## 13. 启用可空引用类型

### 13.1 项目配置

- **所有项目必须启用可空引用类型**
- 在 `.csproj` 文件中设置 `<Nullable>enable</Nullable>`

### 13.2 使用规范

- **明确标记可空类型**：使用 `?` 标记可能为 null 的引用类型
- **避免不必要的 null 检查**：编译器会帮助识别潜在的 null 引用问题
- 让编译器对可能的空引用问题发出警告，在运行前发现问题

**示例：**
```csharp
// ✅ 推荐：明确的可空性标记
public record class ProcessingResult
{
    public required string ImagePath { get; init; }
    public string? ErrorMessage { get; init; }  // 明确标记可空
    public List<string>? Warnings { get; init; } // 明确标记可空
}

public class ImageProcessor
{
    // 参数明确标记可空性
    public ProcessingResult Process(string imagePath, string? modelPath = null)
    {
        // 编译器会在此处要求 null 检查
        if (modelPath is not null)
        {
            LoadModel(modelPath);
        }
        
        return new ProcessingResult { ImagePath = imagePath };
    }
}
```

---

## 14. 使用文件作用域类型实现真正封装

### 14.1 基本原则

- **工具类、辅助类保持在文件内私有**
- 使用 `file` 关键字声明仅在当前文件可见的类型
- 避免污染全局命名空间，帮助强制执行边界

### 14.2 适用场景

- 仅在特定文件内部使用的辅助类
- 内部实现细节
- 临时数据结构

**示例：**
```csharp
namespace ZakYip.BarcodeReadabilityLab.Core.Services;

// ✅ 公共服务接口
public interface IImageValidator
{
    ValidationResult Validate(string path);
}

// ✅ 公共实现类
public class ImageValidator : IImageValidator
{
    public ValidationResult Validate(string path)
    {
        var context = new ValidationContext(path);
        return context.Execute();
    }
}

// ✅ 文件作用域类型：仅在此文件内可见
file class ValidationContext
{
    private readonly string _path;
    
    public ValidationContext(string path)
    {
        _path = path;
    }
    
    public ValidationResult Execute()
    {
        // 验证逻辑
        return new ValidationResult { IsValid = true };
    }
}
```

---

## 15. 使用 record 处理不可变数据

### 15.1 基本原则

- **DTO 和只读数据优先使用 record**
- record 提供值语义、自动实现相等性比较
- record 是不可变数据的理想选择

### 15.2 选择指南

- **简单值对象**：使用 `record` 位置记录
- **复杂数据模型**：使用 `record class` 配合 `required` 和 `init`
- **小型事件载荷**：使用 `readonly record struct` 提高性能

**示例：**
```csharp
// ✅ 简单值对象使用 record
public record ImageMetadata(string Path, long Size, DateTime CreatedAt);

// ✅ 复杂模型使用 record class
public record class BarcodeAnalysisResult
{
    public required Guid AnalysisId { get; init; }
    public required string ImagePath { get; init; }
    public required DateTime AnalyzedAt { get; init; }
    public required decimal Confidence { get; init; }
    public string? DetectedType { get; init; }
    public List<string>? Warnings { get; init; }
}

// ✅ 小型事件载荷使用 readonly record struct
public readonly record struct ImageProcessedEventArgs(
    string ImagePath,
    bool IsSuccessful,
    DateTime ProcessedAt
);
```

---

## 16. 保持方法专注且小巧

### 16.1 基本原则

- **一个方法 = 一个职责**
- 方法应短小精悍，通常不超过 20-30 行
- 较小的方法更易于阅读、测试和重用
- 复杂逻辑拆分为多个小方法

### 16.2 重构指南

- 如果方法需要多层嵌套，考虑提取内部逻辑
- 如果方法有多个职责，拆分为多个方法
- 使用有意义的方法名清晰表达意图

**示例：**
```csharp
// ❌ 避免：方法过大，职责不清
public async Task<AnalysisResult> ProcessImageAsync(string imagePath)
{
    // 验证逻辑
    if (string.IsNullOrEmpty(imagePath))
        throw new ArgumentException("路径不能为空", nameof(imagePath));
    
    if (!File.Exists(imagePath))
        throw new FileNotFoundException("文件不存在", imagePath);
    
    var fileInfo = new FileInfo(imagePath);
    if (fileInfo.Length > MaxFileSize)
        throw new InvalidOperationException("文件过大");
    
    // 加载模型
    if (_model is null)
    {
        var modelPath = Path.Combine(_modelDirectory, "model.zip");
        _model = await LoadModelAsync(modelPath);
    }
    
    // 预处理图片
    var image = await LoadImageAsync(imagePath);
    var preprocessed = PreprocessImage(image);
    
    // 执行预测
    var prediction = _model.Predict(preprocessed);
    
    // 后处理结果
    return new AnalysisResult
    {
        Label = prediction.Label,
        Confidence = prediction.Score
    };
}

// ✅ 推荐：拆分为多个专注的方法
public async Task<AnalysisResult> ProcessImageAsync(string imagePath)
{
    ValidateImagePath(imagePath);
    await EnsureModelLoadedAsync();
    
    var preprocessedImage = await LoadAndPreprocessImageAsync(imagePath);
    var prediction = PredictImage(preprocessedImage);
    
    return CreateAnalysisResult(prediction);
}

private void ValidateImagePath(string imagePath)
{
    if (string.IsNullOrEmpty(imagePath))
        throw new ArgumentException("路径不能为空", nameof(imagePath));
    
    if (!File.Exists(imagePath))
        throw new FileNotFoundException("文件不存在", imagePath);
    
    ValidateFileSize(imagePath);
}

private void ValidateFileSize(string imagePath)
{
    var fileInfo = new FileInfo(imagePath);
    if (fileInfo.Length > MaxFileSize)
        throw new InvalidOperationException("文件过大");
}

private async Task EnsureModelLoadedAsync()
{
    if (_model is null)
    {
        var modelPath = Path.Combine(_modelDirectory, "model.zip");
        _model = await LoadModelAsync(modelPath);
    }
}

private async Task<ProcessedImage> LoadAndPreprocessImageAsync(string imagePath)
{
    var image = await LoadImageAsync(imagePath);
    return PreprocessImage(image);
}

private Prediction PredictImage(ProcessedImage image)
{
    return _model.Predict(image);
}

private AnalysisResult CreateAnalysisResult(Prediction prediction)
{
    return new AnalysisResult
    {
        Label = prediction.Label,
        Confidence = prediction.Score
    };
}
```

---

## 17. 不需要可变性时优先使用 readonly struct

### 17.1 基本原则

- **值类型不需要修改时，使用 `readonly struct`**
- 防止意外更改并提高性能
- 避免值类型的防御性拷贝

### 17.2 适用场景

- 不可变的值类型
- 小型事件参数
- 坐标、尺寸等简单数据结构

**示例：**
```csharp
// ✅ 推荐：不可变的值类型使用 readonly struct
public readonly struct ImageDimensions
{
    public int Width { get; init; }
    public int Height { get; init; }
    
    public ImageDimensions(int width, int height)
    {
        Width = width;
        Height = height;
    }
    
    public int Area => Width * Height;
}

// ✅ 推荐：使用 readonly record struct 更简洁
public readonly record struct Point(int X, int Y);

public readonly record struct Rectangle(Point TopLeft, Point BottomRight)
{
    public int Width => BottomRight.X - TopLeft.X;
    public int Height => BottomRight.Y - TopLeft.Y;
}

// ❌ 避免：可变的 struct 可能导致意外行为
public struct MutablePoint
{
    public int X { get; set; }
    public int Y { get; set; }
}
```

---

## 18. 总结

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
- ✅ 使用 required + init 实现安全对象创建
- ✅ 启用可空引用类型
- ✅ 使用文件作用域类型保持封装
- ✅ 优先使用 record 处理不可变数据
- ✅ 保持方法专注且小巧
- ✅ 优先使用 readonly struct

如有疑问或需要调整规范，请及时沟通。
