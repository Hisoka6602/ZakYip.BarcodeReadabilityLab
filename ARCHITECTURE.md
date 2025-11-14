# 项目架构概览

## 解决方案结构

```
ZakYip.BarcodeReadabilityLab.sln
├── src/
│   ├── ZakYip.BarcodeReadabilityLab.Core/                    [核心领域层]
│   │   ├── Domain/
│   │   │   ├── Models/                                       [领域模型]
│   │   │   └── Contracts/                                    [领域契约接口]
│   │   └── ZakYip.BarcodeReadabilityLab.Core.csproj         (net8.0)
│   │
│   ├── ZakYip.BarcodeReadabilityLab.Application/            [应用服务层]
│   │   ├── Services/                                         [应用服务]
│   │   ├── Options/                                          [配置选项]
│   │   ├── Events/                                           [应用事件]
│   │   └── ZakYip.BarcodeReadabilityLab.Application.csproj  (net8.0)
│   │
│   ├── ZakYip.BarcodeReadabilityLab.Infrastructure.MLNet/   [ML.NET 基础设施层]
│   │   ├── Models/                                           [ML.NET 数据模型]
│   │   ├── Services/                                         [ML.NET 服务实现]
│   │   └── ZakYip.BarcodeReadabilityLab.Infrastructure.MLNet.csproj  (net8.0)
│   │
│   └── ZakYip.BarcodeReadabilityLab.Service/                [Windows 服务宿主]
│       ├── Workers/                                          [后台工作服务]
│       ├── Endpoints/                                        [Minimal API 端点]
│       ├── Configuration/                                    [服务配置]
│       ├── Services/                                         [原有服务]
│       ├── Models/                                           [原有模型]
│       ├── Controllers/                                      [原有控制器]
│       └── ZakYip.BarcodeReadabilityLab.Service.csproj      (net8.0)
```

## 依赖关系

```
┌──────────────────────────────────────────────────────┐
│                                                      │
│         ZakYip.BarcodeReadabilityLab.Service         │
│               (Windows Service Host)                 │
│                                                      │
└───────────┬──────────────┬─────────────┬────────────┘
            │              │             │
            ▼              ▼             ▼
    ┌───────────┐  ┌──────────────┐  ┌──────────────────────┐
    │   Core    │  │ Application  │  │ Infrastructure.MLNet │
    │  (领域核心)│  │  (应用服务)   │  │   (ML.NET 实现)      │
    └───────────┘  └──────┬───────┘  └─────────┬────────────┘
                          │                     │
                          ▼                     ▼
                   ┌────────────────────────────────┐
                   │             Core               │
                   │         (领域核心层)            │
                   └────────────────────────────────┘
```

## 项目职责说明

### 1. Core（核心领域层）
- **目标框架**: .NET 8.0
- **依赖**: 无外部依赖（纯净层）
- **职责**:
  - 定义领域模型（Domain Models）
  - 定义领域契约接口（Domain Contracts）
  - 包含业务核心逻辑和规则
- **特点**:
  - 不依赖任何基础设施
  - 不引用 Entity Framework、HttpClient、UI 框架等
  - 使用 `record class` + `required` 定义不可变模型
  - 启用可空引用类型

### 2. Application（应用服务层）
- **目标框架**: .NET 8.0
- **依赖**: Core
- **职责**:
  - 实现应用层业务逻辑编排
  - 定义配置选项（Options）
  - 定义应用事件（Events）
  - 协调领域逻辑和基础设施
- **包依赖**:
  - Microsoft.Extensions.Logging.Abstractions
  - Microsoft.Extensions.Options

### 3. Infrastructure.MLNet（ML.NET 基础设施层）
- **目标框架**: .NET 8.0
- **依赖**: Core
- **职责**:
  - 实现 Core 层定义的机器学习接口
  - 封装 ML.NET 框架的具体实现
  - 提供模型训练、加载、预测功能
- **包依赖**:
  - Microsoft.ML (5.0.0)
  - Microsoft.ML.ImageAnalytics (5.0.0)

### 4. Service（Windows 服务宿主）
- **目标框架**: .NET 8.0
- **依赖**: Core, Application, Infrastructure.MLNet
- **职责**:
  - Windows 服务宿主
  - 承载 Minimal API
  - 后台工作服务（Workers）
  - HTTP API 端点（Endpoints）
- **包依赖**:
  - Microsoft.Extensions.Hosting (8.0.1)
  - Microsoft.Extensions.Hosting.WindowsServices (8.0.1)
  - Microsoft.ML (5.0.0)
  - Microsoft.ML.ImageAnalytics (5.0.0)

## 关键设计原则

### 1. 依赖方向
- 依赖方向始终从外层指向内层
- Core 层不依赖任何其他层
- Infrastructure 和 Application 都依赖 Core
- Service 依赖所有层

### 2. 不可变数据模型
- 优先使用 `record` 或 `record class`
- 使用 `required` 关键字标记必需属性
- 属性使用 `init` 访问器确保不可变性

### 3. 可空引用类型
- 所有项目启用 `<Nullable>enable</Nullable>`
- 明确标记可空类型，避免 null 引用异常

### 4. 事件驱动架构
- 使用 `record struct` 或 `record class` 定义事件参数
- 事件类型名以 `EventArgs` 结尾
- 小型事件载荷使用 `readonly record struct`

### 5. 方法专注小巧
- 一个方法一个职责
- 方法保持简短（通常不超过 20-30 行）
- 复杂逻辑拆分为多个小方法

### 6. 命名规范
- 类名、接口名、方法名使用英文
- 注释、日志、异常消息使用中文
- 布尔属性使用 Is/Has/Can/Should 前缀

## 后续开发建议

### 阶段 1: 完善 Core 层
- [ ] 定义核心领域模型（BarcodeImage、AnalysisResult 等）
- [ ] 定义领域服务接口（IImageRepository、IModelService 等）
- [ ] 添加领域事件和值对象

### 阶段 2: 实现 Application 层
- [ ] 实现应用服务（ImageProcessingService 等）
- [ ] 定义配置选项（ImageProcessingOptions 等）
- [ ] 定义应用事件（ImageProcessedEventArgs 等）

### 阶段 3: 实现 Infrastructure.MLNet 层
- [ ] 定义 ML.NET 输入/输出模型
- [ ] 实现模型训练服务
- [ ] 实现预测服务
- [ ] 添加模型持久化逻辑

### 阶段 4: 优化 Service 层
- [ ] 迁移现有服务到新架构
- [ ] 实现后台工作服务（Workers）
- [ ] 使用 Minimal API 重构端点（Endpoints）
- [ ] 添加依赖注入配置

## 编码规范

所有代码必须遵守 `.github/copilot-instructions.md` 中定义的编码规范，包括：
- ✅ 注释用中文
- ✅ 命名用英文
- ✅ 优先 record/record class + required
- ✅ 启用可空引用类型
- ✅ 使用文件作用域类型保持封装
- ✅ 优先使用 record 处理不可变数据
- ✅ 保持方法专注且小巧
- ✅ 优先使用 readonly struct
- ✅ 枚举必须有 Description 特性
- ✅ 布尔命名用 Is/Has/Can/Should 前缀
- ✅ 优先 decimal，谨慎用 double
- ✅ LINQ 优先，关注性能
- ✅ Core 层纯净，无基础设施依赖
- ✅ 事件载荷类型名以 EventArgs 结尾
- ✅ 异常和日志消息用中文
