using System.ComponentModel.DataAnnotations;

namespace DandDTemplateParserCSharp.Options;

public sealed class DatabaseOptions
{
    public const string Section = "Database";

    [Required, MinLength(1)]
    public string ConnectionString { get; init; } = string.Empty;
}
