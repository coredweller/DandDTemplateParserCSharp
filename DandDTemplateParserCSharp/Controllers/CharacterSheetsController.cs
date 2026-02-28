using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using DandDTemplateParserCSharp.Domain;
using DandDTemplateParserCSharp.Services;

namespace DandDTemplateParserCSharp.Controllers;

[Authorize]
[EnableRateLimiting("authenticated")]
[ApiController]
[Route("api/v1/sheets")]
public sealed class CharacterSheetsController(ICharacterSheetService service) : ControllerBase
{
    /// <summary>Render a general character sheet and persist it.</summary>
    [HttpPost("general")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RenderGeneral([FromBody] GeneralSheetRequest request, CancellationToken ct)
    {
        var result = await service.RenderGeneralAsync(request, ct);
        return result.Match(
            onSuccess: render => HtmlCreated(render),
            onFailure: MapError);
    }

    /// <summary>Render a legendary character sheet and persist it.</summary>
    [HttpPost("legendary")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RenderLegendary([FromBody] LegendarySheetRequest request, CancellationToken ct)
    {
        var result = await service.RenderLegendaryAsync(request, ct);
        return result.Match(
            onSuccess: render => HtmlCreated(render),
            onFailure: MapError);
    }

    /// <summary>Retrieve a previously rendered character sheet by ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await service.GetByIdAsync(id, ct);
        return result.Match(
            onSuccess: render => Content(render.ResponseHtml, "text/html"),
            onFailure: MapError);
    }

    /// <summary>Find all renders at a given level (1–20). Supports pagination via ?page=1&amp;pageSize=25 (max 100).</summary>
    [HttpGet("by-level/{level:int}")]
    [ProducesResponseType<IReadOnlyList<CharacterSheetSummary>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetByLevel(
        int level, [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default)
    {
        var result = await service.GetByLevelAsync(level, page, pageSize, ct);
        return result.Match(onSuccess: Ok, onFailure: MapError);
    }

    /// <summary>Find all renders of a given sheet type ("general" or "legendary"). Supports pagination via ?page=1&amp;pageSize=25 (max 100).</summary>
    [HttpGet("by-type/{sheetType}")]
    [ProducesResponseType<IReadOnlyList<CharacterSheetSummary>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetBySheetType(
        string sheetType, [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default)
    {
        var result = await service.GetBySheetTypeAsync(sheetType, page, pageSize, ct);
        return result.Match(onSuccess: Ok, onFailure: MapError);
    }

    // ── Result → HTTP mapping ─────────────────────────────────────────────────

    private ContentResult HtmlCreated(CharacterSheetRender render)
    {
        var location = Url.Action(nameof(GetById), new { id = render.Id })!;
        Response.Headers.Location = location;
        return new ContentResult
        {
            Content     = render.ResponseHtml,
            ContentType = "text/html; charset=utf-8",
            StatusCode  = StatusCodes.Status201Created
        };
    }

    private IActionResult MapError(CharacterSheetError error) => error switch
    {
        CharacterSheetError.NotFound e      => NotFound(ProblemFor(404, $"Sheet {e.Id} not found.")),
        CharacterSheetError.ValidationError e => BadRequest(ProblemFor(400, e.Message)),
        CharacterSheetError.DatabaseError e => StatusCode(500, ProblemFor(500, e.Message)),
        _                                   => StatusCode(500, ProblemFor(500, "Unexpected error."))
    };

    private ProblemDetails ProblemFor(int status, string detail) => new()
    {
        Status   = status,
        Detail   = detail,
        Instance = HttpContext.Request.Path
    };
}
