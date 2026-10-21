namespace Orders.Application.Exceptions;

public sealed class InventoryUnavailableException(Exception innerException) : Exception(InventoryMessages.ValidationFailed, innerException);
