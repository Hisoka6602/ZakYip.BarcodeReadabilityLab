# Windows 服务安装与配置指南

## 概述

ZakYip.BarcodeReadabilityLab.Service 现已支持作为 Windows 服务运行，可以实现：

- 自动监控指定目录中的条码图片
- 使用 ML.NET 对图片进行可读性分析
- 将低置信度或无法分析的图片自动移动到指定目录
- 通过 HTTP API 触发训练任务
- 后台处理训练任务队列

## 前置要求

- Windows 操作系统（推荐 Windows Server 2016 或更高版本，Windows 10/11 也可以）
- .NET 8.0 Runtime 或更高版本
- 管理员权限（用于安装和管理 Windows 服务）

## 配置文件说明

在安装服务前，需要编辑 `appsettings.json` 配置文件，设置以下关键参数：

### BarcodeAnalyzerOptions（必需）

```json
"BarcodeAnalyzerOptions": {
  "WatchDirectory": "C:\\BarcodeImages\\Monitor",
  "UnresolvedDirectory": "C:\\BarcodeImages\\UnableToAnalyze",
  "ConfidenceThreshold": 0.9,
  "IsRecursive": false
}
```

- **WatchDirectory**: 监控的目录路径，新图片放入此目录后会自动分析
- **UnresolvedDirectory**: 无法分析或低置信度图片的存放目录
- **ConfidenceThreshold**: 置信度阈值（0.0-1.0），低于此值的图片会被移动到 UnresolvedDirectory
- **IsRecursive**: 是否递归监控子目录

### TrainingOptions（必需）

```json
"TrainingOptions": {
  "TrainingRootDirectory": "C:\\BarcodeImages\\TrainingData",
  "OutputModelDirectory": "C:\\BarcodeImages\\Models",
  "ValidationSplitRatio": 0.2
}
```

- **TrainingRootDirectory**: 训练数据根目录
- **OutputModelDirectory**: 训练后的模型文件输出目录
- **ValidationSplitRatio**: 验证集分割比例（可选，0.0-1.0）

### BarcodeMlModel（必需）

```json
"BarcodeMlModel": {
  "CurrentModelPath": "C:\\BarcodeImages\\Models\\noread-classifier-current.zip"
}
```

- **CurrentModelPath**: 当前用于推理的 ML.NET 模型文件路径

### ApiSettings（可选）

```json
"ApiSettings": {
  "Port": 5000,
  "Urls": "http://localhost:5000"
}
```

- **Urls**: HTTP API 监听地址和端口

## 安装步骤

### 1. 发布应用程序

在项目根目录下运行以下命令，发布应用程序：

```powershell
dotnet publish src\ZakYip.BarcodeReadabilityLab.Service\ZakYip.BarcodeReadabilityLab.Service.csproj -c Release -o C:\Services\BarcodeReadabilityService
```

### 2. 创建必要的目录

确保配置文件中指定的目录已创建：

```powershell
New-Item -Path "C:\BarcodeImages\Monitor" -ItemType Directory -Force
New-Item -Path "C:\BarcodeImages\UnableToAnalyze" -ItemType Directory -Force
New-Item -Path "C:\BarcodeImages\TrainingData" -ItemType Directory -Force
New-Item -Path "C:\BarcodeImages\Models" -ItemType Directory -Force
```

### 3. 配置 appsettings.json

编辑发布目录下的 `appsettings.json` 文件，确保所有路径和配置正确。

### 4. 使用 sc.exe 安装服务

以管理员身份运行 PowerShell，执行以下命令：

```powershell
sc.exe create BarcodeReadabilityService binPath="C:\Services\BarcodeReadabilityService\ZakYip.BarcodeReadabilityLab.Service.exe" start=auto DisplayName="条码可读性分析服务"
```

参数说明：
- **binPath**: 可执行文件的完整路径
- **start=auto**: 设置为自动启动（系统启动时自动运行）
- **DisplayName**: 服务的显示名称

### 5. 启动服务

```powershell
sc.exe start BarcodeReadabilityService
```

或者在"服务"管理器（services.msc）中找到"条码可读性分析服务"并启动。

## 服务管理

### 停止服务

```powershell
sc.exe stop BarcodeReadabilityService
```

### 删除服务

```powershell
sc.exe delete BarcodeReadabilityService
```

### 查看服务状态

```powershell
sc.exe query BarcodeReadabilityService
```

### 修改服务配置

如果需要修改服务配置（如启动类型），可以使用 `sc.exe config` 命令：

```powershell
sc.exe config BarcodeReadabilityService start=demand
```

## 日志查看

服务的日志输出到以下位置：

1. **Windows 事件查看器**：
   - 打开"事件查看器"（eventvwr.msc）
   - 导航到"Windows 日志" -> "应用程序"
   - 筛选源为"BarcodeReadabilityService"的日志

2. **控制台日志**（开发调试时）：
   如果以控制台模式运行（不作为服务），日志会输出到控制台。

## 运行验证

### 1. 检查服务状态

确保服务正在运行：

```powershell
Get-Service BarcodeReadabilityService
```

输出应显示 `Status: Running`。

### 2. 测试目录监控

将一张条码图片复制到监控目录（例如 `C:\BarcodeImages\Monitor`），观察：
- 图片是否被分析
- 如果置信度低，图片是否被移动到 `UnableToAnalyze` 目录
- 日志中是否有相应的分析记录

### 3. 测试 HTTP API

访问 HTTP API 端点，测试训练任务触发：

```powershell
Invoke-WebRequest -Uri "http://localhost:5000/api/training/status" -Method GET
```

## 故障排查

### 服务无法启动

1. 检查 `appsettings.json` 配置是否正确
2. 确保所有目录已创建且有适当的访问权限
3. 检查模型文件路径是否正确
4. 查看 Windows 事件日志中的错误信息

### 目录监控不工作

1. 确认 `WatchDirectory` 路径正确且存在
2. 检查服务是否有读写该目录的权限
3. 查看日志确认 DirectoryMonitoringWorker 是否启动

### 图片未被移动

1. 确认 `UnresolvedDirectory` 路径正确且存在
2. 检查置信度阈值设置是否合理
3. 查看日志中的分析结果和置信度值

### 训练任务不执行

1. 确认 TrainingWorker 是否正常启动
2. 检查训练数据目录结构是否正确
3. 查看日志中的训练任务状态

## 生产环境建议

1. **日志配置**：建议配置日志持久化到文件，而不仅仅依赖控制台输出
2. **监控告警**：集成应用程序性能监控（APM）工具，及时发现异常
3. **定期备份**：定期备份模型文件和配置文件
4. **资源限制**：根据实际负载调整服务的资源配额
5. **安全加固**：限制服务运行账户的权限，仅授予必要的文件系统访问权限

## 更新服务

当需要更新服务时：

1. 停止服务：`sc.exe stop BarcodeReadabilityService`
2. 替换可执行文件和配置文件
3. 启动服务：`sc.exe start BarcodeReadabilityService`

或者使用脚本自动化更新过程。

## 卸载服务

如果需要完全卸载服务：

1. 停止服务：`sc.exe stop BarcodeReadabilityService`
2. 删除服务：`sc.exe delete BarcodeReadabilityService`
3. 删除服务文件夹：`Remove-Item -Path "C:\Services\BarcodeReadabilityService" -Recurse -Force`

## 支持

如有问题，请查看项目文档或提交 Issue。
