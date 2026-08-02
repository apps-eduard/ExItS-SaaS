using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.Organizations;

public sealed class BusinessCustomerId : IEquatable<BusinessCustomerId>
{
    public Guid Value { get; }

    private BusinessCustomerId(Guid value) => Value = value;

    public static BusinessCustomerId New() => new(Guid.NewGuid());

    public static BusinessCustomerId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidBusinessCustomerId,
                "Business customer id is required.");
        }

        return new BusinessCustomerId(value);
    }

    public bool Equals(BusinessCustomerId? other) =>
        other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) =>
        obj is BusinessCustomerId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");

    public static bool operator ==(BusinessCustomerId? left, BusinessCustomerId? right) =>
        Equals(left, right);

    public static bool operator !=(BusinessCustomerId? left, BusinessCustomerId? right) =>
        !Equals(left, right);
}

public sealed class CreditCustomerId : IEquatable<CreditCustomerId>
{
    public Guid Value { get; }

    private CreditCustomerId(Guid value) => Value = value;

    public static CreditCustomerId New() => new(Guid.NewGuid());

    public static CreditCustomerId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCreditCustomerId,
                "Credit customer id is required.");
        }

        return new CreditCustomerId(value);
    }

    public bool Equals(CreditCustomerId? other) =>
        other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) =>
        obj is CreditCustomerId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");

    public static bool operator ==(CreditCustomerId? left, CreditCustomerId? right) =>
        Equals(left, right);

    public static bool operator !=(CreditCustomerId? left, CreditCustomerId? right) =>
        !Equals(left, right);
}

public sealed class CustomerLinkRequestId : IEquatable<CustomerLinkRequestId>
{
    public Guid Value { get; }

    private CustomerLinkRequestId(Guid value) => Value = value;

    public static CustomerLinkRequestId New() => new(Guid.NewGuid());

    public static CustomerLinkRequestId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerLinkRequestId,
                "Customer link request id is required.");
        }

        return new CustomerLinkRequestId(value);
    }

    public bool Equals(CustomerLinkRequestId? other) =>
        other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) =>
        obj is CustomerLinkRequestId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");

    public static bool operator ==(CustomerLinkRequestId? left, CustomerLinkRequestId? right) =>
        Equals(left, right);

    public static bool operator !=(CustomerLinkRequestId? left, CustomerLinkRequestId? right) =>
        !Equals(left, right);
}

public sealed class LinkedCustomerAppUserId : IEquatable<LinkedCustomerAppUserId>
{
    public Guid Value { get; }

    private LinkedCustomerAppUserId(Guid value) => Value = value;

    public static LinkedCustomerAppUserId New() => new(Guid.NewGuid());

    public static LinkedCustomerAppUserId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidLinkedCustomerAppUserId,
                "Linked customer app user id is required.");
        }

        return new LinkedCustomerAppUserId(value);
    }

    public bool Equals(LinkedCustomerAppUserId? other) =>
        other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) =>
        obj is LinkedCustomerAppUserId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");

    public static bool operator ==(LinkedCustomerAppUserId? left, LinkedCustomerAppUserId? right) =>
        Equals(left, right);

    public static bool operator !=(LinkedCustomerAppUserId? left, LinkedCustomerAppUserId? right) =>
        !Equals(left, right);
}
