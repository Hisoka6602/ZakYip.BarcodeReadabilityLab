# ZakYip.BarcodeReadabilityLab

## 项目简介

**条码图片可读性分析实验室** - 基于 ML.NET 的智能条码图片分类与分析系统

本项目是一个企业级的条码图片可读性分析平台，采用分层架构设计，支持自动监控目录、实时分析条码图片、异步训练任务管理、模型版本控制和热切换等功能。系统可作为 Windows 服务运行，提供 HTTP API 和 SignalR 实时通信接口。

### 核心特性

- 🤖 **ML.NET 图像分类**: 基于深度学习的条码可读性智能分析
- 📁 **自动目录监控**: 实时监控指定目录，自动分析新增图片
- 🔄 **模型热切换**: 支持在线更新模型，无需重启服务
- 📊 **训练任务管理**: 异步训练任务队列，支持并发控制
- 🌐 **RESTful API**: 完整的 HTTP API 接口
- 🔔 **SignalR 实时推送**: 训练进度实时通知
- 💾 **持久化存储**: 基于 SQLite 的训练任务历史记录
- 🪟 **Windows 服务**: 支持作为 Windows 服务后台运行
- 🧪 **完整测试**: 单元测试、集成测试覆盖

---

## 目录

- [技术栈](#技术栈)
- [项目架构](#项目架构)
- [项目完成度](#项目完成度)
- [功能说明](#功能说明)
- [API 文档](#api-文档)
- [工作流程](#工作流程)
- [快速开始](#快速开始)
- [当前缺陷](#当前缺陷)
- [未来优化方向](#未来优化方向-按-pr-单位规划)
- [开发指南](#开发指南)
- [相关文档](#相关文档)

---

## 技术栈

### 核心框架
- **.NET 8.0**: 最新的 .NET 平台
- **C# 12**: 使用现代 C# 特性（record class、init、file-scoped namespace 等）

### 机器学习
- **ML.NET 5.0.0**: Microsoft 的机器学习框架
- **Microsoft.ML.Vision**: 图像分类专用 API
- **Microsoft.ML.ImageAnalytics**: 图像数据处理

### Web 框架
- **ASP.NET Core Minimal API**: 轻量级 Web API
- **SignalR**: 实时双向通信
- **Serilog**: 结构化日志记录

### 数据存储
- **Entity Framework Core 8.0**: ORM 框架
- **SQLite**: 轻量级数据库（训练任务历史）

### 图像处理
- **SixLabors.ImageSharp 3.1.12**: 跨平台图像处理库

### 测试
- **xUnit 2.5.6**: 单元测试框架
- **Moq 4.20.72**: Mock 框架
- **Microsoft.AspNetCore.Mvc.Testing**: 集成测试支持

### 其他
- **Microsoft.Extensions.Hosting**: 托管服务框架
- **Windows Services**: Windows 服务支持

---

## 项目架构

### 分层架构

项目采用 **洋葱架构（Onion Architecture）** 设计，严格遵循依赖反转原则：

```
┌─────────────────────────────────────────────────────────┐
│  Presentation Layer (Service)                           │
│  - Program.cs (主入口)                                   │
│  - Minimal API Endpoints                                │
│  - Controllers (传统 MVC，向后兼容)                        │
│  - SignalR Hubs                                         │
│  - Workers (目录监控、训练任务)                           │
└─────────────────────────────────────────────────────────┘
                          ↓ 依赖
┌─────────────────────────────────────────────────────────┐
│  Application Layer                                      │
│  - Services (业务服务)                                   │
│  - Options (配置选项)                                    │
│  - Events (事件定义)                                     │
└─────────────────────────────────────────────────────────┘
                          ↓ 依赖
┌──────────────────────┬──────────────────────────────────┐
│  Infrastructure      │  Infrastructure                  │
│  (MLNet)             │  (Persistence)                   │
│  - ML.NET 训练器     │  - EF Core 数据访问              │
│  - 模型分析器        │  - SQLite 仓储                   │
│  - 预测映射器        │  - 实体映射                      │
└──────────────────────┴──────────────────────────────────┘
                          ↓ 依赖
┌─────────────────────────────────────────────────────────┐
│  Core Layer (Domain)                                    │
│  - Domain Models (领域模型)                              │
│  - Contracts (接口定义)                                  │
│  - Exceptions (业务异常)                                 │
│  - 纯 C# 代码，无外部依赖                                 │
└─────────────────────────────────────────────────────────┘
```

### 项目结构

```
ZakYip.BarcodeReadabilityLab/
├── src/
│   ├── ZakYip.BarcodeReadabilityLab.Core/              # 核心层：领域模型和契约
│   │   └── Domain/
│   │       ├── Models/                                  # 领域模型
│   │       │   ├── BarcodeSample.cs                    # 条码样本
│   │       │   ├── BarcodeAnalysisResult.cs            # 分析结果
│   │       │   ├── NoreadReason.cs                     # 不可读原因枚举
│   │       │   ├── TrainingJob.cs                      # 训练任务
│   │       │   ├── ModelVersion.cs                     # 模型版本
│   │       │   ├── DataAugmentationOptions.cs          # 数据增强选项
│   │       │   └── DataBalancingOptions.cs             # 数据平衡选项
│   │       ├── Contracts/                               # 接口定义
│   │       │   ├── IBarcodeReadabilityAnalyzer.cs      # 分析器接口
│   │       │   ├── IImageClassificationTrainer.cs      # 训练器接口
│   │       │   └── ITrainingJobRepository.cs           # 仓储接口
│   │       └── Exceptions/                              # 业务异常
│   │           └── TrainingException.cs
│   │
│   ├── ZakYip.BarcodeReadabilityLab.Application/       # 应用层：业务服务
│   │   ├── Services/
│   │   │   ├── TrainingJobService.cs                   # 训练任务服务
│   │   │   ├── ModelVersionService.cs                  # 模型版本服务
│   │   │   ├── DirectoryMonitoringService.cs           # 目录监控服务
│   │   │   └── UnresolvedImageRouter.cs                # 未识别图片路由
│   │   ├── Workers/
│   │   │   └── TrainingWorker.cs                       # 训练任务后台处理
│   │   ├── Options/
│   │   │   ├── TrainingOptions.cs                      # 训练选项
│   │   │   └── BarcodeAnalyzerOptions.cs               # 分析器选项
│   │   └── Events/
│   │       └── TrainingProgressEventArgs.cs            # 训练进度事件
│   │
│   ├── ZakYip.BarcodeReadabilityLab.Infrastructure.MLNet/  # 基础设施：ML.NET 实现
│   │   ├── Services/
│   │   │   ├── MlNetImageClassificationTrainer.cs      # ML.NET 训练器
│   │   │   ├── MlNetBarcodeReadabilityAnalyzer.cs      # ML.NET 分析器
│   │   │   ├── MlNetModelVariantAnalyzer.cs            # 多模型对比
│   │   │   └── MlNetPredictionMapper.cs                # 预测映射
│   │   └── Models/
│   │       └── ImagePrediction.cs                       # 预测结果模型
│   │
│   ├── ZakYip.BarcodeReadabilityLab.Infrastructure.Persistence/  # 基础设施：持久化
│   │   ├── Data/
│   │   │   └── TrainingJobDbContext.cs                 # EF Core 上下文
│   │   ├── Entities/
│   │   │   ├── TrainingJobEntity.cs                    # 训练任务实体
│   │   │   └── ModelVersionEntity.cs                   # 模型版本实体
│   │   └── Repositories/
│   │       ├── TrainingJobRepository.cs                # 训练任务仓储
│   │       └── ModelVersionRepository.cs               # 模型版本仓储
│   │
│   └── ZakYip.BarcodeReadabilityLab.Service/           # 表示层：API 和宿主
│       ├── Program.cs                                   # 应用程序入口
│       ├── Endpoints/
│       │   ├── TrainingEndpoints.cs                    # 训练 API 端点
│       │   └── ModelEndpoints.cs                       # 模型管理端点
│       ├── Controllers/
│       │   └── TrainingController.cs                   # 传统控制器（向后兼容）
│       ├── Hubs/
│       │   └── TrainingProgressHub.cs                  # SignalR Hub
│       ├── Workers/
│       │   └── DirectoryMonitoringWorker.cs            # 目录监控后台服务
│       ├── Models/                                      # API 请求/响应模型
│       └── appsettings.json                            # 配置文件
│
└── tests/
    ├── ZakYip.BarcodeReadabilityLab.Core.Tests/        # 核心层测试
    ├── ZakYip.BarcodeReadabilityLab.Application.Tests/ # 应用层测试
    ├── ZakYip.BarcodeReadabilityLab.Service.Tests/     # 服务层测试
    └── ZakYip.BarcodeReadabilityLab.IntegrationTests/  # 集成测试
```

### 架构原则

1. **依赖方向**: 外层依赖内层，Core 层不依赖任何外部库
2. **接口隔离**: 通过接口定义契约，实现解耦
3. **依赖注入**: 所有依赖通过 DI 容器管理
4. **不可变性**: 优先使用 `record class` 和 `init` 属性
5. **清晰边界**: 每层职责明确，不跨层访问

---

## 项目完成度

### 总体完成度：约 75%

项目核心功能已完成，可正常编译、运行和调试。部分高级特性和优化功能待完善。

### ✅ 已完成功能 (100%)

#### 1. 核心架构
- ✅ 四层分层架构（Core、Application、Infrastructure、Service）
- ✅ 依赖注入配置
- ✅ 领域模型定义
- ✅ 接口契约定义

#### 2. ML.NET 图像分类
- ✅ 模型训练功能（基于 ImageClassificationTrainer）
- ✅ 模型加载与推理
- ✅ 7 种 NoreadReason 分类支持
- ✅ 置信度计算
- ✅ 数据增强（旋转、翻转、亮度调整）
- ✅ 数据平衡（过采样、欠采样）
- ✅ 训练超参数配置（学习率、Epoch、Batch Size）

#### 3. 目录监控与自动分析
- ✅ FileSystemWatcher 实时监控
- ✅ 自动触发图片分析
- ✅ 智能路由（高置信度删除，低置信度归档）
- ✅ 多格式支持（.jpg、.jpeg、.png、.bmp）
- ✅ 递归监控选项

#### 4. 训练任务管理
- ✅ 异步任务队列
- ✅ 任务状态追踪（Queued、Running、Completed、Failed、Cancelled）
- ✅ 后台工作器（TrainingWorker）
- ✅ 并发控制（可配置并发数）
- ✅ 训练进度报告
- ✅ 任务持久化（SQLite）

#### 5. 模型版本管理
- ✅ 模型版本注册
- ✅ 模型激活与回滚
- ✅ 模型导入/导出
- ✅ 多模型对比（A/B 测试）
- ✅ 模型元数据存储

#### 6. HTTP API
- ✅ Minimal API 架构
- ✅ 训练任务端点（/api/training）
- ✅ 模型管理端点（/api/models）
- ✅ JSON 序列化配置（camelCase）
- ✅ 全局异常处理中间件
- ✅ 传统 MVC 控制器（向后兼容）

#### 7. 实时通信
- ✅ SignalR Hub 实现
- ✅ 训练进度实时推送

#### 8. Windows 服务支持
- ✅ Windows Service 宿主
- ✅ 服务生命周期管理
- ✅ PowerShell 安装/卸载脚本

#### 9. 日志记录
- ✅ Serilog 结构化日志
- ✅ 中文日志消息
- ✅ 关键操作日志

#### 10. 测试覆盖
- ✅ 核心层单元测试（3 个测试，全部通过）
- ✅ 应用层单元测试（9 个测试，全部通过）
- ✅ 服务层单元测试（4 个测试，3 个通过）
- ✅ 集成测试（2 个测试）

### ⚠️ 部分完成功能 (50-70%)

#### 1. 测试稳定性
- ⚠️ 服务层测试: 1 个测试失败
- ⚠️ 集成测试: 2 个测试失败（需要调查）

#### 2. API 文档
- ❌ 缺少 Swagger/OpenAPI 文档
- ✅ README 中有手动文档

#### 3. 模型评估指标
- ⚠️ 基础的准确率、损失计算
- ❌ 缺少详细的混淆矩阵
- ❌ 缺少 F1 分数、召回率等指标

#### 4. 日志配置
- ⚠️ 基础日志记录完成
- ❌ 缺少日志文件轮转配置
- ❌ 缺少日志级别动态调整

### ❌ 未完成功能 (0-30%)

#### 1. 安全性
- ❌ API 身份验证（无 JWT/API Key）
- ❌ 授权控制（无基于角色的访问控制）
- ❌ API 限流

#### 2. 监控告警
- ❌ 性能指标采集
- ❌ Prometheus/Grafana 集成
- ❌ 邮件告警

#### 3. Web 管理界面
- ❌ 无 Web UI
- ❌ 无训练监控面板
- ❌ 无数据标注工具

#### 4. 高级训练功能
- ❌ 迁移学习（基于预训练模型）
- ❌ 分布式训练
- ❌ 增量训练
- ❌ 自动超参数调优

#### 5. 容器化部署
- ❌ Docker 镜像
- ❌ Docker Compose 配置
- ❌ Kubernetes 部署清单

---

## 功能说明

### 1. 图片监控与分析

系统自动监控指定目录，当检测到新图片时：

1. **验证文件**: 检查文件类型、大小、可用性
2. **加载模型**: 使用当前激活的 ML.NET 模型
3. **执行推理**: 分析图片，返回分类和置信度
4. **智能路由**:
   - **置信度 >= 阈值**: 删除原图片（已成功分类）
   - **置信度 < 阈值**: 复制到待审核目录，生成分析报告

### 2. 训练任务管理

- **异步队列**: 训练任务进入内存队列，后台工作器处理
- **并发控制**: 支持配置最大并发训练数（默认 2）
- **任务状态**: Queued → Running → Completed/Failed/Cancelled
- **进度报告**: 实时更新训练进度（0.0 - 1.0）
- **持久化**: 所有任务记录存储到 SQLite 数据库

### 3. 模型版本管理

- **版本注册**: 训练完成后自动注册新模型版本
- **激活切换**: 支持在线切换激活模型，无需重启
- **版本回滚**: 可回滚到历史版本
- **多模型对比**: 支持同时使用多个模型进行 A/B 测试
- **导入导出**: 支持通过 API 导入/导出模型文件

### 4. 数据增强与平衡

#### 数据增强（Data Augmentation）
- **旋转**: 随机旋转图片（可配置角度范围）
- **翻转**: 水平/垂直翻转
- **亮度调整**: 随机调整图片亮度

#### 数据平衡（Data Balancing）
- **过采样（OverSample）**: 复制少数类样本
- **欠采样（UnderSample）**: 裁剪多数类样本

---

## API 文档

### 基础信息

- **Base URL**: `http://localhost:5000`（可在 appsettings.json 配置）
- **Content-Type**: `application/json`
- **响应格式**: JSON（camelCase 命名）

### 训练任务 API

#### 1. 启动训练任务

```http
POST /api/training/start
Content-Type: application/json

{
  "trainingRootDirectory": "C:\\BarcodeImages\\TrainingData",
  "outputModelDirectory": "C:\\BarcodeImages\\Models",
  "validationSplitRatio": 0.2,
  "learningRate": 0.01,
  "epochs": 50,
  "batchSize": 20,
  "remarks": "第一次训练测试",
  "dataAugmentation": {
    "enable": true,
    "augmentedImagesPerSample": 3,
    "enableRotation": true,
    "rotationDegreeRange": [-15, 15]
  },
  "dataBalancing": {
    "enable": true,
    "strategy": "OverSample",
    "targetSamplesPerClass": 1000
  }
}
```

**响应 200 OK:**
```json
{
  "jobId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "message": "训练任务已创建并加入队列"
}
```

#### 2. 查询训练状态

```http
GET /api/training/status/{jobId}
```

**响应 200 OK:**
```json
{
  "jobId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "state": "运行中",
  "progress": 0.45,
  "learningRate": 0.01,
  "epochs": 50,
  "batchSize": 20,
  "message": "训练任务正在执行",
  "startTime": "2024-01-15T10:30:00Z",
  "completedTime": null,
  "errorMessage": null,
  "remarks": "第一次训练测试",
  "dataAugmentation": {
    "enable": true,
    "augmentedImagesPerSample": 3
  },
  "dataBalancing": {
    "enable": true,
    "strategy": "OverSample"
  }
}
```

#### 3. 取消训练任务

```http
POST /api/training/cancel/{jobId}
```

**响应 202 Accepted:**
```json
{
  "message": "训练任务取消请求已提交"
}
```

### 模型管理 API

#### 1. 下载当前激活模型

```http
GET /api/models/current/download
```

**响应**: 二进制文件流（application/octet-stream）

#### 2. 按版本下载模型

```http
GET /api/models/{versionId}/download
```

**响应**: 二进制文件流（application/octet-stream）

#### 3. 导入模型

```http
POST /api/models/import
Content-Type: multipart/form-data

modelFile: [binary]
versionName: "noread-prod-v1"
deploymentSlot: "Production"
setAsActive: true
notes: "生产环境模型"
```

**响应 201 Created:**
```json
{
  "versionId": "2b5a27d7-32ba-4d52-9f6c-9f23e8437c2f",
  "versionName": "noread-prod-v1",
  "modelPath": "C:\\BarcodeImages\\Models\\noread-prod-v1.zip",
  "isActive": true
}
```

### SignalR Hub

**Hub URL**: `/hubs/training-progress`

**事件**:
- `ReceiveProgress`: 接收训练进度更新

**示例（JavaScript）:**
```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("http://localhost:5000/hubs/training-progress")
    .build();

connection.on("ReceiveProgress", (jobId, progress, message) => {
    console.log(`Job ${jobId}: ${progress * 100}% - ${message}`);
});

await connection.start();
```

---

## 工作流程

### 系统启动流程

```
启动 → 加载配置 → 初始化依赖注入 → 启动后台服务
                                    ↓
                         ┌──────────┴──────────┐
                         │                     │
                 DirectoryMonitoringWorker  TrainingWorker
                         │                     │
                    监控目录变化           处理训练任务队列
                         │                     │
                    分析新图片              执行模型训练
```

### 图片分析流程

```
新图片 → 文件验证 → 加载模型 → ML.NET 推理 → 计算置信度
                                                  ↓
                                        ┌─────────┴─────────┐
                                 置信度 >= 阈值        置信度 < 阈值
                                        ↓                   ↓
                                    删除图片          复制到待审核目录
                                                      生成分析报告
```

### 训练任务流程

```
API 请求 → 参数验证 → 创建任务 → 加入队列 → 返回 JobId
                                          ↓
                                   TrainingWorker 拉取
                                          ↓
                                   更新状态: Running
                                          ↓
                           ┌──────────────┴──────────────┐
                      数据加载            数据增强/平衡          
                           │                   │
                           └──────────┬────────┘
                                      ↓
                            ML.NET ImageClassificationTrainer
                                      ↓
                            保存模型 + 注册版本
                                      ↓
                            更新状态: Completed
                                      ↓
                            SignalR 推送完成通知
```

---

## 快速开始

### 前置要求

- .NET 8.0 SDK 或更高版本
- Windows 10/11 或 Windows Server 2016+ （用于 Windows 服务）
- 至少 4GB RAM（推荐 8GB+）
- 足够的磁盘空间用于训练数据和模型

### 1. 克隆项目

```bash
git clone https://github.com/Hisoka6602/ZakYip.BarcodeReadabilityLab.git
cd ZakYip.BarcodeReadabilityLab
```

### 2. 还原依赖

```bash
dotnet restore
```

### 3. 配置

编辑 `src/ZakYip.BarcodeReadabilityLab.Service/appsettings.json`:

```json
{
  "BarcodeReadabilityService": {
    "MonitorPath": "C:\\BarcodeImages\\Monitor",
    "UnableToAnalyzePath": "C:\\BarcodeImages\\Unresolved",
    "ConfidenceThreshold": 0.8,
    "ModelPath": "C:\\BarcodeImages\\Models\\model.zip"
  },
  "TrainingOptions": {
    "TrainingRootDirectory": "C:\\BarcodeImages\\TrainingData",
    "OutputModelDirectory": "C:\\BarcodeImages\\Models",
    "MaxConcurrentTrainingJobs": 2
  },
  "ApiSettings": {
    "Port": 5000,
    "Urls": "http://localhost:5000"
  }
}
```

### 4. 构建项目

```bash
dotnet build
```

### 5. 运行

#### 方式 1: 开发模式

```bash
cd src/ZakYip.BarcodeReadabilityLab.Service
dotnet run
```

#### 方式 2: 作为 Windows 服务

```powershell
# 管理员权限运行
.\install-service.ps1
```

### 6. 验证

访问 http://localhost:5000/api/training/status/00000000-0000-0000-0000-000000000000

应该返回 404 Not Found（正常，表示 API 工作正常）

### 7. 训练第一个模型

准备训练数据（按类别分目录）:

```
C:\BarcodeImages\TrainingData\
├── Blurry\
│   ├── image1.jpg
│   ├── image2.jpg
│   └── ...
├── LowLight\
│   ├── image1.jpg
│   └── ...
└── Normal\
    └── ...
```

发送训练请求:

```bash
curl -X POST http://localhost:5000/api/training/start \
  -H "Content-Type: application/json" \
  -d "{\"remarks\":\"首次训练\"}"
```

---

## 当前缺陷

### 🔴 严重缺陷（高优先级）

#### 1. 缺少 API 安全控制
- **问题**: API 端点无身份验证，任何人都可以触发训练或管理模型
- **影响**: 生产环境存在重大安全隐患
- **建议**: 实现 JWT 或 API Key 认证

#### 2. 集成测试不稳定
- **问题**: 2 个集成测试失败
- **影响**: CI/CD 流程可能受阻
- **建议**: 调查并修复测试失败原因

### 🟡 一般缺陷（中优先级）

#### 3. 服务层测试失败
- **问题**: 1 个服务层测试失败
- **影响**: 代码质量保证不完整
- **建议**: 修复失败的测试用例

#### 4. 缺少 Swagger 文档
- **问题**: 没有交互式 API 文档
- **影响**: API 使用门槛较高
- **建议**: 集成 Swashbuckle.AspNetCore

#### 5. 日志配置不完善
- **问题**: 缺少日志文件轮转、动态日志级别
- **影响**: 长期运行可能导致磁盘空间问题
- **建议**: 完善 Serilog 配置

#### 6. 错误处理不够细致
- **问题**: 部分异常类型过于宽泛
- **影响**: 调试困难
- **建议**: 细化异常类型，提供更详细错误信息

### 🟢 改进建议（低优先级）

#### 7. 性能优化
- **问题**: ML.NET 训练参数使用默认值
- **影响**: 模型性能可能不是最优
- **建议**: 实现自动超参数调优

#### 8. 监控告警
- **问题**: 无监控告警机制
- **影响**: 异常情况无法及时发现
- **建议**: 集成 Prometheus、Grafana 或邮件告警

#### 9. 容器化支持
- **问题**: 缺少 Docker 支持
- **影响**: 跨平台部署困难
- **建议**: 提供 Dockerfile 和 docker-compose.yml

---

## 未来优化方向 (按 PR 单位规划)

### 第一阶段：稳定性与安全性 (2-3 周)

#### PR #1: 修复测试失败
**目标**: 确保所有测试通过
- 调查并修复 2 个集成测试失败
- 修复 1 个服务层测试失败
- 确保测试覆盖率 > 80%

**预估工作量**: 3-5 天

#### PR #2: 实现 API 身份验证
**目标**: 保护 API 端点安全
- 实现基于 JWT 的认证
- 添加认证中间件
- 更新 API 文档说明认证方式
- 提供 API Key 备选方案

**预估工作量**: 3-4 天

#### PR #3: 集成 Swagger/OpenAPI 文档
**目标**: 改善 API 可用性
- 添加 Swashbuckle.AspNetCore 包
- 配置 Swagger UI
- 为所有端点添加详细的 XML 注释
- 支持在 Swagger UI 中测试 API

**预估工作量**: 2-3 天

#### PR #4: 完善日志配置
**目标**: 提升系统可观测性
- 配置日志文件轮转（按大小或日期）
- 实现动态日志级别调整
- 添加关键操作的结构化日志
- 配置不同环境的日志输出

**预估工作量**: 2-3 天

### 第二阶段：功能增强 (3-4 周)

#### PR #5: 实现模型评估指标
**目标**: 提供训练质量反馈
- 实现混淆矩阵计算
- 添加 F1 分数、召回率、精确率计算
- 在训练完成响应中返回详细评估指标
- 持久化评估结果供后续查询

**预估工作量**: 4-5 天

#### PR #6: 优化训练进度报告
**目标**: 提升用户体验
- 优化 ML.NET 训练进度回调
- 实现更精确的进度计算
- 添加预估剩余时间
- 改进 SignalR 推送性能

**预估工作量**: 3-4 天

#### PR #7: 实现自动超参数调优
**目标**: 提升模型训练灵活性和质量
- 实现网格搜索（Grid Search）
- 实现随机搜索（Random Search）
- 添加超参数验证逻辑
- 提供推荐配置

**预估工作量**: 5-6 天

#### PR #8: 完善异常处理和错误消息
**目标**: 改善调试体验
- 细化异常类型（训练异常、验证异常、模型异常等）
- 提供更详细的错误信息和上下文
- 实现全局异常日志记录
- 添加用户友好的错误响应

**预估工作量**: 3-4 天

### 第三阶段：高级特性 (4-6 周)

#### PR #9: 实现迁移学习支持
**目标**: 提升模型训练效率
- 支持基于预训练模型的微调
- 提供常用预训练模型（ResNet、InceptionV3 等）
- 实现模型迁移 API
- 添加迁移学习文档

**预估工作量**: 7-10 天

#### PR #10: 实现 API 限流和授权
**目标**: 增强 API 安全性和稳定性
- 实现基于 IP 的请求限流
- 实现基于用户的请求配额
- 添加基于角色的授权（RBAC）
- 实现操作审计日志

**预估工作量**: 5-7 天

#### PR #11: 实现监控告警系统
**目标**: 提升系统可靠性
- 集成 Prometheus 指标采集
- 配置 Grafana 仪表板
- 实现邮件/短信告警
- 添加健康检查端点

**预估工作量**: 6-8 天

#### PR #12: 容器化支持
**目标**: 改善部署体验
- 创建 Dockerfile
- 提供 docker-compose.yml
- 支持多阶段构建
- 优化镜像大小
- 提供 Kubernetes 部署清单

**预估工作量**: 4-6 天

### 第四阶段：Web UI (6-8 周)

#### PR #13: Web 管理界面 - 基础框架
**目标**: 提供可视化管理界面
- 选择前端框架（Blazor Server/React/Vue）
- 实现基础框架和路由
- 实现登录/注销功能
- 实现主导航和布局

**预估工作量**: 7-10 天

#### PR #14: Web 管理界面 - 训练管理
**目标**: 可视化训练任务管理
- 实现训练任务列表页面
- 实现训练任务详情页面
- 实现训练任务启动界面
- 实现实时进度监控（WebSocket/SignalR）

**预估工作量**: 7-10 天

#### PR #15: Web 管理界面 - 模型管理
**目标**: 可视化模型版本管理
- 实现模型版本列表
- 实现模型导入/导出界面
- 实现模型激活切换
- 实现模型性能对比图表

**预估工作量**: 7-10 天

#### PR #16: Web 管理界面 - 图片标注工具
**目标**: 提供数据标注功能
- 实现图片上传和预览
- 实现图片标注界面
- 实现批量标注
- 实现标注导出

**预估工作量**: 10-14 天

---

## 开发指南

### 编码规范

请严格遵守 `.github/copilot-instructions.md` 中定义的编码规范。

#### 关键要点

- ✅ **注释用中文**: 所有代码注释必须使用简体中文
- ✅ **命名用英文**: 类名、方法名、变量名等使用英文
- ✅ **优先 record/record class**: 不可变数据模型使用 record
- ✅ **必需属性使用 required**: 确保对象初始化时必须设置
- ✅ **枚举必须有 Description**: 所有枚举成员添加中文 Description
- ✅ **布尔命名**: 使用 Is/Has/Can/Should 前缀
- ✅ **优先 decimal**: 金额、百分比等使用 decimal
- ✅ **启用可空引用类型**: `<Nullable>enable</Nullable>`
- ✅ **异常和日志用中文**: 所有异常消息和日志使用中文

### 构建和测试

```bash
# 还原依赖
dotnet restore

# 构建
dotnet build

# 运行所有测试
dotnet test

# 运行特定项目测试
dotnet test tests/ZakYip.BarcodeReadabilityLab.Core.Tests

# 生成代码覆盖率报告
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

### 数据库迁移

```bash
# 添加迁移
dotnet ef migrations add MigrationName --project src/ZakYip.BarcodeReadabilityLab.Infrastructure.Persistence

# 更新数据库
dotnet ef database update --project src/ZakYip.BarcodeReadabilityLab.Infrastructure.Persistence

# 回滚迁移
dotnet ef database update PreviousMigrationName --project src/ZakYip.BarcodeReadabilityLab.Infrastructure.Persistence
```

### 调试技巧

#### 1. 启用详细日志

在 `appsettings.Development.json` 中设置:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug",
      "Override": {
        "Microsoft": "Information"
      }
    }
  }
}
```

#### 2. 调试训练任务

设置断点在:
- `TrainingJobService.StartTrainingAsync` - 任务创建
- `TrainingWorker.ExecuteAsync` - 任务执行
- `MlNetImageClassificationTrainer.TrainAsync` - 模型训练

#### 3. 调试 API 请求

使用 Postman、curl 或浏览器开发者工具测试 API。

---

## 相关文档

- **[QUICKSTART.md](QUICKSTART.md)** - 5 分钟快速上手指南
- **[ARCHITECTURE.md](ARCHITECTURE.md)** - 项目架构详细说明
- **[USAGE.md](USAGE.md)** - 详细使用示例和场景
- **[TRAINING_SERVICE.md](TRAINING_SERVICE.md)** - 训练服务详细说明
- **[DEPLOYMENT.md](DEPLOYMENT.md)** - Windows 服务部署指南
- **[WINDOWS_SERVICE_SETUP.md](WINDOWS_SERVICE_SETUP.md)** - Windows 服务设置指南
- **[docs/TRAINING_HYPERPARAMETER_GUIDE.md](docs/TRAINING_HYPERPARAMETER_GUIDE.md)** - 训练超参数推荐配置

---

## 贡献指南

欢迎提交 Issue 和 Pull Request！

### 提交 Issue

请提供以下信息：
- 问题描述
- 重现步骤
- 期望行为
- 实际行为
- 环境信息（.NET 版本、操作系统等）

### 提交 Pull Request

1. Fork 本仓库
2. 创建特性分支 (`git checkout -b feature/AmazingFeature`)
3. 提交更改 (`git commit -m '添加某个特性'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 开启 Pull Request

请确保：
- 遵守编码规范
- 添加单元测试
- 更新相关文档
- 所有测试通过

---

## 测试状态

### 当前测试结果

- ✅ **Core 层**: 3/3 通过
- ✅ **Application 层**: 9/9 通过
- ⚠️ **Service 层**: 3/4 通过（1 个失败）
- ⚠️ **Integration 层**: 0/2 通过（2 个失败）

**总计**: 15/18 测试通过 (83.3%)

### 测试覆盖率

- **Core 层**: ~90%
- **Application 层**: ~85%
- **总体**: ~75% (估计)

---

## 许可证

本项目使用 MIT 许可证。详见 [LICENSE](LICENSE) 文件。

---

## 联系方式

- **作者**: Hisoka6602
- **GitHub**: https://github.com/Hisoka6602/ZakYip.BarcodeReadabilityLab
- **问题反馈**: [GitHub Issues](https://github.com/Hisoka6602/ZakYip.BarcodeReadabilityLab/issues)

---

## 更新日志

### 2025-11-16 - 项目修复与重构

#### 修复的编译问题
1. ✅ 修复 MlNetBarcodeReadabilityAnalyzer.cs 语法错误
2. ✅ 修复 ML.NET 5.0 API 兼容性问题（Architecture、Metrics 结构变化）
3. ✅ 修复 SixLabors.ImageSharp API 变化（Brightness 方法）
4. ✅ 修复 IOptionsMonitorCache API 变化（TryUpdate → TryRemove + TryAdd）
5. ✅ 修复 Program.cs 结构问题
6. ✅ 修复测试项目中的多个问题

#### 改进
- 项目现在可以成功编译
- 核心功能测试全部通过
- 更新并完善 README 文档

---

## 致谢

感谢以下开源项目：

- [ML.NET](https://github.com/dotnet/machinelearning) - Microsoft 的机器学习框架
- [ImageSharp](https://github.com/SixLabors/ImageSharp) - 跨平台图像处理库
- [Serilog](https://github.com/serilog/serilog) - 结构化日志库
- [xUnit](https://github.com/xunit/xunit) - 测试框架
- [Moq](https://github.com/moq/moq4) - Mock 框架

---

**Built with ❤️ using .NET 8 and ML.NET**
