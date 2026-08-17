namespace ExItS.PinoyBusinessPOS.Application.CustomerOrdering;

public sealed record PersonalMerchantFulfillmentSelection(
    string FulfillmentType,
    Guid? BranchId,
    string? BranchName,
    bool ShowFulfillmentToggle,
    bool ShowBranchSelector,
    bool CanPlace);

/// <summary>
/// Personal storefront review helpers: auto-select eligible branches and fulfillment mode.
/// </summary>
public static class PersonalMerchantCheckoutUi
{
    public const string Pickup = "Pickup";
    public const string Delivery = "Delivery";

    public static readonly IReadOnlyList<string> PaymentMethodCodes =
    [
        nameof(Domain.CustomerOrdering.CustomerOrderPaymentMethod.Cash),
        nameof(Domain.CustomerOrdering.CustomerOrderPaymentMethod.ManualGCash),
        nameof(Domain.CustomerOrdering.CustomerOrderPaymentMethod.Utang)
    ];

    public static bool PickupAvailable(IEnumerable<CustomerStorefrontBranchDto> branches) =>
        branches.Any(b => b.PickupEnabled);

    public static bool DeliveryAvailable(
        IEnumerable<CustomerStorefrontBranchDto> branches,
        bool canCustomerDelivery) =>
        canCustomerDelivery && branches.Any(b => b.DeliveryEnabled);

    public static string DefaultFulfillment(
        IReadOnlyList<CustomerStorefrontBranchDto> branches,
        bool canCustomerDelivery) =>
        PickupAvailable(branches)
            ? Pickup
            : DeliveryAvailable(branches, canCustomerDelivery)
                ? Delivery
                : Pickup;

    public static IReadOnlyList<CustomerStorefrontBranchDto> EligibleBranches(
        IReadOnlyList<CustomerStorefrontBranchDto> branches,
        bool canCustomerDelivery,
        string fulfillmentType)
    {
        if (string.Equals(fulfillmentType, Delivery, StringComparison.OrdinalIgnoreCase))
        {
            return canCustomerDelivery
                ? branches.Where(b => b.DeliveryEnabled).ToList()
                : [];
        }

        return branches.Where(b => b.PickupEnabled).ToList();
    }

    public static PersonalMerchantFulfillmentSelection Resolve(
        IReadOnlyList<CustomerStorefrontBranchDto> branches,
        bool canCustomerDelivery,
        string requestedFulfillment,
        Guid? currentBranchId)
    {
        var pickupOk = PickupAvailable(branches);
        var deliveryOk = DeliveryAvailable(branches, canCustomerDelivery);
        string fulfillment;
        if (pickupOk && deliveryOk)
        {
            fulfillment = string.Equals(requestedFulfillment, Delivery, StringComparison.OrdinalIgnoreCase)
                ? Delivery
                : Pickup;
        }
        else if (deliveryOk)
        {
            fulfillment = Delivery;
        }
        else
        {
            fulfillment = Pickup;
        }

        var eligible = EligibleBranches(branches, canCustomerDelivery, fulfillment);
        if (eligible.Count == 0)
        {
            return new(
                fulfillment,
                null,
                null,
                ShowFulfillmentToggle: pickupOk && deliveryOk,
                ShowBranchSelector: false,
                CanPlace: false);
        }

        var selected = currentBranchId is Guid id
            ? eligible.FirstOrDefault(b => b.BranchId == id) ?? eligible[0]
            : eligible[0];

        return new(
            fulfillment,
            selected.BranchId,
            selected.Name,
            ShowFulfillmentToggle: pickupOk && deliveryOk,
            ShowBranchSelector: eligible.Count > 1,
            CanPlace: true);
    }
}
