using System.ComponentModel.DataAnnotations;

namespace CoffeeNChillGateway.Dtos;

/// <summary>A menu item as returned to clients.</summary>
public sealed class MenuItemDto
{
    /// <example>Hot Drinks</example>
    public string Category { get; set; } = "";
    /// <example>COF-001</example>
    public string Id { get; set; } = "";
    /// <example>Espresso</example>
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    /// <example>28.50</example>
    public decimal Price { get; set; }
    public bool IsAvailable { get; set; }
}

/// <summary>Body for creating a menu item.</summary>
public sealed class CreateMenuItemDto
{
    /// <example>Hot Drinks</example>
    [Required, StringLength(50, MinimumLength = 2)]
    public string Category { get; set; } = "";

    /// <example>COF-001</example>
    [Required, RegularExpression(@"^[A-Z]{3}-\d{3}$", ErrorMessage = "Id must look like COF-001.")]
    public string Id { get; set; } = "";

    /// <example>Espresso</example>
    [Required, StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = "";

    [StringLength(300)]
    public string? Description { get; set; }

    /// <example>28.50</example>
    [Range(0.01, 10000)]
    public decimal Price { get; set; }

    public bool IsAvailable { get; set; } = true;
}

/// <summary>Body for updating a menu item. Only the fields you send are changed.</summary>
public sealed class UpdateMenuItemDto
{
    [StringLength(100, MinimumLength = 2)] public string? Name { get; set; }
    [StringLength(300)] public string? Description { get; set; }
    [Range(0.01, 10000)] public decimal? Price { get; set; }
    public bool? IsAvailable { get; set; }
}