using DandDTemplateParserCSharp.Controllers;
using DandDTemplateParserCSharp.Domain;

namespace DandDTemplateParserCSharp.Repositories;

public interface ICharacterSheetRepository
{
    Task<Result<CharacterSheetRender, CharacterSheetError.DatabaseError>>                 SaveAsync(CharacterSheetRender render, CancellationToken ct = default);
    Task<Result<CharacterSheetRender?, CharacterSheetError.DatabaseError>>                GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<CharacterSheetSummary>, CharacterSheetError.DatabaseError>> GetByLevelAsync(int level, PageRequest page, CancellationToken ct = default);
    Task<Result<IReadOnlyList<CharacterSheetSummary>, CharacterSheetError.DatabaseError>> GetBySheetTypeAsync(string sheetType, PageRequest page, CancellationToken ct = default);
}
