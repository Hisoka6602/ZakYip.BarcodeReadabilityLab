namespace ZakYip.BarcodeReadabilityLab.Service.Models;

using System;

/// <summary>
/// 模型导入响应
/// </summary>
public sealed record class ModelImportResponse
{
    /// <summary>
    /// 新模型版本标识
    /// </summary>
    public Guid VersionId { get; init; }

    /// <summary>
    /// 模型版本名称
    /// </summary>
    public string VersionName { get; init; } = string.Empty;

    /// <summary>
    /// 保存后的模型文件路径
    /// </summary>
    public string ModelPath { get; init; } = string.Empty;

    /// <summary>
    /// 是否已激活
    /// </summary>
    public bool IsActive { get; init; }
}
