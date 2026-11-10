// ST10472017
// OWASP, 2026. Authentication Cheat Sheet - incorrect and correct response discrepancy. [Online]
// Available at: https://cheatsheetseries.owasp.org/cheatsheets/Authentication_Cheat_Sheet.html
// [Accessed 9 November 2026].
// Microsoft, 2026. Secure authentication flows in ASP.NET Core. [Online]
// Available at: https://learn.microsoft.com/en-us/aspnet/core/security/
// [Accessed 9 November 2026].

using CoffeeNChillGateway.Dtos;
using CoffeeNChillGateway.Entities;
using CoffeeNChillGateway.Exceptions;

namespace CoffeeNChillGateway.Auth;

public interface IAuthService
{
    /// <summary>Creates an account in the given role and returns its safe profile.</summary>
    Task<UserProfileDto> RegisterAsync(RegisterRequestDto dto, string role, CancellationToken ct);

    /// <summary>Checks the credentials and returns a signed token.</summary>
    Task<AuthResponseDto> LoginAsync(LoginRequestDto dto, CancellationToken ct);
}

/// <summary>Registration and login. Kept out of the controller so it can be unit-tested.</summary>
public sealed class AuthService(IUserRepository users, IPasswordService passwords, IJwtTokenService tokens) : IAuthService
{
    public async Task<UserProfileDto> RegisterAsync(RegisterRequestDto dto, string role, CancellationToken ct)
    {
        if (!Roles.All.Contains(role)) throw new BadRequestException("Unknown role.");

        var email = dto.Email.Trim().ToLowerInvariant();
        if (await users.FindByEmailAsync(email, ct) is not null)
            throw new ConflictException("An account with that email already exists.");

        var user = new UserEntity
        {
            PartitionKey = role,
            RowKey = email,
            PasswordHash = passwords.Hash(dto.Password),
            FullName = dto.FullName.Trim(),
            CreatedDate = DateTime.UtcNow
        };
        await users.AddAsync(user, ct);

        return new UserProfileDto { Email = email, FullName = user.FullName, Role = role, CreatedDate = user.CreatedDate };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto dto, CancellationToken ct)
    {
        var user = await users.FindByEmailAsync(dto.Email.Trim().ToLowerInvariant(), ct);

        // One message for both failures, so an attacker cannot discover which emails exist.
        if (user is null || !passwords.Verify(dto.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        var (token, expires) = tokens.CreateToken(user);
        return new AuthResponseDto
        {
            Token = token,
            ExpiresAtUtc = expires,
            Email = user.RowKey,
            FullName = user.FullName,
            Role = user.PartitionKey
        };
    }
}
