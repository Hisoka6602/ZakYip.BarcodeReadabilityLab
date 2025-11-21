using System.ComponentModel;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using ZakYip.BarcodeReadabilityLab.Core.Domain.Models;
using ZakYip.BarcodeReadabilityLab.Core.Enums;

namespace ZakYip.BarcodeReadabilityLab.IntegrationTests.Simulation;

/// <summary>
/// 测试训练数据集构建器，用于生成仿真的训练数据集
/// </summary>
internal sealed class TestTrainingDatasetBuilder : IDisposable
{
    private readonly string _workspaceRoot;
    private bool _disposed;

    private TestTrainingDatasetBuilder(
        string trainingRootDirectory,
        string outputModelDirectory,
        string workspaceRoot,
        IReadOnlyDictionary<string, int> labelDistribution)
    {
        TrainingRootDirectory = trainingRootDirectory;
        OutputModelDirectory = outputModelDirectory;
        _workspaceRoot = workspaceRoot;
        LabelDistribution = labelDistribution;
    }

    /// <summary>
    /// 训练数据根目录
    /// </summary>
    public string TrainingRootDirectory { get; }

    /// <summary>
    /// 输出模型目录
    /// </summary>
    public string OutputModelDirectory { get; }

    /// <summary>
    /// 标签分布（类别名称 -> 样本数量）
    /// </summary>
    public IReadOnlyDictionary<string, int> LabelDistribution { get; }

    /// <summary>
    /// 创建训练数据集，包含 NoreadReason 的所有 7 个类别
    /// </summary>
    /// <param name="samplesPerClass">每个类别的样本数量</param>
    /// <param name="imageSize">图片尺寸（正方形）</param>
    /// <returns>训练数据集构建器实例</returns>
    public static TestTrainingDatasetBuilder CreateWithAllNoreadReasons(int samplesPerClass = 3, int imageSize = 32)
    {
        var workspaceRoot = Path.Combine(
            Path.GetTempPath(),
            "barcode-lab-simulation",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workspaceRoot);

        var trainingRootDirectory = Path.Combine(workspaceRoot, "training");
        Directory.CreateDirectory(trainingRootDirectory);

        var outputModelDirectory = Path.Combine(workspaceRoot, "output");
        Directory.CreateDirectory(outputModelDirectory);

        var labelDistribution = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        // 动态获取 NoreadReason 枚举的所有值和描述
        var noreadReasons = Enum.GetValues<NoreadReason>();
        var classColors = new[]
        {
            Color.Red,
            Color.Blue,
            Color.Green,
            Color.Yellow,
            Color.Purple,
            Color.Orange,
            Color.Cyan
        };

        for (var i = 0; i < noreadReasons.Length; i++)
        {
            var reason = noreadReasons[i];
            var enumType = typeof(NoreadReason);
            var memberInfo = enumType.GetMember(reason.ToString()).FirstOrDefault();
            var descriptionAttribute = memberInfo?.GetCustomAttributes(typeof(DescriptionAttribute), false)
                .FirstOrDefault() as DescriptionAttribute;
            
            // 使用描述作为目录名，如果没有描述则使用枚举名
            var label = descriptionAttribute?.Description ?? reason.ToString();
            var classDirectory = Path.Combine(trainingRootDirectory, label);
            Directory.CreateDirectory(classDirectory);

            var color = classColors[i % classColors.Length];
            labelDistribution[label] = 0;

            for (var sampleIndex = 0; sampleIndex < samplesPerClass; sampleIndex++)
            {
                var filePath = Path.Combine(classDirectory, $"sample-{sampleIndex + 1:D3}.png");
                using var image = new Image<Rgba32>(imageSize, imageSize);
                
                // 为每个样本添加一些变化，使其更真实
                image.Mutate(context => context.BackgroundColor(color));
                image.SaveAsPng(filePath);
                labelDistribution[label]++;
            }
        }

        return new TestTrainingDatasetBuilder(
            trainingRootDirectory,
            outputModelDirectory,
            workspaceRoot,
            labelDistribution);
    }

    /// <summary>
    /// 创建简化的二分类训练数据集（readable/unreadable）
    /// </summary>
    /// <param name="samplesPerClass">每个类别的样本数量</param>
    /// <param name="imageSize">图片尺寸（正方形）</param>
    /// <returns>训练数据集构建器实例</returns>
    public static TestTrainingDatasetBuilder CreateBinaryClassification(int samplesPerClass = 3, int imageSize = 16)
    {
        var workspaceRoot = Path.Combine(
            Path.GetTempPath(),
            "barcode-lab-simulation",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workspaceRoot);

        var trainingRootDirectory = Path.Combine(workspaceRoot, "training");
        Directory.CreateDirectory(trainingRootDirectory);

        var outputModelDirectory = Path.Combine(workspaceRoot, "output");
        Directory.CreateDirectory(outputModelDirectory);

        var labelDistribution = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var classDefinitions = new Dictionary<string, Color>
        {
            ["readable"] = Color.LimeGreen,
            ["unreadable"] = Color.Crimson
        };

        foreach (var (label, color) in classDefinitions)
        {
            var classDirectory = Path.Combine(trainingRootDirectory, label);
            Directory.CreateDirectory(classDirectory);

            labelDistribution[label] = 0;
            for (var index = 0; index < samplesPerClass; index++)
            {
                var filePath = Path.Combine(classDirectory, $"sample-{index + 1}.png");
                using var image = new Image<Rgba32>(imageSize, imageSize);
                image.Mutate(context => context.BackgroundColor(color));
                image.SaveAsPng(filePath);
                labelDistribution[label]++;
            }
        }

        return new TestTrainingDatasetBuilder(
            trainingRootDirectory,
            outputModelDirectory,
            workspaceRoot,
            labelDistribution);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (Directory.Exists(_workspaceRoot))
        {
            try
            {
                Directory.Delete(_workspaceRoot, recursive: true);
            }
            catch
            {
                // 忽略清理错误
            }
        }

        _disposed = true;
    }
}
