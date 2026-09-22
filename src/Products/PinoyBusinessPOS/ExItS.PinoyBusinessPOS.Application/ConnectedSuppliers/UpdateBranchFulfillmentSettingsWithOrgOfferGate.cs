using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Credit;
using ExItS.PinoyBusinessPOS.Application.Platform;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;

/// <summary>
/// Proxies Platform branch fulfillment updates after enforcing org Offer Delivery
/// for DeliveryEnabled OFF→ON transitions. Does not mutate branches when Offer Delivery turns OFF.
/// </summary>
public sealed class UpdateBranchFulfillmentSettingsWithOrgOfferGate(
    IOrganizationFulfillmentSettingsRepository fulfillmentSettings,
    IPlatformBranchFulfillmentGateway platform,
    IPosCommercialAccessAccessor access)
{
    public async Task<ApplicationResult<BranchFulfillmentReadinessDto>> ExecuteAsync(
        Guid organizationId,
        Guid branchId,
        UpdateBranchFulfillmentSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        // ViewSuppliers is enough to read Offer Delivery; Platform enforces branch-edit authority
        // on the proxied update.
        var gate = ConnectedSupplierUseCaseGuard.Access(access, UtangCapability.ViewSuppliers);
        if (!gate.IsSuccess)
        {
            return ConnectedSupplierUseCaseGuard.Failure<BranchFulfillmentReadinessDto>(
                gate.ErrorCode!, gate.ErrorMessage!);
        }

        if (request.DeliveryEnabled == true)
        {
            var current = await platform
                .GetReadinessAsync(organizationId, branchId, cancellationToken)
                .ConfigureAwait(false);
            if (!current.IsSuccess || current.Value is null)
            {
                return ApplicationResult<BranchFulfillmentReadinessDto>.Failure(
                    current.ErrorCode ?? ConnectedSupplierErrorCodes.NotFound,
                    current.ErrorMessage ?? "Branch fulfillment state was not found.");
            }

            var org = PosOrganizationId.From(organizationId);
            var settings = await fulfillmentSettings.GetAsync(org, cancellationToken).ConfigureAwait(false);
            var orgOffer = settings?.OfferDelivery == true;

            if (BranchDeliveryEnablement.IsEnableBlockedByOrgOffer(
                    orgOffer,
                    currentlyDeliveryEnabled: current.Value.DeliveryEnabled,
                    requestedDeliveryEnabled: true))
            {
                return ApplicationResult<BranchFulfillmentReadinessDto>.Failure(
                    ConnectedSupplierErrorCodes.OrganizationDeliveryNotOffered,
                    BranchDeliveryEnablement.OrganizationDeliveryNotOfferedMessage);
            }
        }

        var updated = await platform
            .UpdateSettingsAsync(organizationId, branchId, request, cancellationToken)
            .ConfigureAwait(false);
        if (!updated.IsSuccess || updated.Value is null)
        {
            return ApplicationResult<BranchFulfillmentReadinessDto>.Failure(
                updated.ErrorCode ?? "platform.branch_fulfillment_update_failed",
                updated.ErrorMessage ?? "Could not update branch fulfillment settings.");
        }

        return ApplicationResult<BranchFulfillmentReadinessDto>.Success(updated.Value);
    }
}
