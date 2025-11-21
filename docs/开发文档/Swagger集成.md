# Swagger/OpenAPI 集成文档

## 概述

本项目已完整集成 Swagger/OpenAPI 文档功能，提供交互式 API 文档和测试界面。

## 访问方式

启动应用后，通过以下 URL 访问：

- **Swagger UI（交互式文档）**：http://localhost:4000/api-docs
- **OpenAPI JSON 规范**：http://localhost:4000/api-docs/v1/swagger.json

## 功能特性

### 1. 完整的 API 文档

所有 API 端点都包含：
- ✅ 详细的中文描述
- ✅ 请求参数说明
- ✅ 响应模型定义
- ✅ HTTP 状态码说明
- ✅ 请求/响应示例

### 2. 交互式测试

- ✅ **默认启用 "Try it out"**：可以直接在 UI 中测试 API
- ✅ 支持填写参数并执行请求
- ✅ 实时查看响应结果
- ✅ 显示请求持续时间

### 3. UI 增强功能

- ✅ **深度链接**：支持直接链接到特定端点
- ✅ **过滤器**：快速搜索和筛选端点
- ✅ **分组显示**：按功能模块组织端点
- ✅ **自定义主题**：清晰的文档标题和描述

## 已文档化的 API 端点

### Training API (训练管理)

#### 1. 启动训练任务
```
POST /api/training/start
```
- **功能**：触发一次基于目录的训练任务
- **请求体**：`StartTrainingRequest`（所有字段可选，未提供则使用默认配置）
- **响应**：返回训练任务 ID

**参数说明**：
- `trainingRootDirectory`：训练数据根目录
- `outputModelDirectory`：模型输出目录
- `validationSplitRatio`：验证集分割比例（0.0-1.0）
- `learningRate`：学习率
- `epochs`：训练轮数
- `batchSize`：批大小
- `remarks`：任务备注
- `dataAugmentation`：数据增强配置
- `dataBalancing`：数据平衡配置

#### 2. 查询训练状态
```
GET /api/training/status/{jobId}
```
- **功能**：根据 jobId 查询训练任务的当前状态与进度
- **路径参数**：`jobId` (GUID)
- **响应**：完整的训练任务状态信息

**状态说明**：
- 排队中：任务已创建，等待执行
- 运行中：任务正在训练模型
- 已完成：训练成功完成
- 失败：训练过程中发生错误
- 已取消：任务被用户取消

#### 3. 获取训练历史
```
GET /api/training/history
```
- **功能**：获取所有训练任务的历史记录
- **响应**：按开始时间降序排列的任务列表

### Models API (模型管理)

#### 1. 下载当前激活模型
```
GET /api/models/current/download
```
- **功能**：下载当前在线推理使用的模型文件
- **响应**：二进制流（application/octet-stream）

#### 2. 根据版本下载模型
```
GET /api/models/{versionId}/download
```
- **功能**：下载指定版本的模型文件
- **路径参数**：`versionId` (GUID)
- **响应**：二进制流（application/octet-stream）

#### 3. 导入模型文件
```
POST /api/models/import
```
- **功能**：上传并注册新的模型版本
- **Content-Type**：multipart/form-data
- **响应**：模型版本信息

**参数说明**：
- `ModelFile`：模型文件（通常为 .zip）【必填】
- `VersionName`：自定义版本名称
- `DeploymentSlot`：部署槽位（默认 Production）
- `TrafficPercentage`：流量权重
- `Notes`：备注说明
- `SetAsActive`：是否立即激活（默认 true）

### 传统 API（向后兼容）

#### Training Legacy API
```
POST /api/training-legacy/start
GET  /api/training-legacy/status/{jobId}
POST /api/training-legacy/cancel/{jobId}
```

#### Training Job API
```
POST /api/training-job/start
GET  /api/training-job/status/{jobId}
```

## 技术实现

### 1. NuGet 包
```xml
<PackageReference Include="Swashbuckle.AspNetCore" Version="6.8.1" />
```

### 2. Program.cs 配置

```csharp
// 添加 Swagger 生成器
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "条码可读性分析 API",
        Version = "v1",
        Description = "提供条码图片可读性分析和模型训练的 API 服务",
        Contact = new OpenApiContact
        {
            Name = "ZakYip.BarcodeReadabilityLab",
            Url = new Uri("https://github.com/Hisoka6602/ZakYip.BarcodeReadabilityLab")
        }
    });

    // 包含 XML 注释文档
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    }
});

// 启用 Swagger 中间件
app.UseSwagger(options =>
{
    options.RouteTemplate = "api-docs/{documentName}/swagger.json";
});

// 启用 Swagger UI
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/api-docs/v1/swagger.json", "条码可读性分析 API v1");
    options.RoutePrefix = "api-docs";
    options.DocumentTitle = "条码可读性分析 API 文档";
    options.EnableDeepLinking();
    options.EnableFilter();
    options.EnableTryItOutByDefault();  // 默认启用测试功能
    options.DisplayRequestDuration();
});
```

### 3. XML 文档生成

