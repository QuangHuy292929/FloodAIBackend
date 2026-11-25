namespace FloodAI.Business.Forecasting;

public static class VanUcForecastDefinition
{
    public const string Version = "backtest-2022-h2";
    public const int HistoryHours = 48;
    public const double ResearchThreshold = 142;
    public const string ThresholdType = "TRAIN_Q95_RESEARCH";
    public const string ThresholdSource =
        "ml/runs/van_uc_trung_trang/backtest-2022-h2/manifest.json#high_water_threshold_raw";
    public const string UnitStatus = "UNVERIFIED";

    public static IReadOnlyList<int> Horizons { get; } = Array.AsReadOnly(new[] { 1, 3, 6 });
}
