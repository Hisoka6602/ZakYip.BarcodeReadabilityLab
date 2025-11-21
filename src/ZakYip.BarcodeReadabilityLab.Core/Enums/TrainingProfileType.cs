using System.ComponentModel;

namespace ZakYip.BarcodeReadabilityLab.Core.Enums;

/// <summary>
/// 训练档位类型
/// </summary>
public enum TrainingProfileType
{
    /// <summary>
    /// 调试档位：用于快速开发调试，训练速度优先，质量次要
    /// </summary>
    [Description("调试档位")]
    Debug = 1,

    /// <summary>
    /// 标准档位：用于日常训练，平衡速度和质量
    /// </summary>
    [Description("标准档位")]
    Standard = 2,

    /// <summary>
    /// 高质量档位：用于发布前大规模训练，质量优先，训练时间次要
    /// </summary>
    [Description("高质量档位")]
    HighQuality = 3
}
