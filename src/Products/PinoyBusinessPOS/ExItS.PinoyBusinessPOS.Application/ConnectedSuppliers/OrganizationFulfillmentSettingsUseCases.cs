using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Credit;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;

public sealed record OrganizationFulfillmentSettingsDto(
    Guid OrganizationId,
    bool OfferDelivery);

public sealed record UpdateOrganizationOfferDeliveryRequest(bool OfferDelivery);

public sealed class GetOrganizationFulfillmentSettings(
    IOrganizationFulfillmentSettingsRepository settings,
    IPosCommercialAccessAccessor access)
{
    public async Task<ApplicationResult<OrganizationFulfillmentSettingsDto>> ExecuteAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        var gate = ConnectedSupplierUseCaseGuard.Access(access, UtangCapability.ViewSuppliers);
        if (!gate.IsSuccess)
        {
            return ConnectedSupplierUseCaseGuard.Failure<OrganizationFulfillmentSettingsDto>(
                gate.ErrorCode!, gate.ErrorMessage!);
        }

        var org = PosOrganizationId.From(organizationId);
        var row = await settings.GetAsync(org, cancellationToken).ConfigureAwait(false);
        return ApplicationResult<OrganizationFulfillmentSettingsDto>.Success(
            new OrganizationFulfillmentSettingsDto(organizationId, row?.OfferDelivery == true));
    }
}

public sealed class UpdateOrganizationOfferDelivery(
    IOrganizationFulfillmentSettingsRepository settings,
    IPosUnitOfWork unitOfWork,
    IPosCommercialAccessAccessor access,
    TimeProvider? clock = null)
{
    private readonly TimeProvider _clock = clock ?? TimeProvider.System;

    public async Task<ApplicationResult<OrganizationFulfillmentSettingsDto>> ExecuteAsync(
        Guid organizationId,
        UpdateOrganizationOfferDeliveryRequest request,
        CancellationToken cancellationToken = default)
    {
        var gate = ConnectedSupplierUseCaseGuard.Access(access, UtangCapability.ManageSuppliers);
        if (!gate.IsSuccess)
        {
            return ConnectedSupplierUseCaseGuard.Failure<OrganizationFulfillmentSettingsDto>(
                gate.ErrorCode!, gate.ErrorMessage!);
        }

        var org = PosOrganizationId.From(organizationId);
        var now = _clock.GetUtcNow();
        var row = await settings.GetAsync(org, cancellationToken).ConfigureAwait(false);
        if (row is null)
        {
            row = OrganizationFulfillmentSettings.CreateDefault(org, now);
            row.SetOfferDelivery(request.OfferDelivery, now);
            await settings.AddAsync(row, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            row.SetOfferDelivery(request.OfferDelivery, now);
            await settings.UpdateAsync(row, cancellationToken).ConfigureAwait(false);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return ApplicationResult<OrganizationFulfillmentSettingsDto>.Success(
            new OrganizationFulfillmentSettingsDto(organizationId, row.OfferDelivery));
    }
}

public sealed record UpdateBusinessCustomerDeliveryAllowanceRequest(
    /// <summary>true = inherit/allow, false = block.</summary>
    bool AllowDelivery,
    DateTimeOffset? ExpectedUpdatedAtUtc = null);

public sealed class UpdateBusinessCustomerDeliveryAllowance(
    IConnectedSupplierRelationshipRepository relationships,
    IPosUnitOfWork unitOfWork,
    IPosCommercialAccessAccessor access,
    TimeProvider? clock = null)
{
    private readonly TimeProvider _clock = clock ?? TimeProvider.System;

    public async Task<ApplicationResult<BusinessCustomerDto>> ExecuteAsync(
        Guid orgId,
        Guid connectionId,
        UpdateBusinessCustomerDeliveryAllowanceRequest request,
        CancellationToken cancellationToken = default)
    {
        var gate = ConnectedSupplierUseCaseGuard.Access(access, UtangCapability.ManageSuppliers);
        if (!gate.IsSuccess)
        {
            return ConnectedSupplierUseCaseGuard.Failure<BusinessCustomerDto>(
                gate.ErrorCode!, gate.ErrorMessage!);
        }

        var relationship = await relationships
            .GetAsync(ConnectedSupplierRelationshipId.From(connectionId), cancellationToken)
            .ConfigureAwait(false);
        var supplier = PosOrganizationId.From(orgId);
        if (relationship is null || relationship.SupplierOrganizationId != supplier)
        {
            return ConnectedSupplierUseCaseGuard.Failure<BusinessCustomerDto>(
                ConnectedSupplierErrorCodes.NotFound,
                "Business customer relationship was not found.");
        }

        if (request.ExpectedUpdatedAtUtc is DateTimeOffset expected
            && relationship.UpdatedAtUtc != expected)
        {
            return ConnectedSupplierUseCaseGuard.Failure<BusinessCustomerDto>(
                ConnectedSupplierErrorCodes.ConcurrencyConflict,
                "Business customer was updated by someone else. Refresh and try again.");
        }

        var overrideValue = request.AllowDelivery
            ? CustomerDeliveryOverride.Inherit
            : CustomerDeliveryOverride.Block;
        try
        {
            relationship.SetCustomerDeliveryOverride(overrideValue, _clock.GetUtcNow());
        }
        catch (Domain.Common.DomainException ex)
        {
            return ConnectedSupplierUseCaseGuard.Failure<BusinessCustomerDto>(ex.ErrorCode, ex.Message);
        }

        await relationships.UpdateAsync(relationship, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Minimal DTO projection for response — caller may refetch detail.
        return ApplicationResult<BusinessCustomerDto>.Success(
            ListBusinessCustomers.Map(
                relationship,
                eligibleCount: 0,
                new BuyerRelationshipShareStats(0, 0, 0),
                displayNameIsLive: false,
                organizationMemberAvailable: null));
    }
}
