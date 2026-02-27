using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using DandDTemplateParserCSharp.Controllers;
using DandDTemplateParserCSharp.Domain;
using DandDTemplateParserCSharp.Options;

namespace DandDTemplateParserCSharp.Repositories;

public sealed class CharacterSheetRepository(
    IOptions<DatabaseOptions> dbOptions,
    ILogger<CharacterSheetRepository> logger)
    : ICharacterSheetRepository
{
    private readonly string _connectionString = dbOptions.Value.ConnectionString;

    public async Task<CharacterSheetRender> SaveAsync(CharacterSheetRender render, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO character_sheet_renders (id, sheet_type, character_name, level, response_html, created_at)
            VALUES (@Id, @SheetType, @CharacterName, @Level, @ResponseHtml, @CreatedAt)
            """;

        await using var conn = new SqlConnection(_connectionString);

        logger.LogDebug("Saving character sheet render {RenderId}", render.Id);
        await conn.ExecuteAsync(new CommandDefinition(
            sql,
            new
            {
                Id            = render.Id.ToString(),
                render.SheetType,
                render.CharacterName,
                render.Level,
                render.ResponseHtml,
                render.CreatedAt
            },
            cancellationToken: ct));

        return render;
    }

    public async Task<CharacterSheetRender?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = """
            SELECT id, sheet_type, character_name, level, response_html, created_at
            FROM character_sheet_renders
            WHERE id = @Id
            """;

        await using var conn = new SqlConnection(_connectionString);

        var row = await conn.QuerySingleOrDefaultAsync<RenderRow>(
            new CommandDefinition(sql, new { Id = id.ToString() }, cancellationToken: ct));

        return row is null ? null : Map(row);
    }

    public async Task<IReadOnlyList<CharacterSheetSummary>> GetByLevelAsync(int level, CancellationToken ct = default)
    {
        const string sql = """
            SELECT id, sheet_type, character_name, level, created_at
            FROM character_sheet_renders
            WHERE level = @Level
            ORDER BY created_at DESC
            """;

        await using var conn = new SqlConnection(_connectionString);

        var rows = await conn.QueryAsync<SummaryRow>(
            new CommandDefinition(sql, new { Level = level }, cancellationToken: ct));

        return rows.Select(MapToSummary).ToList();
    }

    public async Task<IReadOnlyList<CharacterSheetSummary>> GetBySheetTypeAsync(string sheetType, CancellationToken ct = default)
    {
        const string sql = """
            SELECT id, sheet_type, character_name, level, created_at
            FROM character_sheet_renders
            WHERE sheet_type = @SheetType
            ORDER BY created_at DESC
            """;

        await using var conn = new SqlConnection(_connectionString);

        var rows = await conn.QueryAsync<SummaryRow>(
            new CommandDefinition(sql, new { SheetType = sheetType }, cancellationToken: ct));

        return rows.Select(MapToSummary).ToList();
    }

    private static CharacterSheetRender Map(RenderRow r) =>
        CharacterSheetRender.Reconstitute(
            Guid.Parse(r.id), r.sheet_type, r.character_name, r.level, r.response_html, r.created_at);

    private static CharacterSheetSummary MapToSummary(SummaryRow r) =>
        new(Guid.Parse(r.id), r.sheet_type, r.character_name, r.level, r.created_at);

    // Private DTOs — column names match DB snake_case exactly for Dapper mapping
    private sealed record RenderRow(
        string   id,
        string   sheet_type,
        string   character_name,
        int      level,
        string   response_html,
        DateTime created_at);

    private sealed record SummaryRow(
        string   id,
        string   sheet_type,
        string   character_name,
        int      level,
        DateTime created_at);
}
