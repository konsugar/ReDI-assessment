using System.ComponentModel.DataAnnotations;

namespace ReDI.Assessment.Api.DTOs;

public class AdminLoginRequest
{
    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Password { get; set; } = string.Empty;
}

public class AdminLoginResponse
{
    public string AccessToken { get; set; } = string.Empty;

    public string TokenType { get; set; } = "Bearer";

    public DateTime ExpiresUtc { get; set; }

    public string Username { get; set; } = string.Empty;
}