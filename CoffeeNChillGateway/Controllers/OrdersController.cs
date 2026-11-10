using CoffeeNChillGateway.Dtos;
using CoffeeNChillGateway.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeNChillGateway.Controllers;

/// <summary>Order placement (public) and order management (staff).</summary>
[ApiController]
[Route("api/orders")]
[Produces("application/json")]
public sealed class OrdersController(IOrderService orders) : ControllerBase
{
    /// <summary>Places an order on the queue without waiting for the barista.</summary>
    /// <remarks>Returns 202 straight away. The background Function writes the order to the Orders table.</remarks>
    /// <response code="202">Order accepted and queued.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="503">The queue is unavailable.</response>
    [HttpPost("queue"), AllowAnonymous]
    [ProducesResponseType(typeof(OrderQueuedResponseDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<OrderQueuedResponseDto>> Queue([FromBody] CreateOrderRequestDto dto, CancellationToken ct)
        => Accepted(await orders.EnqueueAsync(dto, ct));

    /// <summary>Lists orders (Admin or Staff). Optional filter: ?status=Ready</summary>
    /// <response code="200">Newest first, max 200.</response>
    [HttpGet, Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType(typeof(IEnumerable<OrderResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<OrderResponseDto>>> List([FromQuery] string? status, CancellationToken ct)
        => Ok(await orders.ListAsync(status, ct));

    /// <summary>Gets one order so a student can follow its status.</summary>
    /// <response code="200">The order.</response>
    /// <response code="404">Unknown id, or still in the queue.</response>
    [HttpGet("{orderId}"), AllowAnonymous]
    [ProducesResponseType(typeof(OrderResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderResponseDto>> Get(string orderId, CancellationToken ct)
        => Ok(await orders.GetAsync(orderId, ct));

    /// <summary>Moves an order along its lifecycle (Admin or Staff).</summary>
    /// <response code="200">Updated order.</response>
    /// <response code="404">Order not found.</response>
    /// <response code="409">Illegal transition, e.g. Collected back to Preparing.</response>
    [HttpPut("status"), Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType(typeof(OrderResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderResponseDto>> UpdateStatus([FromBody] UpdateOrderStatusRequestDto dto, CancellationToken ct)
        => Ok(await orders.UpdateStatusAsync(dto, ct));
}