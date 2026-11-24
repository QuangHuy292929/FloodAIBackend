namespace FloodAI.DataAccess.Observations;

public sealed record VanUcHourlyRow(
    DateTime TimestampLocal,
    double TrungTrangWaterLevel,
    double HonDauWaterLevel,
    double TrungTrangDischarge);
