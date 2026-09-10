using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Suppliers;

namespace ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;

public sealed record InviteBusinessCustomerRequest(
    string? BuyerPublicOrganizationIdOrQrPayload = null,
    Guid? BuyerOrganizationId = null,
    Guid? RequestedByUserId = null,
    Guid? SupplierBranchId = null);

/// <summary>
/// Seller invites a buyer Organization as a Business Customer (Pending, InitiatedByParty=Supplier).
/// Does not create a POSCustomer or a buyer-side Supplier master until the buyer Accepts.
/// </summary>
public sealed class InviteBusinessCustomerConnection
{
    private readonly IConnectedSupplierRelationshipRepository _relationships;
    private readonly IPosUnitOfWork _uow;
    private readonly IPosCommercialAccessAccessor _access;
    private readonly IPlatformOrganizationPublicResolve _organizationResolve;
    private readonly IPlatformSupplierLocationDirectory _supplierLocations;
    private readonly IOrganizationBusinessNotificationPublisher _notifications;
    private readonly TimeProvider _clock;

    public InviteBusinessCustomerConnection(
        IConnectedSupplierRelationshipRepository relationships,
        IPosUnitOfWork uow,
        IPosCommercialAccessAccessor access,
        IPlatformOrganizationPublicResolve organizationResolve,
        IPlatformSupplierLocationDirectory supplierLocations,
        IOrganizationBusinessNotificationPublisher? notifications = null,
        TimeProvider? clock = null)
    {
        _relationships = relationships;
        _uow = uow;
        _access = access;
        _organizationResolve = organizationResolve;
        _supplierLocations = supplierLocations;
        _notifications = notifications ?? new NoOpOrganizationBusinessNotificationPublisher();
        _clock = clock ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<ConnectedSupplierRelationshipDto>> ExecuteAsync(
        Guid supplierOrganizationId,
        InviteBusinessCustomerRequest request,
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
            var resolvedBuyer = await ResolveBuyerOrganizationAsync(request, ct).ConfigureAwait(false);
            if (!resolvedBuyer.IsSuccess || resolvedBuyer.Value is null)
            {
                return ConnectedSupplierUseCaseGuard.Failure<ConnectedSupplierRelationshipDto>(
                    resolvedBuyer.ErrorCode ?? ApplicationErrorCodes.DomainViolation,
                    resolvedBuyer.ErrorMessage ?? "Could not resolve the buyer organization.");
            }

            var supplier = PosOrganizationId.From(supplierOrganizationId);
            var buyer = PosOrganizationId.From(resolvedBuyer.Value.OrganizationId);
            if (buyer == supplier)
            {
                return ConnectedSupplierUseCaseGuard.Failure<ConnectedSupplierRelationshipDto>(
                    ConnectedSupplierDomainErrorCodes.SelfConnection,
                    "You can't connect your business to itself.");
            }

            if (await _relationships.FindOpenAsync(buyer, supplier, ct).ConfigureAwait(false) is { } existing)
            {
                if (existing.Status == ConnectedSupplierRelationshipStatus.Active)
                {
                    return ConnectedSupplierUseCaseGuard.Failure<ConnectedSupplierRelationshipDto>(
                        ConnectedSupplierErrorCodes.DuplicateRelationship,
                        "Already connected. Open the existing business customer.");
                }

                if (existing.InitiatedByParty == ConnectionInitiatedByParty.Buyer)
                {
                    return ConnectedSupplierUseCaseGuard.Failure<ConnectedSupplierRelationshipDto>(
                        ConnectedSupplierErrorCodes.PendingBuyerRequestExists,
                        "This business already sent your business a connection request. Review it under Connection requests.");
                }

                return ConnectedSupplierUseCaseGuard.Failure<ConnectedSupplierRelationshipDto>(
                    ConnectedSupplierErrorCodes.DuplicateRelationship,
                    "A connection request is already pending for this business.");
            }

            var supplierPublicId = await ResolveOwnPublicIdAsync(supplierOrganizationId, ct)
                .ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(supplierPublicId))
            {
                return ConnectedSupplierUseCaseGuard.Failure<ConnectedSupplierRelationshipDto>(
                    ConnectedSupplierErrorCodes.NotFound,
                    "Could not resolve your business public organization id.");
            }

            var location = await ResolveOwnSupplierLocationAsync(
                    supplierPublicId,
                    request.SupplierBranchId,
                    ct)
                .ConfigureAwait(false);
            if (!location.IsSuccess || location.Value is null)
            {
                return ConnectedSupplierUseCaseGuard.Failure<ConnectedSupplierRelationshipDto>(
                    location.ErrorCode ?? DomainErrorCodes.ConnectedSupplierBranchRequired,
                    location.ErrorMessage ?? "Choose which supplier location this invitation uses.");
            }

            string? supplierDisplayName = null;
            string? supplierPublic = supplierPublicId;
            var supplierIdentity = await _organizationResolve
                .GetOrganizationPublicIdentityAsync(supplierOrganizationId, ct)
                .ConfigureAwait(false);
            if (supplierIdentity.IsSuccess && supplierIdentity.Value is not null)
            {
                supplierDisplayName = supplierIdentity.Value.DisplayName;
                supplierPublic = supplierIdentity.Value.PublicOrganizationId;
            }

            var utcNow = _clock.GetUtcNow();
            var relationship = ConnectedSupplierRelationship.InviteBuyer(
                buyer,
                supplier,
                utcNow,
                request.RequestedByUserId,
                buyerDisplayName: resolvedBuyer.Value.DisplayName,
                buyerPublicOrganizationId: resolvedBuyer.Value.PublicOrganizationId,
                supplierDisplayName: supplierDisplayName,
                supplierPublicOrganizationId: supplierPublic,
                supplierBranchId: location.Value.BranchId,
                supplierBranchName: location.Value.Name);

            await _relationships.AddAsync(relationship, ct).ConfigureAwait(false);
            await _uow.SaveChangesAsync(ct).ConfigureAwait(false);

            var supplierName = string.IsNullOrWhiteSpace(supplierDisplayName)
                ? (supplierPublic ?? "A business")
                : supplierDisplayName!;
            await _notifications.PublishAsync(
                supplierOrganizationId,
                buyer.Value,
                BusinessCustomerConnectionNotificationTypes.Requested,
                relationship.Id.Value.ToString("D"),
                "Business connection request",
                $"{supplierName} wants to connect as a supplier to your business.",
                ct).ConfigureAwait(false);

            return ApplicationResult<ConnectedSupplierRelationshipDto>.Success(
                ConnectedSupplierMapper.Map(relationship, supplierView: true));
        }
        catch (DomainException ex)
        {
            return ConnectedSupplierUseCaseGuard.Failure<ConnectedSupplierRelationshipDto>(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException)
        {
            return ConnectedSupplierUseCaseGuard.Failure<ConnectedSupplierRelationshipDto>(
                ConnectedSupplierErrorCodes.DuplicateRelationship,
                "A connection request is already pending for this business.");
        }
    }

