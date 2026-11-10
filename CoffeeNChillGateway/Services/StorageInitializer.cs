using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using CoffeeNChillGateway.Options;
using Microsoft.Extensions.Options;

namespace CoffeeNChillGateway.Services;

/// <summary>
/// Creates the tables, blob container and queue on startup. In Docker Compose the gateway can
/// start a moment before Azurite is ready, so this retries instead of crashing immediately.
/// </summary>
public sealed class StorageInitializer(
    TableServiceClient tables,
    BlobServiceClient blobs,
    QueueServiceClient queues,
    IOptions<StorageOptions> options,
    ILogger<StorageInitializer> logger) : IHostedService
{
    private const int MaxAttempts = 10;

    public async Task StartAsync(CancellationToken ct)
    {
        var o = options.Value;
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                foreach (var table in new[] { o.MenuTable, o.OrdersTable, o.UsersTable })
                    await tables.CreateTableIfNotExistsAsync(table, ct);

                await blobs.GetBlobContainerClient(o.DocumentsContainer).CreateIfNotExistsAsync(cancellationToken: ct);
                await queues.GetQueueClient(o.OrderQueue).CreateIfNotExistsAsync(cancellationToken: ct);

                logger.LogInformation("Storage ready: tables, container '{Container}' and queue '{Queue}'.", o.DocumentsContainer, o.OrderQueue);
                return;
            }
            catch (Exception ex) when (attempt < MaxAttempts)
            {
                logger.LogWarning(ex, "Storage not ready (attempt {Attempt}/{Max}). Retrying in 3 seconds.", attempt, MaxAttempts);
                await Task.Delay(TimeSpan.FromSeconds(3), ct);
            }
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}