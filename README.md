# ZakYip.BarcodeReadabilityLab

## 项目简介

这是一个**读码图片 Noread 分析实验室**，使用 **ML.NET** 进行条码图片的可读性分析和分类。

## 技术栈

- .NET 9.0
- ML.NET 4.0

## HTTP API 使用说明

本服务提供 HTTP API 端点，用于控制训练任务的触发和查询训练状态。

### API 配置

API 监听地址在 `appsettings.json` 中配置：

```json
{
  "ApiSettings": {
    "Port": 5000,
    "Urls": "http://localhost:5000"
  }
}
```

### API 端点

#### 1. 启动训练任务

**端点:** `POST /api/training/start`

**描述:** 触发一次基于目录的训练任务。如果请求体中未提供参数，则使用配置文件中的默认 `TrainingOptions`。

**请求体（可选）:**

```json
{
  "trainingRootDirectory": "C:\\BarcodeImages\\TrainingData",
  "outputModelDirectory": "C:\\BarcodeImages\\Models",
  "validationSplitRatio": 0.2,
  "remarks": "第一次训练测试"
}
```

**请求体字段说明:**
- `trainingRootDirectory`（可选）：训练数据根目录路径。如果为空，使用配置文件中的默认值。
- `outputModelDirectory`（可选）：训练输出模型文件存放目录路径。如果为空，使用配置文件中的默认值。
- `validationSplitRatio`（可选）：验证集分割比例（0.0 到 1.0 之间）。
- `remarks`（可选）：训练任务备注说明。

**成功响应示例（200 OK）:**

```json
{
  "jobId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "message": "训练任务已创建并加入队列"
}
```

**错误响应示例（400 Bad Request）:**

```json
{
  "error": "训练根目录路径不能为空"
}
```

**使用 curl 示例:**

```bash
# 使用默认配置启动训练
curl -X POST http://localhost:5000/api/training/start \
  -H "Content-Type: application/json" \
  -d "{}"

# 使用自定义参数启动训练
curl -X POST http://localhost:5000/api/training/start \
  -H "Content-Type: application/json" \
  -d "{\"trainingRootDirectory\":\"C:\\\\BarcodeImages\\\\TrainingData\",\"outputModelDirectory\":\"C:\\\\BarcodeImages\\\\Models\",\"validationSplitRatio\":0.2,\"remarks\":\"测试训练\"}"
```

**使用 PowerShell 示例:**

```powershell
# 使用默认配置启动训练
Invoke-RestMethod -Uri "http://localhost:5000/api/training/start" `
  -Method Post `
  -ContentType "application/json" `
  -Body "{}"

# 使用自定义参数启动训练
$body = @{
    trainingRootDirectory = "C:\BarcodeImages\TrainingData"
    outputModelDirectory = "C:\BarcodeImages\Models"
    validationSplitRatio = 0.2
    remarks = "测试训练"
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:5000/api/training/start" `
  -Method Post `
  -ContentType "application/json" `
  -Body $body
```

#### 2. 查询训练任务状态

**端点:** `GET /api/training/status/{jobId}`

**描述:** 根据 `jobId` 查询训练任务的当前状态与进度信息。

**路径参数:**
- `jobId`：训练任务的唯一标识符（GUID 格式）

**成功响应示例（200 OK）:**

```json
{
  "jobId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "state": "运行中",
  "progress": 0.45,
  "message": "训练任务正在执行",
  "startTime": "2024-01-15T10:30:00Z",
  "completedTime": null,
  "errorMessage": null,
  "remarks": "测试训练"
}
```

**响应字段说明:**
- `jobId`：训练任务唯一标识符
- `state`：训练任务状态描述（排队中、运行中、已完成、失败、已取消）
- `progress`：训练进度百分比（0.0 到 1.0 之间，可选）
- `message`：响应消息
- `startTime`：训练开始时间（可选）
- `completedTime`：训练完成时间（可选）
- `errorMessage`：错误信息（训练失败时可用）
- `remarks`：训练任务备注说明（可选）

**错误响应示例（404 Not Found）:**

```json
{
  "error": "训练任务不存在"
}
```

**使用 curl 示例:**

```bash
curl http://localhost:5000/api/training/status/a1b2c3d4-e5f6-7890-abcd-ef1234567890
```

**使用 PowerShell 示例:**

```powershell
$jobId = "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
Invoke-RestMethod -Uri "http://localhost:5000/api/training/status/$jobId" -Method Get
```

### 训练工作流程

1. **触发训练任务**
   - 调用 `POST /api/training/start` 端点
   - 服务返回 `jobId`，任务进入队列

2. **轮询任务状态**
   - 使用返回的 `jobId` 调用 `GET /api/training/status/{jobId}`
   - 根据 `state` 和 `progress` 了解训练进度

3. **任务完成或失败**
   - 当 `state` 为 "已完成" 时，训练成功
   - 当 `state` 为 "失败" 时，查看 `errorMessage` 了解失败原因

### 注意事项

- 所有响应的 JSON 字段名使用小驼峰命名风格（camelCase）
- 训练是长时间任务，不会阻塞 API 调用
- 服务会持续执行目录监控和推理逻辑，与 API 调用互不干扰
- 建议使用轮询方式定期查询训练状态，避免频繁请求

