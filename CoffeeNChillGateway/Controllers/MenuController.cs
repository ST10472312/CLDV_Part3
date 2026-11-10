using CoffeeNChillGateway.Dtos;
using CoffeeNChillGateway.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeNChillGateway.Controllers;

/// <summary>Digital menu. Reading is public; changing it needs a Staff or Admin token.</summary>
[ApiController]
[Route("api/menu")]
[Produces("application/json")]
public sealed class MenuController(IMenuService menu) : ControllerBase
{
    /// <summary>Gets every menu item.</summary>
    /// <response code="200">The full menu.</response>
    [HttpGet, AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<MenuItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<MenuItemDto>>> GetAll(CancellationToken ct)
        => Ok(await menu.GetAllAsync(ct));

    /// <summary>Gets the items in one category.</summary>
    /// <param name="category">For example "Hot Drinks" or "Pastries".</param>
    /// <response code="200">Items in that category (empty list if none).</response>
    [HttpGet("category/{category}"), AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<MenuItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<MenuItemDto>>> GetByCategory(string category, CancellationToken ct)
        => Ok(await menu.GetByCategoryAsync(category, ct));

    /// <summary>Adds a new menu item (Admin or Staff).</summary>
    /// <response code="201">Item created.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="409">An item with that SKU already exists in the category.</response>
    [HttpPost, Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType(typeof(MenuItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MenuItemDto>> Create([FromBody] CreateMenuItemDto dto, CancellationToken ct)
    {
        var created = await menu.CreateAsync(dto, ct);
        return Created($"/api/menu/category/{Uri.EscapeDataString(created.Category)}", created);
    }

    /// <summary>Updates price, availability, name or description (Admin or Staff).</summary>
    /// <response code="200">Updated item.</response>
    /// <response code="404">No such item.</response>
    [HttpPut("{category}/{id}"), Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType(typeof(MenuItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MenuItemDto>> Update(string category, string id, [FromBody] UpdateMenuItemDto dto, CancellationToken ct)
        => Ok(await menu.UpdateAsync(category, id, dto, ct));

    /// <summary>Removes an item from the menu (Admin or Staff).</summary>
    /// <response code="204">Deleted.</response>
    /// <response code="404">No such item.</response>
    [HttpDelete("{category}/{id}"), Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string category, string id, CancellationToken ct)
    {
        await menu.DeleteAsync(category, id, ct);
        return NoContent();
    }
}