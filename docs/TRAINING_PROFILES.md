# 训练档位配置说明

本文档介绍 ZakYip.BarcodeReadabilityLab 项目中的三种训练档位配置及其使用场景。

## 概述

为了在训练速度和模型质量之间取得平衡，项目提供了三种预定义的训练档位：

- **Debug（调试档位）**：用于快速开发调试
- **Standard（标准档位）**：用于日常训练
- **HighQuality（高质量档位）**：用于发布前大规模训练

## 三种档位详细对比

| 配置项 | Debug | Standard | HighQuality |
|-------|-------|----------|-------------|
| **训练轮数 (Epochs)** | 5 | 50 | 100 |
| **批大小 (Batch Size)** | 32 | 20 | 16 |
| **学习率 (Learning Rate)** | 0.01 | 0.01 | 0.005 |
| **L2 正则化** | - | - | 0.0001 |
| **早停 (Early Stopping)** | 关闭 | 开启 (patience=5) | 开启 (patience=10) |
| **数据增强** | 关闭 | 开启（适度） | 开启（完整） |
| **数据平衡** | 关闭 | 过采样 | 过采样 |
| **预处理缓存** | 开启 | 开启 | 开启 |
| **验证集比例** | 20% | 20% | 20% |

## 各档位详细说明

### Debug 档位

**使用场景：**
- 开发新功能时快速验证代码正确性
- 调试训练管道
- 快速迭代实验

**特点：**
- 训练速度最快（5 个 Epoch，约 30 秒 - 1 分钟）
- 不开启数据增强和数据平衡，减少预处理时间
- 不开启早停，确保完整执行流程
- 较大的批大小（32）加速训练
- 模型质量较低，不适合生产使用

**典型耗时：** 30 秒 - 2 分钟（取决于数据集大小）

**预期指标：** Accuracy > 0.70（仅作为 sanity check）

### Standard 档位

**使用场景：**
- 日常模型训练
- 持续集成/持续部署 (CI/CD) 流程
- 增量训练场景
- 一般性能要求的生产环境

**特点：**
- 平衡训练时间和模型质量
- 开启适度数据增强（旋转概率 50%，亮度调整概率 40%）
- 开启早停机制（patience=5），避免过拟合并节省时间
- 开启数据平衡（过采样），处理类别不平衡
- 适中的批大小（20）

**典型耗时：** 5 - 15 分钟（取决于数据集大小和早停触发情况）

**预期指标：** Accuracy > 0.85

### HighQuality 档位

**使用场景：**
- 发布前的最终模型训练
- 关键业务场景的高质量模型
- 模型性能对比基准
- 研究和论文实验

**特点：**
- 训练质量最高，时间最长
- 完整数据增强（旋转概率 70%，更多角度选择）
- 较大的早停耐心值（patience=10），允许更多训练时间
- 添加 L2 正则化防止过拟合
- 较小的批大小（16）和学习率（0.005），更精细的梯度更新
- 每个样本生成 2 个增强副本

**典型耗时：** 15 - 60 分钟（取决于数据集大小和早停触发情况）

**预期指标：** Accuracy > 0.90

## 使用方法

### 1. 在 API 中指定训练档位

```bash
# 使用 Debug 档位
curl -X POST http://localhost:4000/api/training/start \
  -H "Content-Type: application/json" \
  -d '{
    "trainingRootDirectory": "/path/to/data",
    "outputModelDirectory": "/path/to/output",
    "profileType": "Debug"
  }'

# 使用 Standard 档位（默认）
curl -X POST http://localhost:4000/api/training/start \
  -H "Content-Type: application/json" \
  -d '{
    "trainingRootDirectory": "/path/to/data",
    "outputModelDirectory": "/path/to/output"
  }'

# 使用 HighQuality 档位
curl -X POST http://localhost:4000/api/training/start \
  -H "Content-Type: application/json" \
  -d '{
    "trainingRootDirectory": "/path/to/data",
    "outputModelDirectory": "/path/to/output",
    "profileType": "HighQuality"
  }'
```

