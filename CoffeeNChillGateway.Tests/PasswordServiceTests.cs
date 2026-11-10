// ST10472017
// xUnit.net, 2026. Getting started with xUnit.net. [Online]
// Available at: https://xunit.net/docs/getting-started/v2/netfx/visual-studio
// [Accessed 9 November 2026].

using CoffeeNChillGateway.Auth;

namespace CoffeeNChillGateway.Tests;

public class PasswordServiceTests
{
    private readonly BCryptPasswordService _svc = new();

    [Fact]
    public void Hash_IsNotThePlainPassword()
    {
        var hash = _svc.Hash("Student#2026");
        Assert.NotEqual("Student#2026", hash);
        Assert.StartsWith("$2", hash);
    }

    [Fact]
    public void Verify_AcceptsCorrect_RejectsWrong()
    {
        var hash = _svc.Hash("Student#2026");
        Assert.True(_svc.Verify("Student#2026", hash));
        Assert.False(_svc.Verify("student#2026", hash));
    }

    [Fact]
    public void SamePassword_GivesDifferentHashes_BecauseOfSalt()
        => Assert.NotEqual(_svc.Hash("Student#2026"), _svc.Hash("Student#2026"));

    [Fact]
    public void Verify_WithCorruptHash_ReturnsFalse_InsteadOfThrowing()
        => Assert.False(_svc.Verify("anything", "not-a-bcrypt-hash"));
}
