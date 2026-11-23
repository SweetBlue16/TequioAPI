namespace Tequio.Domain.Services;

/// <summary>Signals that a product's current state prevents the requested operation.</summary>
public sealed class ProductConflictException(string message) : InvalidOperationException(message);

/// <summary>Signals that the requested product is unavailable to the current producer.</summary>
public sealed class ProductOwnershipException(string message) : UnauthorizedAccessException(message);
