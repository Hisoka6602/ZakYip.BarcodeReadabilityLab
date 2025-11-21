using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZakYip.BarcodeReadabilityLab.Application.Services;
using ZakYip.BarcodeReadabilityLab.Core.Domain.Contracts;
using ZakYip.BarcodeReadabilityLab.Core.Domain.Models;
using ZakYip.BarcodeReadabilityLab.Core.Enum;
using ZakYip.BarcodeReadabilityLab.Infrastructure.MLNet.Models;

namespace ZakYip.BarcodeReadabilityLab.Application.Tests;

public sealed class ModelVersionServiceTests
{
    private readonly Mock<IModelVersionRepository> _repository = new();
    private readonly Mock<IModelVariantAnalyzer> _variantAnalyzer = new();
    private readonly Mock<IOptionsMonitor<BarcodeMlModelOptions>> _optionsMonitor = new();
    private readonly Mock<IOptionsMonitorCache<BarcodeMlModelOptions>> _optionsCache = new();
    private readonly Mock<ILogger<ModelVersionService>> _logger = new();

    public ModelVersionServiceTests()
    {
        var options = new BarcodeMlModelOptions
        {
            CurrentModelPath = Path.Combine(Path.GetTempPath(), "active-model.zip")
        };

        _optionsMonitor.Setup(monitor => monitor.CurrentValue).Returns(options);
        _optionsCache
            .Setup(cache => cache.TryRemove(It.IsAny<string>()))
            .Returns(true);
        _optionsCache
            .Setup(cache => cache.TryAdd(It.IsAny<string>(), It.IsAny<BarcodeMlModelOptions>()))
            .Returns(true);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldThrow_WhenVersionIdIsEmpty()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentException>(() => service.GetByIdAsync(Guid.Empty));
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnRepositoryResult()
    {
        var version = new ModelVersion
        {
            VersionId = Guid.NewGuid(),
            VersionName = "imported-model",
            ModelPath = Path.Combine(Path.GetTempPath(), "models", "model.zip"),
            TrainingJobId = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow,
            IsActive = false,
            DeploymentSlot = "Production",
            TrafficPercentage = 0.5m,
            Notes = "unit-test",
            EvaluationMetrics = null
        };

        _repository
            .Setup(repo => repo.GetByIdAsync(version.VersionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(version);

        var service = CreateService();

        var result = await service.GetByIdAsync(version.VersionId, CancellationToken.None);

        Assert.Same(version, result);
        _repository.Verify(repo => repo.GetByIdAsync(version.VersionId, It.IsAny<CancellationToken>()), Times.Once);
    }

    private ModelVersionService CreateService()
    {
        return new ModelVersionService(
            _logger.Object,
            _repository.Object,
            _variantAnalyzer.Object,
            _optionsMonitor.Object,
            _optionsCache.Object);
    }
}
