using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;

namespace ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;

/// <summary>
/// Authoritative fulfillment-method readiness for connected PO submit and later seller transitions.
/// Does not silently change the selected method when setup becomes invalid.
/// </summary>
public sealed class ConnectedPoFulfillmentReadinessGate
{
    public const string ErrorCode = ConnectedSupplierErrorCodes.FulfillmentNotReady;

    private readonly ICustomerOrderBranchDirectory _branches;
    private readonly IOrganizationFulfillmentSettingsRepository _fulfillmentSettings;

    public ConnectedPoFulfillmentReadinessGate(
        ICustomerOrderBranchDirectory branches,
        IOrganizationFulfillmentSettingsRepository? fulfillmentSettings = null)
    {
        _branches = branches;
        _fulfillmentSettings = fulfillmentSettings ?? new NullOrganizationFulfillmentSettingsRepository();
    }

    public Task<ApplicationResult> EnsureForSubmitAsync(
        ConnectedSupplierRelationship relationship,
        PurchaseOrder buyerPo,
        bool forBuyerMessage,
        CancellationToken cancellationToken = default) =>
        EnsureAsync(
            relationship,
            buyerPo.FulfillmentMethod,
            buyerPo.IntendedReceivingBranchId,
            buyerPo.SupplierBranchNameSnapshot,
            forBuyerMessage,
            cancellationToken);

    public Task<ApplicationResult> EnsureForLifecycleAsync(
        ConnectedSupplierRelationship relationship,
        ConnectedPurchaseOrder order,
        PurchaseOrder? buyerPo,
        bool forBuyerMessage,
        CancellationToken cancellationToken = default)
    {
        var method = order.EffectiveFulfillmentMethod
            ?? buyerPo?.FulfillmentMethod;
        var receivingBranchId = buyerPo?.IntendedReceivingBranchId;
        var branchName = relationship.SupplierBranchNameSnapshot
            ?? buyerPo?.SupplierBranchNameSnapshot;
        return EnsureAsync(
            relationship,
            method,
            receivingBranchId,
            branchName,
            forBuyerMessage,
            cancellationToken);
    }

    public async Task<ApplicationResult> EnsureAsync(
        ConnectedSupplierRelationship relationship,
        string? fulfillmentMethod,
        Guid? intendedReceivingBranchId,
        string? supplierBranchName,
        bool forBuyerMessage,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fulfillmentMethod))
        {
            return ApplicationResult.Failure(
                ErrorCode,
                forBuyerMessage
                    ? "Select Pickup or Delivery before submitting this purchase order."
                    : "Purchase order is missing a fulfillment method.");
        }

        var method = fulfillmentMethod.Trim();
        var isDelivery = method.Equals(
            ConnectedSupplierCommerceReadiness.FulfillmentDelivery,
            StringComparison.OrdinalIgnoreCase);
        var isPickup = method.Equals(
            ConnectedSupplierCommerceReadiness.FulfillmentPickup,
            StringComparison.OrdinalIgnoreCase);
        if (!isDelivery && !isPickup)
        {
            return ApplicationResult.Failure(
                ErrorCode,
                "Fulfillment method must be Pickup or Delivery.");
        }

        var branchName = string.IsNullOrWhiteSpace(supplierBranchName)
            ? "the selected supplier branch"
            : supplierBranchName.Trim();

        if (relationship.SupplierBranchId is not Guid branchId || branchId == Guid.Empty)
        {
            return ApplicationResult.Failure(
                ErrorCode,
                forBuyerMessage
                    ? "This supplier has not selected a fulfillment branch yet."
                    : "Assign a selling/fulfillment branch before continuing.");
        }

        var fulfillmentSettings = await _fulfillmentSettings
            .GetAsync(relationship.SupplierOrganizationId, cancellationToken)
            .ConfigureAwait(false);
        var orgOfferDelivery = fulfillmentSettings?.OfferDelivery == true;

        var branch = await _branches
            .GetCommerceBranchAsync(
                relationship.SupplierOrganizationId.Value,
                branchId,
                relationship.SupplierPublicOrganizationIdSnapshot,
                cancellationToken)
            .ConfigureAwait(false);

        if (isPickup)
        {
            if (branch is null || !branch.PickupEnabled || !branch.PickupReady)
            {
                return ApplicationResult.Failure(
                    ErrorCode,
                    forBuyerMessage
                        ? $"Pickup is not currently available from {branchName}."
                        : $"Pickup is not ready for {branchName}.");
            }
        }
        else
        {
            var branchDeliveryReady = branch is not null
                && branch.DeliveryEnabled
                && branch.DeliveryReady;
            if (!orgOfferDelivery)
            {
                return ApplicationResult.Failure(
                    ErrorCode,
                    forBuyerMessage
                        ? "Delivery is not currently available from this supplier."
                        : "Organization Offer Delivery is off.");
            }

            if (!branchDeliveryReady)
            {
                return ApplicationResult.Failure(
                    ErrorCode,
                    forBuyerMessage
                        ? $"Delivery is not currently available from {branchName}."
                        : $"Delivery is not ready for {branchName}.");
            }

            if (!EffectiveDeliveryAllowance.IsAllowed(
                    orgOfferDelivery,
                    readyDeliveryBranchExists: true,
                    relationship.CustomerDeliveryOverride))
            {
                return ApplicationResult.Failure(
                    ErrorCode,
                    forBuyerMessage
                        ? "Delivery is disabled for this business relationship."
                        : "Delivery is blocked for this business customer.");
            }
        }

        if (intendedReceivingBranchId is null || intendedReceivingBranchId == Guid.Empty)
        {
            return ApplicationResult.Failure(
                ErrorCode,
                forBuyerMessage
                    ? "Receiving branch setup is incomplete."
                    : "Buyer receiving branch is required for this purchase order.");
        }

        return ApplicationResult.Success();
    }

    private sealed class NullOrganizationFulfillmentSettingsRepository : IOrganizationFulfillmentSettingsRepository
    {
        public Task<OrganizationFulfillmentSettings?> GetAsync(
            PosOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<OrganizationFulfillmentSettings?>(null);

        public Task AddAsync(
            OrganizationFulfillmentSettings settings,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task UpdateAsync(
            OrganizationFulfillmentSettings settings,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
