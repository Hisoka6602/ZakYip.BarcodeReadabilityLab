# 训练任务服务使用说明

## 概述

本服务实现了基于 ML.NET 的图像分类模型训练功能，支持异步训练任务管理和模型热切换。

## 主要特性

1. **异步训练任务管理**: 通过任务队列管理训练任务，支持多个训练任务排队执行
2. **状态追踪**: 实时追踪训练任务状态（排队中、运行中、已完成、失败）
3. **版本化模型**: 训练完成后自动保存带时间戳的模型文件
4. **模型热切换**: 训练完成后自动更新当前在线模型，无需重启服务
5. **中文日志**: 所有日志和错误信息使用中文，便于调试和监控

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
  "status": "Running",
  "progress": 0.5,
  "startTime": "2024-01-01T10:00:00Z",
  "completedTime": null,
  "errorMessage": null,
  "remarks": "第一次训练"
}
```

**状态说明**:
- `Queued` (1): 排队中
- `Running` (2): 运行中
- `Completed` (3): 已完成
- `Failed` (4): 失败
- `Cancelled` (5): 已取消

## 模型文件命名规则

训练完成后，模型文件会保存到输出目录，文件名格式为：

```
noread-classifier-YYYYMMDD-HHmmss.zip
```

例如：
- `noread-classifier-20240101-103045.zip`

同时会创建一个 `noread-classifier-current.zip` 文件，指向最新的训练模型，用于在线推理服务的热切换。

## 配置说明

在 `appsettings.json` 中配置模型路径：

```json
{
  "BarcodeMlModel": {
    "CurrentModelPath": "C:\\BarcodeImages\\Models\\noread-classifier-current.zip"
  }
}
```

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
4. `TrainingWorker` 调用 `MlNetImageClassificationTrainer` 执行训练
5. 训练完成后保存模型文件并更新 `current` 模型
6. 更新任务状态为完成或失败

### 推理与训练解耦

- **推理服务** (`IBarcodeReadabilityAnalyzer`): 使用 `IOptionsMonitor` 监听模型配置变化，支持热切换
- **训练服务** (`ITrainingJobService`): 独立的后台任务，训练完成后通过更新模型文件实现模型切换

## 注意事项

1. **训练时间**: 训练时间取决于样本数量和硬件性能，可能需要几分钟到几小时
2. **资源占用**: 训练过程会占用较多 CPU 和内存资源
3. **并发限制**: 当前实现为单线程处理，一次只能执行一个训练任务
4. **错误处理**: 训练失败时会记录错误日志，可通过状态查询接口获取错误信息
5. **数据验证**: 训练前会验证训练目录是否存在，是否包含有效的图像文件

## 示例：使用 curl 调用 API

### 启动训练任务

```bash
curl -X POST http://localhost:5000/api/training-job/start \
  -H "Content-Type: application/json" \
  -d '{
    "trainingRootDirectory": "C:\\BarcodeImages\\TrainingData",
    "outputModelDirectory": "C:\\BarcodeImages\\Models",
    "validationSplitRatio": 0.2,
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

1. 支持训练进度实时更新
2. 支持训练任务取消
3. 支持多个训练任务并发执行
4. 添加训练参数配置（学习率、训练轮数等）
5. 添加模型评估指标（准确率、召回率等）
