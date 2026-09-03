using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Identity;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Application.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Permissions;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.Suppliers;

namespace ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;

public static class ConnectedSupplierErrorCodes
{
    public const string NotFound = "pos.connected_supplier.not_found";
    public const string DuplicateRelationship = "pos.connected_supplier.relationship.duplicate";
    public const string RelationshipInactive = "pos.connected_supplier.relationship.inactive";
    public const string CancelNotPending = "pos.connected_supplier.cancellation.not_pending";
    public const string ExposureNotFound = "pos.connected_supplier.exposure.not_found";
    public const string LinkNotFound = "pos.connected_supplier.link.not_found";
    public const string IncomingOrderNotFound = "pos.connected_supplier.incoming_order.not_found";
    public const string OrganizationMismatch = "pos.connected_supplier.organization_mismatch";
    public const string BulkValidation = "pos.connected_supplier.buyer_share.bulk_validation";
    public const string ProductBlocked = "pos.connected_supplier.buyer_share.product_blocked";
    public const string MissingDefaultPo = "pos.connected_supplier.buyer_share.missing_default_po";
    public const string BranchResponseForbidden = "pos.connected_supplier.branch_response_forbidden";
    /// <summary>Caller attempted to read supplier requests for a branch they cannot access.</summary>
    public const string BranchReadForbidden = "pos.connected_supplier.branch_read_forbidden";
}

public sealed record ConnectedSupplierRelationshipDto(
    Guid RelationshipId,
    Guid BuyerOrganizationId,
    Guid SupplierOrganizationId,
    string Status,
    DateTimeOffset RequestedAtUtc,
    Guid? RequestedByUserId,
    DateTimeOffset? RespondedAtUtc,
    Guid? RespondedByUserId,
    DateTimeOffset? DisconnectedAtUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    string? CounterpartyDisplayName = null,
    string? CounterpartyPublicOrganizationId = null,
    string CatalogSharingMode = "SelectedOnly",
    decimal? CustomerDiscountPercent = null,
    Guid? SupplierBranchId = null,
    string? SupplierBranchName = null);
public sealed record RequestConnectionRequest(
    Guid? SupplierOrganizationId = null,
    string? SupplierPublicOrganizationIdOrQrPayload = null,
    Guid? RequestedByUserId = null,
    Guid? SupplierBranchId = null);
public sealed record UpdateSupplierLocationRequest(Guid SupplierBranchId);
public sealed record RespondConnectionRequest(
    Guid? RespondedByUserId = null,
    string? CatalogSharingMode = null,
    decimal? CustomerDiscountPercent = null,
    bool ConfirmCatalogSharing = false);
public sealed record CancelConnectionRequest(Guid? CancelledByUserId = null);
public sealed record ConnectionCatalogSettingsDto(
    Guid RelationshipId,
    string CatalogSharingMode,
    decimal? CustomerDiscountPercent,
    int EligibleCount,
    int SharedCount,
    int ExcludedCount,
    int OverrideCount);
public sealed record UpdateConnectionCatalogSettingsRequest(
    string CatalogSharingMode,
    decimal? CustomerDiscountPercent = null,
    bool ConfirmModeChange = false);
public sealed record SupplierProductExposureDto(Guid ExposureId, Guid SupplierOrganizationId, Guid ProductId, string? SkuSnapshot,
    string NameSnapshot, string? CategoryNameSnapshot, string UnitOfMeasureCode, decimal SupplierOrderPrice,
    bool IsOrderable, bool IsExposed, long SyncVersion, DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc,
    decimal? EffectiveSupplierOrderPrice = null);
public sealed record ExposeProductRequest(Guid ProductId, decimal SupplierOrderPrice, bool IsOrderable = true);
public sealed record UpdateExposureRequest(decimal SupplierOrderPrice, bool IsOrderable, bool IsExposed);
public sealed record ConnectedBuyerProductShareDto(Guid ShareId, Guid RelationshipId, Guid BuyerOrganizationId,
    Guid SupplierOrganizationId, Guid SupplierProductId, bool IsShared, decimal? BuyerSpecificPoPrice,
    decimal? EffectiveSupplierOrderPrice, long SyncVersion, DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc,
    string? SkuSnapshot = null, string? NameSnapshot = null, string? UnitOfMeasureCode = null, decimal? SellingPrice = null,
    string? CategoryNameSnapshot = null, decimal? DefaultPoPrice = null, bool IsBlockedFromConnectedBuyers = false);
public sealed record SetBuyerProductShareItem(
    Guid SupplierProductId,
    bool IsShared,
    decimal? BuyerSpecificPoPrice = null,
    decimal? EstablishDefaultPoPrice = null);
public sealed record SetBuyerProductSharesRequest(IReadOnlyList<SetBuyerProductShareItem> Products);
public sealed record ConfirmBuyerProductSharingRequest(
    IReadOnlyList<Guid> ProductIds,
    IReadOnlyDictionary<Guid, decimal>? EstablishDefaultPoPrices = null);
public sealed record BuyerSupplierProductLinkDto(Guid LinkId, Guid RelationshipId, Guid BuyerOrganizationId,
    Guid SupplierOrganizationId, Guid BuyerProductId, Guid SupplierProductId, string? SupplierSkuSnapshot,
    string SupplierNameSnapshot, string UnitOfMeasureCode, decimal LastKnownOrderPrice, bool IsActive,
    long SyncVersion, DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc,
    Guid? BuyerPurchaseUnitId = null, decimal MultiplierToBase = 1m, string? PackageLabel = null);
public sealed record LinkProductRequest(
    Guid BuyerProductId,
    Guid ExposureId,
    Guid? BuyerPurchaseUnitId = null,
    decimal? MultiplierToBase = null,
    string? PackageLabel = null,
    Guid? PurchaseOrderId = null);
public sealed record LinkedProductsDeltaDto(IReadOnlyList<BuyerSupplierProductLinkDto> Changed, IReadOnlyList<Guid> RemovedIds, long Cursor);
public sealed record ConnectedPurchaseOrderLineDto(
    Guid ProductId,
    string NameSnapshot,
    string? SkuSnapshot,
    decimal Qty,
    decimal UnitPriceSnapshot,
    decimal LineTotal,
    string UnitOfMeasureCode,
    decimal? ProposedQty = null,
    decimal? ConfirmedQty = null,
    string Availability = "Pending",
    decimal ProposedLineTotal = 0m,
    decimal ConfirmedLineTotal = 0m);
public sealed record ConnectedPurchaseOrderDto(
    Guid ConnectedPurchaseOrderId,
    Guid RelationshipId,
    Guid BuyerOrganizationId,
    Guid SupplierOrganizationId,
    Guid BuyerPurchaseOrderId,
    string? BuyerPoNumber,
    DateOnly OrderDate,
    string? Notes,
    string Status,
    decimal TotalAmount,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? AcceptedAtUtc,
    DateTimeOffset? DeclinedAtUtc,
    IReadOnlyList<ConnectedPurchaseOrderLineDto> Lines,
    DateTimeOffset? PreparingAtUtc = null,
    DateTimeOffset? FulfilledAtUtc = null,
    DateTimeOffset? WithdrawnAtUtc = null,
    string? DeclineReason = null,
    string? DeclineNote = null,
    string DisplayStatus = "",
    string? BuyerDisplayName = null,
    string? BuyerReceivingStatus = null,
    string PaymentTerm = "Cash",
    string PaymentTermLabel = "Cash",
    decimal ProposedTotalAmount = 0m,
    decimal ConfirmedTotalAmount = 0m,
    DateTimeOffset? ChangesProposedAtUtc = null,
    DateTimeOffset? BuyerRespondedAtUtc = null,
    Guid? SupplierBranchId = null,
    string? SupplierBranchName = null);
public sealed record DeclineIncomingOrderRequest(string? DeclineReason = null, string? DeclineNote = null);
public sealed record ProposeIncomingOrderLineRequest(Guid ProductId, decimal ProposedQty, bool Unavailable = false);
public sealed record ProposeIncomingOrderChangesRequest(IReadOnlyList<ProposeIncomingOrderLineRequest> Lines);
public sealed record DraftReviewLineRequest(Guid SupplierProductId, decimal UnitPriceSnapshot);
public sealed record RevalidateConnectedPoDraftRequest(IReadOnlyList<DraftReviewLineRequest> Lines);
public enum ConnectedPoDraftReviewStatus { Unchanged, PriceChanged, Unavailable, RelationshipInactive }
public sealed record ConnectedPoDraftReviewItem(Guid SupplierProductId, ConnectedPoDraftReviewStatus Status,
    decimal SubmittedPrice, decimal? CurrentPrice);
public sealed record ConnectedPoDraftReviewDto(ConnectedPoDraftReviewStatus OverallStatus, IReadOnlyList<ConnectedPoDraftReviewItem> Items);

public static class ConnectedSupplierMapper
{
    public static ConnectedSupplierRelationshipDto Map(ConnectedSupplierRelationship x, bool supplierView = false) => new(
        x.Id.Value,
        x.BuyerOrganizationId.Value,
        x.SupplierOrganizationId.Value,
        x.Status.ToString(),
        x.RequestedAtUtc,
        x.RequestedByUserId,
        x.RespondedAtUtc,
        x.RespondedByUserId,
        x.DisconnectedAtUtc,
        x.CreatedAtUtc,
        x.UpdatedAtUtc,
        CounterpartyDisplayName: supplierView ? x.BuyerDisplayNameSnapshot : x.SupplierDisplayNameSnapshot,
        CounterpartyPublicOrganizationId: supplierView
            ? x.BuyerPublicOrganizationIdSnapshot
            : x.SupplierPublicOrganizationIdSnapshot,
        CatalogSharingMode: x.CatalogSharingMode.ToString(),
        CustomerDiscountPercent: x.CustomerDiscountPercent,
        SupplierBranchId: x.SupplierBranchId,
        SupplierBranchName: x.SupplierBranchNameSnapshot);
    public static SupplierProductExposureDto Map(SupplierProductExposure x) => new(x.Id.Value,x.SupplierOrganizationId.Value,
        x.ProductId.Value,x.SkuSnapshot,x.NameSnapshot,x.CategoryNameSnapshot,x.UnitOfMeasureCode,x.SupplierOrderPrice,
        x.IsOrderable,x.IsExposed,x.SyncVersion,x.CreatedAtUtc,x.UpdatedAtUtc);
    public static SupplierProductExposureDto Map(SupplierProductExposure x, decimal effectivePrice) =>
        Map(x) with { SupplierOrderPrice = effectivePrice, EffectiveSupplierOrderPrice = effectivePrice };
    public static ConnectedBuyerProductShareDto Map(ConnectedBuyerProductShare x, SupplierProductExposure? exposure = null,
        CatalogProduct? product = null, string? categoryName = null) =>
        MapForManagement(
            CatalogSharingMode.SelectedOnly,
            customerDiscountPercent: null,
            x.RelationshipId.Value,
            x.BuyerOrganizationId.Value,
            x.SupplierOrganizationId.Value,
            product,
            x,
            exposure,
            categoryName,
            shareId: x.Id.Value,
            createdAtUtc: x.CreatedAtUtc,
            updatedAtUtc: x.UpdatedAtUtc);

    public static ConnectedBuyerProductShareDto MapUnshared(
        ConnectedSupplierRelationship relationship,
        PosOrganizationId supplier,
        CatalogProduct product,
        SupplierProductExposure? exposure,
        string? categoryName) =>
        MapForManagement(
            relationship.CatalogSharingMode,
            relationship.CustomerDiscountPercent,
            relationship.Id.Value,
            relationship.BuyerOrganizationId.Value,
            supplier.Value,
            product,
            share: null,
            exposure,
            categoryName,
            shareId: Guid.Empty,
            createdAtUtc: product.CreatedAtUtc,
            updatedAtUtc: product.UpdatedAtUtc);

    public static ConnectedBuyerProductShareDto MapForManagement(
        ConnectedSupplierRelationship relationship,
        CatalogProduct product,
        ConnectedBuyerProductShare? share,
        SupplierProductExposure? exposure,
        string? categoryName) =>
        MapForManagement(
            relationship.CatalogSharingMode,
            relationship.CustomerDiscountPercent,
            relationship.Id.Value,
            relationship.BuyerOrganizationId.Value,
            relationship.SupplierOrganizationId.Value,
            product,
            share,
            exposure,
            categoryName,
            shareId: share?.Id.Value ?? Guid.Empty,
            createdAtUtc: share?.CreatedAtUtc ?? product.CreatedAtUtc,
            updatedAtUtc: share?.UpdatedAtUtc ?? product.UpdatedAtUtc);