    private async Task<ApplicationResult<PlatformOrganizationPublicResolveResult>> ResolveBuyerOrganizationAsync(
        InviteBusinessCustomerRequest request,
        CancellationToken ct)
    {
        var payload = request.BuyerPublicOrganizationIdOrQrPayload?.Trim();
        if (string.IsNullOrWhiteSpace(payload))
        {
            return ApplicationResult<PlatformOrganizationPublicResolveResult>.Failure(
                DomainErrorCodes.ConnectedSupplierRequiresBusinessQr,
                "Scan or enter the buyer Business QR / organization ID (ORG######).");
        }

        var resolved = await _organizationResolve
            .ResolveOrganizationForConnectedSupplierAsync(payload, ct)
            .ConfigureAwait(false);
        if (resolved.IsSuccess && resolved.Value is not null)
        {
            if (request.BuyerOrganizationId is Guid supplied
                && supplied != Guid.Empty
                && supplied != resolved.Value.OrganizationId)
            {
                return ApplicationResult<PlatformOrganizationPublicResolveResult>.Failure(
                    ConnectedSupplierErrorCodes.OrganizationMismatch,
                    "The scanned Business QR does not match the organization id that was provided.");
            }

            return resolved;
        }

        if (request.BuyerOrganizationId is Guid clientOrgId
            && clientOrgId != Guid.Empty
            && LooksLikePublicOrganizationId(payload))
        {
            var publicId = payload.Trim().ToUpperInvariant();
            return ApplicationResult<PlatformOrganizationPublicResolveResult>.Success(
                new PlatformOrganizationPublicResolveResult(clientOrgId, publicId, publicId));
        }

        return resolved;
    }

    private async Task<string?> ResolveOwnPublicIdAsync(Guid organizationId, CancellationToken ct)
    {
        var identity = await _organizationResolve
            .GetOrganizationPublicIdentityAsync(organizationId, ct)
            .ConfigureAwait(false);
        return identity.IsSuccess ? identity.Value?.PublicOrganizationId : null;
    }

    private async Task<ApplicationResult<PlatformSupplierLocationDto>> ResolveOwnSupplierLocationAsync(
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
                "Your business has no active locations for this invitation.");
        }

        if (requestedBranchId is Guid branchId && branchId != Guid.Empty)
        {
            var match = active.FirstOrDefault(x => x.BranchId == branchId);
            if (match is null)
            {
                return ApplicationResult<PlatformSupplierLocationDto>.Failure(
                    DomainErrorCodes.ConnectedSupplierBranchInvalid,
                    "That location is not an active branch of your business.");
            }

            return ApplicationResult<PlatformSupplierLocationDto>.Success(match);
        }

        if (active.Count == 1)
        {
            return ApplicationResult<PlatformSupplierLocationDto>.Success(active[0]);
        }

        return ApplicationResult<PlatformSupplierLocationDto>.Failure(
            DomainErrorCodes.ConnectedSupplierBranchRequired,
            "Choose which supplier location this invitation uses.");
    }

    private static bool LooksLikePublicOrganizationId(string payload) =>
        payload.Trim().StartsWith("ORG", StringComparison.OrdinalIgnoreCase);
}

