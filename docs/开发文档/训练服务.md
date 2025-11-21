# 训练任务服务使用说明

## 概述

本服务实现了基于 ML.NET 的图像分类模型训练功能，支持异步训练任务管理和模型热切换。

## 主要特性

1. **异步训练任务管理**: 通过任务队列管理训练任务，支持多个训练任务排队执行
2. **状态追踪**: 实时追踪训练任务状态（排队中、运行中、已完成、失败）
3. **版本化模型**: 训练完成后自动保存带时间戳的模型文件
4. **模型热切换**: 训练完成后自动更新当前在线模型，无需重启服务
5. **模型评估指标**: 训练完成后自动计算准确率、召回率、F1 分数、混淆矩阵等评估指标
6. **中文日志**: 所有日志和错误信息使用中文，便于调试和监控

## 训练数据结构

训练数据应按以下目录结构组织：

```
训练根目录/
├── Truncated/              # 条码被截断
│   ├── image1.jpg
│   ├── image2.jpg
│   └── ...
├── BlurryOrOutOfFocus/     # 条码模糊或失焦
│   ├── image1.jpg
│   └── ...
├── ReflectionOrOverexposure/  # 反光或高亮过曝
│   └── ...
├── WrinkledOrDeformed/     # 条码褶皱或形变严重
│   └── ...
├── NoBarcodeInImage/       # 画面内无条码
│   └── ...
├── StainedOrObstructed/    # 条码有污渍或遮挡
│   └── ...
└── ClearButNotRecognized/  # 条码清晰但未被识别
    └── ...
```

每个子目录的名称对应一个 `NoreadReason` 枚举值，支持的标签有：
- `Truncated`: 条码被截断
- `BlurryOrOutOfFocus`: 条码模糊或失焦
- `ReflectionOrOverexposure`: 反光或高亮过曝
- `WrinkledOrDeformed`: 条码褶皱或形变严重
- `NoBarcodeInImage`: 画面内无条码
- `StainedOrObstructed`: 条码有污渍或遮挡
- `ClearButNotRecognized`: 条码清晰但未被识别

## API 使用说明

### 1. 启动训练任务

**端点**: `POST /api/training-job/start`

**请求体**:
```json
{
  "trainingRootDirectory": "C:\\BarcodeImages\\TrainingData",
  "outputModelDirectory": "C:\\BarcodeImages\\Models",
  "validationSplitRatio": 0.2,
  "remarks": "第一次训练"
}
```

**参数说明**:
- `trainingRootDirectory` (必需): 训练数据根目录路径
- `outputModelDirectory` (必需): 输出模型文件存放目录路径
- `validationSplitRatio` (可选): 验证集分割比例（0.0 到 1.0 之间）
- `remarks` (可选): 训练任务备注说明

**响应示例**:
```json
{
  "jobId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "message": "训练任务已创建并加入队列"
}
```

### 2. 查询训练任务状态

**端点**: `GET /api/training-job/status/{jobId}`

**响应示例**:
```json
{
  "jobId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "status": "Completed",
  "progress": 1.0,
  "startTime": "2024-01-01T10:00:00Z",
  "completedTime": "2024-01-01T10:15:00Z",
  "errorMessage": null,
  "remarks": "第一次训练",
  "evaluationMetrics": {
    "accuracy": 0.95,
    "macroPrecision": 0.94,
    "macroRecall": 0.93,
    "macroF1Score": 0.935,
    "microPrecision": 0.95,
    "microRecall": 0.95,
    "microF1Score": 0.95,
    "logLoss": 0.15,
    "confusionMatrixJson": "{\"labels\":[\"BlurryOrOutOfFocus\",\"Truncated\"],\"matrix\":[[45,5],[3,47]]}",
    "perClassMetricsJson": "[{\"label\":\"BlurryOrOutOfFocus\",\"precision\":0.9375,\"recall\":0.9,\"f1Score\":0.9184,\"support\":50}]"
  }
}
```