    private static ConnectedBuyerProductShareDto MapForManagement(
        CatalogSharingMode mode,
        decimal? customerDiscountPercent,
        Guid relationshipId,
        Guid buyerOrganizationId,
        Guid supplierOrganizationId,
        CatalogProduct? product,
        ConnectedBuyerProductShare? share,
        SupplierProductExposure? exposure,
        string? categoryName,
        Guid shareId,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        var isShared = product?.IsBlockedFromConnectedBuyers != true
            && product?.CanBeSold == true
            && ConnectedPoPricing.IsProductShared(mode, share);

        decimal? effective = null;
        var selling = product?.SellingPrice;
        if (isShared)
        {
            if (exposure is not null
                && ConnectedPoPricing.TryResolveEffectivePrice(
                    exposure,
                    share,
                    mode,
                    customerDiscountPercent,
                    selling,
                    out var fromExposure,
                    out _))
            {
                effective = fromExposure;
            }
            else if (selling is > 0m)
            {
                // AllEligible display path when Default PO / exposure is not staged yet.
                var baseline = selling.Value;
                effective = customerDiscountPercent is decimal d && d > 0m
                    ? ConnectedPoPricing.RoundMoney(baseline * (1m - (d / 100m)))
                    : ConnectedPoPricing.RoundMoney(baseline);
            }
            else if (share?.BuyerSpecificPoPrice is decimal overridePrice)
            {
                effective = ConnectedPoPricing.RoundMoney(overridePrice);
            }
        }

        var category = !string.IsNullOrWhiteSpace(categoryName)
            ? categoryName
            : exposure?.CategoryNameSnapshot;
        var defaultPo = product?.DefaultConnectedPoPrice
            ?? (exposure is { IsExposed: true } ? exposure.SupplierOrderPrice : null);
        var productId = product?.Id.Value
            ?? share?.SupplierProductId.Value
            ?? exposure?.ProductId.Value
            ?? Guid.Empty;

        return new(
            shareId,
            relationshipId,
            buyerOrganizationId,
            supplierOrganizationId,
            productId,
            isShared,
            share?.BuyerSpecificPoPrice,
            effective,
            share?.SyncVersion ?? 0,
            createdAtUtc,
            updatedAtUtc,
            product?.Sku ?? exposure?.SkuSnapshot,
            product?.Name ?? exposure?.NameSnapshot,
            product is not null ? product.UnitOfMeasure.ToString() : exposure?.UnitOfMeasureCode,
            selling,
            category,
            defaultPo,
            product?.IsBlockedFromConnectedBuyers ?? false);
    }
    public static BuyerSupplierProductLinkDto Map(BuyerSupplierProductLink x) => new(x.Id.Value,x.RelationshipId.Value,
        x.BuyerOrganizationId.Value,x.SupplierOrganizationId.Value,x.BuyerProductId.Value,x.SupplierProductId.Value,
        x.SupplierSkuSnapshot,x.SupplierNameSnapshot,x.UnitOfMeasureCode,x.LastKnownOrderPrice,x.IsActive,x.SyncVersion,x.CreatedAtUtc,x.UpdatedAtUtc,
        x.BuyerPurchaseUnitId,x.MultiplierToBase,x.PackageLabel);
    public static ConnectedPurchaseOrderDto Map(
        ConnectedPurchaseOrder x,
        string? buyerDisplayName = null,
        string? buyerReceivingStatus = null,
        Guid? supplierBranchId = null,
        string? supplierBranchName = null) => new(
        x.Id.Value,
        x.RelationshipId.Value,
        x.BuyerOrganizationId.Value,
        x.SupplierOrganizationId.Value,
        x.BuyerPurchaseOrderId.Value,
        x.BuyerPoNumber,
        x.OrderDate,
        x.Notes,
        x.Status.ToString(),
        x.TotalAmount,
        x.CreatedAtUtc,
        x.UpdatedAtUtc,
        x.AcceptedAtUtc,
        x.DeclinedAtUtc,
        x.Lines.Select(MapLine).ToList(),
        x.PreparingAtUtc,
        x.FulfilledAtUtc,
        x.WithdrawnAtUtc,
        x.DeclineReason?.ToString(),
        x.DeclineNote,
        ConnectedPoDisplayStatus.ForSupplier(x),
        buyerDisplayName,
        buyerReceivingStatus,
        ConnectedPoPaymentTerms.ToApi(x.PaymentTerm),
        ConnectedPoPaymentTerms.ToUiLabel(x.PaymentTerm),
        x.ProposedTotalAmount,
        x.ConfirmedTotalAmount,
        x.ChangesProposedAtUtc,
        x.BuyerRespondedAtUtc,
        supplierBranchId,
        supplierBranchName);

    public static ConnectedPurchaseOrderLineDto MapLine(ConnectedPurchaseOrderLine l) => new(
        l.ProductId.Value,
        l.NameSnapshot,
        l.SkuSnapshot,
        l.Qty,
        l.UnitPriceSnapshot,
        l.LineTotal,
        l.UnitOfMeasureCode,
        l.ProposedQty,
        l.ConfirmedQty,
        l.Availability.ToString(),
        l.ProposedLineTotal,
        l.ConfirmedLineTotal);
}

internal static class ConnectedSupplierUseCaseGuard
{
    public static ApplicationResult Access(IPosCommercialAccessAccessor access, UtangCapability capability) =>
        CommercialAccessGuard.Require(access, capability);
    public static bool BelongsTo(ConnectedSupplierRelationship r, PosOrganizationId org) =>
        r.BuyerOrganizationId == org || r.SupplierOrganizationId == org;
    public static ApplicationResult<T> Failure<T>(string code,string message) => ApplicationResult<T>.Failure(code,message);
}

public sealed class RequestConnection
{
    private readonly IConnectedSupplierRelationshipRepository _relationships;
    private readonly ISupplierRepository _suppliers;
    private readonly IPosUnitOfWork _uow;
    private readonly IPosCommercialAccessAccessor _access;
    private readonly IPlatformOrganizationPublicResolve _organizationResolve;
    private readonly IPlatformSupplierLocationDirectory _supplierLocations;
    private readonly IOrganizationBusinessNotificationPublisher _notifications;
    private readonly TimeProvider _clock;

