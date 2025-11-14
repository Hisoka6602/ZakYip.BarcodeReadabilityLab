# 日志和异常处理文档

## 概述

本文档说明了系统中实施的结构化日志和自定义异常处理机制。

## 结构化日志配置

### Serilog 集成

系统使用 **Serilog** 作为主要日志框架，提供结构化日志功能。

### 日志输出配置

1. **控制台输出**
   - 格式：`[时间戳 级别] 消息 {属性}{换行}{异常}`
   - 适用于开发和调试

2. **文件输出**
   - 路径：`logs/barcode-lab-.log`
   - 滚动策略：每天自动创建新文件
   - 文件大小限制：100MB（超过自动创建新文件）
   - 保留时长：31天
   - 格式：包含完整时间戳、时区、日志级别、源上下文、结构化属性和异常堆栈

### 日志级别配置

- **生产环境** (appsettings.json)
  - 默认：Information
  - Microsoft 框架：Warning
  - 生命周期事件：Information

- **开发环境** (appsettings.Development.json)
  - 默认：Debug
  - Microsoft 框架：Information

### 日志文件示例

```
[2025-11-14 18:41:23.635 +00:00] [INF] [] 应用程序启动中... {"Application":"BarcodeReadabilityLab"}
[2025-11-14 18:41:26.847 +00:00] [INF] [] 应用程序已启动，正在监听地址：http://localhost:5000 {"Application":"BarcodeReadabilityLab"}
[2025-11-14 18:41:26.863 +00:00] [ERR] [ZakYip.BarcodeReadabilityLab.Infrastructure.MLNet.Services.MlNetBarcodeReadabilityAnalyzer] 模型文件不存在 => 模型路径: C:\BarcodeImages\Models\noread-classifier-current.zip {"Application":"BarcodeReadabilityLab"}
```

## 自定义异常类型

系统定义了以下自定义异常类型，按领域划分：

### 1. BarcodeLabException（基础异常类）

所有自定义异常的基类，包含 `ErrorCode` 属性用于标识具体错误类型。

**位置：** `ZakYip.BarcodeReadabilityLab.Core.Domain.Exceptions`

```csharp
public class BarcodeLabException : Exception
{
    public string? ErrorCode { get; init; }
}
```

### 2. ConfigurationException（配置异常）

用于配置相关的错误，如配置项缺失、配置值无效等。

**错误代码示例：**
- `CONFIG_WATCH_DIR_EMPTY` - 监控目录路径未配置
- `CONFIG_WATCH_DIR_CREATE_FAILED` - 无法创建监控目录
- `MODEL_PATH_NOT_CONFIGURED` - ML.NET 模型路径未配置
- `MODEL_FILE_NOT_FOUND` - 模型文件不存在

### 3. TrainingException（训练异常）

用于模型训练相关的错误。

**错误代码示例：**
- `TRAIN_DIR_EMPTY` - 训练根目录路径不能为空
- `OUTPUT_DIR_EMPTY` - 输出模型目录路径不能为空
- `TRAIN_DIR_NOT_FOUND` - 训练根目录不存在
- `INVALID_SPLIT_RATIO` - 验证集分割比例无效
- `NO_TRAINING_DATA` - 训练根目录中没有找到任何训练样本
- `TRAINING_FAILED` - 训练任务失败

### 4. AnalysisException（分析异常）

用于条码图像分析相关的错误。

**错误代码示例：**
- `IMAGE_PATH_EMPTY` - 图片文件路径不能为空
- `IMAGE_FILE_NOT_FOUND` - 图片文件不存在
- `MODEL_LOAD_FAILED` - 加载模型失败
- `PREDICTION_ENGINE_NOT_INITIALIZED` - 预测引擎未初始化

## 结构化日志使用规范

### 日志消息格式

在所有更新的服务中，日志消息遵循以下格式：

```
描述性消息 => 属性1: {值1}, 属性2: {值2}, ...
```

使用 `=>` 分隔符提高可读性，结构化属性使用命名占位符。

### 示例

**正确示例：**
```csharp
_logger.LogInformation(
    "训练任务已加入队列 => JobId: {JobId}, 训练目录: {TrainingRootDirectory}, 输出目录: {OutputModelDirectory}",
    jobId, trainingRootDirectory, outputModelDirectory);
```

**错误日志示例：**
```csharp
_logger.LogError(ex, 
    "训练任务失败 => 错误类型: {ExceptionType}, 目录: {TrainingRootDirectory}", 
    ex.GetType().Name, trainingRootDirectory);
```

## 异常处理最佳实践

### 1. 抛出自定义异常

根据错误类型选择合适的自定义异常：

```csharp
if (string.IsNullOrWhiteSpace(modelPath))
{
    throw new ConfigurationException("ML.NET 模型路径未配置", "MODEL_PATH_NOT_CONFIGURED");
}
```

### 2. 异常链

保留原始异常信息：

```csharp
catch (Exception ex)
{
    throw new TrainingException($"训练任务失败: {ex.Message}", "TRAINING_FAILED", ex);
}
```

### 3. 异常分类捕获

对不同类型的异常进行分类处理：

```csharp
catch (AnalysisException)
{
    // 重新抛出分析异常
    throw;
}
catch (Exception ex)
{
    _logger.LogError(ex, "分析条码样本时发生未预期异常 => ...");
    // 处理其他异常
}
```

## 可观测性优势

实施结构化日志和自定义异常后，系统获得以下优势：

1. **更好的可追溯性** - 每个日志条目包含结构化属性，便于过滤和搜索
2. **错误代码标识** - 使用错误代码快速定位问题类型
3. **日志持久化** - 自动轮转的日志文件，保留31天历史
4. **性能监控** - 记录关键操作的详细信息和时间
5. **问题诊断** - 完整的异常堆栈和上下文信息

## 配置管理

### 修改日志配置

编辑 `appsettings.json` 中的 `Serilog` 节点：

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information"
    },
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "logs/barcode-lab-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 31,
          "fileSizeLimitBytes": 104857600
        }
      }
    ]
  }
}
```

### 常用调整

- **增加日志级别** - 将 `Default` 改为 `Debug` 或 `Verbose`
- **更改保留天数** - 修改 `retainedFileCountLimit`
- **调整文件大小** - 修改 `fileSizeLimitBytes`（字节）
- **更改轮转策略** - 修改 `rollingInterval`（Day、Hour、Minute等）

## 相关文件

- `src/ZakYip.BarcodeReadabilityLab.Core/Domain/Exceptions/` - 自定义异常类
- `src/ZakYip.BarcodeReadabilityLab.Service/Program.cs` - Serilog 配置
- `src/ZakYip.BarcodeReadabilityLab.Service/appsettings.json` - 日志配置
- `src/ZakYip.BarcodeReadabilityLab.Application/Services/` - 更新的应用服务
- `src/ZakYip.BarcodeReadabilityLab.Infrastructure.MLNet/Services/` - 更新的ML服务
