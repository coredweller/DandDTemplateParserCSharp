using DandDTemplateParserCSharp.Controllers;
using DandDTemplateParserCSharp.Domain;

namespace DandDTemplateParserCSharp.Repositories;

public interface ICharacterSheetRepository
{
    Task<CharacterSheetRender>              SaveAsync(CharacterSheetRender render, CancellationToken ct = default);
    Task<CharacterSheetRender?>             GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<CharacterSheetSummary>> GetByLevelAsync(int level, CancellationToken ct = default);
    Task<IReadOnlyList<CharacterSheetSummary>> GetBySheetTypeAsync(string sheetType, CancellationToken ct = default);
}
