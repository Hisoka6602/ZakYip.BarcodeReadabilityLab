# 训练任务持久化文档

## 概述

本项目使用 SQLite 数据库持久化训练任务的状态信息，确保在服务重启后能够恢复任务历史记录。

## 架构设计

### 1. 分层架构

```
Core (领域层)
├── Domain/
│   ├── Models/
│   │   ├── TrainingJob.cs           # 训练任务领域模型
│   │   └── TrainingJobState.cs      # 训练任务状态枚举
│   └── Contracts/
│       └── ITrainingJobRepository.cs # 训练任务仓储接口

Infrastructure.Persistence (持久化层)
├── Data/
│   └── TrainingJobDbContext.cs      # EF Core 数据库上下文
├── Entities/
│   └── TrainingJobEntity.cs         # 数据库实体
├── Repositories/
│   └── TrainingJobRepository.cs     # 仓储实现
└── Extensions/
    └── ServiceCollectionExtensions.cs # 服务注册扩展

Application (应用层)
├── Services/
│   ├── TrainingJobService.cs        # 训练任务服务（使用 IServiceScopeFactory）
│   └── TrainingJobRecoveryService.cs # 任务恢复服务
└── Workers/
    └── TrainingWorker.cs            # 后台训练工作器

Service (服务宿主层)
├── Program.cs                       # 注册持久化服务
└── Endpoints/
    └── TrainingEndpoints.cs         # API 端点（包含历史查询）
```

### 2. 核心组件

#### TrainingJob 领域模型
- 不可变的 record class
- 包含任务状态、开始时间、完成时间、错误信息等
- 使用 `TrainingJobState` 枚举表示状态

#### TrainingJobRepository
- 实现 `ITrainingJobRepository` 接口
- 使用 EF Core 和 SQLite
- 支持添加、更新、按 ID 查询、按状态查询、获取所有任务

#### TrainingJobService
- 使用 `IServiceScopeFactory` 模式解决单例服务依赖作用域服务的问题
- 每次数据库操作时创建新的作用域
- 保证线程安全和正确的 DbContext 生命周期

#### TrainingJobRecoveryService
- 实现 `IHostedService` 接口
- 在服务启动时自动执行
- 将所有"运行中"和"排队中"的任务标记为失败（因为服务重启中断了这些任务）

## 数据库

### 数据库位置

SQLite 数据库文件默认路径：
- **Windows**: `C:\ProgramData\BarcodeReadabilityLab\trainingjobs.db`
- **Linux**: `/usr/share/BarcodeReadabilityLab/trainingjobs.db`

可以通过调用 `AddTrainingJobPersistence(customPath)` 指定自定义路径。

### 数据库表结构

```sql
CREATE TABLE "TrainingJobs" (
    "JobId" TEXT NOT NULL PRIMARY KEY,
    "TrainingRootDirectory" TEXT NOT NULL,
    "OutputModelDirectory" TEXT NOT NULL,
    "ValidationSplitRatio" TEXT NULL,
    "Status" INTEGER NOT NULL,
    "Progress" TEXT NOT NULL,
    "StartTime" TEXT NOT NULL,
    "CompletedTime" TEXT NULL,
    "ErrorMessage" TEXT NULL,
    "Remarks" TEXT NULL
);

CREATE INDEX "IX_TrainingJobs_Status" ON "TrainingJobs" ("Status");
CREATE INDEX "IX_TrainingJobs_StartTime" ON "TrainingJobs" ("StartTime");
```

### 字段说明

| 字段名 | 类型 | 说明 |
|--------|------|------|
| JobId | TEXT (GUID) | 训练任务唯一标识符（主键） |
| TrainingRootDirectory | TEXT | 训练数据根目录路径 |
| OutputModelDirectory | TEXT | 输出模型存放目录 |
| ValidationSplitRatio | DECIMAL | 验证集分割比例（0.0-1.0） |
| Status | INTEGER | 任务状态（1=排队中, 2=运行中, 3=已完成, 4=失败, 5=已取消） |
| Progress | DECIMAL | 进度百分比（0.0-1.0） |
| StartTime | TEXT | 任务开始时间（ISO 8601） |
| CompletedTime | TEXT | 任务完成时间（ISO 8601，可选） |
| ErrorMessage | TEXT | 错误信息（失败时填充） |
| Remarks | TEXT | 备注说明 |

## API 端点

### 1. 启动训练任务
```http
POST /api/training/start
Content-Type: application/json

{
  "trainingRootDirectory": "/path/to/data",
  "outputModelDirectory": "/path/to/output",
  "validationSplitRatio": 0.2,
  "remarks": "训练任务描述"
}
```

