namespace DandDTemplateParserCSharp.Domain;

/// <summary>Validated pagination parameters passed from service to repository.</summary>
public sealed record PageRequest(int Page, int PageSize)
{
    public int Offset => (Page - 1) * PageSize;

    public static readonly PageRequest Default = new(Page: 1, PageSize: 25);
}
