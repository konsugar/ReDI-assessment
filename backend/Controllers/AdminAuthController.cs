using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ReDI.Assessment.Api.DTOs;
using ReDI.Assessment.Api.Settings;

namespace ReDI.Assessment.Api.Controllers;

[ApiController]
[Route("api/admin/auth")]
public class AdminAuthController : ControllerBase
{
    private readonly AdminAuthSettings _adminAuthSettings;

    public AdminAuthController(IOptions<AdminAuthSettings> adminAuthSettings)
    {
        _adminAuthSettings = adminAuthSettings.Value;
    }

    [HttpPost("login")]
    public ActionResult<AdminLoginResponse> Login(AdminLoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(_adminAuthSettings.Username) ||
            string.IsNullOrWhiteSpace(_adminAuthSettings.Password) ||
            string.IsNullOrWhiteSpace(_adminAuthSettings.JwtSecret) ||
            string.IsNullOrWhiteSpace(_adminAuthSettings.Issuer) ||
            string.IsNullOrWhiteSpace(_adminAuthSettings.Audience))
        {
            return StatusCode(500, "Admin authentication is not configured.");
        }

        if (request.Username != _adminAuthSettings.Username ||
            request.Password != _adminAuthSettings.Password)
        {
            return Unauthorized("Invalid username or password.");
        }

        var expiresUtc = DateTime.UtcNow.AddMinutes(_adminAuthSettings.TokenLifetimeMinutes);

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, _adminAuthSettings.Username),
            new(ClaimTypes.Role, "Admin")
        };

        var signingKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_adminAuthSettings.JwtSecret)
        );

        var credentials = new SigningCredentials(
            signingKey,
            SecurityAlgorithms.HmacSha256
        );

        var token = new JwtSecurityToken(
            issuer: _adminAuthSettings.Issuer,
            audience: _adminAuthSettings.Audience,
            claims: claims,
            expires: expiresUtc,
            signingCredentials: credentials
        );

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        return Ok(new AdminLoginResponse
        {
            AccessToken = accessToken,
            ExpiresUtc = expiresUtc,
            Username = _adminAuthSettings.Username
        });
    }
}