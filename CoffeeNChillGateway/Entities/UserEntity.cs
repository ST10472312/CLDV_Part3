// ST10472017
// Microsoft, 2026. Azure Tables client library for .NET. [Online]
// Available at: https://learn.microsoft.com/en-us/dotnet/api/overview/azure/data.tables-readme
// [Accessed 9 November 2026].
// Microsoft, 2026. ITableEntity Interface. [Online]
// Available at: https://learn.microsoft.com/en-us/dotnet/api/azure.data.tables.itableentity
// [Accessed 9 November 2026].

using Azure;
using Azure.Data.Tables;

namespace CoffeeNChillGateway.Entities;

/// <summary>
/// A row in the Users table. PartitionKey is the role ("Admin", "Staff" or "Customer"),
/// RowKey is the lower-case email.
/// </summary>
public sealed class UserEntity : ITableEntity
{
    public string PartitionKey { get; set; } = default!;
    public string RowKey { get; set; } = default!;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    /// <summary>BCrypt hash. The plain password is never stored or logged.</summary>
    public string PasswordHash { get; set; } = "";

    public string FullName { get; set; } = "";
    public DateTime CreatedDate { get; set; }
}
