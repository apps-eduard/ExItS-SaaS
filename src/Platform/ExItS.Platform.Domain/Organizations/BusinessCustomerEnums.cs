namespace ExItS.Platform.Domain.Organizations;

public enum BusinessCustomerStatus
{
    Active = 0,
    Inactive = 1,
    Archived = 2
}

public enum CreditCustomerStatus
{
    Active = 0,
    Closed = 1
}

public enum CustomerLinkRequestStatus
{
    Pending = 0,
    Active = 1,
    Declined = 2,
    Revoked = 3,
    Expired = 4
}

public enum LinkedCustomerAppUserStatus
{
    Active = 0,
    Revoked = 1
}
