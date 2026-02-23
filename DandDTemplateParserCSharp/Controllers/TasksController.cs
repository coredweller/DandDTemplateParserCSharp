using Microsoft.AspNetCore.Mvc;
using DandDTemplateParserCSharp.Domain;
using DandDTemplateParserCSharp.Services;

namespace DandDTemplateParserCSharp.Controllers;

[ApiController]
[Route("api/v1/tasks")]
[Produces("application/json")]
public sealed class TasksController(ITaskService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<Domain.Task>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken ct) =>
        Ok(await service.ListAllAsync(ct));

    [HttpGet("{id:guid}")]
    [ProducesResponseType<Domain.Task>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await service.GetByIdAsync(TaskId.From(id), ct);
        return MapResult(result);
    }

    [HttpPost]
    [ProducesResponseType<Domain.Task>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateTaskRequest request, CancellationToken ct)
    {
        var result = await service.CreateAsync(request.Title, ct);
        return result.Match(
            onSuccess: task => CreatedAtAction(nameof(GetById), new { id = task.Id.Value }, task),
            onFailure: MapError);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await service.DeleteAsync(TaskId.From(id), ct);
        return result.Match(
            onSuccess: _ => (IActionResult)NoContent(),
            onFailure: MapError);
    }

    // ── Result → HTTP mapping ───────────────────────────────────────────────
    private IActionResult MapResult<T>(Result<T> result) =>
        result.Match(onSuccess: value => Ok(value), onFailure: MapError);

    private IActionResult MapError(TaskError error) => error switch
    {
        TaskError.NotFound e        => NotFound(ProblemFor(404, $"Task {e.Id} not found.")),
        TaskError.ValidationError e => BadRequest(ProblemFor(400, e.Message)),
        TaskError.Conflict e        => Conflict(ProblemFor(409, e.Message)),
        _                           => StatusCode(500, ProblemFor(500, "Unexpected error."))
    };

    private ProblemDetails ProblemFor(int status, string detail) => new()
    {
        Status   = status,
        Detail   = detail,
        Instance = HttpContext.Request.Path
    };
}

// ── Request DTO ──────────────────────────────────────────────────────────────
public sealed record CreateTaskRequest(string Title);
