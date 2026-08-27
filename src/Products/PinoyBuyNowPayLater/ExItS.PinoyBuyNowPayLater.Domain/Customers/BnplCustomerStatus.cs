namespace ExItS.PinoyBuyNowPayLater.Domain.Customers;

/// <summary>Profile lifecycle only — not financing eligibility or credit state.</summary>
public enum BnplCustomerStatus
{
    Active = 0,
    Inactive = 1
}
