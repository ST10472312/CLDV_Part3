// ST10472017
// BCrypt.Net, 2026. BCrypt.Net-Next. [Online]
// Available at: https://github.com/BcryptNet/bcrypt.net
// [Accessed 9 November 2026].
// OWASP, 2026. Password Storage Cheat Sheet. [Online]
// Available at: https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html
// [Accessed 9 November 2026].

namespace CoffeeNChillGateway.Auth;

public interface IPasswordService
{
    /// <summary>Returns a salted BCrypt hash of the password.</summary>
    string Hash(string password);

    /// <summary>Returns true when the password matches the stored hash.</summary>
    bool Verify(string password, string hash);
}

/// <summary>BCrypt hashing with a work factor of 11 (about 100 ms per hash on a typical VPS).</summary>
public sealed class BCryptPasswordService : IPasswordService
{
    private const int WorkFactor = 11;

    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public bool Verify(string password, string hash)
    {
        try { return BCrypt.Net.BCrypt.Verify(password, hash); }
        catch (Exception ex) when (ex is BCrypt.Net.SaltParseException or ArgumentException)
        {
            return false;
        }
    }
}
