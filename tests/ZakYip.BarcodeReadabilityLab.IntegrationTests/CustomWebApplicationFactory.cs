using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using ZakYip.BarcodeReadabilityLab.Application.Options;
using ZakYip.BarcodeReadabilityLab.Infrastructure.MLNet.Contracts;
using ZakYip.BarcodeReadabilityLab.Infrastructure.Persistence.Data;
using ZakYip.BarcodeReadabilityLab.Service.Workers;

namespace ZakYip.BarcodeReadabilityLab.IntegrationTests;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var sandboxRoot = Path.Combine(Path.GetTempPath(), "barcode-lab");
        var monitorPath = Path.Combine(sandboxRoot, "monitor");
        var unresolvedPath = Path.Combine(sandboxRoot, "unresolved");
        var trainingDataPath = Path.Combine(sandboxRoot, "training");
        var modelPath = Path.Combine(sandboxRoot, "models");

        Directory.CreateDirectory(monitorPath);
        Directory.CreateDirectory(unresolvedPath);
        Directory.CreateDirectory(trainingDataPath);
        Directory.CreateDirectory(modelPath);

        builder.UseEnvironment("IntegrationTest");
        builder.ConfigureAppConfiguration((context, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BarcodeReadabilityService:MonitorPath"] = monitorPath,
                ["BarcodeReadabilityService:UnableToAnalyzePath"] = unresolvedPath,
                ["BarcodeReadabilityService:TrainingDataPath"] = trainingDataPath,
                ["BarcodeReadabilityService:ModelPath"] = modelPath,
                ["ApiSettings:Urls"] = "http://127.0.0.1:0",
                ["TrainingOptions:EnableResourceMonitoring"] = "false",
                ["TrainingOptions:MaxConcurrentTrainingJobs"] = "1"
            });
        });

        builder.ConfigureServices(services =>
        {
            var hostedServiceDescriptors = services
                .Where(descriptor => descriptor.ServiceType == typeof(IHostedService) && descriptor.ImplementationType == typeof(DirectoryMonitoringWorker))
                .ToList();

            foreach (var descriptor in hostedServiceDescriptors)
            {
                services.Remove(descriptor);
            }

            var dbContextDescriptor = services.SingleOrDefault(descriptor => descriptor.ServiceType == typeof(DbContextOptions<TrainingJobDbContext>));
            if (dbContextDescriptor is not null)
            {
                services.Remove(dbContextDescriptor);
            }

            services.AddDbContext<TrainingJobDbContext>(options =>
            {
                options.UseInMemoryDatabase("IntegrationTests");
            });

            services.RemoveAll<IImageClassificationTrainer>();
            services.AddSingleton<IImageClassificationTrainer, FakeImageClassificationTrainer>();

            using var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TrainingJobDbContext>();
            dbContext.Database.EnsureCreated();
        });
    }
}
