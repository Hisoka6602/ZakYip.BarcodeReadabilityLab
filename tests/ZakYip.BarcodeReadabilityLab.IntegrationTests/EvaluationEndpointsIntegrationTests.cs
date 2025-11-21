using System.Net.Http.Headers;
using System.Text.Json;
using ZakYip.BarcodeReadabilityLab.Service.Models;
using ZakYip.BarcodeReadabilityLab.Service.Models.Evaluation;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ZakYip.BarcodeReadabilityLab.IntegrationTests;

/// <summary>
/// 评估端点集成测试
/// </summary>
public sealed class EvaluationEndpointsIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public EvaluationEndpointsIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AnalyzeSingle_ShouldReturn200_WithValidImage()
    {
        // Arrange
        using var client = _factory.CreateClient();
        using var imageStream = CreateTestImage(100, 100);

        using var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(imageStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(streamContent, "imageFile", "test.jpg");

        // Act
        var response = await client.PostAsync("/api/evaluation/analyze-single", content);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<EvaluateSingleResponse>();
        
        Assert.NotNull(result);
        Assert.NotNull(result.PredictedLabel);
        Assert.NotEmpty(result.PredictedLabel);
        Assert.NotNull(result.PredictedLabelDisplayName);
        Assert.InRange(result.Confidence, 0m, 1m);
        Assert.Null(result.IsCorrect); // 未提供 expectedLabel
        Assert.Null(result.ExpectedLabel);
    }

    [Fact]
    public async Task AnalyzeSingle_ShouldReturnIsCorrect_WhenExpectedLabelProvided()
    {
        // Arrange
        using var client = _factory.CreateClient();
        using var imageStream = CreateTestImage(100, 100);

        using var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(imageStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(streamContent, "imageFile", "test.jpg");
        content.Add(new StringContent("Truncated"), "expectedLabel");

        // Act
        var response = await client.PostAsync("/api/evaluation/analyze-single", content);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<EvaluateSingleResponse>();
        
        Assert.NotNull(result);
        Assert.Equal("Truncated", result.ExpectedLabel);
        Assert.NotNull(result.IsCorrect); // 应该有值
    }

    [Fact]
    public async Task AnalyzeSingle_ShouldReturn400_WhenNoImageProvided()
    {
        // Arrange
        using var client = _factory.CreateClient();
        using var content = new MultipartFormDataContent();

        // Act
        var response = await client.PostAsync("/api/evaluation/analyze-single", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.Contains("图片文件", error.Error);
    }

    [Fact]
    public async Task AnalyzeSingle_ShouldReturn400_WhenInvalidFileType()
    {
        // Arrange
        using var client = _factory.CreateClient();
        using var content = new MultipartFormDataContent();
        
        var textContent = new StringContent("This is not an image");
        textContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        content.Add(textContent, "imageFile", "test.txt");

        // Act
        var response = await client.PostAsync("/api/evaluation/analyze-single", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.Contains("文件类型", error.Error);
    }

    [Fact]
    public async Task AnalyzeSingle_ShouldReturn400_WhenInvalidExpectedLabel()
    {
        // Arrange
        using var client = _factory.CreateClient();
        using var imageStream = CreateTestImage(100, 100);

        using var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(imageStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(streamContent, "imageFile", "test.jpg");
        content.Add(new StringContent("InvalidLabel"), "expectedLabel");

        // Act
        var response = await client.PostAsync("/api/evaluation/analyze-single", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.Contains("expectedLabel", error.Error);
    }

    [Fact]
    public async Task AnalyzeBatch_ShouldReturn200_WithMultipleImages()
    {
        // Arrange
        using var client = _factory.CreateClient();
        using var image1Stream = CreateTestImage(100, 100);
        using var image2Stream = CreateTestImage(150, 150);

        using var content = new MultipartFormDataContent();
        
        var streamContent1 = new StreamContent(image1Stream);
        streamContent1.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(streamContent1, "imageFiles", "img1.jpg");
        
        var streamContent2 = new StreamContent(image2Stream);
        streamContent2.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(streamContent2, "imageFiles", "img2.jpg");

        // Act
        var response = await client.PostAsync("/api/evaluation/analyze-batch", content);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<EvaluateBatchResponse>();
        
        Assert.NotNull(result);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(2, result.Summary.Total);
        Assert.Equal(0, result.Summary.WithExpectedLabel); // 未提供标签
    }

    [Fact]
    public async Task AnalyzeBatch_ShouldCalculateMetrics_WithLabelsJson()
    {
        // Arrange
        using var client = _factory.CreateClient();
        using var image1Stream = CreateTestImage(100, 100);
        using var image2Stream = CreateTestImage(150, 150);

        var labelsMap = new Dictionary<string, string>
        {
            { "img1.jpg", "Truncated" },
            { "img2.jpg", "BlurryOrOutOfFocus" }
        };
        var labelsJson = JsonSerializer.Serialize(labelsMap);

        using var content = new MultipartFormDataContent();
        
        var streamContent1 = new StreamContent(image1Stream);
        streamContent1.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(streamContent1, "imageFiles", "img1.jpg");
        
        var streamContent2 = new StreamContent(image2Stream);
        streamContent2.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(streamContent2, "imageFiles", "img2.jpg");
        
        content.Add(new StringContent(labelsJson), "labelsJson");

        // Act
        var response = await client.PostAsync("/api/evaluation/analyze-batch", content);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<EvaluateBatchResponse>();
        
        Assert.NotNull(result);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(2, result.Summary.Total);
        Assert.Equal(2, result.Summary.WithExpectedLabel);
        Assert.NotNull(result.Summary.Accuracy);
        Assert.InRange(result.Summary.Accuracy.Value, 0m, 1m);
    }

    [Fact]
    public async Task AnalyzeBatch_ShouldReturn400_WhenNoImagesProvided()
    {
        // Arrange
        using var client = _factory.CreateClient();
        using var content = new MultipartFormDataContent();

        // Act
        var response = await client.PostAsync("/api/evaluation/analyze-batch", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.Contains("图片", error.Error);
    }

    [Fact]
    public async Task AnalyzeBatch_ShouldReturn400_WhenInvalidLabelsJson()
    {
        // Arrange
        using var client = _factory.CreateClient();
        using var imageStream = CreateTestImage(100, 100);

        using var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(imageStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(streamContent, "imageFiles", "img1.jpg");
        content.Add(new StringContent("{invalid json"), "labelsJson");

        // Act
        var response = await client.PostAsync("/api/evaluation/analyze-batch", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.Contains("labelsJson", error.Error);
    }

    /// <summary>
    /// 创建测试图片
    /// </summary>
    private static MemoryStream CreateTestImage(int width, int height)
    {
        var image = new Image<Rgba32>(width, height);
        
        // 填充随机颜色
        var random = new Random();
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var color = new Rgba32(
                    (byte)random.Next(256),
                    (byte)random.Next(256),
                    (byte)random.Next(256));
                image[x, y] = color;
            }
        }

        var stream = new MemoryStream();
        image.SaveAsJpeg(stream);
        stream.Position = 0;
        return stream;
    }
}
