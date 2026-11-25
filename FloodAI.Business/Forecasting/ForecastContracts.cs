namespace FloodAI.Business.Forecasting;

public sealed record ForecastModelSummary(string Version, int HorizonHours);

public sealed record ForecastHour(
    DateTime TimestampLocal,
    double WaterLevelTrungTrangRaw,
    double WaterLevelHonDauRaw,
    double DischargeTrungTrangRaw);

public sealed record InferenceModel(int HorizonHours, string ArtifactPath);
public sealed record InferenceBundle(
    string Version, string ManifestHash, double ThresholdValueRaw, IReadOnlyList<InferenceModel> Models);
public sealed record InferencePrediction(int HorizonHours, double PredictedWaterLevelRaw);
public sealed record InferenceResult(
    string Version, string ManifestHash, IReadOnlyList<InferencePrediction> Predictions);

public sealed record HistoricalForecastPoint(
    int HorizonHours,
    DateTime TargetTimeLocal,
    double PredictedWaterLevelRaw,
    double? ObservedWaterLevelRaw,
    string WarningStatus);

public sealed record HistoricalForecast(
    long RunId,
    long StationId,
    DateTime IssueTimeLocal,
    string TimeBasis,
    string ModelVersion,
    string ModelBundleHash,
    string InputHash,
    string SourceName,
    string DataQualityStatus,
    string UnitStatus,
    string ThresholdType,
    string ThresholdSource,
    double ThresholdValueRaw,
    IReadOnlyList<HistoricalForecastPoint> Predictions);
