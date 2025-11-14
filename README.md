# ZakYip.BarcodeReadabilityLab
NoRead图片分类机器人

## 项目简介

这是一个 Windows 服务，用于监控指定目录下的读码图片，并使用 ML.NET 分析 Noread 原因。

### 主要功能

1. **自动监控**：监控指定目录下的条码图片文件
2. **智能分析**：使用 ML.NET 机器学习模型分析 NoRead 原因
3. **智能筛选**：
   - 对于无法分析或分析结果置信度低于阈值（默认 90%，可配置）的图片
   - 自动复制到"无法分析"目录
   - 供人工分类与后续训练使用
4. **HTTP API**：
   - 触发基于目录的数据训练（使用 ML.NET）
   - 查询训练任务状态
   - 取消正在进行的训练任务
5. **便于集成**：提供 RESTful API，便于后续自动化集成

## 配置说明

配置文件位于 `appsettings.json`：

```json
{
  "BarcodeReadabilityService": {
    "MonitorPath": "C:\\BarcodeImages\\Monitor",
    "UnableToAnalyzePath": "C:\\BarcodeImages\\UnableToAnalyze",
    "TrainingDataPath": "C:\\BarcodeImages\\TrainingData",
    "ModelPath": "C:\\BarcodeImages\\Models",
    "ConfidenceThreshold": 0.9,
    "SupportedImageExtensions": [ ".jpg", ".jpeg", ".png", ".bmp" ]
  },
  "ApiSettings": {
    "Port": 5000,
    "Urls": "http://localhost:5000"
  }
}
```

### 配置项说明

- `MonitorPath`: 监控的图片目录
- `UnableToAnalyzePath`: 无法分析图片的输出目录
- `TrainingDataPath`: 训练数据目录（应包含按标签分类的子目录）
- `ModelPath`: ML.NET 模型存储路径
- `ConfidenceThreshold`: 置信度阈值（0-1 之间，默认 0.9）
- `SupportedImageExtensions`: 支持的图片扩展名
- `Urls`: HTTP API 监听地址

## API 接口

### 1. 开始训练

```http
POST /api/training/start
Content-Type: application/json

{
  "trainingDataPath": "C:\\BarcodeImages\\TrainingData"
}
```

响应：
```json
{
  "taskId": "guid-string",
  "message": "Training started successfully"
}
```

### 2. 查询训练状态

```http
GET /api/training/status/{taskId}
```

响应：
```json
{
  "taskId": "guid-string",
  "state": "Running",
  "message": "Training started",
  "startTime": "2024-01-01T00:00:00Z",
  "endTime": null,
  "progress": 0.1
}
```

状态值：
- `NotStarted`: 未开始
- `Running`: 运行中
- `Completed`: 已完成
- `Failed`: 失败
- `Cancelled`: 已取消

### 3. 取消训练

```http
POST /api/training/cancel/{taskId}
```

响应：
```json
{
  "message": "Training cancellation requested successfully"
}
```

## 训练数据结构

训练数据应按以下结构组织：

```
TrainingDataPath/
├── Label1/
│   ├── image1.jpg
│   ├── image2.jpg
│   └── ...
├── Label2/
│   ├── image3.jpg
│   ├── image4.jpg
│   └── ...
└── ...
```

每个子目录名即为该类别的标签。

## 构建和运行

### 构建

```bash
dotnet build
```

### 运行

```bash
dotnet run --project src/ZakYip.BarcodeReadabilityLab.Service
```

### 作为 Windows 服务运行

```bash
# 发布
dotnet publish -c Release

# 安装服务（需要管理员权限）
sc create BarcodeReadabilityService binPath="<发布路径>\ZakYip.BarcodeReadabilityLab.Service.exe"

# 启动服务
sc start BarcodeReadabilityService
```

## 工作流程

1. 服务启动后自动监控 `MonitorPath` 目录
2. 当检测到新图片文件时：
   - 使用 ML.NET 模型进行预测
   - 如果置信度 >= 阈值：标记为已分析并删除
   - 如果置信度 < 阈值或无法分析：复制到 `UnableToAnalyzePath`
3. 人工对无法分析的图片进行分类，放入 `TrainingDataPath` 相应的标签目录
4. 调用训练 API 重新训练模型
5. 新模型自动加载并用于后续预测

## 技术栈

- .NET 9.0
- ML.NET 4.0
- ASP.NET Core (HTTP API)
- Windows Services

