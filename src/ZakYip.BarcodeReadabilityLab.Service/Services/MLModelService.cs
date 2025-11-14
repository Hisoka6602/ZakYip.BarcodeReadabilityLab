using Microsoft.ML;
using Microsoft.ML.Data;
using ZakYip.BarcodeReadabilityLab.Service.Configuration;
using ZakYip.BarcodeReadabilityLab.Service.Models;
using Microsoft.Extensions.Options;

namespace ZakYip.BarcodeReadabilityLab.Service.Services;

public interface IMLModelService
{
    Task<ImagePrediction?> PredictAsync(string imagePath);
    Task TrainModelAsync(string trainingDataPath, CancellationToken cancellationToken = default);
    bool IsModelLoaded { get; }
}

public class MLModelService : IMLModelService
{
    private readonly MLContext _mlContext;
    private readonly BarcodeReadabilityServiceSettings _settings;
    private readonly ILogger<MLModelService> _logger;
    private ITransformer? _model;
    private DataViewSchema? _modelSchema;
    private readonly object _modelLock = new object();

    public bool IsModelLoaded => _model != null;

    public MLModelService(
        IOptions<BarcodeReadabilityServiceSettings> settings,
        ILogger<MLModelService> logger)
    {
        _mlContext = new MLContext(seed: 0);
        _settings = settings.Value;
        _logger = logger;
        LoadExistingModel();
    }

    private void LoadExistingModel()
    {
        try
        {
            var modelPath = Path.Combine(_settings.ModelPath, "model.zip");
            if (File.Exists(modelPath))
            {
                lock (_modelLock)
                {
                    _model = _mlContext.Model.Load(modelPath, out _modelSchema);
                }
                _logger.LogInformation("Model loaded from {ModelPath}", modelPath);
            }
            else
            {
                _logger.LogWarning("No existing model found at {ModelPath}", modelPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading existing model");
        }
    }

    public async Task<ImagePrediction?> PredictAsync(string imagePath)
    {
        if (_model == null)
        {
            _logger.LogWarning("Model not loaded, cannot predict for {ImagePath}", imagePath);
            return null;
        }

        try
        {
            var imageData = new ImageData { ImagePath = imagePath };
            
            ITransformer modelCopy;
            DataViewSchema? schemaCopy;
            lock (_modelLock)
            {
                modelCopy = _model;
                schemaCopy = _modelSchema;
            }

            var predictionEngine = _mlContext.Model.CreatePredictionEngine<ImageData, ImagePrediction>(modelCopy);
            var prediction = predictionEngine.Predict(imageData);

            return await Task.FromResult(prediction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error predicting for image {ImagePath}", imagePath);
            return null;
        }
    }

    public async Task TrainModelAsync(string trainingDataPath, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting model training with data from {TrainingDataPath}", trainingDataPath);

            cancellationToken.ThrowIfCancellationRequested();

            var imageFiles = GetTrainingImageFiles(trainingDataPath);
            if (imageFiles.Count == 0)
            {
                throw new InvalidOperationException("No training images found in the specified directory structure");
            }

            cancellationToken.ThrowIfCancellationRequested();

            var trainingData = _mlContext.Data.LoadFromEnumerable(imageFiles);

            cancellationToken.ThrowIfCancellationRequested();

            var pipeline = _mlContext.Transforms.Conversion.MapValueToKey("Label")
                .Append(_mlContext.Transforms.LoadRawImageBytes("ImageBytes", null, nameof(ImageData.ImagePath)))
                .Append(_mlContext.Transforms.CopyColumns("Features", "ImageBytes"))
                .Append(_mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy("Label", "Features"))
                .Append(_mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

            _logger.LogInformation("Training model with {Count} images...", imageFiles.Count);
            
            cancellationToken.ThrowIfCancellationRequested();
            
            var trainedModel = await Task.Run(() => pipeline.Fit(trainingData), cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            var modelPath = Path.Combine(_settings.ModelPath, "model.zip");
            Directory.CreateDirectory(_settings.ModelPath);
            
            _mlContext.Model.Save(trainedModel, trainingData.Schema, modelPath);

            lock (_modelLock)
            {
                _model = trainedModel;
                _modelSchema = trainingData.Schema;
            }

            _logger.LogInformation("Model training completed and saved to {ModelPath}", modelPath);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Model training was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during model training");
            throw;
        }
    }

    private List<ImageData> GetTrainingImageFiles(string trainingDataPath)
    {
        var imageFiles = new List<ImageData>();

        if (!Directory.Exists(trainingDataPath))
        {
            _logger.LogWarning("Training data path does not exist: {TrainingDataPath}", trainingDataPath);
            return imageFiles;
        }

        var labelDirectories = Directory.GetDirectories(trainingDataPath);

        foreach (var labelDir in labelDirectories)
        {
            var label = Path.GetFileName(labelDir);
            var files = Directory.GetFiles(labelDir, "*.*", SearchOption.TopDirectoryOnly)
                .Where(f => _settings.SupportedImageExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()));

            foreach (var file in files)
            {
                imageFiles.Add(new ImageData
                {
                    ImagePath = file,
                    Label = label
                });
            }
        }

        return imageFiles;
    }
}
