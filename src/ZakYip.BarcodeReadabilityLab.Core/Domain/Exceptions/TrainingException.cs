namespace ZakYip.BarcodeReadabilityLab.Core.Domain.Exceptions;

/// <summary>
/// 模型训练相关异常
/// </summary>
public class TrainingException : BarcodeLabException
{
    public TrainingException()
    {
    }

    public TrainingException(string message)
        : base(message)
    {
    }

    public TrainingException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public TrainingException(string message, string errorCode)
        : base(message, errorCode)
    {
    }

    public TrainingException(string message, string errorCode, Exception innerException)
        : base(message, errorCode, innerException)
    {
    }
}
