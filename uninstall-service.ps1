# Windows 服务卸载脚本
# 此脚本需要以管理员身份运行

param(
    [string]$ServiceName = "BarcodeReadabilityService"
)

# 检查管理员权限
$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
$isAdmin = $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Error "此脚本需要管理员权限。请以管理员身份运行 PowerShell。"
    exit 1
}

Write-Host "开始卸载 Windows 服务..." -ForegroundColor Yellow

# 1. 检查服务是否存在
$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue

if (-not $service) {
    Write-Host "服务 '$ServiceName' 不存在或已被删除。" -ForegroundColor Gray
    exit 0
}

Write-Host "找到服务: $($service.DisplayName)" -ForegroundColor Cyan
Write-Host "当前状态: $($service.Status)" -ForegroundColor Cyan

# 2. 停止服务（如果正在运行）
if ($service.Status -eq 'Running') {
    Write-Host "正在停止服务..." -ForegroundColor Yellow
    try {
        Stop-Service -Name $ServiceName -Force
        Write-Host "服务已停止。" -ForegroundColor Green
    } catch {
        Write-Error "停止服务时发生错误: $_"
        exit 1
    }
}

# 3. 删除服务
Write-Host "正在删除服务..." -ForegroundColor Yellow
$result = sc.exe delete $ServiceName

if ($LASTEXITCODE -eq 0) {
    Write-Host "服务已成功删除！" -ForegroundColor Green
} else {
    Write-Error "删除服务失败！错误代码: $LASTEXITCODE"
    exit 1
}

Write-Host "`n卸载完成！" -ForegroundColor Green
Write-Host "注意：服务文件和数据目录未被删除。" -ForegroundColor Yellow
Write-Host "如需完全清理，请手动删除以下目录:" -ForegroundColor Yellow
Write-Host "  - C:\Services\BarcodeReadabilityService" -ForegroundColor Gray
Write-Host "  - C:\BarcodeImages" -ForegroundColor Gray
