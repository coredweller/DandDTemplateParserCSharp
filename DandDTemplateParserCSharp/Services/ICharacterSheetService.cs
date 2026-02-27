using DandDTemplateParserCSharp.Controllers;
using DandDTemplateParserCSharp.Domain;

namespace DandDTemplateParserCSharp.Services;

public interface ICharacterSheetService
{
    Task<Result<CharacterSheetRender, CharacterSheetError>> RenderGeneralAsync(
        GeneralSheetRequest request, CancellationToken ct = default);

    Task<Result<CharacterSheetRender, CharacterSheetError>> RenderLegendaryAsync(
        LegendarySheetRequest request, CancellationToken ct = default);

    Task<Result<CharacterSheetRender, CharacterSheetError>> GetByIdAsync(
        Guid id, CancellationToken ct = default);

    Task<Result<IReadOnlyList<CharacterSheetSummary>, CharacterSheetError>> GetByLevelAsync(
        int level, CancellationToken ct = default);

    Task<Result<IReadOnlyList<CharacterSheetSummary>, CharacterSheetError>> GetBySheetTypeAsync(
        string sheetType, CancellationToken ct = default);
}