### 2. 在配置文件中自定义档位

如需调整某个档位的参数，可以修改 `appsettings.json` 中的 `TrainingProfiles` 配置：

```json
{
  "TrainingProfiles": {
    "DefaultProfileType": "Standard",
    "Standard": {
      "Epochs": 60,
      "BatchSize": 24,
      "LearningRate": 0.008,
      "EnableEarlyStopping": true,
      "EarlyStoppingPatience": 7,
      ...
    }
  }
}
```

## 早停机制说明

早停（Early Stopping）是一种防止过拟合并节省训练时间的机制：

- **监控指标：** 验证集准确率（Accuracy）
- **触发条件：** 连续 N 个 Epoch 验证集指标无提升
- **最小改进阈值（Min Delta）：** 0.001（Standard）/ 0.0005（HighQuality）
- **效果：** 
  - 避免浪费时间在已经收敛的模型上
  - Standard 档位平均可节省 20-30% 训练时间
  - HighQuality 档位平均可节省 15-25% 训练时间

**示例：**
```
Epoch 1/50: Accuracy = 0.750
Epoch 2/50: Accuracy = 0.820
Epoch 3/50: Accuracy = 0.865
...
Epoch 15/50: Accuracy = 0.920
Epoch 16/50: Accuracy = 0.921 (提升 < 0.001)
Epoch 17/50: Accuracy = 0.920 (无提升)
Epoch 18/50: Accuracy = 0.919 (无提升)
Epoch 19/50: Accuracy = 0.921 (无提升)
Epoch 20/50: Accuracy = 0.920 (无提升)
[早停触发] 连续 5 个 Epoch 无显著提升，停止训练
最终模型：Epoch 15，Accuracy = 0.920
```

## 数据增强策略

### Debug 档位
- 不启用数据增强，加速训练

### Standard 档位
- 轻微旋转：概率 50%，角度 [-10°, 0°, 10°]
- 水平翻转：概率 30%
- 亮度调整：概率 40%，范围 [0.9, 1.1]
- 每个样本生成 1 个增强副本

### HighQuality 档位
- 完整旋转：概率 70%，角度 [-15°, -10°, -5°, 0°, 5°, 10°, 15°]
- 水平翻转：概率 50%
- 亮度调整：概率 60%，范围 [0.85, 1.15]
- 每个样本生成 2 个增强副本

## 预处理缓存机制

所有档位均默认启用预处理缓存，以加速重复训练：

- **首次训练：** 读取原始图片 → 预处理（Resize, Normalize）→ 保存缓存 → 训练
- **后续训练：** 直接加载缓存 → 训练（节省 30-50% 预处理时间）
- **缓存键：** 基于数据集路径哈希 + 预处理配置（尺寸、灰度等）
- **缓存位置：** `{OutputModelDirectory}/ml-workspace/cache/`

## 性能建议

1. **开发阶段：** 使用 Debug 档位快速验证功能
2. **日常训练：** 使用 Standard 档位，平衡速度和质量
3. **生产发布：** 使用 HighQuality 档位，确保最佳模型质量
4. **持续集成：** 使用 Debug 或 Standard 档位，控制 CI 时间
5. **增量训练：** 使用 Standard 档位，在新数据上微调模型

## 常见问题

### Q: 如何选择合适的档位？

A: 根据使用场景：
- 开发/调试 → Debug
- 日常使用 → Standard
- 生产发布 → HighQuality

### Q: 可以混合使用不同档位的参数吗？

A: 可以。在 API 请求中可以指定 `profileType` 的同时，覆盖特定参数（如 `epochs`、`batchSize` 等）。

### Q: 早停会影响模型质量吗？

A: 不会。早停机制保存的是验证集上表现最好的模型，通常能避免过拟合，反而可能提升泛化能力。

### Q: 如何查看训练使用的档位？

A: 查询训练状态 API，响应中包含 `profileType` 字段和完整的超参数快照 `hyperparametersSnapshot`。

## 更新日志

- **v1.0.0：** 初始版本，引入三档位配置和早停机制
