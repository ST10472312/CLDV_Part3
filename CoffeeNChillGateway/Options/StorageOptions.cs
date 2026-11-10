namespace CoffeeNChillGateway.Options;

/// <summary>Names and connection string for Azure Storage (bound from the "Storage" config section).</summary>
public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    public string ConnectionString { get; set; } = "UseDevelopmentStorage=true";
    public string MenuTable { get; set; } = "MenuItems";
    public string OrdersTable { get; set; } = "Orders";
    public string UsersTable { get; set; } = "Users";
    public string DocumentsContainer { get; set; } = "staff-docs";
    public string OrderQueue { get; set; } = "order-processing-queue";
}