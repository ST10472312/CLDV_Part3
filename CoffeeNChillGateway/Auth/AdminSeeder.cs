// ST10472017
// Microsoft, 2026. Background tasks with hosted services in ASP.NET Core. [Online]
// Available at: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services
// [Accessed 9 November 2026].

using CoffeeNChillGateway.Dtos;
using CoffeeNChillGateway.Options;
using Microsoft.Extensions.Options;

namespace CoffeeNChillGateway.Auth;

/// <summary>
/// Creates the first Admin from configuration if it does not exist. Without this nobody could
/// create Staff accounts, because public registration only makes Customers.
/// </summary>
public sealed class AdminSeeder(
    IAuthService auth,
    IUserRepository users,
    IOptions<SeedAdminOptions> options,
    ILogger<AdminSeeder> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        var seed = options.Value;
        if (string.IsNullOrWhiteSpace(seed.Email) || string.IsNullOrWhiteSpace(seed.Password))
        {
            logger.LogWarning("SeedAdmin is not configured; no Admin account was created.");
            return;
        }

        if (await users.FindByEmailAsync(seed.Email, ct) is not null) return;

        await auth.RegisterAsync(
            new RegisterRequestDto { Email = seed.Email, Password = seed.Password, FullName = seed.FullName },
            Roles.Admin, ct);

        logger.LogInformation("Seeded Admin account {Email}.", seed.Email);
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
