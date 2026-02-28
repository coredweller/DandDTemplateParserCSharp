namespace DandDTemplateParserCSharp.Options;

public sealed class SwaggerOptions
{
    public const string Section = "Swagger";

    /// <summary>
    /// Enables the Swagger UI and OpenAPI endpoint. Must be explicitly set to true —
    /// defaults to false so production deployments are safe even if the environment
    /// name is accidentally set to Development.
    /// </summary>
    public bool Enabled { get; init; }
}
