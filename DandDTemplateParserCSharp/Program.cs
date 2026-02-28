using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using DandDTemplateParserCSharp.Middleware;
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
    builder.Services.AddSwaggerGen(options =>
    {
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            In = ParameterLocation.Header,
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Enter your JWT token. Obtain it via POST /api/v1/auth/token."
        });
        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                },
                Array.Empty<string>()
            }
        });
    });

    // ── ProblemDetails (RFC 7807) ──────────────────────────────
    builder.Services.AddProblemDetails();

    // ── Options ────────────────────────────────────────────────
    builder.Services
        .AddOptions<DatabaseOptions>()
        .BindConfiguration(DatabaseOptions.Section)
        .ValidateDataAnnotations()
        .ValidateOnStart();

    builder.Services
        .AddOptions<JwtOptions>()
        .BindConfiguration(JwtOptions.Section)
        .ValidateDataAnnotations()
        .ValidateOnStart();

    // ── Authentication ───────────────────────────────────────
    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer();

    builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
        .Configure<IOptions<JwtOptions>>((bearerOptions, jwtOpts) =>
        {
            var jwt = jwtOpts.Value;
            bearerOptions.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwt.Issuer,
                ValidAudience = jwt.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwt.SigningKey))
            };
        });

    // ── FluentValidation ───────────────────────────────────────
    builder.Services.AddValidatorsFromAssemblyContaining<Program>();

    // ── Application services ───────────────────────────────────
    builder.Services.AddScoped<ICharacterSheetRepository, CharacterSheetRepository>();
    builder.Services.AddScoped<ICharacterSheetService, CharacterSheetService>();

    // ── Health checks ──────────────────────────────────────────
    builder.Services.AddHealthChecks();

    var app = builder.Build();

    // ── Middleware pipeline ────────────────────────────────────
    app.UseMiddleware<ExceptionMiddleware>();  // Catches unhandled exceptions → 500 ProblemDetails
    app.UseStatusCodePages();        // Maps 404/405 to ProblemDetails

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseSerilogRequestLogging();
    app.UseHttpsRedirection();
    app.UseAuthentication();
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
