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

        var saveResult = await repository.SaveAsync(render, ct);
        if (saveResult.IsFailure)
            return Result<CharacterSheetRender, CharacterSheetError>.Failure(saveResult.Error);

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

        var saveResult = await repository.SaveAsync(render, ct);
        if (saveResult.IsFailure)
            return Result<CharacterSheetRender, CharacterSheetError>.Failure(saveResult.Error);

        logger.LogInformation("Rendered legendary sheet {RenderId} for '{CharacterName}'",
            render.Id, render.CharacterName);

        return Result<CharacterSheetRender, CharacterSheetError>.Success(render);
    }

    public async Task<Result<CharacterSheetRender, CharacterSheetError>> GetByIdAsync(
        Guid id, CancellationToken ct = default)
    {
        var dbResult = await repository.GetByIdAsync(id, ct);
        if (dbResult.IsFailure)
            return Result<CharacterSheetRender, CharacterSheetError>.Failure(dbResult.Error);

        if (dbResult.Value is null)
        {
            logger.LogWarning("Character sheet render {RenderId} not found", id);
            return Result<CharacterSheetRender, CharacterSheetError>.Failure(
                new CharacterSheetError.NotFound(id));
        }

        return Result<CharacterSheetRender, CharacterSheetError>.Success(dbResult.Value);
    }

    public async Task<Result<IReadOnlyList<CharacterSheetSummary>, CharacterSheetError>> GetByLevelAsync(
        int level, int page = 1, int pageSize = 25, CancellationToken ct = default)
    {
        if (level is < 1 or > 20)
            return Result<IReadOnlyList<CharacterSheetSummary>, CharacterSheetError>.Failure(
                new CharacterSheetError.ValidationError("Level must be between 1 and 20."));

        if (page < 1)
            return Result<IReadOnlyList<CharacterSheetSummary>, CharacterSheetError>.Failure(
                new CharacterSheetError.ValidationError("Page must be at least 1."));

        if (pageSize is < 1 or > 100)
            return Result<IReadOnlyList<CharacterSheetSummary>, CharacterSheetError>.Failure(
                new CharacterSheetError.ValidationError("PageSize must be between 1 and 100."));

        var paging   = new PageRequest(page, pageSize);
        var dbResult = await repository.GetByLevelAsync(level, paging, ct);
        if (dbResult.IsFailure)
            return Result<IReadOnlyList<CharacterSheetSummary>, CharacterSheetError>.Failure(dbResult.Error);

        logger.LogDebug("Found {Count} renders for level {Level} (page {Page}/{PageSize})",
            dbResult.Value.Count, level, page, pageSize);
        return Result<IReadOnlyList<CharacterSheetSummary>, CharacterSheetError>.Success(dbResult.Value);
    }

    public async Task<Result<IReadOnlyList<CharacterSheetSummary>, CharacterSheetError>> GetBySheetTypeAsync(
        string sheetType, int page = 1, int pageSize = 25, CancellationToken ct = default)
    {
        var normalized = sheetType.Trim().ToLowerInvariant();
        if (normalized is not ("general" or "legendary"))
            return Result<IReadOnlyList<CharacterSheetSummary>, CharacterSheetError>.Failure(
                new CharacterSheetError.ValidationError("sheetType must be 'general' or 'legendary'."));

        if (page < 1)
            return Result<IReadOnlyList<CharacterSheetSummary>, CharacterSheetError>.Failure(
                new CharacterSheetError.ValidationError("Page must be at least 1."));

        if (pageSize is < 1 or > 100)
            return Result<IReadOnlyList<CharacterSheetSummary>, CharacterSheetError>.Failure(
                new CharacterSheetError.ValidationError("PageSize must be between 1 and 100."));

        var paging   = new PageRequest(page, pageSize);
        var dbResult = await repository.GetBySheetTypeAsync(normalized, paging, ct);
        if (dbResult.IsFailure)
            return Result<IReadOnlyList<CharacterSheetSummary>, CharacterSheetError>.Failure(dbResult.Error);

        logger.LogDebug("Found {Count} renders for sheet type '{SheetType}' (page {Page}/{PageSize})",
            dbResult.Value.Count, normalized, page, pageSize);
        return Result<IReadOnlyList<CharacterSheetSummary>, CharacterSheetError>.Success(dbResult.Value);
    }
}
