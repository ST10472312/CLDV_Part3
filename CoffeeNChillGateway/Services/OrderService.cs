using System.Globalization;
using System.Text.Json;
using Azure;
using Azure.Data.Tables;
using Azure.Storage.Queues;
using CoffeeNChillGateway.Dtos;
using CoffeeNChillGateway.Exceptions;
using CoffeeNChillGateway.Options;
using Microsoft.Extensions.Options;

namespace CoffeeNChillGateway.Services;

public interface IOrderService
{
    Task<OrderQueuedResponseDto> EnqueueAsync(CreateOrderRequestDto dto, CancellationToken ct);
    Task<IReadOnlyList<OrderResponseDto>> ListAsync(string? status, CancellationToken ct);
    Task<OrderResponseDto> GetAsync(string orderId, CancellationToken ct);
    Task<OrderResponseDto> UpdateStatusAsync(UpdateOrderStatusRequestDto dto, CancellationToken ct);
}

/// <summary>The JSON message placed on order-processing-queue (same shape as the Part 2 brief).</summary>
public sealed record OrderQueueMessage(
    string OrderId,
    string CustomerName,
    IReadOnlyList<string> SelectedItemSKUs,
    decimal TotalPrice,
    DateTime OrderTimestamp);

public sealed class OrderService : IOrderService
{
    // The only legal forward path; "Failed" is allowed from any unfinished state.
    private static readonly string[] Lifecycle = { "Received", "Preparing", "Ready", "Collected" };
    private const string Failed = "Failed";
    private static readonly System.Text.RegularExpressions.Regex SkuPattern = new(@"^[A-Z]{3}-\d{3}$");

    private readonly QueueClient _queue;
    private readonly TableClient _orders;

    public OrderService(QueueServiceClient queues, TableServiceClient tables, IOptions<StorageOptions> options)
    {
        _queue = queues.GetQueueClient(options.Value.OrderQueue);
        _orders = tables.GetTableClient(options.Value.OrdersTable);
    }

    public async Task<OrderQueuedResponseDto> EnqueueAsync(CreateOrderRequestDto dto, CancellationToken ct)
    {
        // DataAnnotations already checked shape; these rules need a loop.
        var badSku = dto.SelectedItemSKUs.FirstOrDefault(s => !SkuPattern.IsMatch(s ?? ""));
        if (badSku is not null) throw new BadRequestException($"'{badSku}' is not a valid SKU (expected e.g. COF-001).");

        var now = DateTime.UtcNow;
        var orderId = string.IsNullOrWhiteSpace(dto.OrderId)
            ? $"ORD-{now:yyyy}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}"
            : dto.OrderId;

        var message = new OrderQueueMessage(orderId, dto.CustomerName.Trim(), dto.SelectedItemSKUs, dto.TotalPrice, now);

        try
        {
            await _queue.SendMessageAsync(JsonSerializer.Serialize(message), ct);
        }
        catch (Exception ex) when (ex is RequestFailedException or HttpRequestException or IOException)
        {
            throw new ServiceUnavailableException("The order queue is temporarily unavailable. Please try again shortly.", ex);
        }

        return new OrderQueuedResponseDto
        {
            OrderId = orderId,
            Status = "Queued",
            QueuedAtUtc = now,
            StatusUrl = $"/api/orders/{orderId}"
        };
    }

    public async Task<IReadOnlyList<OrderResponseDto>> ListAsync(string? status, CancellationToken ct)
    {
        var results = new List<OrderResponseDto>();
        await foreach (var e in _orders.QueryAsync<TableEntity>(cancellationToken: ct))
        {
            var dto = Map(e);
            if (status is null || dto.Status.Equals(status, StringComparison.OrdinalIgnoreCase))
                results.Add(dto);
        }
        return results.OrderByDescending(o => o.LastUpdatedUtc).Take(200).ToList();
    }

    public async Task<OrderResponseDto> GetAsync(string orderId, CancellationToken ct)
        => Map(await FindOrThrowAsync(orderId, ct));

    public async Task<OrderResponseDto> UpdateStatusAsync(UpdateOrderStatusRequestDto dto, CancellationToken ct)
    {
        var target = Lifecycle.Append(Failed).FirstOrDefault(s => s.Equals(dto.Status.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? throw new BadRequestException("Status must be one of: Received, Preparing, Ready, Collected, Failed.");

        var entity = await FindOrThrowAsync(dto.OrderId, ct);
        var current = entity.TryGetValue("Status", out var s) ? s?.ToString() ?? "" : "";

        var currentIndex = Array.IndexOf(Lifecycle, current);
        var targetIndex = Array.IndexOf(Lifecycle, target);
        var allowed = target == Failed
            ? current != Failed && current != "Collected"
            : currentIndex >= 0 && targetIndex > currentIndex;

        if (!allowed) throw new ConflictException($"Cannot move order {dto.OrderId} from '{current}' to '{target}'.");

        entity["Status"] = target;
        try
        {
            await _orders.UpdateEntityAsync(entity, entity.ETag, TableUpdateMode.Merge, ct);
        }
        catch (RequestFailedException ex) when (ex.Status == 412)
        {
            // The Part 2 Function also advances statuses; the ETag stops us overwriting its change.
            throw new ConflictException("The order was just updated by the processor. Reload and try again.");
        }
        return Map(entity);
    }

    private async Task<TableEntity> FindOrThrowAsync(string orderId, CancellationToken ct)
    {
        var filter = TableClient.CreateQueryFilter($"RowKey eq {orderId}");
        await foreach (var e in _orders.QueryAsync<TableEntity>(filter, maxPerPage: 1, cancellationToken: ct))
            return e;
        throw new NotFoundException($"Order {orderId} was not found. If it was just placed it may still be waiting in the queue.");
    }

    private static OrderResponseDto Map(TableEntity e) => new()
    {
        OrderId = e.RowKey,
        OrderDate = e.PartitionKey,
        CustomerName = Get(e, "CustomerName") ?? "",
        SelectedItemSKUs = ReadSkus(Get(e, "SelectedItemSKUs", "SelectedItemSkus", "Skus", "Items")),
        TotalPrice = decimal.TryParse(Get(e, "TotalPrice"), NumberStyles.Any, CultureInfo.InvariantCulture, out var p) ? p : 0m,
        Status = Get(e, "Status") ?? "",
        LastUpdatedUtc = e.Timestamp
    };

    private static string? Get(TableEntity e, params string[] names)
    {
        foreach (var n in names)
            if (e.TryGetValue(n, out var v) && v is not null)
                return Convert.ToString(v, CultureInfo.InvariantCulture);
        return null;
    }

    private static List<string> ReadSkus(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return new();
        if (raw.TrimStart().StartsWith('['))
        {
            try { return JsonSerializer.Deserialize<List<string>>(raw) ?? new(); }
            catch (JsonException) { /* fall through to comma splitting */ }
        }
        return raw.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    }
}