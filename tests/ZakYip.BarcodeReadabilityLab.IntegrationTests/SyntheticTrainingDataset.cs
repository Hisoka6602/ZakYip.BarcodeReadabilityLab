using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace ZakYip.BarcodeReadabilityLab.IntegrationTests;

internal sealed class SyntheticTrainingDataset : IDisposable
{
    private readonly string _workspaceRoot;
    private bool _disposed;

    private SyntheticTrainingDataset(string trainingRootDirectory, string outputModelDirectory, string workspaceRoot, IReadOnlyDictionary<string, int> labelDistribution)
    {
        TrainingRootDirectory = trainingRootDirectory;
        OutputModelDirectory = outputModelDirectory;
        _workspaceRoot = workspaceRoot;
        LabelDistribution = labelDistribution;
    }

    public string TrainingRootDirectory { get; }

    public string OutputModelDirectory { get; }

    public IReadOnlyDictionary<string, int> LabelDistribution { get; }

    public static SyntheticTrainingDataset Create(int samplesPerClass = 3, int imageSize = 16)
    {
        var workspaceRoot = Path.Combine(Path.GetTempPath(), "barcode-lab-integration", Guid.NewGuid().ToString("N"));
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

        return new SyntheticTrainingDataset(trainingRootDirectory, outputModelDirectory, workspaceRoot, labelDistribution);
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
                Directory.Delete(_workspaceRoot, true);
            }
            catch
            {
                // Ignore cleanup errors in tests.
            }
        }

        _disposed = true;
    }
}
