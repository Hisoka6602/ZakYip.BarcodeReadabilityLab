# Infrastructure.MLNet/Services

## 职责说明

此目录包含 ML.NET 基础设施服务实现，负责机器学习模型的训练、加载和预测。

### 设计原则

- 实现 Core 层定义的接口
- 依赖注入 ML.NET 相关组件
- 方法保持专注且小巧
- 日志消息使用中文
- 异常消息使用中文
- 妥善处理资源释放（如 `IDisposable`）

### 示例

```csharp
namespace ZakYip.BarcodeReadabilityLab.Infrastructure.MLNet.Services;

using Microsoft.ML;
using ZakYip.BarcodeReadabilityLab.Core.Domain.Contracts;

/// <summary>
/// ML.NET 模型服务实现
/// </summary>
public class MLNetModelService : IMLModelService, IDisposable
{
    private readonly MLContext _mlContext;
    private readonly ILogger<MLNetModelService> _logger;
    private ITransformer? _model;
    
    public MLNetModelService(ILogger<MLNetModelService> logger)
    {
        _mlContext = new MLContext();
        _logger = logger;
    }
    
    /// <summary>
    /// 加载模型
    /// </summary>
    public async Task LoadModelAsync(string modelPath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("开始加载模型：{ModelPath}", modelPath);
        
        ValidateModelPath(modelPath);
        _model = await LoadModelFromFileAsync(modelPath, cancellationToken);
        
        _logger.LogInformation("模型加载完成");
    }
    
    private void ValidateModelPath(string modelPath)
    {
        if (string.IsNullOrEmpty(modelPath))
            throw new ArgumentException("模型路径不能为空", nameof(modelPath));
            
        if (!File.Exists(modelPath))
            throw new FileNotFoundException("模型文件不存在", modelPath);
    }
    
    public void Dispose()
    {
        _mlContext?.Dispose();
    }
}
```
