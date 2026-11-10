namespace CoffeeNChillGateway.Exceptions;

/// <summary>Resource does not exist (maps to HTTP 404).</summary>
public sealed class NotFoundException(string message) : Exception(message);

/// <summary>Resource already exists or was changed by someone else (maps to HTTP 409).</summary>
public sealed class ConflictException(string message) : Exception(message);

/// <summary>Client sent something the DTO attributes cannot catch (maps to HTTP 400).</summary>
public sealed class BadRequestException(string message) : Exception(message);

/// <summary>A storage dependency is down (maps to HTTP 503).</summary>
public sealed class ServiceUnavailableException(string message, Exception? inner = null) : Exception(message, inner);