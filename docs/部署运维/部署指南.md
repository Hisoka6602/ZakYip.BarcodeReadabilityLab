# 部署指南 (Deployment Guide)

## Windows 服务部署 (Windows Service Deployment)

### 方法 1: 使用 sc 命令（推荐）

#### 1. 发布应用程序

```powershell
# 发布为自包含应用
dotnet publish src/ZakYip.BarcodeReadabilityLab.Service/ZakYip.BarcodeReadabilityLab.Service.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -o C:\Services\BarcodeReadabilityService

# 或发布为框架依赖应用（需要安装 .NET Runtime）
dotnet publish src/ZakYip.BarcodeReadabilityLab.Service/ZakYip.BarcodeReadabilityLab.Service.csproj `
    -c Release `
    -r win-x64 `
    --self-contained false `
    -o C:\Services\BarcodeReadabilityService
```

#### 2. 配置 appsettings.json

编辑 `C:\Services\BarcodeReadabilityService\appsettings.json` 设置正确的路径：

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

#### 3. 创建服务

以**管理员身份**运行 PowerShell：

```powershell
# 创建 Windows 服务
sc.exe create BarcodeReadabilityService `
    binPath= "C:\Services\BarcodeReadabilityService\ZakYip.BarcodeReadabilityLab.Service.exe" `
    start= auto `
    DisplayName= "Barcode Readability Analysis Service"

# 设置服务描述
sc.exe description BarcodeReadabilityService "监控条码图片并使用 ML.NET 分析 NoRead 原因"
```

#### 4. 启动服务

```powershell
# 启动服务
sc.exe start BarcodeReadabilityService

# 查看服务状态
sc.exe query BarcodeReadabilityService

# 或使用 Get-Service
Get-Service BarcodeReadabilityService
```

#### 5. 停止和删除服务（如需要）

```powershell
# 停止服务
sc.exe stop BarcodeReadabilityService

# 删除服务
sc.exe delete BarcodeReadabilityService
```

### 方法 2: 使用 PowerShell New-Service

```powershell
# 创建服务
New-Service -Name BarcodeReadabilityService `
    -BinaryPathName "C:\Services\BarcodeReadabilityService\ZakYip.BarcodeReadabilityLab.Service.exe" `
    -DisplayName "Barcode Readability Analysis Service" `
    -Description "监控条码图片并使用 ML.NET 分析 NoRead 原因" `
    -StartupType Automatic

# 启动服务
Start-Service BarcodeReadabilityService

# 停止服务（如需要）
Stop-Service BarcodeReadabilityService

# 删除服务（如需要）
Remove-Service BarcodeReadabilityService
```

## 服务管理 (Service Management)

### 查看服务日志

Windows 服务的日志可以在以下位置查看：

1. **Windows 事件查看器**：
   ```powershell
   # 打开事件查看器
   eventvwr.msc
   ```
   导航到：Windows 日志 -> 应用程序

2. **服务输出**（开发调试）：
   ```powershell
   # 直接运行查看控制台输出
   C:\Services\BarcodeReadabilityService\ZakYip.BarcodeReadabilityLab.Service.exe
   ```

### 更新服务

```powershell
# 1. 停止服务
Stop-Service BarcodeReadabilityService

# 2. 备份旧版本（可选）
Copy-Item -Path "C:\Services\BarcodeReadabilityService" `
    -Destination "C:\Services\BarcodeReadabilityService_backup_$(Get-Date -Format 'yyyyMMdd')" `
    -Recurse

# 3. 发布新版本
dotnet publish src/ZakYip.BarcodeReadabilityLab.Service/ZakYip.BarcodeReadabilityLab.Service.csproj `
    -c Release -r win-x64 --self-contained true `
    -o C:\Services\BarcodeReadabilityService

# 4. 恢复配置文件（如果被覆盖）
# Copy-Item -Path "C:\Services\BarcodeReadabilityService_backup\appsettings.json" `
#     -Destination "C:\Services\BarcodeReadabilityService\appsettings.json"

# 5. 启动服务
Start-Service BarcodeReadabilityService
```

### 配置服务恢复选项

在服务失败时自动重启：

```powershell
# 设置失败后的恢复操作
sc.exe failure BarcodeReadabilityService reset= 86400 actions= restart/60000/restart/60000/restart/60000
```

这将在服务失败后每次等待 60 秒后重启，重置期为 24 小时（86400 秒）。

## 防火墙配置 (Firewall Configuration)

如果需要从其他机器访问 API：

```powershell
# 允许入站连接
New-NetFirewallRule -DisplayName "Barcode Service API" `
    -Direction Inbound `
    -LocalPort 5000 `
    -Protocol TCP `
    -Action Allow
```

然后修改 `appsettings.json` 中的 `Urls` 配置：

```json
{
  "ApiSettings": {
    "Port": 5000,
    "Urls": "http://0.0.0.0:5000"
  }
}
```

## 开发环境运行 (Development Environment)

### 直接运行

```powershell
cd C:\path\to\ZakYip.BarcodeReadabilityLab
dotnet run --project src/ZakYip.BarcodeReadabilityLab.Service
```

### 作为控制台应用调试

在 Visual Studio 中直接按 F5 运行，或：

```powershell
dotnet build
dotnet run --project src/ZakYip.BarcodeReadabilityLab.Service --no-build
```

## 故障排除 (Troubleshooting)

### 服务无法启动

1. 检查事件查看器中的错误日志
2. 确认 .NET Runtime 已安装（如果使用框架依赖部署）
3. 确认配置的目录存在且有读写权限
4. 尝试在控制台模式运行查看详细错误

```powershell
C:\Services\BarcodeReadabilityService\ZakYip.BarcodeReadabilityLab.Service.exe
```

### API 无法访问

1. 检查防火墙设置
2. 确认服务已启动
3. 检查端口是否被占用

```powershell
# 检查端口占用
netstat -ano | findstr :5000
```

### 模型训练失败

1. 确认训练数据目录结构正确
2. 确保每个类别至少有足够的图片
3. 检查图片格式是否支持
4. 查看日志中的详细错误信息

## 性能优化建议 (Performance Optimization)

1. **模型路径**：使用 SSD 存储模型和训练数据
2. **监控目录**：避免监控包含大量文件的目录
3. **置信度阈值**：根据实际需求调整阈值
4. **训练频率**：避免频繁重新训练，建议积累一定数量的新样本后再训练

## 安全建议 (Security Recommendations)

1. **API 访问控制**：
   - 在生产环境使用 HTTPS
   - 添加身份验证和授权机制
   - 限制 API 访问的 IP 地址

2. **文件系统权限**：
   - 服务账户只授予必要的目录权限
   - 定期清理旧的无法分析图片

3. **日志管理**：
   - 配置日志轮转
   - 避免日志中记录敏感信息
