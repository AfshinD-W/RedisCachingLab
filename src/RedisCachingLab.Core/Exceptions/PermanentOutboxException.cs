namespace RedisCachingLab.Core.Exceptions;

public class PermanentOutboxException(string message) : Exception(message);