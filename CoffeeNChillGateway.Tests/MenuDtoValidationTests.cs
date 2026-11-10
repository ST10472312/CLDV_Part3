using System.ComponentModel.DataAnnotations;
using CoffeeNChillGateway.Dtos;

namespace CoffeeNChillGateway.Tests;

public class MenuDtoValidationTests
{
    private static List<ValidationResult> Validate(object o)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(o, new ValidationContext(o), results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void ValidMenuItem_HasNoErrors()
    {
        var dto = new CreateMenuItemDto { Category = "Hot Drinks", Id = "COF-001", Name = "Espresso", Price = 28.5m };
        Assert.Empty(Validate(dto));
    }

    [Theory]
    [InlineData("coffee1")]
    [InlineData("COF001")]
    [InlineData("")]
    public void BadSku_IsRejected(string sku)
    {
        var dto = new CreateMenuItemDto { Category = "Hot Drinks", Id = sku, Name = "Espresso", Price = 10m };
        Assert.NotEmpty(Validate(dto));
    }

    [Fact]
    public void ZeroPrice_IsRejected()
    {
        var dto = new CreateMenuItemDto { Category = "Hot Drinks", Id = "COF-001", Name = "Espresso", Price = 0m };
        Assert.NotEmpty(Validate(dto));
    }

    [Fact]
    public void Order_WithoutItems_IsRejected()
    {
        var dto = new CreateOrderRequestDto { CustomerName = "Jane", SelectedItemSKUs = new(), TotalPrice = 10m };
        Assert.NotEmpty(Validate(dto));
    }
}