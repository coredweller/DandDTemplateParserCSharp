using System.ComponentModel.DataAnnotations;

namespace DandDTemplateParserCSharp.Options;

public sealed class RateLimitingOptions
{
    public const string Section = "RateLimiting";

    /// <summary>Max requests to POST /api/v1/auth/token per window per IP address.</summary>
    [Range(1, int.MaxValue)]
    public int TokenPermitLimit { get; init; } = 5;

    /// <summary>Fixed window duration (minutes) for the token endpoint policy.</summary>
    [Range(1, int.MaxValue)]
    public int TokenWindowMinutes { get; init; } = 15;

    /// <summary>Initial and maximum token count for the authenticated-endpoint bucket.</summary>
    [Range(1, int.MaxValue)]
    public int AuthBucketCapacity { get; init; } = 40;

    /// <summary>Tokens replenished per second for authenticated endpoints.</summary>
    [Range(1, int.MaxValue)]
    public int AuthReplenishPerSecond { get; init; } = 10;
}
