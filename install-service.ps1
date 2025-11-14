# Windows 服务安装脚本
# 此脚本需要以管理员身份运行

param(
    [string]$ServicePath = "C:\Services\BarcodeReadabilityService",
    [string]$ServiceName = "BarcodeReadabilityService",
    [string]$DisplayName = "条码可读性分析服务"
)

# 检查管理员权限
$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
$isAdmin = $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Error "此脚本需要管理员权限。请以管理员身份运行 PowerShell。"
    exit 1
}

Write-Host "开始安装 Windows 服务..." -ForegroundColor Green

# 1. 检查服务是否已存在
$existingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($existingService) {
    Write-Host "服务 '$ServiceName' 已存在。正在停止并删除..." -ForegroundColor Yellow
    
    if ($existingService.Status -eq 'Running') {
        Stop-Service -Name $ServiceName -Force
        Write-Host "服务已停止。" -ForegroundColor Green
    }
    
    sc.exe delete $ServiceName
    Start-Sleep -Seconds 2
    Write-Host "旧服务已删除。" -ForegroundColor Green
}

# 2. 创建必要的目录
$directories = @(
    "C:\BarcodeImages\Monitor",
    "C:\BarcodeImages\UnableToAnalyze",
    "C:\BarcodeImages\TrainingData",
    "C:\BarcodeImages\Models"
)

foreach ($dir in $directories) {
    if (-not (Test-Path $dir)) {
        New-Item -Path $dir -ItemType Directory -Force | Out-Null
        Write-Host "已创建目录: $dir" -ForegroundColor Green
    } else {
        Write-Host "目录已存在: $dir" -ForegroundColor Gray
    }
}

# 3. 检查服务可执行文件是否存在
$exePath = Join-Path $ServicePath "ZakYip.BarcodeReadabilityLab.Service.exe"
if (-not (Test-Path $exePath)) {
    Write-Error "服务可执行文件不存在: $exePath"
    Write-Host "请先发布应用程序到 $ServicePath 目录。" -ForegroundColor Yellow
    Write-Host "命令示例: dotnet publish src\ZakYip.BarcodeReadabilityLab.Service\ZakYip.BarcodeReadabilityLab.Service.csproj -c Release -o $ServicePath" -ForegroundColor Yellow
    exit 1
}

# 4. 安装服务
Write-Host "正在安装服务..." -ForegroundColor Cyan
$result = sc.exe create $ServiceName binPath= $exePath start= auto DisplayName= $DisplayName

if ($LASTEXITCODE -eq 0) {
    Write-Host "服务安装成功！" -ForegroundColor Green
    
    # 5. 配置服务描述
    sc.exe description $ServiceName "自动监控条码图片目录，执行 ML.NET 推理，并管理训练任务。"
    
    # 6. 启动服务
    Write-Host "正在启动服务..." -ForegroundColor Cyan
    Start-Service -Name $ServiceName
    
    # 等待服务启动
    Start-Sleep -Seconds 2
    
    # 检查服务状态
    $service = Get-Service -Name $ServiceName
    if ($service.Status -eq 'Running') {
        Write-Host "服务已成功启动！" -ForegroundColor Green
        Write-Host "服务名称: $($service.Name)" -ForegroundColor Cyan
        Write-Host "显示名称: $($service.DisplayName)" -ForegroundColor Cyan
        Write-Host "状态: $($service.Status)" -ForegroundColor Cyan
    } else {
        Write-Warning "服务已安装但未能启动。状态: $($service.Status)"
        Write-Host "请检查配置文件和日志。" -ForegroundColor Yellow
    }
} else {
    Write-Error "服务安装失败！错误代码: $LASTEXITCODE"
    exit 1
}

Write-Host "`n安装完成！" -ForegroundColor Green
Write-Host "使用以下命令管理服务:" -ForegroundColor Cyan
Write-Host "  查看状态: Get-Service $ServiceName" -ForegroundColor Gray
Write-Host "  停止服务: Stop-Service $ServiceName" -ForegroundColor Gray
Write-Host "  启动服务: Start-Service $ServiceName" -ForegroundColor Gray
Write-Host "  重启服务: Restart-Service $ServiceName" -ForegroundColor Gray
