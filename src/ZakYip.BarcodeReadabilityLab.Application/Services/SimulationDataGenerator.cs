namespace ZakYip.BarcodeReadabilityLab.Application.Services;

using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

/// <summary>
/// 仿真数据生成器实现
/// </summary>
public class SimulationDataGenerator : ISimulationDataGenerator
{
    private readonly ILogger<SimulationDataGenerator> _logger;

    // 仿真模式下的示例类别（简化到3个类别）
    private readonly string[] _simulationCategories = new[]
    {
        "Normal",       // 正常可读
        "Blurry",       // 模糊
        "LowLight"      // 光照不足
    };

    public SimulationDataGenerator(ILogger<SimulationDataGenerator> logger)
    {
        _logger = logger;
    }

    public async Task<SimulationDataResult> GenerateTrainingDataAsync(
        string outputDirectory,
        int samplesPerClass = 5)
    {
        try
        {
            _logger.LogInformation("开始生成仿真训练数据到目录：{OutputDirectory}", outputDirectory);

            // 确保输出目录存在
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            var totalSamples = 0;

            // 为每个类别生成样本图片
            foreach (var category in _simulationCategories)
            {
                var categoryPath = Path.Combine(outputDirectory, category);
                Directory.CreateDirectory(categoryPath);

                for (int i = 0; i < samplesPerClass; i++)
                {
                    var imagePath = Path.Combine(categoryPath, $"sample_{i + 1}.png");
                    await GenerateSampleImageAsync(imagePath, category, i);
                    totalSamples++;
                }

                _logger.LogInformation("生成类别 {Category} 的 {Count} 个样本", category, samplesPerClass);
            }

            _logger.LogInformation("✅ 仿真训练数据生成完成：{ClassCount} 个类别，{TotalSamples} 个样本",
                _simulationCategories.Length, totalSamples);

            return new SimulationDataResult
            {
                IsSuccess = true,
                OutputDirectory = outputDirectory,
                ClassCount = _simulationCategories.Length,
                TotalSamples = totalSamples
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 生成仿真训练数据失败");
            return new SimulationDataResult
            {
                IsSuccess = false,
                OutputDirectory = outputDirectory,
                ErrorMessage = $"生成失败：{ex.Message}"
            };
        }
    }

    private async Task GenerateSampleImageAsync(string imagePath, string category, int index)
    {
        // 创建一个简单的合成图片（100x100像素）
        var width = 100;
        var height = 100;

        using var image = new Image<Rgba32>(width, height);

        // 根据类别设置不同的图片特征
        // 直接在创建时设置像素颜色
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Rgba32 color = category switch
                {
                    "Normal" => new Rgba32(255, 255, 255), // 白色 - 正常图片
                    "Blurry" => new Rgba32(211, 211, 211), // 浅灰色 - 模糊图片
                    "LowLight" => new Rgba32(64, 64, 64), // 暗灰色 - 光照不足
                    _ => new Rgba32(255, 255, 255)
                };
                
                image[x, y] = color;
            }
        }

        // 应用效果
        image.Mutate(ctx =>
        {
            if (category == "Blurry")
            {
                // 应用模糊效果
                ctx.GaussianBlur(5);
            }
            else if (category == "LowLight")
            {
                // 应用低亮度
                ctx.Brightness(0.3f);
            }

            // 添加一些随机变化使每张图片不完全相同
            if (index % 2 == 0)
            {
                ctx.Rotate(5);
            }
        });

        await image.SaveAsPngAsync(imagePath);
    }
}
