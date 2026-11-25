namespace FloodAI.DataAccess.Observations;

public static class ObservationQuality
{
    public const string SourceUnverified = "SOURCE_UNVERIFIED";
    public const string Valid = "VALID";
    public const string Suspect = "SUSPECT";
    public const string Invalid = "INVALID";

    public static bool IsKnown(string? status) => status is SourceUnverified or Valid or Suspect or Invalid;
}
