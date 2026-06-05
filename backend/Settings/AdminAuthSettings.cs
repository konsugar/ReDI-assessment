namespace ReDI.Assessment.Api.Settings;

public class AdminAuthSettings
{
    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string JwtSecret { get; set; } = string.Empty;

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public int TokenLifetimeMinutes { get; set; } = 120;
}