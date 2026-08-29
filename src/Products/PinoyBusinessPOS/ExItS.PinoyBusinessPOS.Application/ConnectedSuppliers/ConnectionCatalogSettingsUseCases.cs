using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;

public sealed class GetConnectionCatalogSettings
{
    private readonly IConnectedSupplierRelationshipRepository _relationships;
    private readonly IConnectedBuyerProductShareRepository _shares;
    private readonly IPosCommercialAccessAccessor _access;

    public GetConnectionCatalogSettings(
        IConnectedSupplierRelationshipRepository relationships,
        IConnectedBuyerProductShareRepository shares,
        IPosCommercialAccessAccessor access)
    {
        _relationships = relationships;
        _shares = shares;
        _access = access;
    }

    public async Task<ApplicationResult<ConnectionCatalogSettingsDto>> ExecuteAsync(
        Guid orgId,
        Guid relationshipId,
        CancellationToken ct = default)
    {
        var gate = ConnectedSupplierUseCaseGuard.Access(_access, UtangCapability.ViewSuppliers);
        if (!gate.IsSuccess)
        {
            return ConnectedSupplierUseCaseGuard.Failure<ConnectionCatalogSettingsDto>(
                gate.ErrorCode!,
                gate.ErrorMessage!);
        }

        var r = await _relationships.GetAsync(ConnectedSupplierRelationshipId.From(relationshipId), ct)
            .ConfigureAwait(false);
        var supplier = PosOrganizationId.From(orgId);
        if (r is null || r.SupplierOrganizationId != supplier)
        {
            return ConnectedSupplierUseCaseGuard.Failure<ConnectionCatalogSettingsDto>(
                ConnectedSupplierErrorCodes.NotFound,
                "Relationship was not found.");
        }

        var page = await _shares.SearchForSupplierManagementAsync(
            r.Id,
            supplier,
            query: null,
            category: null,
            shareFilter: null,
            skip: 0,
            take: 1,
            idsOnly: true,
            ct,
            r.CatalogSharingMode).ConfigureAwait(false);

        var shareRows = await _shares.ListAsync(r.Id, ct).ConfigureAwait(false);
        var excludedCount = shareRows.Count(x => !x.IsShared);
        var overrideCount = shareRows.Count(x =>
            x.BuyerSpecificPoPrice is not null
            && ConnectedPoPricing.IsProductShared(r.CatalogSharingMode, x));

        var sharedCount = r.CatalogSharingMode == CatalogSharingMode.AllEligible
            ? Math.Max(0, page.EligibleCount - excludedCount)
            : page.SharedCount;

        return ApplicationResult<ConnectionCatalogSettingsDto>.Success(
            new ConnectionCatalogSettingsDto(
                r.Id.Value,
                r.CatalogSharingMode.ToString(),
                r.CustomerDiscountPercent,
                page.EligibleCount,
                sharedCount,
                excludedCount,
                overrideCount));
    }
}

public sealed class UpdateConnectionCatalogSettings
{
    private readonly IConnectedSupplierRelationshipRepository _relationships;
    private readonly ICatalogProductRepository _products;
    private readonly ISupplierProductExposureRepository _exposures;
    private readonly IPosUnitOfWork _uow;
    private readonly IPosCommercialAccessAccessor _access;
    private readonly TimeProvider _clock;

    public UpdateConnectionCatalogSettings(
        IConnectedSupplierRelationshipRepository relationships,
        IPosUnitOfWork uow,
        IPosCommercialAccessAccessor access,
        ICatalogProductRepository products,
        ISupplierProductExposureRepository exposures,
        TimeProvider? clock = null)
    {
        _relationships = relationships;
        _uow = uow;
        _access = access;
        _products = products;
        _exposures = exposures;
        _clock = clock ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<ConnectionCatalogSettingsDto>> ExecuteAsync(
        Guid orgId,
        Guid relationshipId,
        UpdateConnectionCatalogSettingsRequest request,
        GetConnectionCatalogSettings read,
        CancellationToken ct = default)
    {
        var gate = ConnectedSupplierUseCaseGuard.Access(_access, UtangCapability.ManageSuppliers);
        if (!gate.IsSuccess)
        {
            return ConnectedSupplierUseCaseGuard.Failure<ConnectionCatalogSettingsDto>(
                gate.ErrorCode!,
                gate.ErrorMessage!);
        }

        var r = await _relationships.GetAsync(ConnectedSupplierRelationshipId.From(relationshipId), ct)
            .ConfigureAwait(false);
        var supplier = PosOrganizationId.From(orgId);
        if (r is null || r.SupplierOrganizationId != supplier || r.Status != ConnectedSupplierRelationshipStatus.Active)
        {
            return ConnectedSupplierUseCaseGuard.Failure<ConnectionCatalogSettingsDto>(
                ConnectedSupplierErrorCodes.NotFound,
                "Active relationship was not found.");
        }

        var mode = RespondConnection.ParseCatalogSharingMode(request.CatalogSharingMode);
        if (mode is null)
        {
            return ConnectedSupplierUseCaseGuard.Failure<ConnectionCatalogSettingsDto>(
                ConnectedSupplierErrorCodes.BulkValidation,
                "Catalog sharing mode must be SelectedOnly or AllEligible.");
        }

        if (mode == CatalogSharingMode.AllEligible
            && r.CatalogSharingMode != CatalogSharingMode.AllEligible
            && !request.ConfirmModeChange)
        {
            return ConnectedSupplierUseCaseGuard.Failure<ConnectionCatalogSettingsDto>(
                ConnectedSupplierErrorCodes.BulkValidation,
                "Confirm to make all eligible products available to this customer except exclusions.");
        }

        try
        {
            r.ConfigureCatalogSharing(mode.Value, request.CustomerDiscountPercent, _clock.GetUtcNow());
            if (mode == CatalogSharingMode.AllEligible)
            {
                await AllEligibleCatalogBootstrap.EnsureExposuresFromSellingPriceAsync(
                        supplier,
                        _products,
                        _exposures,
                        _clock.GetUtcNow(),
                        ct)
                    .ConfigureAwait(false);
            }

            await _relationships.UpdateAsync(r, ct).ConfigureAwait(false);
            await _uow.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            return ConnectedSupplierUseCaseGuard.Failure<ConnectionCatalogSettingsDto>(ex.ErrorCode, ex.Message);
        }

        return await read.ExecuteAsync(orgId, relationshipId, ct).ConfigureAwait(false);
    }
}
