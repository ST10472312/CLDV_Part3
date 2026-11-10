// ST10472017
// Microsoft, 2026. JwtSecurityTokenHandler Class. [Online]
// Available at: https://learn.microsoft.com/en-us/dotnet/api/system.identitymodel.tokens.jwt.jwtsecuritytokenhandler
// [Accessed 9 November 2026].
// Microsoft, 2026. ClaimTypes Class. [Online]
// Available at: https://learn.microsoft.com/en-us/dotnet/api/system.security.claims.claimtypes
// [Accessed 9 November 2026].

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CoffeeNChillGateway.Entities;
using CoffeeNChillGateway.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CoffeeNChillGateway.Auth;

public interface IJwtTokenService
{
    /// <summary>Builds a signed token for the user and returns it with its expiry.</summary>
    (string Token, DateTime ExpiresUtc) CreateToken(UserEntity user);
}

/// <summary>Creates signed HS256 tokens containing Name, Email and Role claims and an expiry.</summary>
public sealed class JwtTokenService(IOptions<JwtOptions> options) : IJwtTokenService
{
    private readonly JwtOptions _jwt = options.Value;

    public (string Token, DateTime ExpiresUtc) CreateToken(UserEntity user)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.RowKey),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.RowKey),
            new(ClaimTypes.Role, user.PartitionKey)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(_jwt.ExpiryMinutes);

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
