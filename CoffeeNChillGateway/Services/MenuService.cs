using System.Globalization;
using Azure;
using Azure.Data.Tables;
using CoffeeNChillGateway.Dtos;
using CoffeeNChillGateway.Exceptions;
using CoffeeNChillGateway.Options;
using Microsoft.Extensions.Options;

namespace CoffeeNChillGateway.Services;

public interface IMenuService
{
    Task<IReadOnlyList<MenuItemDto>> GetAllAsync(CancellationToken ct);
    Task<IReadOnlyList<MenuItemDto>> GetByCategoryAsync(string category, CancellationToken ct);
    Task<MenuItemDto> CreateAsync(CreateMenuItemDto dto, CancellationToken ct);
    Task<MenuItemDto> UpdateAsync(string category, string id, UpdateMenuItemDto dto, CancellationToken ct);
    Task DeleteAsync(string category, string id, CancellationToken ct);
}

/// <summary>Menu CRUD on the MenuItems table (PartitionKey = Category, RowKey = SKU).</summary>
public sealed class MenuService : IMenuService
{
    private readonly TableClient _table;

    public MenuService(TableServiceClient tables, IOptions<StorageOptions> options)
        => _table = tables.GetTableClient(options.Value.MenuTable);

    public async Task<IReadOnlyList<MenuItemDto>> GetAllAsync(CancellationToken ct)
    {
        var items = new List<MenuItemDto>();
        await foreach (var e in _table.QueryAsync<TableEntity>(cancellationToken: ct))
            items.Add(Map(e));
        return items;
    }

    public async Task<IReadOnlyList<MenuItemDto>> GetByCategoryAsync(string category, CancellationToken ct)
    {
        var filter = TableClient.CreateQueryFilter($"PartitionKey eq {category}");
        var items = new List<MenuItemDto>();
        await foreach (var e in _table.QueryAsync<TableEntity>(filter, cancellationToken: ct))
            items.Add(Map(e));
        return items;
    }

    public async Task<MenuItemDto> CreateAsync(CreateMenuItemDto dto, CancellationToken ct)
    {
        var entity = new TableEntity(dto.Category.Trim(), dto.Id.Trim().ToUpperInvariant())
        {
            ["Name"] = dto.Name.Trim(),
            ["Description"] = dto.Description?.Trim() ?? "",
            ["Price"] = (double)dto.Price,          // Table Storage has no decimal type
            ["IsAvailable"] = dto.IsAvailable
        };

        try
        {
            await _table.AddEntityAsync(entity, ct);
        }
        catch (RequestFailedException ex) when (ex.Status == 409)
        {
            throw new ConflictException($"Menu item {entity.RowKey} already exists in '{entity.PartitionKey}'.");
        }
        return Map(entity);
    }

    public async Task<MenuItemDto> UpdateAsync(string category, string id, UpdateMenuItemDto dto, CancellationToken ct)
    {
        var entity = await GetEntityOrThrowAsync(category, id, ct);

        if (dto.Name is not null) entity["Name"] = dto.Name.Trim();
        if (dto.Description is not null) entity["Description"] = dto.Description.Trim();
        if (dto.Price is not null) entity["Price"] = (double)dto.Price.Value;
        if (dto.IsAvailable is not null) entity["IsAvailable"] = dto.IsAvailable.Value;

        try
        {
            // ETag = optimistic concurrency: fails if someone else changed the row since we read it.
            await _table.UpdateEntityAsync(entity, entity.ETag, TableUpdateMode.Merge, ct);
        }
        catch (RequestFailedException ex) when (ex.Status == 412)
        {
            throw new ConflictException("The menu item was changed by someone else. Reload and try again.");
        }
        return Map(entity);
    }

    public async Task DeleteAsync(string category, string id, CancellationToken ct)
    {
        await GetEntityOrThrowAsync(category, id, ct);   // gives a proper 404 instead of a silent success
        await _table.DeleteEntityAsync(category, id.ToUpperInvariant(), cancellationToken: ct);
    }

    private async Task<TableEntity> GetEntityOrThrowAsync(string category, string id, CancellationToken ct)
    {
        var result = await _table.GetEntityIfExistsAsync<TableEntity>(category, id.ToUpperInvariant(), cancellationToken: ct);
        if (!result.HasValue) throw new NotFoundException($"Menu item {id} was not found in '{category}'.");
        return result.Value!;
    }

    private static MenuItemDto Map(TableEntity e) => new()
    {
        Category = e.PartitionKey,
        Id = e.RowKey,
        Name = e.TryGetValue("Name", out var n) ? n?.ToString() ?? "" : "",
        Description = e.TryGetValue("Description", out var d) ? d?.ToString() ?? "" : "",
        Price = e.TryGetValue("Price", out var p) && p is not null ? Convert.ToDecimal(p, CultureInfo.InvariantCulture) : 0m,
        IsAvailable = e.TryGetValue("IsAvailable", out var a) && a is not null && Convert.ToBoolean(a)
    };
}