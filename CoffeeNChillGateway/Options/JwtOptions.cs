// ST10472017
// Microsoft, 2026. Options pattern in ASP.NET Core. [Online]
// Available at: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options
// [Accessed 9 November 2026].

namespace CoffeeNChillGateway.Options;

/// <summary>Settings for signing and validating tokens (config section "Jwt").</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "CoffeeNChill.Gateway";
    public string Audience { get; set; } = "CoffeeNChill.Clients";

    /// <summary>Signing secret. At least 32 characters. Never commit the production value.</summary>
    public string Key { get; set; } = "";

    public int ExpiryMinutes { get; set; } = 60;
}

/// <summary>Optional first Admin account created on startup if it does not exist yet.</summary>
public sealed class SeedAdminOptions
{
    public const string SectionName = "SeedAdmin";

    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string FullName { get; set; } = "Canteen Admin";
}
