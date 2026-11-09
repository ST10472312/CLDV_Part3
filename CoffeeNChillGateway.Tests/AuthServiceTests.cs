// ST10472017
// xUnit.net, 2026. Theory data and InlineData. [Online]
// Available at: https://xunit.net/docs/getting-started/v2/netfx/visual-studio
// [Accessed 9 November 2026].
// Microsoft, 2026. Unit testing C# with xUnit. [Online]
// Available at: https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-with-dotnet-test
// [Accessed 9 November 2026].

using CoffeeNChillGateway.Auth;
using CoffeeNChillGateway.Dtos;
using CoffeeNChillGateway.Entities;
using CoffeeNChillGateway.Exceptions;
using CoffeeNChillGateway.Options;

namespace CoffeeNChillGateway.Tests;

public class AuthServiceTests
{
    /// <summary>In-memory stand-in for the Users table, so these tests need no Azurite.</summary>
    private sealed class FakeUserRepository : IUserRepository
    {
        public readonly Dictionary<string, UserEntity> Store = new();

        public Task<UserEntity?> FindByEmailAsync(string email, CancellationToken ct)
            => Task.FromResult(Store.GetValueOrDefault(email.Trim().ToLowerInvariant()));

        public Task AddAsync(UserEntity user, CancellationToken ct)
        {
            Store[user.RowKey] = user;
            return Task.CompletedTask;
        }
    }

    private static (AuthService Service, FakeUserRepository Repo) Create()
    {
        var repo = new FakeUserRepository();
        var jwt = new JwtTokenService(Microsoft.Extensions.Options.Options.Create(new JwtOptions
        {
            Issuer = "i", Audience = "a", Key = new string('k', 40), ExpiryMinutes = 5
        }));
        return (new AuthService(repo, new BCryptPasswordService(), jwt), repo);
    }

    private static RegisterRequestDto Dto(string email = "Jane@Campus.ac.za")
        => new() { Email = email, Password = "Student#2026", FullName = "Jane Smith" };

    [Fact]
    public async Task Register_StoresHashedPassword_LowercaseEmail_AndRoleAsPartitionKey()
    {
        var (svc, repo) = Create();
        var profile = await svc.RegisterAsync(Dto(), Roles.Customer, default);

        Assert.Equal("jane@campus.ac.za", profile.Email);
        var stored = repo.Store["jane@campus.ac.za"];
        Assert.Equal("Customer", stored.PartitionKey);
        Assert.NotEqual("Student#2026", stored.PasswordHash);
    }

    [Fact]
    public async Task Register_Twice_ThrowsConflict()
    {
        var (svc, _) = Create();
        await svc.RegisterAsync(Dto(), Roles.Customer, default);
        await Assert.ThrowsAsync<ConflictException>(() => svc.RegisterAsync(Dto("jane@campus.ac.za"), Roles.Customer, default));
    }

    [Fact]
    public async Task Login_WithCorrectPassword_ReturnsTokenAndRole()
    {
        var (svc, _) = Create();
        await svc.RegisterAsync(Dto(), Roles.Staff, default);

        var result = await svc.LoginAsync(new LoginRequestDto { Email = "jane@campus.ac.za", Password = "Student#2026" }, default);

        Assert.Equal("Staff", result.Role);
        Assert.Equal(3, result.Token.Split('.').Length);
    }

    [Theory]
    [InlineData("jane@campus.ac.za", "WrongPassword")]
    [InlineData("nobody@campus.ac.za", "Student#2026")]
    public async Task Login_WithBadCredentials_ThrowsUnauthorized(string email, string password)
    {
        var (svc, _) = Create();
        await svc.RegisterAsync(Dto(), Roles.Customer, default);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => svc.LoginAsync(new LoginRequestDto { Email = email, Password = password }, default));
    }

    [Fact]
    public async Task Register_WithUnknownRole_ThrowsBadRequest()
    {
        var (svc, _) = Create();
        await Assert.ThrowsAsync<BadRequestException>(() => svc.RegisterAsync(Dto(), "SuperUser", default));
    }
}
