namespace ExItS.Platform.Application.Common;

/// <summary>Raised when a persistence layer unique constraint is violated.</summary>
public sealed class PersistenceConflictException : Exception
{
    public string ErrorCode { get; }

    public PersistenceConflictException(string errorCode, string message)
        : base(message)
    {
        ErrorCode = errorCode;
    }
}