**评估指标说明**:
- `accuracy`: 准确率，正确分类的样本占总样本的比例
- `macroPrecision`: 宏平均精确率，所有类别精确率的算术平均值
- `macroRecall`: 宏平均召回率，所有类别召回率的算术平均值
- `macroF1Score`: 宏平均 F1 分数，所有类别 F1 分数的算术平均值
- `microPrecision`: 微平均精确率，全局统计的精确率
- `microRecall`: 微平均召回率，全局统计的召回率
- `microF1Score`: 微平均 F1 分数，全局统计的 F1 分数
- `logLoss`: 对数损失，评估模型预测概率的质量（值越小越好）
- `confusionMatrixJson`: 混淆矩阵的 JSON 表示
- `perClassMetricsJson`: 每个类别的详细评估指标（精确率、召回率、F1、支持数）

**状态说明**:
- `Queued` (1): 排队中
- `Running` (2): 运行中
- `Completed` (3): 已完成
- `Failed` (4): 失败
- `Cancelled` (5): 已取消

### 3. 实时监控训练进度

训练进度更新支持两种模式：**轮询模式**和**WebSocket 推送模式**。

#### 3.1 轮询模式（HTTP 接口）

客户端可以定期调用状态查询接口来获取最新进度：

```bash
# 每隔 5 秒查询一次训练状态
while true; do
  curl http://localhost:5000/api/training-job/status/3fa85f64-5717-4562-b3fc-2c963f66afa6
  sleep 5
done
```

**优点**：
- 实现简单，无需特殊客户端支持
- 兼容性好，适用于各种环境

**缺点**：
- 有延迟，实时性较差
- 频繁轮询可能对服务器造成压力

#### 3.2 WebSocket 推送模式（SignalR）

通过 SignalR 实现实时推送，服务器主动推送进度更新。

**SignalR Hub 端点**: `/hubs/training-progress`

**JavaScript 客户端示例**:

```javascript
// 1. 安装 SignalR 客户端
// npm install @microsoft/signalr

import * as signalR from "@microsoft/signalr";

// 2. 创建连接
const connection = new signalR.HubConnectionBuilder()
    .withUrl("http://localhost:5000/hubs/training-progress")
    .withAutomaticReconnect()
    .build();

// 3. 监听进度更新事件
connection.on("ProgressUpdated", (data) => {
    console.log(`训练进度: ${(data.progress * 100).toFixed(1)}%`);
    console.log(`消息: ${data.message || '无'}`);
    console.log(`时间戳: ${data.timestamp}`);
    
    // 更新 UI
    updateProgressBar(data.progress);
});

// 4. 启动连接
await connection.start();

// 5. 订阅特定训练任务
const jobId = "3fa85f64-5717-4562-b3fc-2c963f66afa6";
await connection.invoke("SubscribeToJob", jobId);

// 6. 完成后取消订阅
// await connection.invoke("UnsubscribeFromJob", jobId);

// 7. 断开连接
// await connection.stop();
```

**C# 客户端示例**:

```csharp
using Microsoft.AspNetCore.SignalR.Client;

// 1. 创建连接
var connection = new HubConnectionBuilder()
    .WithUrl("http://localhost:5000/hubs/training-progress")
    .WithAutomaticReconnect()
    .Build();

// 2. 监听进度更新事件
connection.On<object>("ProgressUpdated", (data) =>
{
    // 处理进度更新
    Console.WriteLine($"收到进度更新: {data}");
});

// 3. 启动连接
await connection.StartAsync();

// 4. 订阅特定训练任务
var jobId = "3fa85f64-5717-4562-b3fc-2c963f66afa6";
await connection.InvokeAsync("SubscribeToJob", jobId);

// 5. 完成后取消订阅
// await connection.InvokeAsync("UnsubscribeFromJob", jobId);

// 6. 断开连接
// await connection.StopAsync();
```

**推送消息格式**:

```json
{
  "jobId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "progress": 0.5,
  "message": "开始训练模型",
  "timestamp": "2024-01-01T10:00:00.000Z"
}
```

**训练进度阶段说明**:

