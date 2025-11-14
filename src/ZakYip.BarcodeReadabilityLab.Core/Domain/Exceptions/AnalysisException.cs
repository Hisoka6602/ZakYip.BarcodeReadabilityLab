namespace ZakYip.BarcodeReadabilityLab.Core.Domain.Exceptions;

/// <summary>
/// 条码图像分析相关异常
/// </summary>
public class AnalysisException : BarcodeLabException
{
    public AnalysisException()
    {
    }

    public AnalysisException(string message)
        : base(message)
    {
    }

    public AnalysisException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public AnalysisException(string message, string errorCode)
        : base(message, errorCode)
    {
    }

    public AnalysisException(string message, string errorCode, Exception innerException)
        : base(message, errorCode, innerException)
    {
    }
}
