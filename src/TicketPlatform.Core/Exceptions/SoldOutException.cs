namespace TicketPlatform.Core.Exceptions;

public sealed class SoldOutException(string message) : Exception(message);