| 进度值 | 阶段说明 |
|--------|----------|
| 0.05 | 开始扫描训练数据 |
| 0.15 | 加载训练数据到内存 |
| 0.25 | 构建训练管道 |
| 0.30 | 开始训练模型 |
| 0.80 | 评估模型性能 |
| 0.90 | 训练完成，保存模型 |
| 1.00 | 训练任务完成 |

**优点**：
- 实时性强，几乎零延迟
- 服务器主动推送，无需客户端轮询
- 支持双向通信

**缺点**：
- 需要客户端支持 WebSocket
- 需要维护长连接


## 模型文件命名规则

训练完成后，模型文件会保存到输出目录，文件名格式为：

```
noread-classifier-YYYYMMDD-HHmmss.zip
```

例如：
- `noread-classifier-20240101-103045.zip`

同时会创建一个 `noread-classifier-current.zip` 文件，指向最新的训练模型，用于在线推理服务的热切换。

## 配置说明

在 `appsettings.json` 中配置模型路径和训练选项：

```json
{
  "BarcodeMlModel": {
    "CurrentModelPath": "C:\\BarcodeImages\\Models\\noread-classifier-current.zip"
  },
  "TrainingOptions": {
    "TrainingRootDirectory": "C:\\BarcodeImages\\TrainingData",
    "OutputModelDirectory": "C:\\BarcodeImages\\Models",
    "ValidationSplitRatio": 0.2,
    "LearningRate": 0.01,
    "Epochs": 50,
    "BatchSize": 20,
    "MaxConcurrentTrainingJobs": 2,
    "EnableResourceMonitoring": true,
    "ResourceMonitoringIntervalSeconds": 10
  }
}
```

### 训练配置项说明

- `TrainingRootDirectory`: 训练数据根目录路径
- `OutputModelDirectory`: 输出模型文件存放目录路径
- `ValidationSplitRatio`: 验证集分割比例（0.0 到 1.0 之间）
- `LearningRate`: 学习率（0 到 1 之间，不含 0，默认 0.01）
- `Epochs`: 训练轮数（正整数，默认 50，建议范围 10-200）
- `BatchSize`: 批大小（正整数，默认 20，建议范围 8-128）
- `MaxConcurrentTrainingJobs`: 最大并发训练任务数量（默认值：1，建议值：2-4，取决于系统资源）
- `EnableResourceMonitoring`: 是否启用资源监控（默认值：false，启用后会定期记录 CPU 和内存使用情况）
- `ResourceMonitoringIntervalSeconds`: 资源监控间隔（秒）（默认值：5，建议值：10-30）

## 架构说明

### 服务分层

1. **Application 层**:
   - `ITrainingJobService`: 训练任务服务接口
   - `TrainingJobService`: 训练任务服务实现（管理任务队列和状态）
   - `TrainingWorker`: 后台工作器（BackgroundService），从队列中取出任务并执行

2. **Infrastructure.MLNet 层**:
   - `IImageClassificationTrainer`: 图像分类训练器接口
   - `MlNetImageClassificationTrainer`: ML.NET 训练器实现

3. **Service 层**:
   - `TrainingJobController`: HTTP API 控制器

### 训练流程

1. 客户端调用 `/api/training-job/start` 启动训练任务
2. `TrainingJobService` 生成任务 ID，将任务加入队列，返回任务 ID
3. `TrainingWorker` 后台服务从队列中取出任务
4. `TrainingWorker` 等待获取训练槽位（通过 SemaphoreSlim 控制并发度）
5. `TrainingWorker` 调用 `MlNetImageClassificationTrainer` 执行训练
6. 训练完成后保存模型文件并更新 `current` 模型
7. 释放训练槽位，更新任务状态为完成或失败

### 并发训练机制

- **并发控制**: 使用 `SemaphoreSlim` 控制同时执行的训练任务数量
- **槽位管理**: 每个训练任务在执行前必须获取一个训练槽位，执行完成后释放槽位
- **任务队列**: 当所有槽位都被占用时，新的训练任务会在队列中等待
- **资源监控**: 可选启用资源监控，定期记录 CPU 和内存使用情况
- **日志记录**: 详细记录训练任务的启动、执行、完成和槽位使用情况

