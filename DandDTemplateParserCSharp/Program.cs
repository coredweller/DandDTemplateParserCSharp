using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
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

    // ── CORS ────────────────────────────────────────────────────
    builder.Services.AddCors(options =>
    {
        var origins = builder.Configuration
            .GetSection(CorsOptions.Section)
            .Get<CorsOptions>()?.AllowedOrigins ?? [];

        options.AddPolicy("ApiCors", policy =>
        {
            if (origins.Length > 0)
                policy.WithOrigins(origins)
                      .WithHeaders("Authorization", "Content-Type")
                      .WithMethods("GET", "POST");
            else
                policy.SetIsOriginAllowed(_ => false); // deny all cross-origin by default
        });
    });

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

    // ── Rate limiting ──────────────────────────────────────────
    builder.Services
        .AddOptions<RateLimitingOptions>()
        .BindConfiguration(RateLimitingOptions.Section)
        .ValidateDataAnnotations()
        .ValidateOnStart();

    builder.Services.AddRateLimiter(options =>
    {
        // Read limits from config at startup — values are static and don't change at runtime.
        var rl = builder.Configuration
            .GetSection(RateLimitingOptions.Section)
            .Get<RateLimitingOptions>() ?? new RateLimitingOptions();

        options.OnRejected = async (context, ct) =>
        {
            context.HttpContext.Response.StatusCode  = StatusCodes.Status429TooManyRequests;
            context.HttpContext.Response.ContentType = "application/problem+json";

            context.HttpContext.Response.Headers.RetryAfter =
                context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
                    ? ((int)retryAfter.TotalSeconds).ToString()
                    : "1"; // token bucket replenishes continuously; 1s is a safe minimum hint

            var logger = context.HttpContext.RequestServices
                .GetRequiredService<ILogger<Program>>();
            logger.LogWarning("Rate limit exceeded for {Path} from {RemoteIp}",
                context.HttpContext.Request.Path,
                context.HttpContext.Connection.RemoteIpAddress);

            await context.HttpContext.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status   = StatusCodes.Status429TooManyRequests,
                Detail   = "Too many requests. Please slow down.",
                Instance = context.HttpContext.Request.Path
            }, ct);
        };

        // Fixed-window limit on the unauthenticated token endpoint, keyed by client IP.
        options.AddPolicy("token", httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = rl.TokenPermitLimit,
                    Window      = TimeSpan.FromMinutes(rl.TokenWindowMinutes),
                    QueueLimit  = 0
                }));

        // Token bucket for authenticated endpoints, keyed by JWT sub claim.
        options.AddPolicy("authenticated", httpContext =>
            RateLimitPartition.GetTokenBucketLimiter(
                partitionKey: httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous",
                factory: _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit          = rl.AuthBucketCapacity,
                    ReplenishmentPeriod = TimeSpan.FromSeconds(1),
                    TokensPerPeriod     = rl.AuthReplenishPerSecond,
                    QueueLimit          = 0
                }));
    });

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

    // ── If deployed behind a load balancer / reverse proxy, uncomment this so
    //    RemoteIpAddress reflects the real client IP via X-Forwarded-For.
    //    Also configure KnownProxies/KnownNetworks in ForwardedHeadersOptions.
    // app.UseForwardedHeaders();

    app.UseCors("ApiCors");    // Before auth so OPTIONS preflight bypasses authentication
    app.UseAuthentication();
    app.UseRateLimiter();      // After UseAuthentication so the "authenticated" policy can read HttpContext.User
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