    public RequestConnection(
        IConnectedSupplierRelationshipRepository relationships,
        ISupplierRepository suppliers,
        IPosUnitOfWork uow,
        IPosCommercialAccessAccessor access,
        IPlatformOrganizationPublicResolve organizationResolve,
        IPlatformSupplierLocationDirectory supplierLocations,
        IOrganizationBusinessNotificationPublisher? notifications = null,
        TimeProvider? clock = null)
    {
        _relationships = relationships;
        _suppliers = suppliers;
        _uow = uow;
        _access = access;
        _organizationResolve = organizationResolve;
        _supplierLocations = supplierLocations;
        _notifications = notifications ?? new NoOpOrganizationBusinessNotificationPublisher();
        _clock = clock ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<ConnectedSupplierRelationshipDto>> ExecuteAsync(
        Guid organizationId,
        RequestConnectionRequest request,
        CancellationToken ct = default)
    {
        var gate = ConnectedSupplierUseCaseGuard.Access(_access, UtangCapability.ManageSuppliers);
        if (!gate.IsSuccess)
        {
            return ConnectedSupplierUseCaseGuard.Failure<ConnectedSupplierRelationshipDto>(
                gate.ErrorCode!,
                gate.ErrorMessage!);
        }

        try
        {
            var resolvedSupplier = await ResolveSupplierOrganizationAsync(request, ct).ConfigureAwait(false);
            if (!resolvedSupplier.IsSuccess)
            {
                return ConnectedSupplierUseCaseGuard.Failure<ConnectedSupplierRelationshipDto>(
                    resolvedSupplier.ErrorCode!,
                    resolvedSupplier.ErrorMessage!);
            }

            var buyer = PosOrganizationId.From(organizationId);
            var supplier = PosOrganizationId.From(resolvedSupplier.Value!.OrganizationId);
            if (buyer == supplier)
            {
                return ConnectedSupplierUseCaseGuard.Failure<ConnectedSupplierRelationshipDto>(
                    ConnectedSupplierDomainErrorCodes.SelfConnection,
                    "You can't connect your business to itself.");
            }

            if (await _relationships.FindOpenAsync(buyer, supplier, ct).ConfigureAwait(false) is { } existing)
            {
                var message = existing.Status == ConnectedSupplierRelationshipStatus.Active
                    ? "Your businesses are already connected."
                    : "A connection request has already been sent to this supplier.";
                return ConnectedSupplierUseCaseGuard.Failure<ConnectedSupplierRelationshipDto>(
                    ConnectedSupplierErrorCodes.DuplicateRelationship,
                    message);
            }

            var location = await ResolveSupplierLocationAsync(
                    resolvedSupplier.Value.PublicOrganizationId,
                    request.SupplierBranchId,
                    ct)
                .ConfigureAwait(false);
            if (!location.IsSuccess)
            {
                return ConnectedSupplierUseCaseGuard.Failure<ConnectedSupplierRelationshipDto>(
                    location.ErrorCode!,
                    location.ErrorMessage!);
            }

            var utcNow = _clock.GetUtcNow();
            string? buyerDisplayName = null;
            string? buyerPublicId = null;
            var buyerIdentity = await _organizationResolve
                .GetOrganizationPublicIdentityAsync(organizationId, ct)
                .ConfigureAwait(false);
            if (buyerIdentity.IsSuccess && buyerIdentity.Value is not null)
            {
                buyerDisplayName = buyerIdentity.Value.DisplayName;
                buyerPublicId = buyerIdentity.Value.PublicOrganizationId;
            }

            var relationship = ConnectedSupplierRelationship.Request(
                buyer,
                supplier,
                utcNow,
                request.RequestedByUserId,
                buyerDisplayName: buyerDisplayName,
                buyerPublicOrganizationId: buyerPublicId,
                supplierDisplayName: resolvedSupplier.Value.DisplayName,
                supplierPublicOrganizationId: resolvedSupplier.Value.PublicOrganizationId,
                supplierBranchId: location.Value!.BranchId,
                supplierBranchName: location.Value.Name);
            await _relationships.AddAsync(relationship, ct).ConfigureAwait(false);

            // Buyer-side Supplier master so Pending/Active relationships appear on Suppliers list.
            var supplierName = string.IsNullOrWhiteSpace(resolvedSupplier.Value.DisplayName)
                ? resolvedSupplier.Value.PublicOrganizationId
                : resolvedSupplier.Value.DisplayName;
            var normalizedName = Supplier.Normalize(Supplier.NormalizeName(supplierName));
            var nameConflict = await _suppliers
                .FindActiveByNormalizedNameAsync(buyer, normalizedName, ct)
                .ConfigureAwait(false);
            if (nameConflict is not null)
            {
                supplierName = $"{supplierName} ({resolvedSupplier.Value.PublicOrganizationId})";
            }

            var code = await _suppliers.AllocateNextSupplierCodeAsync(buyer, ct).ConfigureAwait(false);
            var buyerSupplier = Supplier.Create(
                buyer,
                code,
                supplierName,
                utcNow,
                notes: resolvedSupplier.Value.PublicOrganizationId);
            buyerSupplier.AttachConnectedRelationship(relationship.Id, utcNow);
            await _suppliers.AddAsync(buyerSupplier, ct).ConfigureAwait(false);

            await _uow.SaveChangesAsync(ct).ConfigureAwait(false);

            var buyerName = string.IsNullOrWhiteSpace(buyerDisplayName)
                ? (buyerPublicId ?? "A business")
                : buyerDisplayName;
            var locationLabel = string.IsNullOrWhiteSpace(location.Value!.Name)
                ? null
                : location.Value.Name.Trim();
            var preview = locationLabel is null
                ? $"{buyerName} wants to connect with your business as a supplier."
                : $"{buyerName} wants to connect\nLocation: {locationLabel}";
            await _notifications.PublishAsync(
                organizationId,
                supplier.Value,
                SupplierConnectionNotificationTypes.Requested,
                relationship.Id.Value.ToString("D"),
                "Supplier connection request",
                preview,
                ct,
                targetBranchId: location.Value.BranchId).ConfigureAwait(false);

            return ApplicationResult<ConnectedSupplierRelationshipDto>.Success(
                ConnectedSupplierMapper.Map(relationship, supplierView: false));
        }
        catch (DomainException ex)
        {
            return ConnectedSupplierUseCaseGuard.Failure<ConnectedSupplierRelationshipDto>(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ConnectedSupplierUseCaseGuard.Failure<ConnectedSupplierRelationshipDto>(ex.ErrorCode, ex.Message);
        }
    }

    private async Task<ApplicationResult<PlatformSupplierLocationDto>> ResolveSupplierLocationAsync(
        string publicOrganizationId,
        Guid? requestedBranchId,
        CancellationToken ct)
    {
        var listed = await _supplierLocations
            .ListActiveLocationsAsync(publicOrganizationId, ct)
            .ConfigureAwait(false);
        if (!listed.IsSuccess)
        {
            return ApplicationResult<PlatformSupplierLocationDto>.Failure(
                listed.ErrorCode!,
                listed.ErrorMessage!);
        }

        var active = listed.Value ?? [];
        if (active.Count == 0)
        {
            return ApplicationResult<PlatformSupplierLocationDto>.Failure(
                DomainErrorCodes.ConnectedSupplierBranchInvalid,
                "This supplier has no active locations to connect.");
        }

        if (requestedBranchId is Guid branchId && branchId != Guid.Empty)
        {
            var match = active.FirstOrDefault(x => x.BranchId == branchId);
            if (match is null)
            {
                return ApplicationResult<PlatformSupplierLocationDto>.Failure(
                    DomainErrorCodes.ConnectedSupplierBranchInvalid,
                    "That supplier location is not an active branch of this business.");
            }

            return ApplicationResult<PlatformSupplierLocationDto>.Success(match);
        }

        if (active.Count == 1)
        {
            return ApplicationResult<PlatformSupplierLocationDto>.Success(active[0]);
        }

        return ApplicationResult<PlatformSupplierLocationDto>.Failure(
            DomainErrorCodes.ConnectedSupplierBranchRequired,
            "Choose which supplier location supplies you.");
    }

    private async Task<ApplicationResult<PlatformOrganizationPublicResolveResult>> ResolveSupplierOrganizationAsync(
        RequestConnectionRequest request,
        CancellationToken ct)
    {
        var payload = request.SupplierPublicOrganizationIdOrQrPayload?.Trim();
        if (string.IsNullOrWhiteSpace(payload))
        {
            // Guid alone is no longer accepted for new connected-supplier links — Business QR / public ID required.
            return ApplicationResult<PlatformOrganizationPublicResolveResult>.Failure(
                DomainErrorCodes.ConnectedSupplierRequiresBusinessQr,
                "Scan or enter the supplier Business QR / organization ID (ORG######). A Guid alone is not accepted.");
        }

        var purposeReject = TryRejectNonBusinessPayload(payload);
        if (purposeReject is not null)
        {
            return purposeReject;
        }

        var resolved = await _organizationResolve
            .ResolveOrganizationForConnectedSupplierAsync(payload, ct)
            .ConfigureAwait(false);
        if (resolved.IsSuccess && resolved.Value is not null)
        {
            if (request.SupplierOrganizationId is Guid suppliedGuid
                && suppliedGuid != Guid.Empty
                && suppliedGuid != resolved.Value.OrganizationId)
            {
                return ApplicationResult<PlatformOrganizationPublicResolveResult>.Failure(
                    ConnectedSupplierErrorCodes.OrganizationMismatch,
                    "The scanned Business QR does not match the supplier organization id that was provided.");
            }

            return resolved;
        }

        // React resolves via /platform-api (cookie session) then posts both ids to POS.
        // POS→Platform may still lack the HttpOnly cookie; accept the client-resolved pair when
        // Platform auth failed and the payload is a public organization id.
        if (request.SupplierOrganizationId is Guid clientOrgId
            && clientOrgId != Guid.Empty
            && LooksLikePublicOrganizationId(payload)
            && IsPlatformSessionFailure(resolved))
        {
            var publicId = payload.Trim().ToUpperInvariant();
            return ApplicationResult<PlatformOrganizationPublicResolveResult>.Success(
                new PlatformOrganizationPublicResolveResult(clientOrgId, publicId, publicId));
        }

        return resolved;
    }

    private static bool LooksLikePublicOrganizationId(string payload)
    {
        var trimmed = payload.Trim();
        if (trimmed.Length != 9 || !trimmed.StartsWith("ORG", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        for (var i = 3; i < trimmed.Length; i++)
        {
            if (!char.IsDigit(trimmed[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsPlatformSessionFailure(
        ApplicationResult<PlatformOrganizationPublicResolveResult> resolved)
    {
        var message = resolved.ErrorMessage ?? string.Empty;
        return message.Contains("401", StringComparison.OrdinalIgnoreCase)
               || message.Contains("Unauthorized", StringComparison.OrdinalIgnoreCase)
               || message.Contains("Platform sign-in", StringComparison.OrdinalIgnoreCase)
               || message.Contains("session is missing", StringComparison.OrdinalIgnoreCase);
    }

    private static ApplicationResult<PlatformOrganizationPublicResolveResult>? TryRejectNonBusinessPayload(
        string payload)
    {
        var trimmed = payload.Trim();
        if (trimmed.StartsWith("exits://qr/v1/personal", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("exits://user/v1/", StringComparison.OrdinalIgnoreCase))
        {
            return ApplicationResult<PlatformOrganizationPublicResolveResult>.Failure(
                DomainErrorCodes.ConnectedSupplierRequiresBusinessQr,
                "Connected suppliers require a Business QR, not a Personal QR.");
        }

        if (trimmed.StartsWith("exits://qr/v1/pos-device-registration", StringComparison.OrdinalIgnoreCase))
        {
            return ApplicationResult<PlatformOrganizationPublicResolveResult>.Failure(
                DomainErrorCodes.ConnectedSupplierQrPurposeMismatch,
                "This is a device registration code. Scan the supplier's Business QR instead.");
        }

        if (ExItsQrPurposeGuard.TryParsePurpose(trimmed, out var purpose, out _))
        {
            if (string.Equals(purpose, ExItsQrPurposeGuard.Personal, StringComparison.OrdinalIgnoreCase))
            {
                return ApplicationResult<PlatformOrganizationPublicResolveResult>.Failure(
                    DomainErrorCodes.ConnectedSupplierRequiresBusinessQr,
                    "Connected suppliers require a Business QR, not a Personal QR.");
            }

            if (string.Equals(purpose, ExItsQrPurposeGuard.PosDeviceRegistration, StringComparison.OrdinalIgnoreCase))
            {
                return ApplicationResult<PlatformOrganizationPublicResolveResult>.Failure(
                    DomainErrorCodes.ConnectedSupplierQrPurposeMismatch,
                    "This is a device registration code. Scan the supplier's Business QR instead.");
            }
        }

        return null;
    }
}

public sealed class RespondConnection
{
    private readonly IConnectedSupplierRelationshipRepository _relationships;
    private readonly IPosUnitOfWork _uow;
    private readonly IPosCommercialAccessAccessor _access;
    private readonly IOrganizationBusinessNotificationPublisher _notifications;
    private readonly IAuthorizedBranchGroupingDirectory _branchAccess;
    private readonly ICatalogProductRepository? _products;
    private readonly ISupplierProductExposureRepository? _exposures;
    private readonly TimeProvider _clock;

    public RespondConnection(
        IConnectedSupplierRelationshipRepository relationships,
        IPosUnitOfWork uow,
        IPosCommercialAccessAccessor access,
        IAuthorizedBranchGroupingDirectory branchAccess,
        IOrganizationBusinessNotificationPublisher? notifications = null,
        TimeProvider? clock = null,
        ICatalogProductRepository? products = null,
        ISupplierProductExposureRepository? exposures = null)
    {
        _relationships = relationships;
        _uow = uow;
        _access = access;
        _branchAccess = branchAccess ?? throw new ArgumentNullException(nameof(branchAccess));
        _notifications = notifications ?? new NoOpOrganizationBusinessNotificationPublisher();
        _clock = clock ?? TimeProvider.System;
        _products = products;
        _exposures = exposures;
    }

    public async Task<ApplicationResult<ConnectedSupplierRelationshipDto>> ExecuteAsync(
        Guid orgId,
        Guid relationshipId,
        bool approve,
        RespondConnectionRequest request,
        CancellationToken ct = default)
    {
        var gate = ConnectedSupplierUseCaseGuard.Access(_access, UtangCapability.ManageSuppliers);
        if (!gate.IsSuccess)
        {
            return ConnectedSupplierUseCaseGuard.Failure<ConnectedSupplierRelationshipDto>(
                gate.ErrorCode!,
                gate.ErrorMessage!);
        }

        var r = await _relationships.GetAsync(ConnectedSupplierRelationshipId.From(relationshipId), ct);
        var org = PosOrganizationId.From(orgId);
        if (r is null || r.SupplierOrganizationId != org)
        {
            return ConnectedSupplierUseCaseGuard.Failure<ConnectedSupplierRelationshipDto>(
                ConnectedSupplierErrorCodes.NotFound,
                "This connection request is no longer available.");
        }

        var respondScope = await _branchAccess.ListAuthorizedAsync(orgId, ct).ConfigureAwait(false);
        if (!SupplierConnectionBranchRouting.CanRespondForSupplierBranch(
                r.SupplierBranchId,
                respondScope.IsOrganizationWide,
                respondScope.Branches.Select(b => b.BranchId)))
        {
            return ConnectedSupplierUseCaseGuard.Failure<ConnectedSupplierRelationshipDto>(
                ConnectedSupplierErrorCodes.BranchResponseForbidden,
                "You cannot respond to a connection request for this supplier location.");
        }

        try
        {
            if (approve)
            {
                var mode = ParseCatalogSharingMode(request.CatalogSharingMode) ?? CatalogSharingMode.SelectedOnly;
                if (mode == CatalogSharingMode.AllEligible && !request.ConfirmCatalogSharing)
                {
                    return ConnectedSupplierUseCaseGuard.Failure<ConnectedSupplierRelationshipDto>(
                        ConnectedSupplierErrorCodes.BulkValidation,
                        "Confirm catalog sharing to share all eligible products with this customer.");
                }

                r.Approve(_clock.GetUtcNow(), request.RespondedByUserId);
                r.ConfigureCatalogSharing(mode, request.CustomerDiscountPercent, _clock.GetUtcNow());
                if (mode == CatalogSharingMode.AllEligible)
                {
                    await AllEligibleCatalogBootstrap.EnsureExposuresFromSellingPriceAsync(
                            org,
                            _products,
                            _exposures,
                            _clock.GetUtcNow(),
                            ct)
                        .ConfigureAwait(false);
                }
            }
            else
            {
                r.Decline(_clock.GetUtcNow(), request.RespondedByUserId);
            }

            await _relationships.UpdateAsync(r, ct);
            await _uow.SaveChangesAsync(ct);

            var relatedId = r.Id.Value.ToString("D");
            await _notifications.MarkRelatedReadAsync(
                orgId,
                SupplierConnectionNotificationTypes.Requested,
                relatedId,
                ct).ConfigureAwait(false);

            var supplierName = string.IsNullOrWhiteSpace(r.SupplierDisplayNameSnapshot)
                ? (r.SupplierPublicOrganizationIdSnapshot ?? "The supplier")
                : r.SupplierDisplayNameSnapshot;
            var buyerName = string.IsNullOrWhiteSpace(r.BuyerDisplayNameSnapshot)
                ? (r.BuyerPublicOrganizationIdSnapshot ?? "A business")
                : r.BuyerDisplayNameSnapshot;

            if (approve)
            {
                await _notifications.PublishAsync(
                    orgId,
                    r.BuyerOrganizationId.Value,
                    SupplierConnectionNotificationTypes.Accepted,
                    relatedId,
                    "Supplier connection accepted",
                    $"{supplierName} accepted your supplier connection request.",
                    ct).ConfigureAwait(false);

                // Supplier-side inbox history (same org). Requested may be missing if publish failed earlier.
                await _notifications.PublishAsync(
                    orgId,
                    orgId,
                    SupplierConnectionNotificationTypes.AcceptedConfirmation,
                    relatedId,
                    "Connection accepted",
                    $"{buyerName} is now a connected buyer.",
                    ct).ConfigureAwait(false);
            }
            else
            {
                await _notifications.PublishAsync(
                    orgId,
                    r.BuyerOrganizationId.Value,
                    SupplierConnectionNotificationTypes.Declined,
                    relatedId,
                    "Supplier connection declined",
                    $"{supplierName} declined your supplier connection request.",
                    ct).ConfigureAwait(false);

                await _notifications.PublishAsync(
                    orgId,
                    orgId,
                    SupplierConnectionNotificationTypes.DeclinedConfirmation,
                    relatedId,
                    "Connection declined",
                    $"You declined the connection request from {buyerName}.",
                    ct).ConfigureAwait(false);
            }

            return ApplicationResult<ConnectedSupplierRelationshipDto>.Success(
                ConnectedSupplierMapper.Map(r, supplierView: true));
        }
        catch (DomainException ex)
        {
            var message = ex.ErrorCode == ConnectedSupplierDomainErrorCodes.InvalidTransition
                ? "This connection request is no longer available."
                : ex.Message;
            return ConnectedSupplierUseCaseGuard.Failure<ConnectedSupplierRelationshipDto>(ex.ErrorCode, message);
        }
    }

    internal static CatalogSharingMode? ParseCatalogSharingMode(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        return Enum.TryParse<CatalogSharingMode>(raw.Trim(), ignoreCase: true, out var mode)
            ? mode
            : null;
    }
}

public sealed class DisconnectConnectedSupplier
{
    private readonly IConnectedSupplierRelationshipRepository _relationships;private readonly IPosUnitOfWork _uow;
    private readonly IPosCommercialAccessAccessor _access;private readonly TimeProvider _clock;
    public DisconnectConnectedSupplier(IConnectedSupplierRelationshipRepository r,IPosUnitOfWork u,IPosCommercialAccessAccessor a,TimeProvider? c=null)
    {_relationships=r;_uow=u;_access=a;_clock=c??TimeProvider.System;}
    public async Task<ApplicationResult<ConnectedSupplierRelationshipDto>> ExecuteAsync(Guid orgId,Guid id,CancellationToken ct=default)
    {
        var gate=ConnectedSupplierUseCaseGuard.Access(_access,UtangCapability.ManageSuppliers);
        if(!gate.IsSuccess)return ConnectedSupplierUseCaseGuard.Failure<ConnectedSupplierRelationshipDto>(gate.ErrorCode!,gate.ErrorMessage!);
        var r=await _relationships.GetAsync(ConnectedSupplierRelationshipId.From(id),ct);var org=PosOrganizationId.From(orgId);
        if(r is null||!ConnectedSupplierUseCaseGuard.BelongsTo(r,org))return ConnectedSupplierUseCaseGuard.Failure<ConnectedSupplierRelationshipDto>(ConnectedSupplierErrorCodes.NotFound,"Relationship was not found.");
        try {r.Disconnect(_clock.GetUtcNow());await _relationships.UpdateAsync(r,ct);await _uow.SaveChangesAsync(ct);
            return ApplicationResult<ConnectedSupplierRelationshipDto>.Success(ConnectedSupplierMapper.Map(r,supplierView:r.SupplierOrganizationId==org));}
        catch(DomainException ex){return ConnectedSupplierUseCaseGuard.Failure<ConnectedSupplierRelationshipDto>(ex.ErrorCode,ex.Message);}
    }
}

/// <summary>
/// Buyer cancels its own pending supplier-connection request (Pending -> Declined).
/// This must not mutate supplier master Active/Inactive status.
/// </summary>
public sealed class CancelPendingConnection
{
    private readonly IConnectedSupplierRelationshipRepository _relationships;
    private readonly IPosUnitOfWork _uow;
    private readonly IPosCommercialAccessAccessor _access;
    private readonly IOrganizationBusinessNotificationPublisher _notifications;
    private readonly TimeProvider _clock;

    public CancelPendingConnection(
        IConnectedSupplierRelationshipRepository relationships,
        IPosUnitOfWork uow,
        IPosCommercialAccessAccessor access,
        IOrganizationBusinessNotificationPublisher? notifications = null,
        TimeProvider? clock = null)
    {
        _relationships = relationships;
        _uow = uow;
        _access = access;
        _notifications = notifications ?? new NoOpOrganizationBusinessNotificationPublisher();
        _clock = clock ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<ConnectedSupplierRelationshipDto>> ExecuteAsync(
        Guid orgId,
        Guid relationshipId,
        CancelConnectionRequest request,
        CancellationToken ct = default)
    {
        var gate = ConnectedSupplierUseCaseGuard.Access(_access, UtangCapability.ManageSuppliers);
        if (!gate.IsSuccess)
        {
            return ConnectedSupplierUseCaseGuard.Failure<ConnectedSupplierRelationshipDto>(
                gate.ErrorCode!,
                gate.ErrorMessage!);
        }

        var org = PosOrganizationId.From(orgId);
        var r = await _relationships
            .GetAsync(ConnectedSupplierRelationshipId.From(relationshipId), ct)
            .ConfigureAwait(false);

        if (r is null || r.BuyerOrganizationId != org)
        {
            return ConnectedSupplierUseCaseGuard.Failure<ConnectedSupplierRelationshipDto>(
                ConnectedSupplierErrorCodes.NotFound,
                "This connection request is no longer available.");
        }

        if (r.Status != ConnectedSupplierRelationshipStatus.Pending)
        {
            return ConnectedSupplierUseCaseGuard.Failure<ConnectedSupplierRelationshipDto>(
                ConnectedSupplierErrorCodes.CancelNotPending,
                "Only pending connection requests can be cancelled.");
        }

        try
        {
            r.Decline(_clock.GetUtcNow(), request.CancelledByUserId);
            await _relationships.UpdateAsync(r, ct).ConfigureAwait(false);
            await _uow.SaveChangesAsync(ct).ConfigureAwait(false);

            // Ensure supplier-side incoming request UI/notifications no longer show this as actionable.
            var relatedId = r.Id.Value.ToString("D");
            await _notifications.MarkRelatedReadAsync(
                r.SupplierOrganizationId.Value,
                SupplierConnectionNotificationTypes.Requested,
                relatedId,
                ct).ConfigureAwait(false);

            return ApplicationResult<ConnectedSupplierRelationshipDto>.Success(
                ConnectedSupplierMapper.Map(r, supplierView: false));
        }
        catch (DomainException ex)
        {
            var message = ex.ErrorCode == ConnectedSupplierDomainErrorCodes.InvalidTransition
                ? "This connection request is no longer available."
                : ex.Message;
            return ConnectedSupplierUseCaseGuard.Failure<ConnectedSupplierRelationshipDto>(ex.ErrorCode, message);
        }
    }
}

public sealed class UpdateSupplierLocation
{
    private readonly IConnectedSupplierRelationshipRepository _relationships;
    private readonly IPosUnitOfWork _uow;
    private readonly IPosCommercialAccessAccessor _access;
    private readonly IPlatformSupplierLocationDirectory _supplierLocations;
    private readonly TimeProvider _clock;

    public UpdateSupplierLocation(
        IConnectedSupplierRelationshipRepository relationships,
        IPosUnitOfWork uow,
        IPosCommercialAccessAccessor access,
        IPlatformSupplierLocationDirectory supplierLocations,
        TimeProvider? clock = null)
    {
        _relationships = relationships;
        _uow = uow;
        _access = access;
        _supplierLocations = supplierLocations;
        _clock = clock ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<ConnectedSupplierRelationshipDto>> ExecuteAsync(
        Guid organizationId,
        Guid relationshipId,
        UpdateSupplierLocationRequest request,
        CancellationToken ct = default)
    {
        var gate = ConnectedSupplierUseCaseGuard.Access(_access, UtangCapability.ManageSuppliers);
        if (!gate.IsSuccess)
        {
            return ConnectedSupplierUseCaseGuard.Failure<ConnectedSupplierRelationshipDto>(
                gate.ErrorCode!,
                gate.ErrorMessage!);
        }

        var org = PosOrganizationId.From(organizationId);
        var relationship = await _relationships
            .GetAsync(ConnectedSupplierRelationshipId.From(relationshipId), ct)
            .ConfigureAwait(false);
        if (relationship is null || relationship.BuyerOrganizationId != org)
        {
            return ConnectedSupplierUseCaseGuard.Failure<ConnectedSupplierRelationshipDto>(
                ConnectedSupplierErrorCodes.NotFound,
                "Relationship was not found.");
        }

        var publicId = relationship.SupplierPublicOrganizationIdSnapshot;
        if (string.IsNullOrWhiteSpace(publicId))
        {
            return ConnectedSupplierUseCaseGuard.Failure<ConnectedSupplierRelationshipDto>(
                DomainErrorCodes.ConnectedSupplierBranchInvalid,
                "Supplier public business id is missing for this connection.");
        }

        var listed = await _supplierLocations.ListActiveLocationsAsync(publicId, ct).ConfigureAwait(false);
        if (!listed.IsSuccess)
        {
            return ConnectedSupplierUseCaseGuard.Failure<ConnectedSupplierRelationshipDto>(
                listed.ErrorCode!,
                listed.ErrorMessage!);
        }

        var match = (listed.Value ?? []).FirstOrDefault(x => x.BranchId == request.SupplierBranchId);
        if (match is null)
        {
            return ConnectedSupplierUseCaseGuard.Failure<ConnectedSupplierRelationshipDto>(
                DomainErrorCodes.ConnectedSupplierBranchInvalid,
                "That supplier location is not an active branch of this business.");
        }

        try
        {
            relationship.SetSupplierLocation(match.BranchId, match.Name, _clock.GetUtcNow());
            await _relationships.UpdateAsync(relationship, ct).ConfigureAwait(false);
            await _uow.SaveChangesAsync(ct).ConfigureAwait(false);
            return ApplicationResult<ConnectedSupplierRelationshipDto>.Success(
                ConnectedSupplierMapper.Map(relationship, supplierView: false));
        }
        catch (DomainException ex)
        {
            return ConnectedSupplierUseCaseGuard.Failure<ConnectedSupplierRelationshipDto>(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class ListRelationships
{
    private readonly IConnectedSupplierRelationshipRepository _relationships;private readonly IPosCommercialAccessAccessor _access;
    public ListRelationships(IConnectedSupplierRelationshipRepository r,IPosCommercialAccessAccessor a){_relationships=r;_access=a;}

    /// <param name="workspaceBranchId">
    /// Supplier branch workspace. When set, supplier-view pending/active lists are exact-branch scoped.
    /// </param>
    /// <param name="organizationWideInbox">
    /// Explicit Owner/Admin global inbox. Null preserves legacy unscoped unit-test callers.
    /// False with no branch fails closed (empty supplier list).
    /// </param>
    public async Task<ApplicationResult<IReadOnlyList<ConnectedSupplierRelationshipDto>>> ExecuteAsync(
        Guid orgId,
        bool supplierView,
        CancellationToken ct = default,
        Guid? workspaceBranchId = null,
        bool? organizationWideInbox = null)
    {
        var gate = ConnectedSupplierUseCaseGuard.Access(_access, UtangCapability.ViewSuppliers);
        if (!gate.IsSuccess)
        {
            return ConnectedSupplierUseCaseGuard.Failure<IReadOnlyList<ConnectedSupplierRelationshipDto>>(
                gate.ErrorCode!,
                gate.ErrorMessage!);
        }

        var items = await _relationships.ListAsync(PosOrganizationId.From(orgId), supplierView, ct);
        if (supplierView)
        {
            if (workspaceBranchId is Guid branch)
            {
                items = items
                    .Where(x => SupplierConnectionBranchRouting.IsVisibleInSupplierInbox(
                        x.SupplierBranchId,
                        branch,
                        organizationWideInbox: false))
                    .ToList();
            }
            else if (organizationWideInbox == true)
            {
                // Global management inbox: keep all, including legacy null-branch rows.
            }
            else if (organizationWideInbox == false)
            {
                items = Array.Empty<ConnectedSupplierRelationship>();
            }
        }

        return ApplicationResult<IReadOnlyList<ConnectedSupplierRelationshipDto>>.Success(
            items.Select(x => ConnectedSupplierMapper.Map(x, supplierView)).ToList());
    }
}

public sealed class ExposeProduct
{
    private readonly ISupplierProductExposureRepository _exposures;private readonly ICatalogProductRepository _products;
    private readonly IPosUnitOfWork _uow;private readonly IPosCommercialAccessAccessor _access;private readonly TimeProvider _clock;
    public ExposeProduct(ISupplierProductExposureRepository e,ICatalogProductRepository p,IPosUnitOfWork u,IPosCommercialAccessAccessor a,TimeProvider? c=null)
    {_exposures=e;_products=p;_uow=u;_access=a;_clock=c??TimeProvider.System;}
    public async Task<ApplicationResult<SupplierProductExposureDto>> ExecuteAsync(Guid orgId,ExposeProductRequest request,CancellationToken ct=default)
    {
        var gate=ConnectedSupplierUseCaseGuard.Access(_access,UtangCapability.ManageSuppliers);
        if(!gate.IsSuccess)return ConnectedSupplierUseCaseGuard.Failure<SupplierProductExposureDto>(gate.ErrorCode!,gate.ErrorMessage!);
        var org=PosOrganizationId.From(orgId);var product=await _products.GetByIdAsync(org,CatalogProductId.From(request.ProductId),ct);
        if(product is null)return ConnectedSupplierUseCaseGuard.Failure<SupplierProductExposureDto>(ApplicationErrorCodes.ProductNotFound,"Product was not found.");
        try {var now=_clock.GetUtcNow();product.EnableConnectedBuyerAvailability(now);product.SetDefaultConnectedPoPrice(request.SupplierOrderPrice,now);
            await _products.UpdateAsync(product,ct);await Catalog.ConnectedProductExposureSync.SyncAsync(product,_exposures,now,ct);
            var existing=await _exposures.GetByProductAsync(org,product.Id,ct)
                ?? throw new InvalidOperationException("Exposure synchronization did not create an exposure.");
            if(!request.IsOrderable){existing.MarkNotOrderable(now);await _exposures.UpdateAsync(existing,ct);}
            await _uow.SaveChangesAsync(ct);return ApplicationResult<SupplierProductExposureDto>.Success(ConnectedSupplierMapper.Map(existing));
        }catch(DomainException ex){return ConnectedSupplierUseCaseGuard.Failure<SupplierProductExposureDto>(ex.ErrorCode,ex.Message);}
    }
}

public sealed class UpdateExposure
{
    private readonly ISupplierProductExposureRepository _exposures;private readonly IPosUnitOfWork _uow;
    private readonly IPosCommercialAccessAccessor _access;private readonly TimeProvider _clock;
    public UpdateExposure(ISupplierProductExposureRepository e,IPosUnitOfWork u,IPosCommercialAccessAccessor a,TimeProvider? c=null)
    {_exposures=e;_uow=u;_access=a;_clock=c??TimeProvider.System;}
    public async Task<ApplicationResult<SupplierProductExposureDto>> ExecuteAsync(Guid orgId,Guid id,UpdateExposureRequest request,CancellationToken ct=default)
    {var gate=ConnectedSupplierUseCaseGuard.Access(_access,UtangCapability.ManageSuppliers);
     if(!gate.IsSuccess)return ConnectedSupplierUseCaseGuard.Failure<SupplierProductExposureDto>(gate.ErrorCode!,gate.ErrorMessage!);
     var e=await _exposures.GetAsync(SupplierProductExposureId.From(id),ct);var org=PosOrganizationId.From(orgId);
     if(e is null||e.SupplierOrganizationId!=org)return ConnectedSupplierUseCaseGuard.Failure<SupplierProductExposureDto>(ConnectedSupplierErrorCodes.ExposureNotFound,"Exposure was not found.");
     try {if(!request.IsExposed)e.Deactivate(_clock.GetUtcNow());else e.UpdateOffer(e.NameSnapshot,e.UnitOfMeasureCode,request.SupplierOrderPrice,request.IsOrderable,_clock.GetUtcNow(),e.SkuSnapshot,e.CategoryNameSnapshot);
       await _exposures.UpdateAsync(e,ct);await _uow.SaveChangesAsync(ct);return ApplicationResult<SupplierProductExposureDto>.Success(ConnectedSupplierMapper.Map(e));}
     catch(DomainException ex){return ConnectedSupplierUseCaseGuard.Failure<SupplierProductExposureDto>(ex.ErrorCode,ex.Message);}}
}

public sealed class ListExposures
{
    private readonly ISupplierProductExposureRepository _exposures;private readonly IPosCommercialAccessAccessor _access;
    public ListExposures(ISupplierProductExposureRepository e,IPosCommercialAccessAccessor a){_exposures=e;_access=a;}
    public async Task<ApplicationResult<IReadOnlyList<SupplierProductExposureDto>>> ExecuteAsync(Guid orgId,CancellationToken ct=default)
    {var gate=ConnectedSupplierUseCaseGuard.Access(_access,UtangCapability.ViewSuppliers);
     if(!gate.IsSuccess)return ConnectedSupplierUseCaseGuard.Failure<IReadOnlyList<SupplierProductExposureDto>>(gate.ErrorCode!,gate.ErrorMessage!);
     var x=await _exposures.ListAsync(PosOrganizationId.From(orgId),ct);return ApplicationResult<IReadOnlyList<SupplierProductExposureDto>>.Success(x.Select(item=>ConnectedSupplierMapper.Map(item)).ToList());}
}

public sealed class ListBuyerProductShares
{
    private readonly IConnectedSupplierRelationshipRepository _relationships;
    private readonly IConnectedBuyerProductShareRepository _shares;
    private readonly IPosCommercialAccessAccessor _access;
    public ListBuyerProductShares(
        IConnectedSupplierRelationshipRepository relationships,
        IConnectedBuyerProductShareRepository shares,
        IPosCommercialAccessAccessor access)
    {
        _relationships = relationships;
        _shares = shares;
        _access = access;
    }

    public async Task<ApplicationResult<IReadOnlyList<ConnectedBuyerProductShareDto>>> ExecuteAsync(
        Guid orgId, Guid relationshipId, CancellationToken ct = default)
    {
        var gate = ConnectedSupplierUseCaseGuard.Access(_access, UtangCapability.ViewSuppliers);
        if (!gate.IsSuccess)
        {
            return ConnectedSupplierUseCaseGuard.Failure<IReadOnlyList<ConnectedBuyerProductShareDto>>(
                gate.ErrorCode!, gate.ErrorMessage!);
        }

        var relationship = await _relationships.GetAsync(ConnectedSupplierRelationshipId.From(relationshipId), ct)
            .ConfigureAwait(false);
        var supplier = PosOrganizationId.From(orgId);
        if (relationship is null || relationship.SupplierOrganizationId != supplier)
        {
            return ConnectedSupplierUseCaseGuard.Failure<IReadOnlyList<ConnectedBuyerProductShareDto>>(
                ConnectedSupplierErrorCodes.NotFound, "Relationship was not found.");
        }

        var page = await _shares.SearchForSupplierManagementAsync(
                relationship.Id, supplier, null, null, null, 0, 10_000, idsOnly: false, ct,
                relationship.CatalogSharingMode)
            .ConfigureAwait(false);
        var result = new List<ConnectedBuyerProductShareDto>(page.Rows.Count);
        foreach (var row in page.Rows)
        {
            result.Add(ConnectedSupplierMapper.MapForManagement(
                relationship, row.Product, row.Share, row.Exposure, row.CategoryName));
        }

        return ApplicationResult<IReadOnlyList<ConnectedBuyerProductShareDto>>.Success(result);
    }
}

public sealed class ListEligibleProductsForSharing(ListBuyerProductShares inner)
{
    public Task<ApplicationResult<IReadOnlyList<ConnectedBuyerProductShareDto>>> ExecuteAsync(Guid orgId,Guid relationshipId,CancellationToken ct=default)=>
        inner.ExecuteAsync(orgId,relationshipId,ct);
}

public sealed class SetBuyerProductShares
{
    private readonly IConnectedSupplierRelationshipRepository _relationships;
    private readonly ISupplierProductExposureRepository _exposures;
    private readonly IConnectedBuyerProductShareRepository _shares;
    private readonly ICatalogProductRepository _products;
    private readonly IPosUnitOfWork _uow;
    private readonly IPosCommercialAccessAccessor _access;
    private readonly TimeProvider _clock;
    public SetBuyerProductShares(
        IConnectedSupplierRelationshipRepository relationships,
        ISupplierProductExposureRepository exposures,
        IConnectedBuyerProductShareRepository shares,
        ICatalogProductRepository products,
        IPosUnitOfWork uow,
        IPosCommercialAccessAccessor access,
        TimeProvider? clock = null)
    {
        _relationships = relationships;
        _exposures = exposures;
        _shares = shares;
        _products = products;
        _uow = uow;
        _access = access;
        _clock = clock ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<IReadOnlyList<ConnectedBuyerProductShareDto>>> ExecuteAsync(
        Guid orgId, Guid relationshipId, IReadOnlyList<SetBuyerProductShareItem> products, CancellationToken ct = default)
    {
        var gate = ConnectedSupplierUseCaseGuard.Access(_access, UtangCapability.ManageSuppliers);
        if (!gate.IsSuccess)
        {
            return ConnectedSupplierUseCaseGuard.Failure<IReadOnlyList<ConnectedBuyerProductShareDto>>(
                gate.ErrorCode!, gate.ErrorMessage!);
        }

        var relationship = await _relationships.GetAsync(ConnectedSupplierRelationshipId.From(relationshipId), ct)
            .ConfigureAwait(false);
        var supplier = PosOrganizationId.From(orgId);
        if (relationship is null
            || relationship.SupplierOrganizationId != supplier
            || relationship.Status != ConnectedSupplierRelationshipStatus.Active)
        {
            return ConnectedSupplierUseCaseGuard.Failure<IReadOnlyList<ConnectedBuyerProductShareDto>>(
                ConnectedSupplierErrorCodes.NotFound, "Active relationship was not found.");
        }

        var now = _clock.GetUtcNow();
        var result = new List<ConnectedBuyerProductShareDto>();
        foreach (var item in products.GroupBy(x => x.SupplierProductId).Select(x => x.Last()))
        {
            var productId = CatalogProductId.From(item.SupplierProductId);
            var product = await _products.GetByIdAsync(supplier, productId, ct).ConfigureAwait(false);
            if (product is null
                || product.OrganizationId != supplier
                || product.Status != CatalogProductStatus.Active)
            {
                return ConnectedSupplierUseCaseGuard.Failure<IReadOnlyList<ConnectedBuyerProductShareDto>>(
                    ConnectedSupplierErrorCodes.NotFound, "Product was not found.");
            }

            if (item.IsShared && product.IsBlockedFromConnectedBuyers)
            {
                return ConnectedSupplierUseCaseGuard.Failure<IReadOnlyList<ConnectedBuyerProductShareDto>>(
                    ConnectedSupplierErrorCodes.ProductBlocked,
                    $"'{product.Name}' is blocked from connected buyers.");
            }

            if (item.IsShared)
            {
                if (product.DefaultConnectedPoPrice is null)
                {
                    if (item.EstablishDefaultPoPrice is null)
                    {
                        return ConnectedSupplierUseCaseGuard.Failure<IReadOnlyList<ConnectedBuyerProductShareDto>>(
                            ConnectedSupplierErrorCodes.MissingDefaultPo,
                            $"'{product.Name}' needs a Default PO price before it can be shared.");
                    }

                    try
                    {
                        product.SetDefaultConnectedPoPrice(item.EstablishDefaultPoPrice.Value, now);
                        product.AllowForConnectedBuyers(now);
                    }
                    catch (DomainException ex)
                    {
                        return ConnectedSupplierUseCaseGuard.Failure<IReadOnlyList<ConnectedBuyerProductShareDto>>(
                            ex.ErrorCode, ex.Message);
                    }

                    await _products.UpdateAsync(product, ct).ConfigureAwait(false);
                    await Catalog.ConnectedProductExposureSync.SyncAsync(product, _exposures, now, ct)
                        .ConfigureAwait(false);
                }
                else if (!product.IsBlockedFromConnectedBuyers)
                {
                    await Catalog.ConnectedProductExposureSync.SyncAsync(product, _exposures, now, ct)
                        .ConfigureAwait(false);
                }
            }

            var exposure = await _exposures.GetByProductAsync(supplier, productId, ct).ConfigureAwait(false);
            if (item.IsShared && (exposure is null || !exposure.IsExposed))
            {
                return ConnectedSupplierUseCaseGuard.Failure<IReadOnlyList<ConnectedBuyerProductShareDto>>(
                    ConnectedSupplierErrorCodes.MissingDefaultPo,
                    $"'{product.Name}' needs a Default PO price before it can be shared.");
            }

            var share = await _shares.FindAsync(relationship.Id, productId, ct).ConfigureAwait(false);
            if (share is null)
            {
                share = ConnectedBuyerProductShare.Share(
                    relationship.Id, relationship.BuyerOrganizationId, supplier, productId, now, item.BuyerSpecificPoPrice);
                if (!item.IsShared)
                {
                    share.Unshare(now);
                }

                await _shares.AddAsync(share, ct).ConfigureAwait(false);
            }
            else
            {
                share.SetBuyerSpecificPoPrice(item.BuyerSpecificPoPrice, now);
                share.SetShared(item.IsShared, now);
                await _shares.UpdateAsync(share, ct).ConfigureAwait(false);
            }

            result.Add(ConnectedSupplierMapper.Map(share, exposure, product));
        }

        await _uow.SaveChangesAsync(ct).ConfigureAwait(false);
        return ApplicationResult<IReadOnlyList<ConnectedBuyerProductShareDto>>.Success(result);
    }
}

public sealed class UpsertBuyerProductShare(SetBuyerProductShares inner)
{
    public async Task<ApplicationResult<ConnectedBuyerProductShareDto>> ExecuteAsync(Guid orgId,Guid relationshipId,SetBuyerProductShareItem item,CancellationToken ct=default)
    {
        var result=await inner.ExecuteAsync(orgId,relationshipId,[item],ct);
        return result.IsSuccess
            ? ApplicationResult<ConnectedBuyerProductShareDto>.Success(result.Value![0])
            : ApplicationResult<ConnectedBuyerProductShareDto>.Failure(result.ErrorCode!,result.ErrorMessage!);
    }
}

public sealed class ConfirmBuyerProductSharing(SetBuyerProductShares inner)
{
    public Task<ApplicationResult<IReadOnlyList<ConnectedBuyerProductShareDto>>> ExecuteAsync(
        Guid orgId, Guid relationshipId, ConfirmBuyerProductSharingRequest request, CancellationToken ct = default)
    {
        var prices = request.EstablishDefaultPoPrices;
        var items = (request.ProductIds ?? [])
            .Select(id =>
            {
                decimal? establish = null;
                if (prices is not null && prices.TryGetValue(id, out var price))
                {
                    establish = price;
                }

                return new SetBuyerProductShareItem(id, true, BuyerSpecificPoPrice: null, EstablishDefaultPoPrice: establish);
            })
            .ToList();
        return inner.ExecuteAsync(orgId, relationshipId, items, ct);
    }
}

public sealed class SearchExposedCatalog
{
    private readonly IConnectedSupplierRelationshipRepository _relationships;private readonly ISupplierProductExposureRepository _exposures;
    private readonly IConnectedBuyerProductShareRepository _shares;
    private readonly IPosCommercialAccessAccessor _access;
    public SearchExposedCatalog(IConnectedSupplierRelationshipRepository r,ISupplierProductExposureRepository e,IPosCommercialAccessAccessor a,
        IConnectedBuyerProductShareRepository shares)
    {_relationships=r;_exposures=e;_access=a;_shares=shares;}
    public async Task<ApplicationResult<PagedResult<SupplierProductExposureDto>>> ExecuteAsync(Guid orgId,Guid relationshipId,string? query,string? category,int? page,int? pageSize,CancellationToken ct=default)
    {var gate=ConnectedSupplierUseCaseGuard.Access(_access,UtangCapability.ViewPurchasing);
     if(!gate.IsSuccess)return ConnectedSupplierUseCaseGuard.Failure<PagedResult<SupplierProductExposureDto>>(gate.ErrorCode!,gate.ErrorMessage!);
     var r=await _relationships.GetAsync(ConnectedSupplierRelationshipId.From(relationshipId),ct);var buyer=PosOrganizationId.From(orgId);
     if(r is null||r.BuyerOrganizationId!=buyer)return ConnectedSupplierUseCaseGuard.Failure<PagedResult<SupplierProductExposureDto>>(ConnectedSupplierErrorCodes.NotFound,"Relationship was not found.");
     if(r.Status!=ConnectedSupplierRelationshipStatus.Active)return ConnectedSupplierUseCaseGuard.Failure<PagedResult<SupplierProductExposureDto>>(ConnectedSupplierErrorCodes.RelationshipInactive,"Relationship is not active.");
     var p=Math.Max(page??1,1);var size=Math.Clamp(pageSize??25,1,50);
     var (items,shares,total)=await _shares.SearchSharedCatalogAsync(
         r.Id,r.SupplierOrganizationId,query,category,(p-1)*size,size,ct,r.CatalogSharingMode);
     var sharesByProduct=shares.ToDictionary(x=>x.SupplierProductId.Value);
     return ApplicationResult<PagedResult<SupplierProductExposureDto>>.Success(new(items.Select(x=>
       {
           sharesByProduct.TryGetValue(x.ProductId.Value, out var share);
           var resolved = ConnectedPoPricing.TryResolveEffectivePrice(
               x,
               share,
               r.CatalogSharingMode,
               r.CustomerDiscountPercent,
               sellingPrice: null,
               out var price,
               out _);
           return ConnectedSupplierMapper.Map(x, resolved ? price : x.SupplierOrderPrice);
       }).ToList(),total,p,size));}
}

public sealed class LinkProduct
{
    private readonly IConnectedSupplierRelationshipRepository _relationships;private readonly ISupplierProductExposureRepository _exposures;
    private readonly IConnectedBuyerProductShareRepository _shares;
    private readonly IBuyerSupplierProductLinkRepository _links;private readonly ICatalogProductRepository _products;
    private readonly ICatalogProductUnitRepository _units;
    private readonly IPurchaseOrderRepository? _purchaseOrders;
    private readonly IPosUnitOfWork _uow;private readonly IPosCommercialAccessAccessor _access;private readonly TimeProvider _clock;
    public LinkProduct(IConnectedSupplierRelationshipRepository r,ISupplierProductExposureRepository e,IBuyerSupplierProductLinkRepository l,
        ICatalogProductRepository p,ICatalogProductUnitRepository units,IPosUnitOfWork u,IPosCommercialAccessAccessor a,
        IConnectedBuyerProductShareRepository shares,TimeProvider? c=null,IPurchaseOrderRepository? purchaseOrders=null)
    {_relationships=r;_exposures=e;_links=l;_products=p;_units=units;_uow=u;_access=a;_shares=shares;_clock=c??TimeProvider.System;_purchaseOrders=purchaseOrders;}
    public async Task<ApplicationResult<BuyerSupplierProductLinkDto>> ExecuteAsync(Guid orgId,Guid relationshipId,LinkProductRequest request,CancellationToken ct=default)
    {var gate=ConnectedSupplierUseCaseGuard.Access(_access,UtangCapability.ManagePurchasing);
     if(!gate.IsSuccess)return ConnectedSupplierUseCaseGuard.Failure<BuyerSupplierProductLinkDto>(gate.ErrorCode!,gate.ErrorMessage!);
     var buyer=PosOrganizationId.From(orgId);var r=await _relationships.GetAsync(ConnectedSupplierRelationshipId.From(relationshipId),ct);
     if(r is null||r.BuyerOrganizationId!=buyer||r.Status!=ConnectedSupplierRelationshipStatus.Active)return ConnectedSupplierUseCaseGuard.Failure<BuyerSupplierProductLinkDto>(ConnectedSupplierErrorCodes.NotFound,"Active relationship was not found.");
     var product=await _products.GetByIdAsync(buyer,CatalogProductId.From(request.BuyerProductId),ct);
     var exposure=await _exposures.GetAsync(SupplierProductExposureId.From(request.ExposureId),ct);
     if(product is null||product.OrganizationId!=buyer||product.Status!=CatalogProductStatus.Active)
       return ConnectedSupplierUseCaseGuard.Failure<BuyerSupplierProductLinkDto>(ConnectedSupplierErrorCodes.ExposureNotFound,"Buyer product was not found.");
     if(exposure is null||exposure.SupplierOrganizationId!=r.SupplierOrganizationId||!exposure.IsExposed||!exposure.IsOrderable)
       return ConnectedSupplierUseCaseGuard.Failure<BuyerSupplierProductLinkDto>(ConnectedSupplierErrorCodes.ExposureNotFound,"Exposure was not found.");
     var share=await _shares.FindAsync(r.Id,exposure.ProductId,ct);
     if(!ConnectedPoPricing.TryResolveEffectivePrice(exposure,share,r.CatalogSharingMode,r.CustomerDiscountPercent,null,out var effectivePrice,out _))
       return ConnectedSupplierUseCaseGuard.Failure<BuyerSupplierProductLinkDto>(ConnectedSupplierErrorCodes.ExposureNotFound,"This product is not shared with your business.");
     var existingBySupplier=await _links.FindBySupplierProductAsync(r.Id,exposure.ProductId,ct);
     if(existingBySupplier is not null)
     {
       if(existingBySupplier.BuyerProductId==product.Id)
       {
         await BindPurchaseOrderIfRequestedAsync(buyer, request.PurchaseOrderId, exposure.ProductId, product.Id, ct).ConfigureAwait(false);
         await _uow.SaveChangesAsync(ct).ConfigureAwait(false);
         return ApplicationResult<BuyerSupplierProductLinkDto>.Success(ConnectedSupplierMapper.Map(existingBySupplier));
       }
       return ConnectedSupplierUseCaseGuard.Failure<BuyerSupplierProductLinkDto>(ConnectedSupplierErrorCodes.BulkValidation,"This supplier product is already linked to another catalog product.");
     }
     Guid? buyerPurchaseUnitId=request.BuyerPurchaseUnitId;
     var multiplier=request.MultiplierToBase??1m;
     if(buyerPurchaseUnitId is not null)
     {
       var unit=await _units.GetByIdAsync(buyer,ProductUnitId.From(buyerPurchaseUnitId.Value),ct);
       if(unit is null||unit.ProductId!=product.Id||!unit.IsActive||unit.Kind!=ProductUnitKind.Purchase)
         return ConnectedSupplierUseCaseGuard.Failure<BuyerSupplierProductLinkDto>(DomainErrorCodes.InvalidProductUnitId,"Buyer purchase unit must be an active purchase unit for the buyer product.");
       multiplier=request.MultiplierToBase??unit.MultiplierToBase;
     }
     var existing=await _links.FindAsync(r.Id,product.Id,ct);
     if(existing is not null)
     {
       await BindPurchaseOrderIfRequestedAsync(buyer, request.PurchaseOrderId, exposure.ProductId, product.Id, ct).ConfigureAwait(false);
       await _uow.SaveChangesAsync(ct).ConfigureAwait(false);
       return ApplicationResult<BuyerSupplierProductLinkDto>.Success(ConnectedSupplierMapper.Map(existing));
     }
     var link=BuyerSupplierProductLink.Create(r.Id,buyer,r.SupplierOrganizationId,product.Id,exposure,_clock.GetUtcNow(),
       buyerPurchaseUnitId:buyerPurchaseUnitId,multiplierToBase:multiplier,packageLabel:request.PackageLabel,effectiveOrderPrice:effectivePrice);
     await _links.AddAsync(link,ct);
     await BindPurchaseOrderIfRequestedAsync(buyer, request.PurchaseOrderId, exposure.ProductId, product.Id, ct).ConfigureAwait(false);
     await _uow.SaveChangesAsync(ct);return ApplicationResult<BuyerSupplierProductLinkDto>.Success(ConnectedSupplierMapper.Map(link));}

    private async Task BindPurchaseOrderIfRequestedAsync(
        PosOrganizationId buyer,
        Guid? purchaseOrderId,
        CatalogProductId supplierProductId,
        CatalogProductId buyerProductId,
        CancellationToken ct)
    {
        if (purchaseOrderId is not Guid poId || poId == Guid.Empty || _purchaseOrders is null)
        {
            return;
        }

        var po = await _purchaseOrders.GetByIdAsync(buyer, PurchaseOrderId.From(poId), ct).ConfigureAwait(false);
        if (po is null || po.OrganizationId != buyer)
        {
            return;
        }

        po.BindBuyerProductForSupplierProduct(supplierProductId, buyerProductId, _clock.GetUtcNow());
        await _purchaseOrders.UpdateAsync(po, ct).ConfigureAwait(false);
    }
}

public sealed class UnlinkProduct
{
    private readonly IBuyerSupplierProductLinkRepository _links;private readonly IPosUnitOfWork _uow;private readonly IPosCommercialAccessAccessor _access;private readonly TimeProvider _clock;
    public UnlinkProduct(IBuyerSupplierProductLinkRepository l,IPosUnitOfWork u,IPosCommercialAccessAccessor a,TimeProvider? c=null){_links=l;_uow=u;_access=a;_clock=c??TimeProvider.System;}
    public async Task<ApplicationResult<BuyerSupplierProductLinkDto>> ExecuteAsync(Guid orgId,Guid id,CancellationToken ct=default)
    {var gate=ConnectedSupplierUseCaseGuard.Access(_access,UtangCapability.ManagePurchasing);if(!gate.IsSuccess)return ConnectedSupplierUseCaseGuard.Failure<BuyerSupplierProductLinkDto>(gate.ErrorCode!,gate.ErrorMessage!);
     var link=await _links.GetAsync(BuyerSupplierProductLinkId.From(id),ct);if(link is null||link.BuyerOrganizationId!=PosOrganizationId.From(orgId))return ConnectedSupplierUseCaseGuard.Failure<BuyerSupplierProductLinkDto>(ConnectedSupplierErrorCodes.LinkNotFound,"Link was not found.");
     link.Unlink(_clock.GetUtcNow());await _links.UpdateAsync(link,ct);await _uow.SaveChangesAsync(ct);return ApplicationResult<BuyerSupplierProductLinkDto>.Success(ConnectedSupplierMapper.Map(link));}
}

public sealed class ListLinks
{
    private readonly IBuyerSupplierProductLinkRepository _links;private readonly IPosCommercialAccessAccessor _access;
    public ListLinks(IBuyerSupplierProductLinkRepository l,IPosCommercialAccessAccessor a){_links=l;_access=a;}
    public async Task<ApplicationResult<IReadOnlyList<BuyerSupplierProductLinkDto>>> ExecuteAsync(Guid orgId,Guid relationshipId,CancellationToken ct=default)
    {var gate=ConnectedSupplierUseCaseGuard.Access(_access,UtangCapability.ViewPurchasing);if(!gate.IsSuccess)return ConnectedSupplierUseCaseGuard.Failure<IReadOnlyList<BuyerSupplierProductLinkDto>>(gate.ErrorCode!,gate.ErrorMessage!);
     var x=await _links.ListAsync(ConnectedSupplierRelationshipId.From(relationshipId),PosOrganizationId.From(orgId),ct);return ApplicationResult<IReadOnlyList<BuyerSupplierProductLinkDto>>.Success(x.Select(ConnectedSupplierMapper.Map).ToList());}
}

public sealed class SyncLinkedProductsDelta
{
    private readonly IBuyerSupplierProductLinkRepository _links;private readonly IPosCommercialAccessAccessor _access;
    public SyncLinkedProductsDelta(IBuyerSupplierProductLinkRepository l,IPosCommercialAccessAccessor a){_links=l;_access=a;}
    public async Task<ApplicationResult<LinkedProductsDeltaDto>> ExecuteAsync(Guid orgId,Guid relationshipId,long sinceVersion,CancellationToken ct=default)
    {var gate=ConnectedSupplierUseCaseGuard.Access(_access,UtangCapability.ViewPurchasing);if(!gate.IsSuccess)return ConnectedSupplierUseCaseGuard.Failure<LinkedProductsDeltaDto>(gate.ErrorCode!,gate.ErrorMessage!);
     var x=await _links.DeltaAsync(ConnectedSupplierRelationshipId.From(relationshipId),PosOrganizationId.From(orgId),Math.Max(0,sinceVersion),ct);
     var changed=x.Where(i=>i.IsActive).Select(ConnectedSupplierMapper.Map).ToList();var removed=x.Where(i=>!i.IsActive).Select(i=>i.Id.Value).ToList();
     return ApplicationResult<LinkedProductsDeltaDto>.Success(new(changed,removed,x.Count==0?sinceVersion:x.Max(i=>i.SyncVersion)));}
}

public sealed class SupplierIncomingOrderQuery
{
    private readonly IConnectedPurchaseOrderRepository _orders;
    private readonly IConnectedSupplierRelationshipRepository _relationships;
    private readonly IPurchaseOrderRepository? _buyerOrders;
    private readonly IPosCommercialAccessAccessor _access;

    public SupplierIncomingOrderQuery(
        IConnectedPurchaseOrderRepository o,
        IPosCommercialAccessAccessor a,
        IConnectedSupplierRelationshipRepository? relationships = null,
        IPurchaseOrderRepository? buyerOrders = null)
    {
        _orders = o;
        _access = a;
        _relationships = relationships!;
        _buyerOrders = buyerOrders;
    }

    public async Task<ApplicationResult<IReadOnlyList<ConnectedPurchaseOrderDto>>> ExecuteAsync(
        Guid orgId,
        string? statusFilter = null,
        CancellationToken ct = default)
    {
        var gate = ConnectedSupplierUseCaseGuard.Access(_access, UtangCapability.ViewPurchasing);
        if (!gate.IsSuccess)
        {
            return ConnectedSupplierUseCaseGuard.Failure<IReadOnlyList<ConnectedPurchaseOrderDto>>(gate.ErrorCode!, gate.ErrorMessage!);
        }

        var supplier = PosOrganizationId.From(orgId);
        var list = await _orders.ListIncomingAsync(supplier, ct).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(statusFilter)
            && !string.Equals(statusFilter, "All", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(statusFilter, "ActionNeeded", StringComparison.OrdinalIgnoreCase))
        {
            list = list.Where(o => string.Equals(o.Status.ToString(), statusFilter, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        else if (string.Equals(statusFilter, "ActionNeeded", StringComparison.OrdinalIgnoreCase))
        {
            list = list.Where(o => o.Status is ConnectedPurchaseOrderStatus.New
                or ConnectedPurchaseOrderStatus.Accepted
                or ConnectedPurchaseOrderStatus.Preparing).ToList();
        }

        var mapped = new List<ConnectedPurchaseOrderDto>(list.Count);
        foreach (var order in list)
        {
            string? buyerName = null;
            if (_relationships is not null)
            {
                var rel = await _relationships.GetAsync(order.RelationshipId, ct).ConfigureAwait(false);
                buyerName = rel?.BuyerDisplayNameSnapshot;
            }

            Guid? supplierBranchId = null;
            string? supplierBranchName = null;
            if (_buyerOrders is not null)
            {
                var buyerPo = await _buyerOrders
                    .GetByIdAsync(order.BuyerOrganizationId, order.BuyerPurchaseOrderId, ct)
                    .ConfigureAwait(false);
                supplierBranchId = buyerPo?.SupplierBranchId;
                supplierBranchName = buyerPo?.SupplierBranchNameSnapshot;
            }

            mapped.Add(ConnectedSupplierMapper.Map(
                order,
                buyerName,
                supplierBranchId: supplierBranchId,
                supplierBranchName: supplierBranchName));
        }

        return ApplicationResult<IReadOnlyList<ConnectedPurchaseOrderDto>>.Success(mapped);
    }
}

public sealed class GetIncomingOrder
{
    private readonly IConnectedPurchaseOrderRepository _orders;
    private readonly IConnectedSupplierRelationshipRepository _relationships;
    private readonly IPurchaseOrderRepository? _buyerOrders;
    private readonly IPosCommercialAccessAccessor _access;

    public GetIncomingOrder(
        IConnectedPurchaseOrderRepository orders,
        IPosCommercialAccessAccessor access,
        IConnectedSupplierRelationshipRepository relationships,
        IPurchaseOrderRepository? buyerOrders = null)
    {
        _orders = orders;
        _access = access;
        _relationships = relationships;
        _buyerOrders = buyerOrders;
    }

    public async Task<ApplicationResult<ConnectedPurchaseOrderDto>> ExecuteAsync(Guid orgId, Guid id, CancellationToken ct = default)
    {
        var gate = ConnectedSupplierUseCaseGuard.Access(_access, UtangCapability.ViewPurchasing);
        if (!gate.IsSuccess)
        {
            return ConnectedSupplierUseCaseGuard.Failure<ConnectedPurchaseOrderDto>(gate.ErrorCode!, gate.ErrorMessage!);
        }

        var order = await _orders.GetAsync(ConnectedPurchaseOrderId.From(id), ct).ConfigureAwait(false);
        if (order is null || order.SupplierOrganizationId != PosOrganizationId.From(orgId))
        {
            return ConnectedSupplierUseCaseGuard.Failure<ConnectedPurchaseOrderDto>(
                ConnectedSupplierErrorCodes.IncomingOrderNotFound, "Incoming order was not found.");
        }

        var rel = await _relationships.GetAsync(order.RelationshipId, ct).ConfigureAwait(false);
        string? receiving = null;
        Guid? supplierBranchId = null;
        string? supplierBranchName = null;
        if (_buyerOrders is not null)
        {
            var buyerPo = await _buyerOrders
                .GetByIdAsync(order.BuyerOrganizationId, order.BuyerPurchaseOrderId, ct)
                .ConfigureAwait(false);
            if (buyerPo is not null)
            {
                receiving = ConnectedPoDisplayStatus.ForSupplier(order, buyerPo);
                supplierBranchId = buyerPo.SupplierBranchId;
                supplierBranchName = buyerPo.SupplierBranchNameSnapshot;
            }
        }

        return ApplicationResult<ConnectedPurchaseOrderDto>.Success(
            ConnectedSupplierMapper.Map(
                order,
                rel?.BuyerDisplayNameSnapshot,
                receiving,
                supplierBranchId,
                supplierBranchName));
    }
}

public sealed class RespondIncomingOrder
{
    private readonly IConnectedPurchaseOrderRepository _orders;
    private readonly IPurchaseOrderRepository? _buyerOrders;
    private readonly IBuyerSupplierProductLinkRepository? _links;
    private readonly IConnectedSupplierRelationshipRepository _relationships;
    private readonly IOrganizationBusinessNotificationPublisher _notifications;
    private readonly IPosUnitOfWork _uow;
    private readonly IPosCommercialAccessAccessor _access;
    private readonly TimeProvider _clock;

    public RespondIncomingOrder(
        IConnectedPurchaseOrderRepository o,
        IPosUnitOfWork u,
        IPosCommercialAccessAccessor a,
        TimeProvider? c = null,
        IOrganizationBusinessNotificationPublisher? notifications = null,
        IConnectedSupplierRelationshipRepository? relationships = null,
        IPurchaseOrderRepository? buyerOrders = null,
        IBuyerSupplierProductLinkRepository? links = null)
    {
        _orders = o;
        _uow = u;
        _access = a;
        _clock = c ?? TimeProvider.System;
        _notifications = notifications ?? new NoOpOrganizationBusinessNotificationPublisher();
        _relationships = relationships!;
        _buyerOrders = buyerOrders;
        _links = links;
    }

    public async Task<ApplicationResult<ConnectedPurchaseOrderDto>> ExecuteAsync(
        Guid orgId,
        Guid id,
        bool accept,
        DeclineIncomingOrderRequest? decline = null,
        CancellationToken ct = default)
    {
        var gate = ConnectedSupplierUseCaseGuard.Access(_access, UtangCapability.ManagePurchasing);
        if (!gate.IsSuccess)
        {
            return ConnectedSupplierUseCaseGuard.Failure<ConnectedPurchaseOrderDto>(gate.ErrorCode!, gate.ErrorMessage!);
        }

        var o = await _orders.GetAsync(ConnectedPurchaseOrderId.From(id), ct).ConfigureAwait(false);
        if (o is null || o.SupplierOrganizationId != PosOrganizationId.From(orgId))
        {
            return ConnectedSupplierUseCaseGuard.Failure<ConnectedPurchaseOrderDto>(
                ConnectedSupplierErrorCodes.IncomingOrderNotFound, "Incoming order was not found.");
        }

        try
        {
            var now = _clock.GetUtcNow();
            if (accept)
            {
                if (o.Status == ConnectedPurchaseOrderStatus.Accepted)
                {
                    return ApplicationResult<ConnectedPurchaseOrderDto>.Success(ConnectedSupplierMapper.Map(o));
                }

                o.Accept(now);
                if (_buyerOrders is not null && _links is not null)
                {
                    var buyerPo = await _buyerOrders
                        .GetByIdAsync(o.BuyerOrganizationId, o.BuyerPurchaseOrderId, ct)
                        .ConfigureAwait(false);
                    if (buyerPo is not null)
                    {
                        await ConnectedPoConfirmation
                            .AlignBuyerOutstandingAsync(buyerPo, o, _links, now, ct)
                            .ConfigureAwait(false);
                        await _buyerOrders.UpdateAsync(buyerPo, ct).ConfigureAwait(false);
                    }
                }
            }
            else
            {
                if (o.Status == ConnectedPurchaseOrderStatus.Declined)
                {
                    return ApplicationResult<ConnectedPurchaseOrderDto>.Success(ConnectedSupplierMapper.Map(o));
                }

                ConnectedPoDeclineReason? reason = null;
                if (!string.IsNullOrWhiteSpace(decline?.DeclineReason)
                    && Enum.TryParse<ConnectedPoDeclineReason>(decline.DeclineReason, true, out var parsed))
                {
                    reason = parsed;
                }

                o.Decline(now, reason, decline?.DeclineNote);
            }

            await _orders.UpdateAsync(o, ct).ConfigureAwait(false);
            await _uow.SaveChangesAsync(ct).ConfigureAwait(false);

            var poLabel = o.BuyerPoNumber ?? o.BuyerPurchaseOrderId.Value.ToString("D");
            string? supplierName = null;
            if (_relationships is not null)
            {
                var rel = await _relationships.GetAsync(o.RelationshipId, ct).ConfigureAwait(false);
                supplierName = rel?.SupplierDisplayNameSnapshot;
            }

            await _notifications.PublishAsync(
                orgId,
                o.BuyerOrganizationId.Value,
                accept
                    ? ConnectedPurchaseOrderNotificationTypes.Accepted
                    : ConnectedPurchaseOrderNotificationTypes.Declined,
                o.BuyerPurchaseOrderId.Value.ToString("D"),
                accept ? "Purchase order accepted" : "Purchase order declined",
                accept
                    ? $"{supplierName ?? "Supplier"} accepted PO {poLabel}."
                    : $"{supplierName ?? "Supplier"} declined PO {poLabel}.",
                ct).ConfigureAwait(false);

            return ApplicationResult<ConnectedPurchaseOrderDto>.Success(ConnectedSupplierMapper.Map(o));
        }
        catch (DomainException ex)
        {
            return ConnectedSupplierUseCaseGuard.Failure<ConnectedPurchaseOrderDto>(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ConnectedSupplierUseCaseGuard.Failure<ConnectedPurchaseOrderDto>(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class AcceptIncoming(RespondIncomingOrder inner)
{
    public Task<ApplicationResult<ConnectedPurchaseOrderDto>> ExecuteAsync(Guid orgId, Guid id, CancellationToken ct = default) =>
        inner.ExecuteAsync(orgId, id, true, null, ct);
}

public sealed class DeclineIncoming(RespondIncomingOrder inner)
{
    public Task<ApplicationResult<ConnectedPurchaseOrderDto>> ExecuteAsync(
        Guid orgId,
        Guid id,
        DeclineIncomingOrderRequest? request = null,
        CancellationToken ct = default) =>
        inner.ExecuteAsync(orgId, id, false, request, ct);
}

public sealed class ProposeIncomingOrderChanges
{
    private readonly IConnectedPurchaseOrderRepository _orders;
    private readonly IConnectedSupplierRelationshipRepository _relationships;
    private readonly IOrganizationBusinessNotificationPublisher _notifications;
    private readonly IPosUnitOfWork _uow;
    private readonly IPosCommercialAccessAccessor _access;
    private readonly TimeProvider _clock;

    public ProposeIncomingOrderChanges(
        IConnectedPurchaseOrderRepository orders,
        IPosUnitOfWork uow,
        IPosCommercialAccessAccessor access,
        TimeProvider? clock = null,
        IOrganizationBusinessNotificationPublisher? notifications = null,
        IConnectedSupplierRelationshipRepository? relationships = null)
    {
        _orders = orders;
        _uow = uow;
        _access = access;
        _clock = clock ?? TimeProvider.System;
        _notifications = notifications ?? new NoOpOrganizationBusinessNotificationPublisher();
        _relationships = relationships!;
    }

    public async Task<ApplicationResult<ConnectedPurchaseOrderDto>> ExecuteAsync(
        Guid orgId,
        Guid id,
        ProposeIncomingOrderChangesRequest request,
        Guid? actorId = null,
        CancellationToken ct = default)
    {
        var gate = ConnectedSupplierUseCaseGuard.Access(_access, UtangCapability.ManagePurchasing);
        if (!gate.IsSuccess)
        {
            return ConnectedSupplierUseCaseGuard.Failure<ConnectedPurchaseOrderDto>(gate.ErrorCode!, gate.ErrorMessage!);
        }

        var o = await _orders.GetAsync(ConnectedPurchaseOrderId.From(id), ct).ConfigureAwait(false);
        if (o is null || o.SupplierOrganizationId != PosOrganizationId.From(orgId))
        {
            return ConnectedSupplierUseCaseGuard.Failure<ConnectedPurchaseOrderDto>(
                ConnectedSupplierErrorCodes.IncomingOrderNotFound, "Incoming order was not found.");
        }

        try
        {
            if (o.Status == ConnectedPurchaseOrderStatus.ChangesProposed)
            {
                return ApplicationResult<ConnectedPurchaseOrderDto>.Success(ConnectedSupplierMapper.Map(o));
            }

            var proposals = (request.Lines ?? [])
                .Select(l => new ConnectedPoLineProposal(
                    CatalogProductId.From(l.ProductId),
                    l.ProposedQty,
                    l.Unavailable))
                .ToList();
            o.ProposeLineChanges(proposals, _clock.GetUtcNow(), actorId);
            await _orders.UpdateAsync(o, ct).ConfigureAwait(false);
            await _uow.SaveChangesAsync(ct).ConfigureAwait(false);

            var poLabel = o.BuyerPoNumber ?? o.BuyerPurchaseOrderId.Value.ToString("D");
            string? supplierName = null;
            if (_relationships is not null)
            {
                var rel = await _relationships.GetAsync(o.RelationshipId, ct).ConfigureAwait(false);
                supplierName = rel?.SupplierDisplayNameSnapshot;
            }

            await _notifications.PublishAsync(
                orgId,
                o.BuyerOrganizationId.Value,
                ConnectedPurchaseOrderNotificationTypes.ChangesProposed,
                o.BuyerPurchaseOrderId.Value.ToString("D"),
                "Purchase order changes proposed",
                $"{supplierName ?? "Supplier"} proposed quantity changes for PO {poLabel}.",
                ct).ConfigureAwait(false);

            return ApplicationResult<ConnectedPurchaseOrderDto>.Success(ConnectedSupplierMapper.Map(o));
        }
        catch (DomainException ex)
        {
            return ConnectedSupplierUseCaseGuard.Failure<ConnectedPurchaseOrderDto>(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ConnectedSupplierUseCaseGuard.Failure<ConnectedPurchaseOrderDto>(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class StartPreparingIncomingOrder
{
    private readonly IConnectedPurchaseOrderRepository _orders;
    private readonly IOrganizationBusinessNotificationPublisher _notifications;
    private readonly IConnectedSupplierRelationshipRepository _relationships;
    private readonly IPosUnitOfWork _uow;
    private readonly IPosCommercialAccessAccessor _access;
    private readonly TimeProvider _clock;

    public StartPreparingIncomingOrder(
        IConnectedPurchaseOrderRepository orders,
        IPosUnitOfWork uow,
        IPosCommercialAccessAccessor access,
        TimeProvider? clock = null,
        IOrganizationBusinessNotificationPublisher? notifications = null,
        IConnectedSupplierRelationshipRepository? relationships = null)
    {
        _orders = orders;
        _uow = uow;
        _access = access;
        _clock = clock ?? TimeProvider.System;
        _notifications = notifications ?? new NoOpOrganizationBusinessNotificationPublisher();
        _relationships = relationships!;
    }

    public async Task<ApplicationResult<ConnectedPurchaseOrderDto>> ExecuteAsync(Guid orgId, Guid id, CancellationToken ct = default)
    {
        var gate = ConnectedSupplierUseCaseGuard.Access(_access, UtangCapability.ManagePurchasing);
        if (!gate.IsSuccess)
        {
            return ConnectedSupplierUseCaseGuard.Failure<ConnectedPurchaseOrderDto>(gate.ErrorCode!, gate.ErrorMessage!);
        }

        var o = await _orders.GetAsync(ConnectedPurchaseOrderId.From(id), ct).ConfigureAwait(false);
        if (o is null || o.SupplierOrganizationId != PosOrganizationId.From(orgId))
        {
            return ConnectedSupplierUseCaseGuard.Failure<ConnectedPurchaseOrderDto>(
                ConnectedSupplierErrorCodes.IncomingOrderNotFound, "Incoming order was not found.");
        }

        try
        {
            if (o.Status == ConnectedPurchaseOrderStatus.Preparing)
            {
                return ApplicationResult<ConnectedPurchaseOrderDto>.Success(ConnectedSupplierMapper.Map(o));
            }

            o.StartPreparing(_clock.GetUtcNow());
            await _orders.UpdateAsync(o, ct).ConfigureAwait(false);
            await _uow.SaveChangesAsync(ct).ConfigureAwait(false);

            var poLabel = o.BuyerPoNumber ?? o.BuyerPurchaseOrderId.Value.ToString("D");
            string? supplierName = null;
            if (_relationships is not null)
            {
                var rel = await _relationships.GetAsync(o.RelationshipId, ct).ConfigureAwait(false);
                supplierName = rel?.SupplierDisplayNameSnapshot;
            }

            await _notifications.PublishAsync(
                orgId,
                o.BuyerOrganizationId.Value,
                ConnectedPurchaseOrderNotificationTypes.Preparing,
                o.BuyerPurchaseOrderId.Value.ToString("D"),
                "Purchase order preparing",
                $"{supplierName ?? "Supplier"} is preparing PO {poLabel}.",
                ct).ConfigureAwait(false);

            return ApplicationResult<ConnectedPurchaseOrderDto>.Success(ConnectedSupplierMapper.Map(o));
        }
        catch (DomainException ex)
        {
            return ConnectedSupplierUseCaseGuard.Failure<ConnectedPurchaseOrderDto>(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ConnectedSupplierUseCaseGuard.Failure<ConnectedPurchaseOrderDto>(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class MarkIncomingOrderFulfilled
{
    private readonly IConnectedPurchaseOrderRepository _orders;
    private readonly IOrganizationBusinessNotificationPublisher _notifications;
    private readonly IConnectedSupplierRelationshipRepository _relationships;
    private readonly IPosUnitOfWork _uow;
    private readonly IPosCommercialAccessAccessor _access;
    private readonly TimeProvider _clock;

    public MarkIncomingOrderFulfilled(
        IConnectedPurchaseOrderRepository orders,
        IPosUnitOfWork uow,
        IPosCommercialAccessAccessor access,
        TimeProvider? clock = null,
        IOrganizationBusinessNotificationPublisher? notifications = null,
        IConnectedSupplierRelationshipRepository? relationships = null)
    {
        _orders = orders;
        _uow = uow;
        _access = access;
        _clock = clock ?? TimeProvider.System;
        _notifications = notifications ?? new NoOpOrganizationBusinessNotificationPublisher();
        _relationships = relationships!;
    }

    public async Task<ApplicationResult<ConnectedPurchaseOrderDto>> ExecuteAsync(Guid orgId, Guid id, CancellationToken ct = default)
    {
        var gate = ConnectedSupplierUseCaseGuard.Access(_access, UtangCapability.ManagePurchasing);
        if (!gate.IsSuccess)
        {
            return ConnectedSupplierUseCaseGuard.Failure<ConnectedPurchaseOrderDto>(gate.ErrorCode!, gate.ErrorMessage!);
        }

        var o = await _orders.GetAsync(ConnectedPurchaseOrderId.From(id), ct).ConfigureAwait(false);
        if (o is null || o.SupplierOrganizationId != PosOrganizationId.From(orgId))
        {
            return ConnectedSupplierUseCaseGuard.Failure<ConnectedPurchaseOrderDto>(
                ConnectedSupplierErrorCodes.IncomingOrderNotFound, "Incoming order was not found.");
        }

        try
        {
            if (o.Status == ConnectedPurchaseOrderStatus.Fulfilled)
            {
                return ApplicationResult<ConnectedPurchaseOrderDto>.Success(ConnectedSupplierMapper.Map(o));
            }

            o.MarkFulfilled(_clock.GetUtcNow());
            await _orders.UpdateAsync(o, ct).ConfigureAwait(false);
            await _uow.SaveChangesAsync(ct).ConfigureAwait(false);

            var poLabel = o.BuyerPoNumber ?? o.BuyerPurchaseOrderId.Value.ToString("D");
            string? supplierName = null;
            if (_relationships is not null)
            {
                var rel = await _relationships.GetAsync(o.RelationshipId, ct).ConfigureAwait(false);
                supplierName = rel?.SupplierDisplayNameSnapshot;
            }

            await _notifications.PublishAsync(
                orgId,
                o.BuyerOrganizationId.Value,
                ConnectedPurchaseOrderNotificationTypes.Fulfilled,
                o.BuyerPurchaseOrderId.Value.ToString("D"),
                "Purchase order ready",
                $"PO {poLabel} is ready to receive.",
                ct).ConfigureAwait(false);

            return ApplicationResult<ConnectedPurchaseOrderDto>.Success(ConnectedSupplierMapper.Map(o));
        }
        catch (DomainException ex)
        {
            return ConnectedSupplierUseCaseGuard.Failure<ConnectedPurchaseOrderDto>(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ConnectedSupplierUseCaseGuard.Failure<ConnectedPurchaseOrderDto>(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class RevalidateConnectedPoDraft
{
    private readonly IConnectedSupplierRelationshipRepository _relationships;private readonly ISupplierProductExposureRepository _exposures;private readonly IPosCommercialAccessAccessor _access;
    private readonly IConnectedBuyerProductShareRepository _shares;
    public RevalidateConnectedPoDraft(IConnectedSupplierRelationshipRepository r,ISupplierProductExposureRepository e,IPosCommercialAccessAccessor a,IConnectedBuyerProductShareRepository shares){_relationships=r;_exposures=e;_access=a;_shares=shares;}
    public async Task<ApplicationResult<ConnectedPoDraftReviewDto>> ExecuteAsync(Guid orgId,Guid relationshipId,RevalidateConnectedPoDraftRequest request,CancellationToken ct=default)
    {var gate=ConnectedSupplierUseCaseGuard.Access(_access,UtangCapability.ViewPurchasing);if(!gate.IsSuccess)return ConnectedSupplierUseCaseGuard.Failure<ConnectedPoDraftReviewDto>(gate.ErrorCode!,gate.ErrorMessage!);
     var r=await _relationships.GetAsync(ConnectedSupplierRelationshipId.From(relationshipId),ct);if(r is null||r.BuyerOrganizationId!=PosOrganizationId.From(orgId))return ConnectedSupplierUseCaseGuard.Failure<ConnectedPoDraftReviewDto>(ConnectedSupplierErrorCodes.NotFound,"Relationship was not found.");
     if(r.Status!=ConnectedSupplierRelationshipStatus.Active)return ApplicationResult<ConnectedPoDraftReviewDto>.Success(new(ConnectedPoDraftReviewStatus.RelationshipInactive,
       request.Lines.Select(l=>new ConnectedPoDraftReviewItem(l.SupplierProductId,ConnectedPoDraftReviewStatus.RelationshipInactive,l.UnitPriceSnapshot,null)).ToList()));
     var items=new List<ConnectedPoDraftReviewItem>();foreach(var line in request.Lines){var productId=CatalogProductId.From(line.SupplierProductId);
       var e=await _exposures.GetByProductAsync(r.SupplierOrganizationId,productId,ct);var share=await _shares.FindAsync(r.Id,productId,ct);
       var price=0m;var available=e is not null&&ConnectedPoPricing.TryResolveEffectivePrice(e,share,r.CatalogSharingMode,r.CustomerDiscountPercent,null,out price,out _);
       var status=!available?ConnectedPoDraftReviewStatus.Unavailable:price!=Domain.Sales.SaleMoney.RoundMoney(line.UnitPriceSnapshot)?ConnectedPoDraftReviewStatus.PriceChanged:ConnectedPoDraftReviewStatus.Unchanged;
       items.Add(new(line.SupplierProductId,status,line.UnitPriceSnapshot,available?price:null));}
     var overall=items.Any(i=>i.Status==ConnectedPoDraftReviewStatus.Unavailable)?ConnectedPoDraftReviewStatus.Unavailable:
       items.Any(i=>i.Status==ConnectedPoDraftReviewStatus.PriceChanged)?ConnectedPoDraftReviewStatus.PriceChanged:ConnectedPoDraftReviewStatus.Unchanged;
     return ApplicationResult<ConnectedPoDraftReviewDto>.Success(new(overall,items));}
}
