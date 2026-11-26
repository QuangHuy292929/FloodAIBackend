namespace FloodAI.DataAccess.Observations;

public static class ObservationMetadata
{
    public const string Observed = "OBSERVED";
    public const string Estimated = "ESTIMATED";
    public const string Synthetic = "SYNTHETIC";
    public const string Unclassified = "UNCLASSIFIED";
    public const string SourceLocalUnverified = "SOURCE_LOCAL_UNVERIFIED";
    public const string VietnamLocal = "VIETNAM_LOCAL";
    public const string Utc = "UTC";
    public const string PerSource = "PER_SOURCE";
    public const string VerifiedUnit = "VERIFIED";
    public const string UnverifiedUnit = "UNVERIFIED";

    public static bool IsKnownOrigin(string? value) => value is Observed or Estimated or Synthetic or Unclassified;

    public static bool IsKnownTimeBasis(string? value) => value is SourceLocalUnverified or VietnamLocal or Utc;
}
