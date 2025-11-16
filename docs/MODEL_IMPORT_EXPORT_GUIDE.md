# 模型导入导出使用指南

## 概述

ZakYip.BarcodeReadabilityLab 提供完整的模型导入和导出功能，允许您：

- 📥 **导入模型**: 上传外部训练的模型文件并注册到系统
- 📤 **导出模型**: 下载当前激活的模型或历史版本模型
- 🔄 **版本管理**: 管理多个模型版本，支持激活、回滚和 A/B 测试

---

## API 端点

### 1. 导入模型

**端点**: `POST /api/models/import`

**Content-Type**: `multipart/form-data`

#### 请求参数

| 参数名 | 类型 | 必需 | 说明 |
|--------|------|------|------|
| `ModelFile` | IFormFile | 是 | 模型文件（通常为 .zip 格式） |
| `VersionName` | string | 否 | 自定义版本名称，如不提供则使用文件名 |
| `DeploymentSlot` | string | 否 | 部署槽位，默认为 "Production" |
| `TrafficPercentage` | decimal? | 否 | 流量权重（0-1 之间），用于 A/B 测试 |
| `Notes` | string | 否 | 模型备注说明 |
| `SetAsActive` | bool | 否 | 是否立即激活，默认为 true |

#### 响应示例

**成功响应 (201 Created)**:
```json
{
  "versionId": "2b5a27d7-32ba-4d52-9f6c-9f23e8437c2f",
  "versionName": "noread-prod-v1",
  "modelPath": "/path/to/models/noread-prod-v1-20251116123456789.zip",
  "isActive": true
}
```

**失败响应 (400 Bad Request)**:
```json
{
  "error": "必须上传有效的模型文件"
}
```

#### 使用示例

**cURL**:
```bash
curl -X POST http://localhost:5000/api/models/import \
  -F "ModelFile=@/path/to/model.zip" \
  -F "VersionName=production-v1" \
  -F "DeploymentSlot=Production" \
  -F "SetAsActive=true" \
  -F "Notes=生产环境第一版模型"
```

**C# HttpClient**:
```csharp
using var client = new HttpClient();
using var content = new MultipartFormDataContent();

// 添加模型文件
using var fileStream = File.OpenRead("/path/to/model.zip");
using var fileContent = new StreamContent(fileStream);
fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
content.Add(fileContent, "ModelFile", "model.zip");

// 添加其他参数
content.Add(new StringContent("production-v1"), "VersionName");
content.Add(new StringContent("Production"), "DeploymentSlot");
content.Add(new StringContent("true"), "SetAsActive");
content.Add(new StringContent("生产环境第一版模型"), "Notes");

var response = await client.PostAsync(
    "http://localhost:5000/api/models/import", 
    content);
response.EnsureSuccessStatusCode();

var result = await response.Content.ReadFromJsonAsync<ModelImportResponse>();
Console.WriteLine($"导入成功，版本 ID: {result.VersionId}");
```

**PowerShell**:
```powershell
$modelPath = "C:\Models\model.zip"
$uri = "http://localhost:5000/api/models/import"

$form = @{
    ModelFile = Get-Item -Path $modelPath
    VersionName = "production-v1"
    DeploymentSlot = "Production"
    SetAsActive = "true"
    Notes = "生产环境第一版模型"
}

$response = Invoke-RestMethod -Uri $uri -Method Post -Form $form
Write-Host "导入成功，版本 ID: $($response.versionId)"
```

---

### 2. 下载当前激活模型

**端点**: `GET /api/models/current/download`

下载当前在线推理使用的激活模型文件。

#### 响应

成功时返回模型文件的二进制流（`application/octet-stream`）。

#### 使用示例

**cURL**:
```bash
curl -X GET http://localhost:5000/api/models/current/download \
  --output current-model.zip
```

**C# HttpClient**:
```csharp
using var client = new HttpClient();
var response = await client.GetAsync(
    "http://localhost:5000/api/models/current/download");
response.EnsureSuccessStatusCode();

await using var fileStream = File.Create("current-model.zip");
await response.Content.CopyToAsync(fileStream);
Console.WriteLine("模型下载成功");
```

**PowerShell**:
```powershell
$uri = "http://localhost:5000/api/models/current/download"
Invoke-WebRequest -Uri $uri -OutFile "current-model.zip"
Write-Host "模型下载成功"
```

---

### 3. 按版本下载模型

**端点**: `GET /api/models/{versionId}/download`

根据模型版本 ID 下载特定版本的模型文件。

#### 路径参数

| 参数名 | 类型 | 说明 |
|--------|------|------|
| `versionId` | Guid | 模型版本标识 |

#### 响应

成功时返回模型文件的二进制流（`application/octet-stream`）。

#### 使用示例

**cURL**:
```bash
curl -X GET http://localhost:5000/api/models/2b5a27d7-32ba-4d52-9f6c-9f23e8437c2f/download \
  --output model-v1.zip
```

