using ExItS.PinoyBusinessPOS.Application.Branches;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Parties;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Parties;

namespace ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;

public sealed record BusinessCustomerBranchAccessListDto(
    Guid ConnectionId,
    Guid? HomeBranchId,
    IReadOnlyList<CustomerBranchAccessItemDto> Items);

/// <summary>
/// Manage explicit Business Customer branch visibility (home = SupplierBranchId).
/// Persist branch ids only — Area selection is a UI bulk helper.
/// </summary>
public sealed class BusinessCustomerBranchAccessService
{
    private readonly IConnectedSupplierRelationshipRepository _relationships;
    private readonly IPosUnitOfWork _uow;
    private readonly IPosCommercialAccessAccessor _access;
    private readonly PartyBranchAccessGovernanceAuthority _governance;
    private readonly IPartyBranchAccessActorAccessor _actorAccessor;
    private readonly TimeProvider _clock;

    public BusinessCustomerBranchAccessService(
        IConnectedSupplierRelationshipRepository relationships,
        IPosUnitOfWork uow,
        IPosCommercialAccessAccessor access,
        PartyBranchAccessGovernanceAuthority governance,
        IPartyBranchAccessActorAccessor actorAccessor,
        TimeProvider? clock = null)
    {
        _relationships = relationships;
        _uow = uow;
        _access = access;
        _governance = governance;
        _actorAccessor = actorAccessor;
        _clock = clock ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<BusinessCustomerBranchAccessListDto>> ListAsync(
        Guid supplierOrganizationId,
        Guid connectionId,
        CancellationToken ct = default)
    {
        var gate = RequireGovernance();
        if (!gate.IsSuccess)
        {
            return ApplicationResult<BusinessCustomerBranchAccessListDto>.Failure(gate);
        }

        var relationship = await LoadOwnedAsync(supplierOrganizationId, connectionId, ct)
            .ConfigureAwait(false);
        if (relationship is null)
        {
            return ConnectedSupplierUseCaseGuard.Failure<BusinessCustomerBranchAccessListDto>(
                ConnectedSupplierErrorCodes.NotFound,
                "Business customer relationship was not found.");
        }

        return ApplicationResult<BusinessCustomerBranchAccessListDto>.Success(MapList(relationship));
    }

    public async Task<ApplicationResult> GrantAsync(
        Guid supplierOrganizationId,
        Guid connectionId,
        GrantPartyBranchAccessRequest request,
        CancellationToken ct = default)
    {
        var gate = RequireGovernance();
        if (!gate.IsSuccess)
        {
            return gate;
        }

        if (request.BranchId == Guid.Empty)
        {
            return ApplicationResult.Failure(
                DomainErrorCodes.InvalidBranchId,
                "BranchId is required.");
        }

        var relationship = await LoadOwnedAsync(supplierOrganizationId, connectionId, ct)
            .ConfigureAwait(false);
        if (relationship is null)
        {
            return ApplicationResult.Failure(
                ConnectedSupplierErrorCodes.NotFound,
                "Business customer relationship was not found.");
        }

        try
        {
            relationship.ShareSupplierBranch(request.BranchId, _clock.GetUtcNow());
            await _relationships.UpdateAsync(relationship, ct).ConfigureAwait(false);
            await _uow.SaveChangesAsync(ct).ConfigureAwait(false);
            return ApplicationResult.Success();
        }
        catch (DomainException ex)
        {
            return ApplicationResult.Failure(ex.ErrorCode, ex.Message);
        }
    }

    public async Task<ApplicationResult> RevokeAsync(
        Guid supplierOrganizationId,
        Guid connectionId,
        GrantPartyBranchAccessRequest request,
        CancellationToken ct = default)
    {
        var gate = RequireGovernance();
        if (!gate.IsSuccess)
        {
            return gate;
        }

        var relationship = await LoadOwnedAsync(supplierOrganizationId, connectionId, ct)
            .ConfigureAwait(false);
        if (relationship is null)
        {
            return ApplicationResult.Failure(
                ConnectedSupplierErrorCodes.NotFound,
                "Business customer relationship was not found.");
        }

        try
        {
            relationship.UnshareSupplierBranch(request.BranchId, _clock.GetUtcNow());
            await _relationships.UpdateAsync(relationship, ct).ConfigureAwait(false);
            await _uow.SaveChangesAsync(ct).ConfigureAwait(false);
            return ApplicationResult.Success();
        }
        catch (DomainException ex)
        {
            return ApplicationResult.Failure(ex.ErrorCode, ex.Message);
        }
    }

    private async Task<ConnectedSupplierRelationship?> LoadOwnedAsync(
        Guid supplierOrganizationId,
        Guid connectionId,
        CancellationToken ct)
    {
        var relationship = await _relationships
            .GetAsync(ConnectedSupplierRelationshipId.From(connectionId), ct)
            .ConfigureAwait(false);
        var supplier = PosOrganizationId.From(supplierOrganizationId);
        if (relationship is null || relationship.SupplierOrganizationId != supplier)
        {
            return null;
        }

        return relationship;
    }

    private ApplicationResult RequireGovernance()
    {
        var commercial = ConnectedSupplierUseCaseGuard.Access(_access, UtangCapability.ManageSuppliers);
        if (!commercial.IsSuccess)
        {
            return commercial;
        }

        if (!_governance.CanBypassBranchFilter(_actorAccessor.GetActor()))
        {
            return ApplicationResult.Failure(
                ApplicationErrorCodes.CustomerBranchAccessForbidden,
                "Only Owner/Admin can manage business customer branch access.");
        }

        return ApplicationResult.Success();
    }

    private static BusinessCustomerBranchAccessListDto MapList(ConnectedSupplierRelationship r)
    {
        var items = new List<CustomerBranchAccessItemDto>();
        if (r.SupplierBranchId is Guid home)
        {
            items.Add(new CustomerBranchAccessItemDto(
                home,
                PartyBranchGrantSources.ToCode(PartyBranchGrantSource.CreateAtBranch),
                r.CreatedAtUtc));
        }

        foreach (var branchId in r.SharedSupplierBranchIds)
        {
            items.Add(new CustomerBranchAccessItemDto(
                branchId,
                PartyBranchGrantSources.ToCode(PartyBranchGrantSource.ExplicitAssign),
                r.UpdatedAtUtc));
        }

        return new BusinessCustomerBranchAccessListDto(
            r.Id.Value,
            r.SupplierBranchId,
            items);
    }
}
