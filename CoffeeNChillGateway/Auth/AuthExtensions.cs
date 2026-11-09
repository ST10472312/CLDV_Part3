// ST10472017
// Microsoft, 2026. Configure JWT bearer authentication in ASP.NET Core. [Online]
// Available at: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication
// [Accessed 9 November 2026].
// Microsoft, 2026. TokenValidationParameters Class. [Online]
// Available at: https://learn.microsoft.com/en-us/dotnet/api/microsoft.identitymodel.tokens.tokenvalidationparameters
// [Accessed 9 November 2026].

using System.Security.Claims;
using System.Text;
using CoffeeNChillGateway.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace CoffeeNChillGateway.Auth;

public static class AuthExtensions
{
    /// <summary>Registers password hashing, token creation, JWT bearer validation and the admin seeder.</summary>
    public static IServiceCollection AddCoffeeAuth(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<JwtOptions>(config.GetSection(JwtOptions.SectionName));
        services.Configure<SeedAdminOptions>(config.GetSection(SeedAdminOptions.SectionName));

        var jwt = config.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        if (string.IsNullOrWhiteSpace(jwt.Key) || jwt.Key.Length < 32)
            throw new InvalidOperationException("Jwt:Key must be set and at least 32 characters long.");

        services.AddSingleton<IPasswordService, BCryptPasswordService>();
        services.AddSingleton<IUserRepository, UserRepository>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IAuthService, AuthService>();
        services.AddHostedService<AdminSeeder>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    RoleClaimType = ClaimTypes.Role,
                    NameClaimType = ClaimTypes.Name
                };
            });

        services.AddAuthorization();
        return services;
    }
}
