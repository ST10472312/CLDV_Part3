using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using CoffeeNChillGateway.Dtos;
using CoffeeNChillGateway.Exceptions;
using CoffeeNChillGateway.Options;
using Microsoft.Extensions.Options;

namespace CoffeeNChillGateway.Services;

public sealed record DocumentDownload(Stream Content, string ContentType, string FileName);

public interface IDocumentService
{
    Task<DocumentInfoDto> UploadAsync(string fileName, string contentType, long length, Stream content, CancellationToken ct);
    Task<IReadOnlyList<DocumentInfoDto>> ListAsync(CancellationToken ct);
    Task<DocumentDownload> DownloadAsync(string fileName, CancellationToken ct);
    Task DeleteAsync(string fileName, CancellationToken ct);
}

public sealed class DocumentService : IDocumentService
{
    private const long MaxBytes = 10 * 1024 * 1024; // 10 MB
    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".pdf", ".txt", ".docx", ".png", ".jpg", ".jpeg" };

    private readonly BlobContainerClient _container;
    private readonly ILogger<DocumentService> _logger;

    public DocumentService(BlobServiceClient blobs, IOptions<StorageOptions> options, ILogger<DocumentService> logger)
    {
        _container = blobs.GetBlobContainerClient(options.Value.DocumentsContainer);
        _logger = logger;
    }

    public async Task<DocumentInfoDto> UploadAsync(string fileName, string contentType, long length, Stream content, CancellationToken ct)
    {
        var name = Path.GetFileName(fileName);   // strips any ../ path tricks
        if (string.IsNullOrWhiteSpace(name)) throw new BadRequestException("A file name is required.");
        if (length <= 0) throw new BadRequestException("The file is empty.");
        if (length > MaxBytes) throw new BadRequestException("The file is larger than the 10 MB limit.");
        if (!AllowedExtensions.Contains(Path.GetExtension(name)))
            throw new BadRequestException($"File type not allowed. Allowed: {string.Join(", ", AllowedExtensions)}.");

        var blob = _container.GetBlobClient(name);
        var options = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType }
        };
        await blob.UploadAsync(content, options, ct);   // streams straight to storage, never buffers the whole file
        _logger.LogInformation("Uploaded staff document {FileName} ({Bytes} bytes)", name, length);

        var props = (await blob.GetPropertiesAsync(cancellationToken: ct)).Value;
        return new DocumentInfoDto { FileName = name, SizeBytes = props.ContentLength, ContentType = props.ContentType, LastModifiedUtc = props.LastModified };
    }

    public async Task<IReadOnlyList<DocumentInfoDto>> ListAsync(CancellationToken ct)
    {
        var list = new List<DocumentInfoDto>();
        await foreach (var item in _container.GetBlobsAsync(cancellationToken: ct))
        {
            list.Add(new DocumentInfoDto
            {
                FileName = item.Name,
                SizeBytes = item.Properties.ContentLength ?? 0,
                ContentType = item.Properties.ContentType ?? "",
                LastModifiedUtc = item.Properties.LastModified
            });
        }
        return list;
    }

    public async Task<DocumentDownload> DownloadAsync(string fileName, CancellationToken ct)
    {
        var name = Path.GetFileName(fileName);
        var blob = _container.GetBlobClient(name);
        if (!(await blob.ExistsAsync(ct)).Value) throw new NotFoundException($"Document '{name}' was not found.");

        var props = (await blob.GetPropertiesAsync(cancellationToken: ct)).Value;
        var stream = await blob.OpenReadAsync(cancellationToken: ct);   // streamed to the client
        return new DocumentDownload(stream, props.ContentType ?? "application/octet-stream", name);
    }

    public async Task DeleteAsync(string fileName, CancellationToken ct)
    {
        var name = Path.GetFileName(fileName);
        var deleted = await _container.DeleteBlobIfExistsAsync(name, cancellationToken: ct);
        if (!deleted.Value) throw new NotFoundException($"Document '{name}' was not found.");
        _logger.LogInformation("Deleted staff document {FileName}", name);
    }
}