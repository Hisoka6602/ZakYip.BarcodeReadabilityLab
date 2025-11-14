# 使用示例 (Usage Examples)

## 1. 初始设置 (Initial Setup)

### 创建必要的目录结构 (Create Required Directory Structure)

```powershell
# 创建监控目录
New-Item -Path "C:\BarcodeImages\Monitor" -ItemType Directory -Force

# 创建无法分析目录
New-Item -Path "C:\BarcodeImages\UnableToAnalyze" -ItemType Directory -Force

# 创建训练数据目录及标签子目录
New-Item -Path "C:\BarcodeImages\TrainingData" -ItemType Directory -Force
New-Item -Path "C:\BarcodeImages\TrainingData\模糊" -ItemType Directory -Force
New-Item -Path "C:\BarcodeImages\TrainingData\损坏" -ItemType Directory -Force
New-Item -Path "C:\BarcodeImages\TrainingData\污渍" -ItemType Directory -Force
New-Item -Path "C:\BarcodeImages\TrainingData\光照不足" -ItemType Directory -Force

# 创建模型目录
New-Item -Path "C:\BarcodeImages\Models" -ItemType Directory -Force
```

## 2. 准备训练数据 (Prepare Training Data)

将已分类的图片放入对应的标签目录：

```
C:\BarcodeImages\TrainingData\
├── 模糊\
│   ├── blur_001.jpg
│   ├── blur_002.jpg
│   └── ...
├── 损坏\
│   ├── damaged_001.jpg
│   ├── damaged_002.jpg
│   └── ...
├── 污渍\
│   ├── stain_001.jpg
│   └── ...
└── 光照不足\
    ├── lowlight_001.jpg
    └── ...
```

## 3. API 调用示例 (API Call Examples)

### 使用 PowerShell

#### 启动训练
```powershell
$body = @{
    trainingDataPath = "C:\BarcodeImages\TrainingData"
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri "http://localhost:5000/api/training/start" `
    -Method Post `
    -ContentType "application/json" `
    -Body $body

Write-Host "Training Task ID: $($response.taskId)"
```

#### 查询训练状态
```powershell
$taskId = "your-task-id-here"
$status = Invoke-RestMethod -Uri "http://localhost:5000/api/training/status/$taskId" `
    -Method Get

Write-Host "State: $($status.state)"
Write-Host "Progress: $($status.progress)"
Write-Host "Message: $($status.message)"
```

#### 取消训练
```powershell
$taskId = "your-task-id-here"
$response = Invoke-RestMethod -Uri "http://localhost:5000/api/training/cancel/$taskId" `
    -Method Post

Write-Host $response.message
```

### 使用 cURL

#### 启动训练
```bash
curl -X POST http://localhost:5000/api/training/start \
  -H "Content-Type: application/json" \
  -d '{"trainingDataPath":"C:\\BarcodeImages\\TrainingData"}'
```

#### 查询训练状态
```bash
curl -X GET http://localhost:5000/api/training/status/{taskId}
```

#### 取消训练
```bash
curl -X POST http://localhost:5000/api/training/cancel/{taskId}
```

## 4. 工作流程示例 (Workflow Example)

### 场景：首次训练和自动监控

```powershell
# 1. 准备训练数据（手动分类至少 20-30 张每个类别的图片）
# 将图片分类放入 C:\BarcodeImages\TrainingData 的各个子目录

# 2. 启动服务
cd C:\path\to\ZakYip.BarcodeReadabilityLab
dotnet run --project src/ZakYip.BarcodeReadabilityLab.Service

# 3. 在另一个终端中，启动训练
$training = @{
    trainingDataPath = "C:\BarcodeImages\TrainingData"
} | ConvertTo-Json

$result = Invoke-RestMethod -Uri "http://localhost:5000/api/training/start" `
    -Method Post -ContentType "application/json" -Body $training

$taskId = $result.taskId
Write-Host "Training started with task ID: $taskId"

# 4. 等待训练完成（轮询状态）
do {
    Start-Sleep -Seconds 5
    $status = Invoke-RestMethod -Uri "http://localhost:5000/api/training/status/$taskId" -Method Get
    Write-Host "Training progress: $($status.progress * 100)% - $($status.state)"
} while ($status.state -eq "Running")

Write-Host "Training completed with state: $($status.state)"

# 5. 现在可以将新图片放入监控目录进行自动分析
Copy-Item "C:\NewBarcodeImages\*.jpg" -Destination "C:\BarcodeImages\Monitor"

# 6. 检查无法分析的图片
Get-ChildItem "C:\BarcodeImages\UnableToAnalyze" | Format-Table Name, LastWriteTime
```

## 5. 持续改进流程 (Continuous Improvement)

```powershell
# 1. 定期检查无法分析的图片
$unanalyzedImages = Get-ChildItem "C:\BarcodeImages\UnableToAnalyze\*.jpg"
Write-Host "Found $($unanalyzedImages.Count) unanalyzed images"

# 2. 人工分类这些图片到训练数据目录
# 手动将图片移动到对应的标签目录

# 3. 重新训练模型
$retraining = @{
    trainingDataPath = "C:\BarcodeImages\TrainingData"
} | ConvertTo-Json

$result = Invoke-RestMethod -Uri "http://localhost:5000/api/training/start" `
    -Method Post -ContentType "application/json" -Body $retraining

Write-Host "Retraining started: $($result.taskId)"

# 4. 模型会在训练完成后自动加载，无需重启服务
```

## 6. 配置不同的置信度阈值 (Configure Different Confidence Thresholds)

修改 `appsettings.json`：

```json
{
  "BarcodeReadabilityService": {
    "ConfidenceThreshold": 0.85
  }
}
```

重启服务使配置生效。置信度阈值越低，越多图片会被自动分类；越高，越多图片会进入人工审核。

## 7. 监控服务日志 (Monitor Service Logs)

服务会输出详细的日志信息，包括：
- 文件检测事件
- 预测结果和置信度
- 训练进度
- 错误信息

查看日志以了解服务运行状态。

## 注意事项 (Important Notes)

1. **训练数据质量**：确保每个类别至少有 20-30 张高质量的图片
2. **图片格式**：支持 .jpg, .jpeg, .png, .bmp 格式
3. **目录权限**：确保服务有权限读写配置的目录
4. **模型更新**：训练完成后模型会自动加载，无需重启服务
5. **并发训练**：同一时间可以有多个训练任务，但建议等待上一个完成
6. **取消训练**：训练可以随时取消，但已完成的部分无法恢复
