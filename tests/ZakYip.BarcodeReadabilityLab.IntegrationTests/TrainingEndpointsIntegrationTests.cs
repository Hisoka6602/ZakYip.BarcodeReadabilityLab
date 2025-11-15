using ZakYip.BarcodeReadabilityLab.Service.Models;

namespace ZakYip.BarcodeReadabilityLab.IntegrationTests;

public sealed class TrainingEndpointsIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private static readonly TimeSpan StatusPollingInterval = TimeSpan.FromMilliseconds(200);
    private static readonly TimeSpan StatusTimeout = TimeSpan.FromSeconds(10);
    private readonly CustomWebApplicationFactory _factory;

    public TrainingEndpointsIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task StartTrainingEndpoint_ShouldProcessJobEndToEnd()
    {
        using var dataset = SyntheticTrainingDataset.Create(samplesPerClass: 2);
        using var client = _factory.CreateClient();

        var request = new StartTrainingRequest
        {
            TrainingRootDirectory = dataset.TrainingRootDirectory,
            OutputModelDirectory = dataset.OutputModelDirectory,
            ValidationSplitRatio = 0.1m,
            Remarks = "integration-test"
        };

        var startResponse = await client.PostAsJsonAsync("/api/training/start", request);
        startResponse.EnsureSuccessStatusCode();

        var startPayload = await startResponse.Content.ReadFromJsonAsync<StartTrainingResponse>();
        Assert.NotNull(startPayload);
        Assert.NotEqual(Guid.Empty, startPayload!.JobId);

        var finalStatus = await WaitForCompletionAsync(client, startPayload.JobId, StatusTimeout);
        Assert.Equal("已完成", finalStatus.State);
        Assert.NotNull(finalStatus.EvaluationMetrics);
        Assert.Equal(0.95m, finalStatus.EvaluationMetrics!.Accuracy);
        Assert.Equal("integration-test", finalStatus.Remarks);

        var history = await client.GetFromJsonAsync<List<TrainingJobResponse>>("/api/training/history");
        Assert.NotNull(history);
        Assert.Contains(history!, job => job.JobId == startPayload.JobId && job.State == "已完成");
    }

    [Fact]
    public async Task GetTrainingStatus_ShouldReturnNotFound_ForUnknownJob()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/training/status/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static async Task<TrainingJobResponse> WaitForCompletionAsync(HttpClient client, Guid jobId, TimeSpan timeout)
    {
        using var timeoutSource = new CancellationTokenSource(timeout);

        while (!timeoutSource.IsCancellationRequested)
        {
            var response = await client.GetAsync($"/api/training/status/{jobId}", timeoutSource.Token);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var statusPayload = await response.Content.ReadFromJsonAsync<TrainingJobResponse>(timeoutSource.Token);
                if (statusPayload is not null)
                {
                    if (statusPayload.State == "已完成")
                    {
                        return statusPayload;
                    }

                    if (statusPayload.State == "失败")
                    {
                        throw new InvalidOperationException($"Training job {jobId} failed: {statusPayload.ErrorMessage}");
                    }
                }
            }

            await Task.Delay(StatusPollingInterval, timeoutSource.Token);
        }

        throw new TimeoutException($"Training job {jobId} did not complete within {timeout}.");
    }
}