### 资源监控

- **监控指标**: CPU 使用率、内存使用率、进程内存占用
- **跨平台支持**: 支持 Windows 和 Linux 平台
- **监控间隔**: 可配置监控间隔，默认为 5 秒
- **日志输出**: 监控信息会定期输出到日志，包括：
  - CPU 使用率（百分比）
  - 内存使用率（百分比）
  - 已用内存 / 总内存（MB）
  - 运行中的训练任务数 / 最大并发数

### 推理与训练解耦

- **推理服务** (`IBarcodeReadabilityAnalyzer`): 使用 `IOptionsMonitor` 监听模型配置变化，支持热切换
- **训练服务** (`ITrainingJobService`): 独立的后台任务，训练完成后通过更新模型文件实现模型切换

## 注意事项

1. **训练时间**: 训练时间取决于样本数量和硬件性能，可能需要几分钟到几小时
2. **资源占用**: 训练过程会占用较多 CPU 和内存资源
3. **并发训练**: 支持多个训练任务并发执行，通过 `MaxConcurrentTrainingJobs` 配置最大并发数量
4. **资源监控**: 启用资源监控后，系统会定期记录 CPU 和内存使用情况，便于性能优化
5. **错误处理**: 训练失败时会记录错误日志，可通过状态查询接口获取错误信息
6. **数据验证**: 训练前会验证训练目录是否存在，是否包含有效的图像文件

### 并发训练建议

- **单核或双核 CPU**: 建议 `MaxConcurrentTrainingJobs` 设置为 1
- **四核 CPU**: 建议 `MaxConcurrentTrainingJobs` 设置为 2
- **八核或更多 CPU**: 建议 `MaxConcurrentTrainingJobs` 设置为 2-4
- **内存限制**: 每个训练任务可能占用 2-4 GB 内存，请确保系统有足够的可用内存
- **磁盘 I/O**: 并发训练会增加磁盘 I/O 压力，建议使用 SSD 提升性能

## 示例：使用 curl 调用 API

### 启动训练任务

```bash
curl -X POST http://localhost:5000/api/training-job/start \
  -H "Content-Type: application/json" \
  -d '{
    "trainingRootDirectory": "C:\\BarcodeImages\\TrainingData",
    "outputModelDirectory": "C:\\BarcodeImages\\Models",
    "validationSplitRatio": 0.2,
    "learningRate": 0.01,
    "epochs": 50,
    "batchSize": 20,
    "remarks": "测试训练"
  }'
```

### 查询任务状态

```bash
curl http://localhost:5000/api/training-job/status/3fa85f64-5717-4562-b3fc-2c963f66afa6
```

## 故障排查

### 训练任务启动失败

**可能原因**:
- 训练根目录不存在或路径错误
- 训练根目录中没有子目录或图像文件
- 磁盘空间不足

**解决方法**:
1. 检查训练根目录路径是否正确
2. 确保训练数据按照规定的目录结构组织
3. 检查磁盘空间是否充足

### 训练任务执行失败

**可能原因**:
- 图像文件损坏或格式不支持
- 内存不足
- 标签名称不匹配

**解决方法**:
1. 查看日志获取详细错误信息
2. 验证图像文件完整性
3. 确保子目录名称与 `NoreadReason` 枚举值匹配

## 未来改进

1. ~~支持训练进度实时更新~~ (已完成)
2. ~~添加模型评估指标（准确率、召回率等）~~ (已完成)
3. ~~支持多个训练任务并发执行~~ (已完成)
4. ~~添加训练资源（CPU、内存）监控~~ (已完成)
5. 支持训练任务取消
6. ~~添加训练参数配置（学习率、训练轮数等）~~ (已完成)
7. 支持训练过程中的指标监控（损失函数值、验证集准确率等）
8. 添加混淆矩阵可视化界面
9. 支持导出详细的评估报告
10. 支持基于资源使用情况的智能调度
