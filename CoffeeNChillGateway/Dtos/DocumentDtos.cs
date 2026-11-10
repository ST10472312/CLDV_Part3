namespace CoffeeNChillGateway.Dtos;

/// <summary>Metadata about a staff document stored in the staff-docs container.</summary>
public sealed class DocumentInfoDto
{
    public string FileName { get; set; } = "";
    public long SizeBytes { get; set; }
    public string ContentType { get; set; } = "";
    public DateTimeOffset? LastModifiedUtc { get; set; }
}