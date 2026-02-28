using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using DandDTemplateParserCSharp.Options;

namespace DandDTemplateParserCSharp.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(IOptions<JwtOptions> jwtOptions, ILogger<AuthController> logger) : ControllerBase
{
    [AllowAnonymous]
    [EnableRateLimiting("token")]
    [HttpPost("token")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TokenResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Token([FromBody] TokenRequest request)
    {
        var options = jwtOptions.Value;

        if (!string.Equals(request.ApiSecret, options.ApiSecret, StringComparison.Ordinal))
        {
            logger.LogWarning("Token request rejected: invalid API secret");
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Detail = "Invalid credentials.",
                Instance = HttpContext.Request.Path
            });
        }

        var keyBytes = Encoding.UTF8.GetBytes(options.SigningKey);
        var securityKey = new SymmetricSecurityKey(keyBytes);
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var now = DateTime.UtcNow;
        var expiration = now.AddMinutes(options.ExpirationMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, "api-user"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, new DateTimeOffset(now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            notBefore: now,
            expires: expiration,
            signingCredentials: credentials);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        var expiresInSeconds = (int)TimeSpan.FromMinutes(options.ExpirationMinutes).TotalSeconds;

        logger.LogInformation("JWT issued for api-user, expires in {ExpiresInSeconds}s", expiresInSeconds);

        return Ok(new TokenResponse(tokenString, expiresInSeconds));
    }
}

// ── Auth DTOs ────────────────────────────────────────────────────────────────

public sealed record TokenRequest(string ApiSecret);

public sealed record TokenResponse(string AccessToken, int ExpiresIn);
