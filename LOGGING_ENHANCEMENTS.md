# 日志配置增强文档

## 概述

本文档描述了系统日志配置的完善功能，包括日志文件轮转、动态日志级别调整、审计日志和性能监控等高级特性。

## 新增功能

### 1. 多环境日志配置

系统现在支持为不同环境配置独立的日志策略：

#### Development 环境
- **日志级别**: Debug
- **日志输出**: 控制台和文件
- **轮转策略**: 按天
- **目标**: 最大化调试信息

#### Staging 环境
- **日志级别**: Information
- **日志输出**: 控制台和文件
- **轮转策略**: 按小时（Hour）
- **文件大小限制**: 50MB
- **保留时长**: 168 小时（7天）
- **日志路径**: `logs/staging/barcode-lab-.log`
- **目标**: 模拟生产环境，便于测试

#### Production 环境
- **日志级别**: Warning（应用代码为 Information）
- **日志输出**: 控制台和文件
- **轮转策略**: 按天
- **文件大小限制**: 200MB
- **保留时长**: 90 天
- **日志路径**: `logs/production/barcode-lab-.log`
- **目标**: 性能优化，仅记录关键信息

### 2. 日志文件轮转策略

#### 支持的轮转间隔
- `Minute` - 每分钟创建新日志文件
- `Hour` - 每小时创建新日志文件
- `Day` - 每天创建新日志文件（默认）
- `Month` - 每月创建新日志文件
- `Year` - 每年创建新日志文件
- `Infinite` - 不自动轮转

#### 文件大小限制
- 支持配置 `fileSizeLimitBytes` 参数
- 当文件超过大小限制时自动创建新文件
- 可通过 `rollOnFileSizeLimit` 启用或禁用

#### 文件保留策略
- 通过 `retainedFileCountLimit` 配置保留的文件数量
- 自动删除超出限制的旧日志文件

### 3. 动态日志级别调整

#### API 端点

##### 获取当前日志级别
```http
GET /api/logging/level
```

**响应示例：**
```json
{
  "level": "Information",
  "message": "当前日志级别获取成功"
}
```

##### 设置日志级别
```http
PUT /api/logging/level
Content-Type: application/json

{
  "level": "Debug",
  "operator": "admin"
}
```

**支持的日志级别：**
- `Verbose` - 最详细的日志
- `Debug` - 调试信息
- `Information` - 常规信息（默认）
- `Warning` - 警告信息
- `Error` - 错误信息
- `Fatal` - 致命错误

**响应示例：**
```json
{
  "level": "Debug",
  "message": "日志级别已从 Information 更改为 Debug"
}
```

#### 使用场景

1. **临时调试**：生产环境遇到问题时，临时提升日志级别到 Debug
2. **性能优化**：降低日志级别以减少 I/O 开销
3. **故障排查**：动态调整特定模块的日志级别

#### 注意事项

- 日志级别更改立即生效，无需重启服务
- 更改不会持久化到配置文件
- 服务重启后会恢复到配置文件中的级别

### 4. 审计日志和性能监控

#### 审计日志中间件

系统自动记录所有 API 请求的详细信息：

**记录的信息：**
- 请求 ID（TraceIdentifier）
- HTTP 方法
- 请求路径
- 客户端 IP 地址
- 响应状态码
- 请求处理时间

**日志示例：**
```
[2025-11-16 10:30:15.234 +00:00] [INF] [AuditLoggingMiddleware] API 请求开始 => RequestId: 0HMVF..., Method: POST, Path: /api/training/start, RemoteIp: 192.168.1.100
[2025-11-16 10:30:16.456 +00:00] [INF] [AuditLoggingMiddleware] API 请求完成 => RequestId: 0HMVF..., Method: POST, Path: /api/training/start, StatusCode: 200, Duration: 1222ms
```

#### 性能监控

系统自动检测慢操作并记录警告：

**配置项：**
- `EnablePerformanceLog`: 是否启用性能日志（默认：true）
- `SlowOperationThresholdMs`: 慢操作阈值，单位毫秒（默认：1000ms）

**慢操作日志示例：**
```
[2025-11-16 10:35:22.789 +00:00] [WRN] [AuditLoggingMiddleware] API 请求完成（慢操作）=> RequestId: 0HMVG..., Method: POST, Path: /api/training/start, StatusCode: 200, Duration: 3456ms
```

### 5. 日志配置选项

#### LoggingOptions 配置类

在 `appsettings.json` 中配置：

```json
{
  "LoggingOptions": {
    "MinimumLevel": "Information",
    "EnableAuditLog": true,
    "EnablePerformanceLog": true,
    "SlowOperationThresholdMs": 1000,
    "LogFilePath": "logs/barcode-lab-.log",
    "RollingInterval": "Day",
    "FileSizeLimitBytes": 104857600,
    "RetainedFileCountLimit": 31,
    "RollOnFileSizeLimit": true
  }
}
```

**参数说明：**
- `MinimumLevel`: 最小日志级别
- `EnableAuditLog`: 是否启用审计日志
- `EnablePerformanceLog`: 是否启用性能监控
- `SlowOperationThresholdMs`: 慢操作阈值（毫秒）
- `LogFilePath`: 日志文件路径模板
- `RollingInterval`: 轮转间隔（Minute/Hour/Day/Month/Year/Infinite）
- `FileSizeLimitBytes`: 单个日志文件大小限制（字节）
- `RetainedFileCountLimit`: 保留的日志文件数量
- `RollOnFileSizeLimit`: 是否在达到大小限制时轮转

