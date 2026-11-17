namespace Tequio.Domain.Services;

/// <summary>A delivery catalog uniqueness or association conflict.</summary>
public sealed class DeliveryPointConflictException(string message) : Exception(message);
