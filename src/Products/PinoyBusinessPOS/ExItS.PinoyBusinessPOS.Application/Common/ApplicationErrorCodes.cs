namespace ExItS.PinoyBusinessPOS.Application.Common;

public static class ApplicationErrorCodes
{
    public const string CustomerNotFound = "pos.customer.not_found";
    public const string MobileConflict = "pos.customer.mobile.conflict";
    public const string ConcurrencyConflict = "pos.concurrency_conflict";
    public const string OrganizationRequired = "pos.organization.required";
    public const string DomainViolation = "pos.domain_violation";
}

public sealed class PersistenceConflictException : Exception
{
    public string ErrorCode { get; }

    public PersistenceConflictException(string errorCode, string message)
        : base(message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        ErrorCode = errorCode;
    }
}

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);

public static class PosPagination
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public static (int Skip, int Take) Normalize(int? page, int? pageSize)
    {
        var take = Math.Clamp(pageSize ?? DefaultPageSize, 1, MaxPageSize);
        var pageNumber = Math.Max(page ?? 1, 1);
        return ((pageNumber - 1) * take, take);
    }
}
