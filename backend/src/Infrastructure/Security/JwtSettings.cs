using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Security;

public class JwtSettings
{
    [Required]
    public string Secret { get; set; } = string.Empty;

    [Required]
    public int ExpiryMinutes { get; set; }

    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;
}
