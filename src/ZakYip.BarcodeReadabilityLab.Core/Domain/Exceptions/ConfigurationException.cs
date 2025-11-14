namespace ZakYip.BarcodeReadabilityLab.Core.Domain.Exceptions;

/// <summary>
/// 配置相关异常
/// </summary>
public class ConfigurationException : BarcodeLabException
{
    public ConfigurationException()
    {
    }

    public ConfigurationException(string message)
        : base(message)
    {
    }

    public ConfigurationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public ConfigurationException(string message, string errorCode)
        : base(message, errorCode)
    {
    }

    public ConfigurationException(string message, string errorCode, Exception innerException)
        : base(message, errorCode, innerException)
    {
    }
}
