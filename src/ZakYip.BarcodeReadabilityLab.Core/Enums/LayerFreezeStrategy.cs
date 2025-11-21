using System.ComponentModel;

namespace ZakYip.BarcodeReadabilityLab.Core.Enums;

/// <summary>
/// 层冻结策略
/// </summary>
public enum LayerFreezeStrategy
{
    /// <summary>
    /// 全部冻结 - 冻结所有预训练层，仅训练新添加的分类层
    /// </summary>
    [Description("全部冻结 - 冻结所有预训练层，仅训练新添加的分类层")]
    FreezeAll = 1,

    /// <summary>
    /// 部分冻结 - 冻结前面的层，解冻后面的层进行微调
    /// </summary>
    [Description("部分冻结 - 冻结前面的层，解冻后面的层进行微调")]
    FreezePartial = 2,

    /// <summary>
    /// 全部解冻 - 解冻所有层，允许完全微调
    /// </summary>
    [Description("全部解冻 - 解冻所有层，允许完全微调")]
    UnfreezeAll = 3
}
