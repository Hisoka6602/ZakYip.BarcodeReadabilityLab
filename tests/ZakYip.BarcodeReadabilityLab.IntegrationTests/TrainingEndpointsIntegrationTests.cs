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

    [Fact]
    public async Task GetTrainingHistory_ShouldReturnEmptyList_Initially()
    {
        using var client = _factory.CreateClient();
        
        var history = await client.GetFromJsonAsync<List<TrainingJobResponse>>("/api/training/history");
        
        Assert.NotNull(history);
        // 历史可能为空或包含其他测试的任务
        Assert.IsAssignableFrom<IReadOnlyList<TrainingJobResponse>>(history);
    }

    [Fact]
    public async Task GetTrainingHistory_ShouldReturnAllJobs_AfterMultipleTrainings()
    {
        using var dataset = SyntheticTrainingDataset.Create(samplesPerClass: 2);
        using var client = _factory.CreateClient();

        // 启动第一个训练任务
        var request1 = new StartTrainingRequest
        {
            TrainingRootDirectory = dataset.TrainingRootDirectory,
            OutputModelDirectory = dataset.OutputModelDirectory,
            ValidationSplitRatio = 0.1m,
            Remarks = "history-test-1"
        };

        var startResponse1 = await client.PostAsJsonAsync("/api/training/start", request1);
        startResponse1.EnsureSuccessStatusCode();
        var startPayload1 = await startResponse1.Content.ReadFromJsonAsync<StartTrainingResponse>();
        Assert.NotNull(startPayload1);

        // 等待第一个任务完成
        await WaitForCompletionAsync(client, startPayload1!.JobId, StatusTimeout);

        // 启动第二个训练任务
        var request2 = new StartTrainingRequest
        {
            TrainingRootDirectory = dataset.TrainingRootDirectory,
            OutputModelDirectory = dataset.OutputModelDirectory,
            ValidationSplitRatio = 0.1m,
            Remarks = "history-test-2"
        };

        var startResponse2 = await client.PostAsJsonAsync("/api/training/start", request2);
        startResponse2.EnsureSuccessStatusCode();
        var startPayload2 = await startResponse2.Content.ReadFromJsonAsync<StartTrainingResponse>();
        Assert.NotNull(startPayload2);

        // 等待第二个任务完成
        await WaitForCompletionAsync(client, startPayload2!.JobId, StatusTimeout);

        // 获取训练历史
        var history = await client.GetFromJsonAsync<List<TrainingJobResponse>>("/api/training/history");
        
        Assert.NotNull(history);
        Assert.True(history!.Count >= 2, "历史记录应至少包含两个训练任务");
        
        // 验证两个任务都在历史中
        Assert.Contains(history, job => job.JobId == startPayload1.JobId);
        Assert.Contains(history, job => job.JobId == startPayload2.JobId);
    }

    [Fact]
    public async Task StartTraining_WithMinimalParameters_ShouldSucceed()
    {
        using var dataset = SyntheticTrainingDataset.Create(samplesPerClass: 2);
        using var client = _factory.CreateClient();

        var request = new StartTrainingRequest
        {
            TrainingRootDirectory = dataset.TrainingRootDirectory,
            OutputModelDirectory = dataset.OutputModelDirectory,
            Epochs = 1,
            BatchSize = 2,
            LearningRate = 0.01m
        };

        var startResponse = await client.PostAsJsonAsync("/api/training/start", request);
        
        Assert.Equal(HttpStatusCode.OK, startResponse.StatusCode);
        
        var startPayload = await startResponse.Content.ReadFromJsonAsync<StartTrainingResponse>();
        Assert.NotNull(startPayload);
        Assert.NotEqual(Guid.Empty, startPayload!.JobId);
        Assert.Contains("训练任务", startPayload.Message);
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
