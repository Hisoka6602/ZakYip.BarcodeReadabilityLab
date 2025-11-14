namespace ZakYip.BarcodeReadabilityLab.Core.Domain.Exceptions;

/// <summary>
/// 条码可读性实验室系统的基础异常类
/// </summary>
public class BarcodeLabException : Exception
{
    /// <summary>
    /// 异常代码，用于标识具体错误类型
    /// </summary>
    public string? ErrorCode { get; init; }

    public BarcodeLabException()
    {
    }

    public BarcodeLabException(string message)
        : base(message)
    {
    }

    public BarcodeLabException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public BarcodeLabException(string message, string errorCode)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    public BarcodeLabException(string message, string errorCode, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}
