using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using DandDTemplateParserCSharp.Domain;
using DandDTemplateParserCSharp.Options;

namespace DandDTemplateParserCSharp.Repositories;

public sealed class CharacterSheetRepository(
    IOptions<DatabaseOptions> dbOptions,
    ILogger<CharacterSheetRepository> logger)
    : ICharacterSheetRepository
{
    private readonly string _connectionString = dbOptions.Value.ConnectionString;

    public async Task<Result<CharacterSheetRender, CharacterSheetError.DatabaseError>> SaveAsync(
        CharacterSheetRender render, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO character_sheet_renders (id, sheet_type, character_name, level, response_html, created_at)
            VALUES (@Id, @SheetType, @CharacterName, @Level, @ResponseHtml, @CreatedAt)
            """;

        try
        {
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

            return Result<CharacterSheetRender, CharacterSheetError.DatabaseError>.Success(render);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save character sheet render {RenderId}", render.Id);
            return Result<CharacterSheetRender, CharacterSheetError.DatabaseError>.Failure(
                new CharacterSheetError.DatabaseError("A database error occurred."));
        }
    }

    public async Task<Result<CharacterSheetRender?, CharacterSheetError.DatabaseError>> GetByIdAsync(
        Guid id, CancellationToken ct = default)
    {
        const string sql = """
            SELECT id, sheet_type, character_name, level, response_html, created_at
            FROM character_sheet_renders
            WHERE id = @Id
            """;

        try
        {
            await using var conn = new SqlConnection(_connectionString);
            var row = await conn.QuerySingleOrDefaultAsync<RenderRow>(
                new CommandDefinition(sql, new { Id = id.ToString() }, cancellationToken: ct));

            return Result<CharacterSheetRender?, CharacterSheetError.DatabaseError>.Success(
                row is null ? null : Map(row));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve character sheet render {RenderId}", id);
            return Result<CharacterSheetRender?, CharacterSheetError.DatabaseError>.Failure(
                new CharacterSheetError.DatabaseError("A database error occurred."));
        }
    }

    public async Task<Result<IReadOnlyList<CharacterSheetSummary>, CharacterSheetError.DatabaseError>> GetByLevelAsync(
        int level, PageRequest page, CancellationToken ct = default)
    {
        const string sql = """
            SELECT id, sheet_type, character_name, level, created_at
            FROM character_sheet_renders
            WHERE level = @Level
            ORDER BY created_at DESC
            OFFSET @Offset ROWS
            FETCH NEXT @PageSize ROWS ONLY
            """;

        try
        {
            await using var conn = new SqlConnection(_connectionString);
            var rows = await conn.QueryAsync<SummaryRow>(
                new CommandDefinition(sql,
                    new { Level = level, page.Offset, page.PageSize },
                    cancellationToken: ct));

            return Result<IReadOnlyList<CharacterSheetSummary>, CharacterSheetError.DatabaseError>.Success(
                rows.Select(MapToSummary).ToList());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve renders for level {Level}", level);
            return Result<IReadOnlyList<CharacterSheetSummary>, CharacterSheetError.DatabaseError>.Failure(
                new CharacterSheetError.DatabaseError("A database error occurred."));
        }
    }

    public async Task<Result<IReadOnlyList<CharacterSheetSummary>, CharacterSheetError.DatabaseError>> GetBySheetTypeAsync(
        string sheetType, PageRequest page, CancellationToken ct = default)
    {
        const string sql = """
            SELECT id, sheet_type, character_name, level, created_at
            FROM character_sheet_renders
            WHERE sheet_type = @SheetType
            ORDER BY created_at DESC
            OFFSET @Offset ROWS
            FETCH NEXT @PageSize ROWS ONLY
            """;

        try
        {
            await using var conn = new SqlConnection(_connectionString);
            var rows = await conn.QueryAsync<SummaryRow>(
                new CommandDefinition(sql,
                    new { SheetType = sheetType, page.Offset, page.PageSize },
                    cancellationToken: ct));

            return Result<IReadOnlyList<CharacterSheetSummary>, CharacterSheetError.DatabaseError>.Success(
                rows.Select(MapToSummary).ToList());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve renders for sheet type '{SheetType}'", sheetType);
            return Result<IReadOnlyList<CharacterSheetSummary>, CharacterSheetError.DatabaseError>.Failure(
                new CharacterSheetError.DatabaseError("A database error occurred."));
        }
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
        DateTimeOffset created_at);

    private sealed record SummaryRow(
        string   id,
        string   sheet_type,
        string   character_name,
        int      level,
        DateTimeOffset created_at);
}
