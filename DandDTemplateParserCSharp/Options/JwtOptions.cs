using System.ComponentModel.DataAnnotations;

namespace DandDTemplateParserCSharp.Options;

public sealed class JwtOptions
{
    public const string Section = "Jwt";

    [Required, MinLength(32)]
    public string SigningKey { get; init; } = string.Empty;

    [Required, MinLength(8)]
    public string ApiSecret { get; init; } = string.Empty;

    [Required]
    public string Issuer { get; init; } = string.Empty;

    [Required]
    public string Audience { get; init; } = string.Empty;

    public int ExpirationMinutes { get; init; } = 60;
}