**C# HttpClient**:
```csharp
var versionId = Guid.Parse("2b5a27d7-32ba-4d52-9f6c-9f23e8437c2f");
using var client = new HttpClient();
var response = await client.GetAsync(
    $"http://localhost:5000/api/models/{versionId}/download");
response.EnsureSuccessStatusCode();

await using var fileStream = File.Create("model-v1.zip");
await response.Content.CopyToAsync(fileStream);
Console.WriteLine("模型下载成功");
```

**PowerShell**:
```powershell
$versionId = "2b5a27d7-32ba-4d52-9f6c-9f23e8437c2f"
$uri = "http://localhost:5000/api/models/$versionId/download"
Invoke-WebRequest -Uri $uri -OutFile "model-v1.zip"
Write-Host "模型下载成功"
```

---

## 完整工作流示例

### 场景 1: 导入新训练的模型并激活

```bash
# 1. 导入模型
curl -X POST http://localhost:5000/api/models/import \
  -F "ModelFile=@/path/to/new-model.zip" \
  -F "VersionName=v2.0" \
  -F "SetAsActive=true" \
  -F "Notes=包含数据增强的改进版本"

# 响应示例:
# {
#   "versionId": "abc123...",
#   "versionName": "v2.0",
#   "modelPath": "/models/v2.0-20251116123456.zip",
#   "isActive": true
# }

# 2. 验证新模型已激活（可选）
curl -X GET http://localhost:5000/api/models/current/download \
  --output current-model.zip
```

### 场景 2: 备份当前模型

```bash
# 下载当前激活的模型作为备份
curl -X GET http://localhost:5000/api/models/current/download \
  --output backup-$(date +%Y%m%d).zip
```

### 场景 3: 导入模型但不立即激活

```bash
# 导入模型到 Staging 槽位用于测试
curl -X POST http://localhost:5000/api/models/import \
  -F "ModelFile=@/path/to/test-model.zip" \
  -F "VersionName=v2.1-beta" \
  -F "DeploymentSlot=Staging" \
  -F "SetAsActive=false" \
  -F "Notes=测试版本，待验证"
```

### 场景 4: A/B 测试配置

```bash
# 导入新模型并分配 20% 流量
curl -X POST http://localhost:5000/api/models/import \
  -F "ModelFile=@/path/to/experimental-model.zip" \
  -F "VersionName=experimental-v1" \
  -F "DeploymentSlot=Production" \
  -F "TrafficPercentage=0.2" \
  -F "SetAsActive=true" \
  -F "Notes=实验性模型，20% 流量测试"
```

---

## 错误处理

### 常见错误及解决方案

#### 1. "必须上传有效的模型文件" (400 Bad Request)

**原因**: 未提供模型文件或文件为空。

**解决**: 确保在请求中包含有效的模型文件。

#### 2. "服务未配置模型存储目录" (400 Bad Request)

**原因**: `appsettings.json` 中未配置 `BarcodeReadabilityService:ModelPath`。

**解决**: 在配置文件中添加模型存储路径：
```json
{
  "BarcodeReadabilityService": {
    "ModelPath": "C:\\BarcodeImages\\Models"
  }
}
```

#### 3. "模型文件不存在" (404 Not Found)

**原因**: 请求的模型文件已被删除或路径不正确。

**解决**: 
- 验证模型文件是否存在于配置的路径
- 使用正确的版本 ID

#### 4. "指定的模型版本不存在" (404 Not Found)

**原因**: 提供的版本 ID 不存在于数据库中。

**解决**: 
- 验证版本 ID 是否正确
- 查询可用的模型版本列表

---

## 最佳实践

### 1. 版本命名规范

建议使用语义化版本命名：
- `production-v1.0.0` - 生产环境主版本
- `hotfix-v1.0.1` - 修复版本
- `experimental-v2.0.0-alpha` - 实验版本

### 2. 模型备份策略

```bash
# 每日自动备份脚本（Linux/macOS）
#!/bin/bash
BACKUP_DIR="/backups/models"
DATE=$(date +%Y%m%d)
mkdir -p "$BACKUP_DIR"

curl -X GET http://localhost:5000/api/models/current/download \
  --output "$BACKUP_DIR/model-$DATE.zip"
```

```powershell
# 每日自动备份脚本（Windows PowerShell）
$backupDir = "C:\Backups\Models"
$date = Get-Date -Format "yyyyMMdd"
New-Item -ItemType Directory -Force -Path $backupDir

$uri = "http://localhost:5000/api/models/current/download"
Invoke-WebRequest -Uri $uri -OutFile "$backupDir\model-$date.zip"
```

### 3. 部署槽位使用

- **Production**: 生产环境使用的稳定模型
- **Staging**: 预发布环境，用于最终验证
- **Development**: 开发环境，用于快速迭代
- **Experimental**: 实验性模型，用于 A/B 测试

### 4. 导入前验证

在导入模型之前，建议先在本地或测试环境验证模型：
1. 检查模型文件完整性（文件大小、格式）
2. 在测试环境中进行推理测试
3. 评估模型性能指标

