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

        await repository.SaveAsync(render, ct);
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

        await repository.SaveAsync(render, ct);
        logger.LogInformation("Rendered legendary sheet {RenderId} for '{CharacterName}'",
            render.Id, render.CharacterName);

        return Result<CharacterSheetRender, CharacterSheetError>.Success(render);
    }

    public async Task<Result<CharacterSheetRender, CharacterSheetError>> GetByIdAsync(
        Guid id, CancellationToken ct = default)
    {
        var render = await repository.GetByIdAsync(id, ct);

        if (render is null)
        {
            logger.LogWarning("Character sheet render {RenderId} not found", id);
            return Result<CharacterSheetRender, CharacterSheetError>.Failure(
                new CharacterSheetError.NotFound(id));
        }

        return Result<CharacterSheetRender, CharacterSheetError>.Success(render);
    }
}
