namespace ZakYip.BarcodeReadabilityLab.Application.Services;

using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;

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
        image.Mutate(ctx =>
        {
            switch (category)
            {
                case "Normal":
                    // 正常图片：清晰的颜色和对比度
                    ctx.Fill(Color.White);
                    ctx.DrawLines(Color.Black, 3, new PointF[]
                    {
                        new PointF(10, 50),
                        new PointF(30, 30),
                        new PointF(50, 70),
                        new PointF(70, 40),
                        new PointF(90, 60)
                    });
                    break;

                case "Blurry":
                    // 模糊图片：使用低对比度颜色
                    ctx.Fill(Color.LightGray);
                    ctx.DrawLines(Color.Gray, 2, new PointF[]
                    {
                        new PointF(10, 50),
                        new PointF(30, 30),
                        new PointF(50, 70),
                        new PointF(70, 40),
                        new PointF(90, 60)
                    });
                    // 应用模糊效果
                    ctx.GaussianBlur(5);
                    break;

                case "LowLight":
                    // 光照不足：暗色背景和低亮度
                    ctx.Fill(Color.DarkGray);
                    ctx.DrawLines(Color.DimGray, 2, new PointF[]
                    {
                        new PointF(10, 50),
                        new PointF(30, 30),
                        new PointF(50, 70),
                        new PointF(70, 40),
                        new PointF(90, 60)
                    });
                    ctx.Brightness(0.3f);
                    break;
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