### 5. 灰度发布流程

```bash
# 步骤 1: 导入新模型但不激活
curl -X POST http://localhost:5000/api/models/import \
  -F "ModelFile=@new-model.zip" \
  -F "VersionName=v2.0" \
  -F "SetAsActive=false"

# 步骤 2: 在测试环境验证新模型
# ... 测试过程 ...

# 步骤 3: 逐步增加流量（10% -> 50% -> 100%）
# 注: 流量分配功能需配合负载均衡器或自定义中间件实现
```

---

## 集成示例

### 与 CI/CD 集成

**GitHub Actions 示例**:
```yaml
name: Deploy Model

on:
  workflow_dispatch:
    inputs:
      model_path:
        description: 'Path to model file'
        required: true
      version_name:
        description: 'Version name'
        required: true

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - name: Upload Model
        run: |
          curl -X POST ${{ secrets.API_URL }}/api/models/import \
            -F "ModelFile=@${{ github.event.inputs.model_path }}" \
            -F "VersionName=${{ github.event.inputs.version_name }}" \
            -F "SetAsActive=true" \
            -F "Notes=Deployed via GitHub Actions"
```

**Azure DevOps Pipeline 示例**:
```yaml
trigger: none

parameters:
  - name: modelPath
    type: string
    displayName: 'Model File Path'
  - name: versionName
    type: string
    displayName: 'Version Name'

pool:
  vmImage: 'ubuntu-latest'

steps:
  - task: PowerShell@2
    displayName: 'Deploy Model'
    inputs:
      targetType: 'inline'
      script: |
        $uri = "$(ApiUrl)/api/models/import"
        $form = @{
            ModelFile = Get-Item -Path "${{ parameters.modelPath }}"
            VersionName = "${{ parameters.versionName }}"
            SetAsActive = "true"
            Notes = "Deployed via Azure DevOps"
        }
        Invoke-RestMethod -Uri $uri -Method Post -Form $form
```

---

## 故障排查

### 查看应用日志

日志中会包含模型导入和导出的详细信息：
```bash
# Linux/macOS
tail -f /var/log/barcode-readability-lab/application.log

# Windows (Serilog 默认路径)
Get-Content "C:\ProgramData\BarcodeReadabilityLab\Logs\*.log" -Wait -Tail 50
```

### 常见日志消息

**成功导入**:
```
[INF] 成功导入模型文件 => VersionId: abc123..., Path: /path/to/model.zip
[INF] 注册模型版本 => VersionId: abc123..., Name: v2.0, Slot: Production, Active: True
```

**失败导入**:
```
[ERR] 导入模型文件失败
System.IO.IOException: The process cannot access the file because it is being used by another process.
```

---

## 安全考虑

### 1. 访问控制

在生产环境中，建议为模型管理 API 添加身份验证：
- 使用 JWT 令牌或 API Key
- 实施基于角色的访问控制（RBAC）
- 审计所有模型导入/导出操作

### 2. 文件验证

建议在导入模型前进行验证：
- 检查文件大小限制
- 验证文件类型和格式
- 扫描恶意文件

### 3. 存储安全

- 确保模型存储目录有适当的文件系统权限
- 定期备份模型文件
- 考虑加密敏感模型文件

---

## FAQ

**Q: 模型文件必须是 .zip 格式吗？**

A: 是的，ML.NET 导出的模型通常是 .zip 格式。系统会自动处理文件扩展名，但建议使用 .zip 格式以确保兼容性。

**Q: 可以同时激活多个模型版本吗？**

A: 在同一个部署槽位中，只有一个模型可以标记为激活状态。但您可以在不同的部署槽位（如 Production、Staging）中各有一个激活模型。

**Q: 导入的模型会自动验证吗？**

A: 系统会验证文件存在性和基本格式，但不会执行模型推理验证。建议在导入前在测试环境中验证模型。

**Q: 如何删除旧的模型版本？**

A: 目前系统不提供删除 API，您需要手动从文件系统和数据库中删除。未来版本将添加模型版本清理功能。

**Q: 模型导入会影响正在进行的推理吗？**

A: 如果设置 `SetAsActive=false`，导入不会影响当前推理。如果设置为 `true`，新模型会立即生效，但正在进行的推理请求会继续使用旧模型。

---

## 相关文档

- [README.md](../README.md) - 项目概览
- [ARCHITECTURE.md](../ARCHITECTURE.md) - 架构说明
- [USAGE.md](../USAGE.md) - 使用指南
- [API 文档](http://localhost:5000/api-docs) - Swagger 交互式文档（服务运行时可访问）

---

## 更新日志

### 2025-11-16
- ✅ 添加模型导入功能
- ✅ 添加模型导出（下载）功能
- ✅ 添加版本管理支持
- ✅ 添加部署槽位和流量分配
- ✅ 创建集成测试套件
- ✅ 编写完整使用文档

---

**如有任何问题或建议，请提交 [GitHub Issue](https://github.com/Hisoka6602/ZakYip.BarcodeReadabilityLab/issues)。**
