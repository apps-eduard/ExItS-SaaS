namespace ExItS.PinoyBusinessPOS.Domain.Common;

public static class DomainErrorCodes
{
    public const string InvalidCustomerId = "pos.customer.id.invalid";
    public const string InvalidOrganizationId = "pos.organization.id.invalid";
    public const string InvalidDisplayName = "pos.customer.display_name.invalid";
    public const string InvalidMobileNumber = "pos.customer.mobile.invalid";
    public const string InvalidAddress = "pos.customer.address.invalid";
    public const string InvalidNotes = "pos.customer.notes.invalid";
    public const string InvalidUtcTimestamp = "pos.timestamp.invalid";
    public const string InvalidCustomerStatusTransition = "pos.customer.status.invalid_transition";
    public const string CustomerNotActive = "pos.customer.not_active";
}
