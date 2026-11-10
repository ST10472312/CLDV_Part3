// ST10472017
// Microsoft, 2026. Azure Tables client library for .NET. [Online]
// Available at: https://learn.microsoft.com/en-us/dotnet/api/overview/azure/data.tables-readme
// [Accessed 9 November 2026].
// Microsoft, 2026. Design for querying - Azure Table storage. [Online]
// Available at: https://learn.microsoft.com/en-us/azure/storage/tables/table-storage-design-for-query
// [Accessed 9 November 2026].

using Azure;
using Azure.Data.Tables;
using CoffeeNChillGateway.Entities;
using CoffeeNChillGateway.Exceptions;
using CoffeeNChillGateway.Options;
using Microsoft.Extensions.Options;

namespace CoffeeNChillGateway.Auth;

public interface IUserRepository
{
    /// <summary>Finds a user by email, or null when no account exists.</summary>
    Task<UserEntity?> FindByEmailAsync(string email, CancellationToken ct);

    /// <summary>Inserts a new user. Throws ConflictException when the email is taken.</summary>
    Task AddAsync(UserEntity user, CancellationToken ct);
}

/// <summary>
/// Users table access. Because the PartitionKey is the role, finding someone by email means
/// three fast point reads (Admin, Staff, Customer) instead of scanning the whole table.
/// </summary>
public sealed class UserRepository : IUserRepository
{
    private readonly TableClient _table;

    public UserRepository(TableServiceClient tables, IOptions<StorageOptions> options)
        => _table = tables.GetTableClient(options.Value.UsersTable);

    public async Task<UserEntity?> FindByEmailAsync(string email, CancellationToken ct)
    {
        var key = email.Trim().ToLowerInvariant();
        foreach (var role in Roles.All)
        {
            var result = await _table.GetEntityIfExistsAsync<UserEntity>(role, key, cancellationToken: ct);
            if (result.HasValue) return result.Value;
        }
        return null;
    }

    public async Task AddAsync(UserEntity user, CancellationToken ct)
    {
        try
        {
            await _table.AddEntityAsync(user, ct);
        }
        catch (RequestFailedException ex) when (ex.Status == 409)
        {
            throw new ConflictException("An account with that email already exists.");
        }
    }
}
