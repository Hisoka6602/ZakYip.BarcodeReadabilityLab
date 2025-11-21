namespace ZakYip.BarcodeReadabilityLab.Infrastructure.MLNet.Services;

using System;
using ZakYip.BarcodeReadabilityLab.Core.Domain.Models;
using ZakYip.BarcodeReadabilityLab.Core.Enum;

/// <summary>
/// ML.NET 预测结果辅助方法
/// </summary>
internal static class MlNetPredictionMapper
{
    /// <summary>
    /// 将模型输出标签映射为 NoreadReason
    /// </summary>
    public static (NoreadReason? reason, bool isSuccess) MapLabelToNoreadReason(string label)
    {
        if (string.IsNullOrWhiteSpace(label))
            return (null, false);

        // 尝试直接按枚举名称解析
        if (Enum.TryParse<NoreadReason>(label, ignoreCase: true, out var reasonByName))
            return (reasonByName, true);

        // 尝试解析为数值表示
        if (int.TryParse(label, out var numericValue) && Enum.IsDefined(typeof(NoreadReason), numericValue))
            return ((NoreadReason)numericValue, true);

        return (null, false);
    }
}
