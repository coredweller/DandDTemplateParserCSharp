using FluentValidation;
using DandDTemplateParserCSharp.Options;
using DandDTemplateParserCSharp.Repositories;
using DandDTemplateParserCSharp.Services;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ── Logging ────────────────────────────────────────────────
    builder.Host.UseSerilog((ctx, services, config) =>
        config.ReadFrom.Configuration(ctx.Configuration)
              .ReadFrom.Services(services)
              .Enrich.FromLogContext());

    // ── Controllers + API explorer ─────────────────────────────
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // ── ProblemDetails (RFC 7807) ──────────────────────────────
    builder.Services.AddProblemDetails();

    // ── Options ────────────────────────────────────────────────
    builder.Services
        .AddOptions<DatabaseOptions>()
        .BindConfiguration(DatabaseOptions.Section)
        .ValidateDataAnnotations()
        .ValidateOnStart();

    // ── FluentValidation ───────────────────────────────────────
    builder.Services.AddValidatorsFromAssemblyContaining<Program>();

    // ── Application services ───────────────────────────────────
    builder.Services.AddScoped<ITaskRepository, DapperTaskRepository>();
    builder.Services.AddScoped<ITaskService, TaskService>();
    builder.Services.AddScoped<ICharacterSheetRepository, CharacterSheetRepository>();
    builder.Services.AddScoped<ICharacterSheetService, CharacterSheetService>();

    // ── Health checks ──────────────────────────────────────────
    builder.Services.AddHealthChecks();

    var app = builder.Build();

    // ── Middleware pipeline ────────────────────────────────────
    app.UseExceptionHandler();       // Maps unhandled exceptions to ProblemDetails
    app.UseStatusCodePages();        // Maps 404/405 to ProblemDetails

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseSerilogRequestLogging();
    app.UseHttpsRedirection();
    app.UseAuthorization();
    app.MapControllers();
    app.MapHealthChecks("/api/v1/health");

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application failed to start");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

return 0;

public partial class Program { }
