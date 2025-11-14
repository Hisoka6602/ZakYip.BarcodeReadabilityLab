# 快速开始 (Quick Start)

## 5 分钟快速上手

### 1. 克隆并构建项目

```powershell
git clone https://github.com/Hisoka6602/ZakYip.BarcodeReadabilityLab.git
cd ZakYip.BarcodeReadabilityLab
dotnet build
```

### 2. 创建测试目录

```powershell
# 创建所需的目录结构
New-Item -Path "C:\BarcodeImages\Monitor" -ItemType Directory -Force
New-Item -Path "C:\BarcodeImages\UnableToAnalyze" -ItemType Directory -Force
New-Item -Path "C:\BarcodeImages\TrainingData\模糊" -ItemType Directory -Force
New-Item -Path "C:\BarcodeImages\TrainingData\清晰" -ItemType Directory -Force
New-Item -Path "C:\BarcodeImages\Models" -ItemType Directory -Force
```

### 3. 准备训练数据

将一些测试图片放入训练目录：

```
C:\BarcodeImages\TrainingData\
├── 模糊\          (放入至少 10 张模糊图片)
│   ├── blur1.jpg
│   ├── blur2.jpg
│   └── ...
└── 清晰\          (放入至少 10 张清晰图片)
    ├── clear1.jpg
    ├── clear2.jpg
    └── ...
```

### 4. 启动服务

```powershell
dotnet run --project src/ZakYip.BarcodeReadabilityLab.Service
```

你应该看到类似以下的输出：
```
info: ZakYip.BarcodeReadabilityLab.Service.Services.ImageMonitoringService[0]
      Image Monitoring Service started
info: ZakYip.BarcodeReadabilityLab.Service.Services.ImageMonitoringService[0]
      Directories ensured: Monitor=C:\BarcodeImages\Monitor, UnableToAnalyze=C:\BarcodeImages\UnableToAnalyze
info: ZakYip.BarcodeReadabilityLab.Service.Services.ImageMonitoringService[0]
      File system watcher started for path: C:\BarcodeImages\Monitor
```

### 5. 训练模型

在另一个 PowerShell 窗口中：

```powershell
# 准备训练请求
$training = @{
    trainingDataPath = "C:\BarcodeImages\TrainingData"
} | ConvertTo-Json

# 启动训练
$result = Invoke-RestMethod -Uri "http://localhost:5000/api/training/start" `
    -Method Post `
    -ContentType "application/json" `
    -Body $training

# 保存任务 ID
$taskId = $result.taskId
Write-Host "训练任务 ID: $taskId"
```

### 6. 监控训练进度

```powershell
# 查询训练状态
do {
    Start-Sleep -Seconds 3
    $status = Invoke-RestMethod -Uri "http://localhost:5000/api/training/status/$taskId" -Method Get
    $progress = [math]::Round($status.progress * 100, 2)
    Write-Host "训练状态: $($status.state) - 进度: $progress%"
} while ($status.state -eq "Running")

Write-Host "训练完成！最终状态: $($status.state)"
```

### 7. 测试图片分析

```powershell
# 复制测试图片到监控目录
Copy-Item "C:\path\to\your\test\image.jpg" -Destination "C:\BarcodeImages\Monitor\"
```

服务会自动：
1. 检测到新图片
2. 使用训练好的模型进行分析
3. 如果置信度 >= 90%：删除图片（已成功分析）
4. 如果置信度 < 90%：复制到 `C:\BarcodeImages\UnableToAnalyze`

### 8. 查看结果

```powershell
# 查看无法分析的图片
Get-ChildItem "C:\BarcodeImages\UnableToAnalyze" | Format-Table Name, LastWriteTime

# 查看分析原因
Get-Content "C:\BarcodeImages\UnableToAnalyze\*.txt"
```

## 测试 API 端点

### 使用浏览器或 cURL

```bash
# 查看训练状态（在浏览器中访问）
http://localhost:5000/api/training/status/{taskId}

# 使用 cURL 启动训练
curl -X POST http://localhost:5000/api/training/start \
  -H "Content-Type: application/json" \
  -d "{\"trainingDataPath\":\"C:\\\\BarcodeImages\\\\TrainingData\"}"
```

## 常见问题

### Q: 服务启动失败
**A:** 检查配置文件中的路径是否存在，确保有读写权限。

### Q: 训练失败
**A:** 确保每个类别至少有 10 张图片，且图片格式正确（.jpg, .png, .bmp）。

### Q: 所有图片都被标记为"无法分析"
**A:** 可能模型尚未训练或训练数据不足。先完成第一次训练。

### Q: API 返回 404
**A:** 确认服务已启动，检查端口 5000 是否被占用。

## 下一步

- 阅读 [README.md](README.md) 了解详细功能说明
- 阅读 [USAGE.md](USAGE.md) 学习更多使用场景
- 阅读 [DEPLOYMENT.md](DEPLOYMENT.md) 了解如何部署为 Windows 服务

## 配置调整

如果需要修改配置，编辑 `src/ZakYip.BarcodeReadabilityLab.Service/appsettings.json`：

```json
{
  "BarcodeReadabilityService": {
    "ConfidenceThreshold": 0.85  // 降低阈值，更少图片进入人工审核
  }
}
```

重启服务使配置生效。
