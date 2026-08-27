namespace ExItS.PinoyBuyNowPayLater.Domain.Customers;

public static class BnplCustomerErrorCodes
{
    public const string InvalidCustomerId = "bnpl.customer.invalid_id";
    public const string InvalidOrganizationId = "bnpl.customer.invalid_organization_id";
    public const string InvalidDisplayName = "bnpl.customer.invalid_display_name";
    public const string InvalidMobile = "bnpl.customer.invalid_mobile";
    public const string InvalidEmail = "bnpl.customer.invalid_email";
    public const string InvalidPersonalPublicUserId = "bnpl.customer.invalid_personal_public_user_id";
    public const string InvalidCommerceCustomerId = "bnpl.customer.invalid_commerce_customer_id";
    public const string IdempotencyConflict = "bnpl.customer.idempotency_conflict";
    public const string PersonalLinkConflict = "bnpl.customer.personal_link_conflict";
    public const string CommerceLinkConflict = "bnpl.customer.commerce_link_conflict";
    public const string NotFound = "bnpl.customer.not_found";
    public const string ImmutableIdentity = "bnpl.customer.immutable_identity";
}