/// <summary>Pending connection requests where the current organization is the recipient.</summary>
public sealed class ListIncomingConnectionRequests
{
    private readonly IConnectedSupplierRelationshipRepository _relationships;
    private readonly IPosCommercialAccessAccessor _access;
    private readonly IAuthorizedBranchGroupingDirectory? _branchAccess;

    public ListIncomingConnectionRequests(
        IConnectedSupplierRelationshipRepository relationships,
        IPosCommercialAccessAccessor access,
        IAuthorizedBranchGroupingDirectory? branchAccess = null)
    {
        _relationships = relationships;
        _access = access;
        _branchAccess = branchAccess;
    }

    public async Task<ApplicationResult<IReadOnlyList<ConnectedSupplierRelationshipDto>>> ExecuteAsync(
        Guid organizationId,
        CancellationToken ct = default)
    {
        var gate = ConnectedSupplierUseCaseGuard.Access(_access, UtangCapability.ManageSuppliers);
        if (!gate.IsSuccess)
        {
            return ConnectedSupplierUseCaseGuard.Failure<IReadOnlyList<ConnectedSupplierRelationshipDto>>(
                gate.ErrorCode!,
                gate.ErrorMessage!);
        }

        var org = PosOrganizationId.From(organizationId);
        var asSupplier = await _relationships.ListAsync(org, supplierView: true, ct).ConfigureAwait(false);
        var asBuyer = await _relationships.ListAsync(org, supplierView: false, ct).ConfigureAwait(false);

        var incoming = asSupplier
            .Concat(asBuyer)
            .Where(r => r.Status == ConnectedSupplierRelationshipStatus.Pending && r.IsRecipient(org))
            .GroupBy(r => r.Id.Value)
            .Select(g => g.First())
            .OrderByDescending(r => r.RequestedAtUtc)
            .ToList();

        if (_branchAccess is not null)
        {
            var scope = await _branchAccess.ListAuthorizedAsync(organizationId, ct).ConfigureAwait(false);
            if (!scope.IsOrganizationWide)
            {
                var authorized = scope.Branches.Select(b => b.BranchId).ToHashSet();
                incoming = incoming
                    .Where(r =>
                        r.InitiatedByParty == ConnectionInitiatedByParty.Supplier
                        || (r.SupplierBranchId is Guid branchId && authorized.Contains(branchId)))
                    .ToList();
            }
        }

        var mapped = incoming
            .Select(r => ConnectedSupplierMapper.Map(r, supplierView: r.SupplierOrganizationId == org))
            .ToList();
        return ApplicationResult<IReadOnlyList<ConnectedSupplierRelationshipDto>>.Success(mapped);
    }
}

/// <summary>Creates exactly one buyer-side connected Supplier master when a seller invitation is accepted.</summary>
public static class BuyerConnectedSupplierMaster
{
    public static async Task EnsureAsync(
        ISupplierRepository suppliers,
        ConnectedSupplierRelationship relationship,
        DateTimeOffset utcNow,
        CancellationToken ct)
    {
        var buyer = relationship.BuyerOrganizationId;
        var existing = await suppliers
            .FindByConnectedRelationshipIdAsync(buyer, relationship.Id, ct)
            .ConfigureAwait(false);
        if (existing is not null)
        {
            return;
        }

        var supplierName = string.IsNullOrWhiteSpace(relationship.SupplierDisplayNameSnapshot)
            ? (relationship.SupplierPublicOrganizationIdSnapshot ?? "Connected supplier")
            : relationship.SupplierDisplayNameSnapshot!;
        var normalizedName = Supplier.Normalize(Supplier.NormalizeName(supplierName));
        var nameConflict = await suppliers
            .FindActiveByNormalizedNameAsync(buyer, normalizedName, ct)
            .ConfigureAwait(false);
        if (nameConflict is not null)
        {
            supplierName =
                $"{supplierName} ({relationship.SupplierPublicOrganizationIdSnapshot ?? relationship.SupplierOrganizationId.Value.ToString("N")[..8]})";
        }

        var code = await suppliers.AllocateNextSupplierCodeAsync(buyer, ct).ConfigureAwait(false);
        var master = Supplier.Create(
            buyer,
            code,
            supplierName,
            utcNow,
            notes: relationship.SupplierPublicOrganizationIdSnapshot);
        master.AttachConnectedRelationship(relationship.Id, utcNow);
        await suppliers.AddAsync(master, ct).ConfigureAwait(false);
    }
}
