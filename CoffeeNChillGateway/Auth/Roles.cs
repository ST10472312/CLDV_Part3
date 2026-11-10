// ST10472017
// Microsoft, 2026. Role-based authorization in ASP.NET Core. [Online]
// Available at: https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles
// [Accessed 9 November 2026].

namespace CoffeeNChillGateway.Auth;

/// <summary>The three roles. The role is also the PartitionKey in the Users table.</summary>
public static class Roles
{
    public const string Admin = "Admin";
    public const string Staff = "Staff";
    public const string Customer = "Customer";

    public static readonly string[] All = { Admin, Staff, Customer };
}
