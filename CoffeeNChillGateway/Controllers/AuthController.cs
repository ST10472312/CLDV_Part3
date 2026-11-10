// ST10472017
// Microsoft, 2026. Create web APIs with ASP.NET Core. [Online]
// Available at: https://learn.microsoft.com/en-us/aspnet/core/web-api/
// [Accessed 9 November 2026].
// Microsoft, 2026. Simple authorization in ASP.NET Core. [Online]
// Available at: https://learn.microsoft.com/en-us/aspnet/core/security/authorization/simple
// [Accessed 9 November 2026].

using System.Security.Claims;
using CoffeeNChillGateway.Auth;
using CoffeeNChillGateway.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeNChillGateway.Controllers;

/// <summary>Registration, login and account management.</summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public sealed class AuthController(IAuthService auth) : ControllerBase
{
    /// <summary>Registers a new Customer account.</summary>
    /// <remarks>The password is hashed with BCrypt before it is stored. A role cannot be chosen here.</remarks>
    /// <response code="201">Account created.</response>
    /// <response code="400">Validation failed (bad email or weak password).</response>
    /// <response code="409">Email already registered.</response>
    [HttpPost("register"), AllowAnonymous]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserProfileDto>> Register([FromBody] RegisterRequestDto dto, CancellationToken ct)
    {
        var profile = await auth.RegisterAsync(dto, Roles.Customer, ct);
        return Created("/api/auth/me", profile);
    }

    /// <summary>Logs in and returns a signed JWT bearer token.</summary>
    /// <response code="200">Token plus its expiry.</response>
    /// <response code="401">Wrong email or password.</response>
    [HttpPost("login"), AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginRequestDto dto, CancellationToken ct)
        => Ok(await auth.LoginAsync(dto, ct));

    /// <summary>Creates a Staff or Admin account (Admin only).</summary>
    /// <response code="201">Account created.</response>
    /// <response code="403">Signed in, but not an Admin.</response>
    [HttpPost("staff"), Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserProfileDto>> CreateStaff([FromBody] CreateStaffRequestDto dto, CancellationToken ct)
    {
        var profile = await auth.RegisterAsync(dto, dto.Role, ct);
        return Created("/api/auth/me", profile);
    }

    /// <summary>Shows who the token says you are. Useful to check your claims.</summary>
    /// <response code="200">Name, email and role from the token.</response>
    [HttpGet("me"), Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Me() => Ok(new
    {
        name = User.Identity?.Name,
        email = User.FindFirstValue(ClaimTypes.Email),
        role = User.FindFirstValue(ClaimTypes.Role)
    });
}
