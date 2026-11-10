using System.ComponentModel.DataAnnotations;

namespace CoffeeNChillGateway.Dtos;

/// <summary>Body for placing an order.</summary>
public sealed class CreateOrderRequestDto
{
    /// <summary>Optional. Leave empty and the gateway generates one (ORD-2026-AB12CD34).</summary>
    [RegularExpression(@"^ORD-\d{4}-[A-Z0-9]{4,8}$", ErrorMessage = "OrderId must look like ORD-2026-8801.")]
    public string? OrderId { get; set; }

    /// <example>Jane Smith</example>
    [Required, StringLength(100, MinimumLength = 2)]
    public string CustomerName { get; set; } = "";

    /// <example>["COF-001","PAS-104"]</example>
    [Required, MinLength(1), MaxLength(50)]
    public List<string> SelectedItemSKUs { get; set; } = new();

    /// <example>65.00</example>
    [Range(0.01, 100000)]
    public decimal TotalPrice { get; set; }
}

/// <summary>Returned immediately (HTTP 202) once the order is on the queue.</summary>
public sealed class OrderQueuedResponseDto
{
    public string OrderId { get; set; } = "";
    public string Status { get; set; } = "Queued";
    public DateTime QueuedAtUtc { get; set; }
    /// <summary>Poll this route to watch Received, Preparing, Ready, Collected.</summary>
    public string StatusUrl { get; set; } = "";
}

/// <summary>An order as stored in the Orders table.</summary>
public sealed class OrderResponseDto
{
    public string OrderId { get; set; } = "";
    public string OrderDate { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public List<string> SelectedItemSKUs { get; set; } = new();
    public decimal TotalPrice { get; set; }
    public string Status { get; set; } = "";
    public DateTimeOffset? LastUpdatedUtc { get; set; }
}

/// <summary>Body for staff to move an order through its lifecycle.</summary>
public sealed class UpdateOrderStatusRequestDto
{
    /// <example>ORD-2026-8801</example>
    [Required] public string OrderId { get; set; } = "";

    /// <summary>Preparing, Ready, Collected or Failed.</summary>
    /// <example>Preparing</example>
    [Required] public string Status { get; set; } = "";
}