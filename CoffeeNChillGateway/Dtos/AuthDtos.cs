// ST10472017
// Microsoft, 2026. System.ComponentModel.DataAnnotations Namespace. [Online]
// Available at: https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations
// [Accessed 9 November 2026].
// Microsoft, 2026. Model validation in ASP.NET Core MVC. [Online]
// Available at: https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation
// [Accessed 9 November 2026].

using System.ComponentModel.DataAnnotations;

namespace CoffeeNChillGateway.Dtos;

/// <summary>Public self-registration. Always creates a Customer; there is deliberately no Role field.</summary>
public class RegisterRequestDto
{
    /// <example>jane.smith@campus.ac.za</example>
    [Required, EmailAddress, StringLength(120)]
    public string Email { get; set; } = "";

    /// <summary>At least 8 characters with an upper-case letter, a lower-case letter and a digit.</summary>
    /// <example>Student#2026</example>
    [Required, RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,100}$",
        ErrorMessage = "Password must be 8 to 100 characters with upper-case, lower-case and a digit.")]
    public string Password { get; set; } = "";

    /// <example>Jane Smith</example>
    [Required, StringLength(100, MinimumLength = 2)]
    public string FullName { get; set; } = "";
}

/// <summary>Admin-only body for creating Staff or Admin accounts.</summary>
public sealed class CreateStaffRequestDto : RegisterRequestDto
{
    /// <summary>Staff or Admin.</summary>
    /// <example>Staff</example>
    [Required, RegularExpression("^(Staff|Admin)$", ErrorMessage = "Role must be Staff or Admin.")]
    public string Role { get; set; } = "Staff";
}

/// <summary>Credentials sent to the login endpoint.</summary>
public sealed class LoginRequestDto
{
    /// <example>barista@coffeenchill.ac.za</example>
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required] public string Password { get; set; } = "";
}

/// <summary>What the client receives after a successful login.</summary>
public sealed class AuthResponseDto
{
    /// <summary>Send as header: Authorization: Bearer {token}</summary>
    public string Token { get; set; } = "";
    public DateTime ExpiresAtUtc { get; set; }
    public string Email { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Role { get; set; } = "";
}

/// <summary>Safe view of a user. Never includes the hash.</summary>
public sealed class UserProfileDto
{
    public string Email { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Role { get; set; } = "";
    public DateTime CreatedDate { get; set; }
}
