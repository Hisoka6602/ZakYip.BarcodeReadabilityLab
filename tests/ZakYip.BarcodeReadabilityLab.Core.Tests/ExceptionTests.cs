using ZakYip.BarcodeReadabilityLab.Core.Domain.Exceptions;

namespace ZakYip.BarcodeReadabilityLab.Core.Tests;

/// <summary>
/// 异常类测试
/// </summary>
public sealed class ExceptionTests
{
    [Fact]
    public void AnalysisException_DefaultConstructor_ShouldCreateInstance()
    {
        // Act
        var exception = new AnalysisException();

        // Assert
        Assert.NotNull(exception);
        Assert.IsType<AnalysisException>(exception);
        Assert.IsAssignableFrom<BarcodeLabException>(exception);
    }

    [Fact]
    public void AnalysisException_MessageConstructor_ShouldSetMessage()
    {
        // Arrange
        var message = "分析失败";

        // Act
        var exception = new AnalysisException(message);

        // Assert
        Assert.Equal(message, exception.Message);
    }

    [Fact]
    public void AnalysisException_MessageAndInnerExceptionConstructor_ShouldSetBoth()
    {
        // Arrange
        var message = "分析失败";
        var innerException = new InvalidOperationException("内部错误");

        // Act
        var exception = new AnalysisException(message, innerException);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(innerException, exception.InnerException);
    }

    [Fact]
    public void AnalysisException_MessageAndErrorCodeConstructor_ShouldSetBoth()
    {
        // Arrange
        var message = "分析失败";
        var errorCode = "ANALYSIS_001";

        // Act
        var exception = new AnalysisException(message, errorCode);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(errorCode, exception.ErrorCode);
    }

    [Fact]
    public void AnalysisException_FullConstructor_ShouldSetAllProperties()
    {
        // Arrange
        var message = "分析失败";
        var errorCode = "ANALYSIS_001";
        var innerException = new InvalidOperationException("内部错误");

        // Act
        var exception = new AnalysisException(message, errorCode, innerException);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(errorCode, exception.ErrorCode);
        Assert.Equal(innerException, exception.InnerException);
    }

    [Fact]
    public void ConfigurationException_DefaultConstructor_ShouldCreateInstance()
    {
        // Act
        var exception = new ConfigurationException();

        // Assert
        Assert.NotNull(exception);
        Assert.IsType<ConfigurationException>(exception);
        Assert.IsAssignableFrom<BarcodeLabException>(exception);
    }

    [Fact]
    public void ConfigurationException_MessageConstructor_ShouldSetMessage()
    {
        // Arrange
        var message = "配置错误";

        // Act
        var exception = new ConfigurationException(message);

        // Assert
        Assert.Equal(message, exception.Message);
    }

    [Fact]
    public void ConfigurationException_MessageAndInnerExceptionConstructor_ShouldSetBoth()
    {
        // Arrange
        var message = "配置错误";
        var innerException = new InvalidOperationException("内部错误");

        // Act
        var exception = new ConfigurationException(message, innerException);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(innerException, exception.InnerException);
    }

    [Fact]
    public void ConfigurationException_MessageAndErrorCodeConstructor_ShouldSetBoth()
    {
        // Arrange
        var message = "配置错误";
        var errorCode = "CONFIG_001";

        // Act
        var exception = new ConfigurationException(message, errorCode);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(errorCode, exception.ErrorCode);
    }

    [Fact]
    public void ConfigurationException_FullConstructor_ShouldSetAllProperties()
    {
        // Arrange
        var message = "配置错误";
        var errorCode = "CONFIG_001";
        var innerException = new InvalidOperationException("内部错误");

        // Act
        var exception = new ConfigurationException(message, errorCode, innerException);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(errorCode, exception.ErrorCode);
        Assert.Equal(innerException, exception.InnerException);
    }

    [Fact]
    public void TrainingException_MessageAndErrorCodeConstructor_ShouldSetBoth()
    {
        // Arrange
        var message = "训练失败";
        var errorCode = "TRAIN_001";

        // Act
        var exception = new TrainingException(message, errorCode);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(errorCode, exception.ErrorCode);
    }

    [Fact]
    public void TrainingException_FullConstructor_ShouldSetAllProperties()
    {
        // Arrange
        var message = "训练失败";
        var errorCode = "TRAIN_001";
        var innerException = new InvalidOperationException("内部错误");

        // Act
        var exception = new TrainingException(message, errorCode, innerException);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(errorCode, exception.ErrorCode);
        Assert.Equal(innerException, exception.InnerException);
    }

    [Fact]
    public void BarcodeLabException_ShouldBeThrowable()
    {
        // Arrange
        var message = "基础异常";
        var errorCode = "BASE_001";

        // Act
        BarcodeLabException? exception = null;
        try
        {
            throw new BarcodeLabException(message, errorCode);
        }
        catch (BarcodeLabException ex)
        {
            exception = ex;
        }

        // Assert
        Assert.NotNull(exception);
        Assert.Equal(message, exception!.Message);
        Assert.Equal(errorCode, exception.ErrorCode);
    }
}