项目文件配置：
```xml
<PropertyGroup>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <NoWarn>$(NoWarn);1591</NoWarn>
</PropertyGroup>
```

### 4. XML 注释示例

#### Controller 注释
```csharp
/// <summary>
/// 训练任务管理控制器
/// </summary>
/// <remarks>
/// 提供完整的条码可读性模型训练任务管理功能，包括：
/// - 启动训练任务
/// - 查询训练任务状态
/// 
/// 支持与训练任务持久化存储集成，可查询历史任务记录。
/// </remarks>
[ApiController]
[Route("api/training-job")]
public class TrainingJobController : ControllerBase
```

#### 端点注释
```csharp
/// <summary>
/// 启动训练任务
/// </summary>
/// <param name="request">训练请求参数</param>
/// <param name="cancellationToken">取消令牌</param>
/// <returns>训练任务 ID</returns>
/// <response code="200">训练任务成功创建并加入队列</response>
/// <response code="400">请求参数无效或训练目录不存在</response>
/// <response code="500">服务器内部错误</response>
[HttpPost("start")]
[ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
public async Task<IActionResult> StartTrainingAsync(...)
```

#### Minimal API 注释
```csharp
group.MapPost("/start", StartTrainingAsync)
    .WithName("StartTraining")
    .WithSummary("启动训练任务")
    .WithDescription(@"触发一次基于目录的训练任务。

**功能说明：**
- 如果请求体中未提供参数，则使用配置文件中的默认 TrainingOptions
- 训练数据目录应包含按类别组织的子目录（如 readable、unreadable）
- 支持自定义验证集分割比例
- 可添加备注说明便于管理历史任务

**返回值：**
- 成功时返回训练任务 ID，可用于后续查询任务状态")
    .Produces<StartTrainingResponse>(StatusCodes.Status200OK)
    .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
    .ProducesProblem(StatusCodes.Status500InternalServerError);
```

#### 模型注释
```csharp
/// <summary>
/// 启动训练任务的请求模型
/// </summary>
/// <remarks>
/// 所有字段均为可选，如果未提供则使用配置文件中的默认值。
/// 训练数据目录应包含按类别（如 readable、unreadable）组织的子目录结构。
/// </remarks>
/// <example>
/// {
///   "trainingRootDirectory": "C:\\BarcodeImages\\Training",
///   "outputModelDirectory": "C:\\Models\\Output",
///   "validationSplitRatio": 0.2,
///   "remarks": "第一次训练测试"
/// }
/// </example>
public record class StartTrainingRequest
{
    /// <summary>
    /// 训练数据根目录路径（可选，为空时使用配置文件中的默认值）
    /// </summary>
    /// <example>C:\BarcodeImages\Training</example>
    public string? TrainingRootDirectory { get; init; }
}
```

## 集成测试

创建了专门的集成测试验证 Swagger 功能：

```csharp
[Fact]
public async Task SwaggerJson_ShouldBeAccessible()
{
    var response = await _client.GetAsync("/api-docs/v1/swagger.json");
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.Contains("条码可读性分析 API", content);
}

[Fact]
public async Task SwaggerUI_ShouldBeAccessible()
{
    var response = await _client.GetAsync("/api-docs");
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.Contains("swagger-ui", content);
}

[Fact]
public async Task SwaggerJson_ShouldContainAllEndpoints()
{
    var response = await _client.GetAsync("/api-docs/v1/swagger.json");
    var content = await response.Content.ReadAsStringAsync();
    
    Assert.Contains("/api/training/start", content);
    Assert.Contains("/api/training/status/{jobId}", content);
    Assert.Contains("/api/models/import", content);
}
```

## 使用示例

### 1. 浏览 API 文档
1. 启动应用程序
2. 在浏览器中打开 http://localhost:4000/api-docs
3. 浏览所有可用的 API 端点和模型定义

### 2. 测试 API
1. 在 Swagger UI 中找到要测试的端点
2. 点击端点展开详细信息
3. 点击 "Try it out" 按钮
4. 填写必要的参数
5. 点击 "Execute" 执行请求
6. 查看响应结果

### 3. 获取 OpenAPI 规范
访问 http://localhost:4000/api-docs/v1/swagger.json 获取完整的 OpenAPI 3.0 规范文件，可用于：
- 导入到 Postman 等 API 测试工具
- 生成客户端 SDK
- API 网关配置
- 文档生成工具

## 遵循的编码规范

所有 XML 注释和文档都严格遵循项目编码规范：
- ✅ 注释使用简体中文
- ✅ 类名、方法名、属性名使用英文
- ✅ 所有枚举成员都有 Description 特性
- ✅ 模型使用 record class + required
- ✅ 布尔属性使用 Is/Has/Can/Should 前缀

## 总结

Swagger/OpenAPI 集成已完成，提供：
- ✅ 完整的 API 文档
- ✅ 交互式测试界面
- ✅ 详细的中文注释
- ✅ 示例数据
- ✅ 所有端点覆盖
- ✅ 通过集成测试验证

用户可以直接通过 Swagger UI 浏览和测试所有 API，无需额外工具或文档。