## 配置示例

### 高频轮转配置（用于高流量环境）

```json
{
  "LoggingOptions": {
    "RollingInterval": "Hour",
    "FileSizeLimitBytes": 52428800,
    "RetainedFileCountLimit": 168
  }
}
```

### 低频轮转配置（用于低流量环境）

```json
{
  "LoggingOptions": {
    "RollingInterval": "Day",
    "FileSizeLimitBytes": 209715200,
    "RetainedFileCountLimit": 90
  }
}
```

### 调试模式配置

```json
{
  "LoggingOptions": {
    "MinimumLevel": "Debug",
    "EnableAuditLog": true,
    "EnablePerformanceLog": true,
    "SlowOperationThresholdMs": 500
  }
}
```

### 生产模式配置

```json
{
  "LoggingOptions": {
    "MinimumLevel": "Warning",
    "EnableAuditLog": true,
    "EnablePerformanceLog": false,
    "SlowOperationThresholdMs": 2000
  }
}
```

## 使用指南

### 1. 切换环境

通过设置环境变量来切换不同的配置：

**Windows:**
```powershell
$env:ASPNETCORE_ENVIRONMENT="Staging"
dotnet run
```

**Linux/Mac:**
```bash
export ASPNETCORE_ENVIRONMENT=Staging
dotnet run
```

### 2. 动态调整日志级别

使用 curl 或 Postman 调用 API：

```bash
# 获取当前日志级别
curl http://localhost:4000/api/logging/level

# 设置为 Debug 级别
curl -X PUT http://localhost:4000/api/logging/level \
  -H "Content-Type: application/json" \
  -d '{"level":"Debug","operator":"admin"}'

# 恢复为 Information 级别
curl -X PUT http://localhost:4000/api/logging/level \
  -H "Content-Type: application/json" \
  -d '{"level":"Information","operator":"admin"}'
```

### 3. 查看日志文件

日志文件存储在 `logs/` 目录下：

```bash
# 查看最新的日志
tail -f logs/barcode-lab-20251116.log

# 搜索特定关键词
grep "错误" logs/barcode-lab-*.log

# 查看慢操作
grep "慢操作" logs/barcode-lab-*.log
```

### 4. 监控审计日志

审计日志会自动记录所有 API 请求：

```bash
# 查看所有 API 请求
grep "API 请求" logs/barcode-lab-*.log

# 查看失败的请求
grep "API 请求失败" logs/barcode-lab-*.log

# 统计请求数量
grep "API 请求完成" logs/barcode-lab-*.log | wc -l
```

## 性能影响

### 日志级别对性能的影响

| 日志级别 | CPU 影响 | I/O 影响 | 推荐场景 |
|---------|---------|---------|---------|
| Verbose | 高 | 极高 | 仅开发调试 |
| Debug | 中高 | 高 | 开发和 Staging |
| Information | 低 | 中 | 生产环境（默认）|
| Warning | 极低 | 低 | 高性能生产环境 |
| Error | 极低 | 极低 | 特殊优化场景 |

### 优化建议

1. **生产环境使用 Warning 或 Information 级别**
2. **合理设置文件大小限制和保留数量**
3. **慢操作阈值不要设置过低（建议 ≥ 1000ms）**
4. **避免在高频路径启用 Debug 日志**

## 故障排查

### 常见问题

#### 1. 日志文件未创建

**原因**：日志目录权限不足

**解决**：确保应用有写入权限
```bash
mkdir -p logs
chmod 755 logs
```

#### 2. 日志级别更改不生效

**原因**：配置文件中的 Override 设置覆盖了全局设置

**解决**：检查 `appsettings.json` 中的 `Serilog.MinimumLevel.Override` 配置

#### 3. 日志文件过大

**原因**：轮转策略配置不当

**解决**：调整 `RollingInterval` 和 `FileSizeLimitBytes` 参数

## 相关文件

- `src/ZakYip.BarcodeReadabilityLab.Service/Configuration/LoggingOptions.cs` - 日志配置选项
- `src/ZakYip.BarcodeReadabilityLab.Service/Services/LogLevelManager.cs` - 日志级别管理服务
- `src/ZakYip.BarcodeReadabilityLab.Service/Endpoints/LoggingEndpoints.cs` - 日志管理 API
- `src/ZakYip.BarcodeReadabilityLab.Service/Middleware/AuditLoggingMiddleware.cs` - 审计日志中间件
- `src/ZakYip.BarcodeReadabilityLab.Service/appsettings.json` - 默认配置
- `src/ZakYip.BarcodeReadabilityLab.Service/appsettings.Development.json` - 开发环境配置
- `src/ZakYip.BarcodeReadabilityLab.Service/appsettings.Staging.json` - 预发布环境配置
- `src/ZakYip.BarcodeReadabilityLab.Service/appsettings.Production.json` - 生产环境配置

## 总结

通过本次日志配置完善，系统获得了以下增强：

1. ✅ **灵活的日志轮转策略** - 支持多种时间间隔和大小限制
2. ✅ **动态日志级别调整** - 无需重启即可调整日志级别
3. ✅ **全面的审计日志** - 自动记录所有 API 请求
4. ✅ **性能监控** - 自动检测和告警慢操作
5. ✅ **多环境支持** - 针对不同环境的优化配置
6. ✅ **结构化日志** - 便于查询和分析

这些功能显著提升了系统的可观测性和可维护性。
