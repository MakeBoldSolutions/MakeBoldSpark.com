using System.ComponentModel.DataAnnotations;

namespace MakeBoldSpark.Api.Features.Auth;

public class LoginRequest
{
    [Required]
    public required string Email { get; set; }

    /// <summary>Capped at 256 characters; rejected before any password-hash verification is attempted (see research.md).</summary>
    [Required]
    [StringLength(256)]
    public required string Password { get; set; }
}

public class LoginResponse
{
    public required string AccessToken { get; set; }
    public required DateTimeOffset ExpiresAt { get; set; }
    public required string DisplayName { get; set; }
}

/// <summary>The single, generic failure body returned for every rejection reason — unknown email, wrong password, non-admin account, or throttling (FR-001a).</summary>
public class LoginErrorResponse
{
    public string Message { get; set; } = "Invalid email or password.";
}
