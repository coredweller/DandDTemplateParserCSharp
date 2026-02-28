namespace DandDTemplateParserCSharp.Options;

public sealed class CorsOptions
{
    public const string Section = "Cors";

    /// <summary>
    /// Origins permitted to make cross-origin requests (e.g. "http://localhost:5173").
    /// Empty array means no cross-origin access is granted — the secure default for production.
    /// </summary>
    public string[] AllowedOrigins { get; init; } = [];
}
