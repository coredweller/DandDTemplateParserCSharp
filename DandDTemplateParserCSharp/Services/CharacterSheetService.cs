using FluentValidation;
using DandDTemplateParserCSharp.Controllers;
using DandDTemplateParserCSharp.Domain;
using DandDTemplateParserCSharp.Repositories;

namespace DandDTemplateParserCSharp.Services;

public sealed class CharacterSheetService(
    ICharacterSheetRepository repository,
    IValidator<GeneralSheetRequest> generalValidator,
    IValidator<LegendarySheetRequest> legendaryValidator,
    ILogger<CharacterSheetService> logger)
    : ICharacterSheetService
{
    public async Task<Result<CharacterSheetRender, CharacterSheetError>> RenderGeneralAsync(
        GeneralSheetRequest request, CancellationToken ct = default)
    {
        var validation = await generalValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            var msg = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
            return Result<CharacterSheetRender, CharacterSheetError>.Failure(
                new CharacterSheetError.ValidationError(msg));
        }

        var html   = CharacterSheetHtmlBuilder.BuildGeneral(request);
        var render = CharacterSheetRender.Create("general", request.CharacterName, request.Level, html);

        try
        {
            await repository.SaveAsync(render, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save general sheet render {RenderId}", render.Id);
            return Result<CharacterSheetRender, CharacterSheetError>.Failure(
                new CharacterSheetError.DatabaseError("A database error occurred."));
        }

        logger.LogInformation("Rendered general sheet {RenderId} for '{CharacterName}'",
            render.Id, render.CharacterName);

        return Result<CharacterSheetRender, CharacterSheetError>.Success(render);
    }

    public async Task<Result<CharacterSheetRender, CharacterSheetError>> RenderLegendaryAsync(
        LegendarySheetRequest request, CancellationToken ct = default)
    {
        var validation = await legendaryValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            var msg = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
            return Result<CharacterSheetRender, CharacterSheetError>.Failure(
                new CharacterSheetError.ValidationError(msg));
        }

        var html   = CharacterSheetHtmlBuilder.BuildLegendary(request);
        var render = CharacterSheetRender.Create("legendary", request.CharacterName, request.Level, html);

        try
        {
            await repository.SaveAsync(render, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save legendary sheet render {RenderId}", render.Id);
            return Result<CharacterSheetRender, CharacterSheetError>.Failure(
                new CharacterSheetError.DatabaseError("A database error occurred."));
        }

        logger.LogInformation("Rendered legendary sheet {RenderId} for '{CharacterName}'",
            render.Id, render.CharacterName);

        return Result<CharacterSheetRender, CharacterSheetError>.Success(render);
    }

    public async Task<Result<CharacterSheetRender, CharacterSheetError>> GetByIdAsync(
        Guid id, CancellationToken ct = default)
    {
        CharacterSheetRender? render;
        try
        {
            render = await repository.GetByIdAsync(id, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve render {RenderId}", id);
            return Result<CharacterSheetRender, CharacterSheetError>.Failure(
                new CharacterSheetError.DatabaseError("A database error occurred."));
        }

        if (render is null)
        {
            logger.LogWarning("Character sheet render {RenderId} not found", id);
            return Result<CharacterSheetRender, CharacterSheetError>.Failure(
                new CharacterSheetError.NotFound(id));
        }

        return Result<CharacterSheetRender, CharacterSheetError>.Success(render);
    }

    public async Task<Result<IReadOnlyList<CharacterSheetSummary>, CharacterSheetError>> GetByLevelAsync(
        int level, CancellationToken ct = default)
    {
        if (level is < 1 or > 20)
            return Result<IReadOnlyList<CharacterSheetSummary>, CharacterSheetError>.Failure(
                new CharacterSheetError.ValidationError("Level must be between 1 and 20."));

        IReadOnlyList<CharacterSheetSummary> results;
        try
        {
            results = await repository.GetByLevelAsync(level, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve renders for level {Level}", level);
            return Result<IReadOnlyList<CharacterSheetSummary>, CharacterSheetError>.Failure(
                new CharacterSheetError.DatabaseError("A database error occurred."));
        }

        logger.LogDebug("Found {Count} renders for level {Level}", results.Count, level);
        return Result<IReadOnlyList<CharacterSheetSummary>, CharacterSheetError>.Success(results);
    }

    public async Task<Result<IReadOnlyList<CharacterSheetSummary>, CharacterSheetError>> GetBySheetTypeAsync(
        string sheetType, CancellationToken ct = default)
    {
        var normalized = sheetType.Trim().ToLowerInvariant();
        if (normalized is not ("general" or "legendary"))
            return Result<IReadOnlyList<CharacterSheetSummary>, CharacterSheetError>.Failure(
                new CharacterSheetError.ValidationError("sheetType must be 'general' or 'legendary'."));

        IReadOnlyList<CharacterSheetSummary> results;
        try
        {
            results = await repository.GetBySheetTypeAsync(normalized, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve renders for sheet type '{SheetType}'", normalized);
            return Result<IReadOnlyList<CharacterSheetSummary>, CharacterSheetError>.Failure(
                new CharacterSheetError.DatabaseError("A database error occurred."));
        }

        logger.LogDebug("Found {Count} renders for sheet type '{SheetType}'", results.Count, normalized);
        return Result<IReadOnlyList<CharacterSheetSummary>, CharacterSheetError>.Success(results);
    }
}