响应：
```json
{
  "jobId": "guid",
  "message": "训练任务已创建并加入队列"
}
```

### 2. 查询任务状态
```http
GET /api/training/status/{jobId}
```

响应：
```json
{
  "jobId": "guid",
  "state": "运行中",
  "progress": 0.5,
  "message": "训练任务正在执行",
  "startTime": "2024-01-01T10:00:00Z",
  "completedTime": null,
  "errorMessage": null,
  "remarks": "训练任务描述"
}
```

### 3. 获取训练历史（新增）
```http
GET /api/training/history
```

响应：
```json
[
  {
    "jobId": "guid-1",
    "state": "已完成",
    "progress": 1.0,
    "message": "训练任务已完成",
    "startTime": "2024-01-01T10:00:00Z",
    "completedTime": "2024-01-01T11:30:00Z",
    "errorMessage": null,
    "remarks": "第一次训练"
  },
  {
    "jobId": "guid-2",
    "state": "失败",
    "progress": 0.3,
    "message": "训练任务失败: 数据目录不存在",
    "startTime": "2024-01-01T09:00:00Z",
    "completedTime": "2024-01-01T09:05:00Z",
    "errorMessage": "数据目录不存在",
    "remarks": null
  }
]
```

## 使用方式

### 在 Program.cs 中注册服务

```csharp
// 注册训练任务持久化服务（必须在 AddBarcodeAnalyzerServices 之前）
builder.Services.AddTrainingJobPersistence();

// 或者指定自定义数据库路径
builder.Services.AddTrainingJobPersistence("/custom/path/trainingjobs.db");

// 注册应用服务
builder.Services.AddBarcodeAnalyzerServices();
```

### 服务启动流程

1. **服务启动**：调用 `AddTrainingJobPersistence()` 注册服务并初始化数据库
2. **数据库创建**：如果数据库不存在，自动创建表和索引
3. **任务恢复**：`TrainingJobRecoveryService` 启动，将未完成任务标记为失败
4. **正常运行**：接受 API 请求，持久化任务状态

## 状态转换

```
Queued (排队中)
    ↓
Running (运行中)
    ↓
Completed (已完成) / Failed (失败) / Cancelled (已取消)
```

### 特殊情况

- **服务重启**：所有 `Queued` 和 `Running` 状态的任务自动标记为 `Failed`，错误信息为"服务重启，训练任务被中断"
- **训练异常**：捕获异常并更新任务状态为 `Failed`，记录错误信息

## 技术特点

### 1. 依赖注入模式

使用 `IServiceScopeFactory` 解决单例服务依赖作用域 DbContext 的问题：

```csharp
using var scope = _scopeFactory.CreateScope();
var repository = scope.ServiceProvider.GetRequiredService<ITrainingJobRepository>();
// 使用 repository...
```

### 2. 不可变数据模型

使用 `record class` 确保数据不可变性：

```csharp
var updatedJob = trainingJob with
{
    Status = TrainingJobState.Completed,
    Progress = 1.0m,
    CompletedTime = DateTimeOffset.UtcNow
};
```

### 3. 实体与模型分离

- **TrainingJobEntity**：数据库实体，可变
- **TrainingJob**：领域模型，不可变
- 通过 `ToModel()` 和 `FromModel()` 方法转换

### 4. 异步操作

所有数据库操作都是异步的，避免阻塞线程：

```csharp
await repository.AddAsync(trainingJob, cancellationToken);
await repository.UpdateAsync(updatedJob, cancellationToken);
var job = await repository.GetByIdAsync(jobId, cancellationToken);
```

## 故障排查

### 数据库连接问题

如果遇到数据库连接问题，检查：
1. 数据库文件路径是否有写权限
2. SQLite 依赖是否正确安装
3. 日志中是否有 EF Core 错误信息

### 任务状态不更新

检查：
1. `TrainingWorker` 是否正常运行
2. 日志中是否有异常
3. 使用 `/api/training/history` 查看完整历史

### 服务启动失败

检查：
1. 确保在 `AddBarcodeAnalyzerServices()` 之前调用 `AddTrainingJobPersistence()`
2. 检查数据库目录权限
3. 查看启动日志中的详细错误信息

## 未来改进

- [ ] 添加任务分页查询
- [ ] 支持按状态筛选历史
- [ ] 添加任务取消功能
- [ ] 实现任务重试机制
- [ ] 添加任务执行时长统计
- [ ] 支持并发训练任务限制
