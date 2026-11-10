// ST10472017
// Microsoft, 2026. JwtSecurityTokenHandler.ValidateToken Method. [Online]
// Available at: https://learn.microsoft.com/en-us/dotnet/api/system.identitymodel.tokens.jwt.jwtsecuritytokenhandler.validatetoken
// [Accessed 9 November 2026].

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CoffeeNChillGateway.Auth;
using CoffeeNChillGateway.Entities;
using CoffeeNChillGateway.Options;
using Microsoft.IdentityModel.Tokens;

namespace CoffeeNChillGateway.Tests;

public class JwtTokenServiceTests
{
    private static readonly JwtOptions Settings = new()
    {
        Issuer = "test-issuer",
        Audience = "test-audience",
        Key = "unit-test-key-that-is-definitely-longer-than-32-chars",
        ExpiryMinutes = 5
    };

    // Fully qualified: "Options" alone would resolve to our own CoffeeNChillGateway.Options namespace.
    private static JwtTokenService CreateService() =>
        new(Microsoft.Extensions.Options.Options.Create(Settings));

    private static UserEntity Staff() => new()
    {
        PartitionKey = Roles.Staff,
        RowKey = "barista@coffeenchill.ac.za",
        FullName = "Bea Barista"
    };

    private static TokenValidationParameters Validation(string key) => new()
    {
        ValidIssuer = Settings.Issuer,
        ValidAudience = Settings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
        RoleClaimType = ClaimTypes.Role,
        NameClaimType = ClaimTypes.Name
    };

    [Fact]
    public void Token_ContainsNameEmailRole_AndFutureExpiry()
    {
        var (token, expires) = CreateService().CreateToken(Staff());
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.Role && c.Value == "Staff");
        Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.Email && c.Value == "barista@coffeenchill.ac.za");
        Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.Name && c.Value == "Bea Barista");
        Assert.True(expires > DateTime.UtcNow);
        Assert.True(jwt.ValidTo > DateTime.UtcNow);
    }

    [Fact]
    public void Token_ValidatesWithTheRightKey_AndGivesStaffRole()
    {
        var (token, _) = CreateService().CreateToken(Staff());
        var principal = new JwtSecurityTokenHandler().ValidateToken(token, Validation(Settings.Key), out _);
        Assert.True(principal.IsInRole("Staff"));
        Assert.False(principal.IsInRole("Admin"));
    }

    [Fact]
    public void Token_IsRejected_WithTheWrongKey()
    {
        var (token, _) = CreateService().CreateToken(Staff());
        Assert.ThrowsAny<SecurityTokenException>(() =>
            new JwtSecurityTokenHandler().ValidateToken(token, Validation("a-completely-different-key-32-chars-minimum!!"), out _));
    }
}
