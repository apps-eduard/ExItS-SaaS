using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Inventory;

namespace ExItS.PinoyBusinessPOS.Api.Inventory;

/// <summary>
/// Hierarchical stop-selling policy for normal POS sales (org → org category → branch → branch category).
/// Does not mutate ExpirationWarningDays / branch expiration tracking enablement.
/// </summary>
internal static class InventoryExpirySalePolicyEndpoints
{
    public static IEndpointRouteBuilder MapInventoryExpirySalePolicyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/pos/inventory/expiry-sale-policy");

        group.MapGet("/organization", GetOrganizationDefault);
        group.MapPut("/organization", PutOrganizationDefault);
        group.MapGet("/organization/categories", ListOrganizationCategories);
        group.MapPut("/organization/categories/{categoryId:guid}", PutOrganizationCategory);
        group.MapDelete("/organization/categories/{categoryId:guid}", DeleteOrganizationCategory);

        group.MapGet("/branch", GetBranchDefault);
        group.MapPut("/branch", PutBranchDefault);
        group.MapDelete("/branch", DeleteBranchDefault);
        group.MapGet("/branch/categories", ListBranchCategories);
        group.MapPut("/branch/categories/{categoryId:guid}", PutBranchCategory);
        group.MapDelete("/branch/categories/{categoryId:guid}", DeleteBranchCategory);

        group.MapGet("/effective", GetEffective);

        return app;
    }

    private static async Task<IResult> GetOrganizationDefault(
        HttpRequest request,
        GetOrganizationExpirySalePolicy useCase,
        IPosCommercialAccessAccessor access,
        CancellationToken ct)
    {
        if (!TryAuthorize(request, access, UtangCapability.ViewInventory, out var organizationId, out var problem))
        {
            return problem!;
        }

        var dto = await useCase.ExecuteAsync(organizationId, ct).ConfigureAwait(false);
        return Results.Ok(dto);
    }

    private static async Task<IResult> PutOrganizationDefault(
        HttpRequest request,
        UpsertExpirySalePolicyRequest body,
        PutOrganizationExpirySalePolicy useCase,
        IPosCommercialAccessAccessor access,
        CancellationToken ct)
    {
        // Org inventory policy: ManageInventory (same capability owners use for org inventory mutations).
        if (!TryAuthorize(request, access, UtangCapability.ManageInventory, out var organizationId, out var problem)
            || !PosOrganizationScope.TryGetActorId(request, out var actorId, out problem))
        {
            return problem!;
        }

        var result = await useCase.ExecuteAsync(organizationId, body, actorId, ct).ConfigureAwait(false);
        return PosApiResults.FromResult(result, Results.Ok);
    }

    private static async Task<IResult> ListOrganizationCategories(
        HttpRequest request,
        ListOrganizationCategoryExpirySalePolicies useCase,
        IPosCommercialAccessAccessor access,
        CancellationToken ct)
    {
        if (!TryAuthorize(request, access, UtangCapability.ViewInventory, out var organizationId, out var problem))
        {
            return problem!;
        }

        var dto = await useCase.ExecuteAsync(organizationId, ct).ConfigureAwait(false);
        return Results.Ok(dto);
    }

    private static async Task<IResult> PutOrganizationCategory(
        HttpRequest request,
        Guid categoryId,
        UpsertExpirySalePolicyRequest body,
        PutOrganizationCategoryExpirySalePolicy useCase,
        IPosCommercialAccessAccessor access,
        CancellationToken ct)
    {
        if (!TryAuthorize(request, access, UtangCapability.ManageInventory, out var organizationId, out var problem)
            || !PosOrganizationScope.TryGetActorId(request, out var actorId, out problem))
        {
            return problem!;
        }

        var result = await useCase.ExecuteAsync(organizationId, categoryId, body, actorId, ct).ConfigureAwait(false);
        return PosApiResults.FromResult(result, Results.Ok);
    }

    private static async Task<IResult> DeleteOrganizationCategory(
        HttpRequest request,
        Guid categoryId,
        DeleteOrganizationCategoryExpirySalePolicy useCase,
        IPosCommercialAccessAccessor access,
        CancellationToken ct)
    {
        if (!TryAuthorize(request, access, UtangCapability.ManageInventory, out var organizationId, out var problem))
        {
            return problem!;
        }

        var result = await useCase.ExecuteAsync(organizationId, categoryId, ct).ConfigureAwait(false);
        return PosApiResults.FromResult(result, () => Results.NoContent());
    }

    private static async Task<IResult> GetBranchDefault(
        HttpRequest request,
        GetBranchExpirySalePolicy useCase,
        BranchInventoryContextResolver branchResolver,
        IPosCommercialAccessAccessor access,
        CancellationToken ct)
    {
        if (!TryAuthorize(request, access, UtangCapability.ViewInventory, out var organizationId, out var problem))
        {
            return problem!;
        }

        var branchResolved = await ResolveActingBranchAsync(request, organizationId, branchResolver, ct)
            .ConfigureAwait(false);
        if (!branchResolved.Success)
        {
            return branchResolved.Problem!;
        }

        var dto = await useCase
            .ExecuteAsync(organizationId, branchResolved.BranchId, ct)
            .ConfigureAwait(false);
        return Results.Ok(dto);
    }

    private static async Task<IResult> PutBranchDefault(
        HttpRequest request,
        UpsertExpirySalePolicyRequest body,
        PutBranchExpirySalePolicy useCase,
        BranchInventoryContextResolver branchResolver,
        IPosCommercialAccessAccessor access,
        CancellationToken ct)
    {
        if (!TryAuthorize(request, access, UtangCapability.ManageInventory, out var organizationId, out var problem)
            || !PosOrganizationScope.TryGetActorId(request, out var actorId, out problem))
        {
            return problem!;
        }

        var branchResolved = await ResolveActingBranchAsync(request, organizationId, branchResolver, ct)
            .ConfigureAwait(false);
        if (!branchResolved.Success)
        {
            return branchResolved.Problem!;
        }

        var result = await useCase
            .ExecuteAsync(organizationId, branchResolved.BranchId, body, actorId, ct)
            .ConfigureAwait(false);
        return PosApiResults.FromResult(result, Results.Ok);
    }

    private static async Task<IResult> DeleteBranchDefault(
        HttpRequest request,
        DeleteBranchExpirySalePolicy useCase,
        BranchInventoryContextResolver branchResolver,
        IPosCommercialAccessAccessor access,
        CancellationToken ct)
    {
        if (!TryAuthorize(request, access, UtangCapability.ManageInventory, out var organizationId, out var problem))
        {
            return problem!;
        }

        var branchResolved = await ResolveActingBranchAsync(request, organizationId, branchResolver, ct)
            .ConfigureAwait(false);
        if (!branchResolved.Success)
        {
            return branchResolved.Problem!;
        }

        var result = await useCase
            .ExecuteAsync(organizationId, branchResolved.BranchId, ct)
            .ConfigureAwait(false);
        return PosApiResults.FromResult(result, () => Results.NoContent());
    }

    private static async Task<IResult> ListBranchCategories(
        HttpRequest request,
        ListBranchCategoryExpirySalePolicies useCase,
        BranchInventoryContextResolver branchResolver,
        IPosCommercialAccessAccessor access,
        CancellationToken ct)
    {
        if (!TryAuthorize(request, access, UtangCapability.ViewInventory, out var organizationId, out var problem))
        {
            return problem!;
        }

        var branchResolved = await ResolveActingBranchAsync(request, organizationId, branchResolver, ct)
            .ConfigureAwait(false);
        if (!branchResolved.Success)
        {
            return branchResolved.Problem!;
        }

        var dto = await useCase
            .ExecuteAsync(organizationId, branchResolved.BranchId, ct)
            .ConfigureAwait(false);
        return Results.Ok(dto);
    }

    private static async Task<IResult> PutBranchCategory(
        HttpRequest request,
        Guid categoryId,
        UpsertExpirySalePolicyRequest body,
        PutBranchCategoryExpirySalePolicy useCase,
        BranchInventoryContextResolver branchResolver,
        IPosCommercialAccessAccessor access,
        CancellationToken ct)
    {
        if (!TryAuthorize(request, access, UtangCapability.ManageInventory, out var organizationId, out var problem)
            || !PosOrganizationScope.TryGetActorId(request, out var actorId, out problem))
        {
            return problem!;
        }

        var branchResolved = await ResolveActingBranchAsync(request, organizationId, branchResolver, ct)
            .ConfigureAwait(false);
        if (!branchResolved.Success)
        {
            return branchResolved.Problem!;
        }

        var result = await useCase
            .ExecuteAsync(organizationId, branchResolved.BranchId, categoryId, body, actorId, ct)
            .ConfigureAwait(false);
        return PosApiResults.FromResult(result, Results.Ok);
    }

    private static async Task<IResult> DeleteBranchCategory(
        HttpRequest request,
        Guid categoryId,
        DeleteBranchCategoryExpirySalePolicy useCase,
        BranchInventoryContextResolver branchResolver,
        IPosCommercialAccessAccessor access,
        CancellationToken ct)
    {
        if (!TryAuthorize(request, access, UtangCapability.ManageInventory, out var organizationId, out var problem))
        {
            return problem!;
        }

        var branchResolved = await ResolveActingBranchAsync(request, organizationId, branchResolver, ct)
            .ConfigureAwait(false);
        if (!branchResolved.Success)
        {
            return branchResolved.Problem!;
        }

        var result = await useCase
            .ExecuteAsync(organizationId, branchResolved.BranchId, categoryId, ct)
            .ConfigureAwait(false);
        return PosApiResults.FromResult(result, () => Results.NoContent());
    }

    private static async Task<IResult> GetEffective(
        HttpRequest request,
        Guid? categoryId,
        GetEffectiveExpirySalePolicy useCase,
        BranchInventoryContextResolver branchResolver,
        IPosCommercialAccessAccessor access,
        CancellationToken ct)
    {
        if (!TryAuthorize(request, access, UtangCapability.ViewInventory, out var organizationId, out var problem))
        {
            return problem!;
        }

        var branchResolved = await ResolveActingBranchAsync(request, organizationId, branchResolver, ct)
            .ConfigureAwait(false);
        if (!branchResolved.Success)
        {
            return branchResolved.Problem!;
        }

        var dto = await useCase
            .ExecuteAsync(organizationId, branchResolved.BranchId, categoryId, ct)
            .ConfigureAwait(false);
        return Results.Ok(dto);
    }

    private static async Task<(bool Success, Guid BranchId, IResult? Problem)> ResolveActingBranchAsync(
        HttpRequest request,
        Guid organizationId,
        BranchInventoryContextResolver resolver,
        CancellationToken ct)
    {
        if (!PosOrganizationScope.TryGetOptionalBranchId(request, out var branchId) || branchId is null)
        {
            return (false, Guid.Empty, PosApiResults.Problem(
                ApplicationErrorCodes.InventoryBranchRequired,
                "Header 'X-Pos-Branch-Id' is required for branch inventory.",
                StatusCodes.Status400BadRequest));
        }

        var resolved = await resolver.ResolveAsync(organizationId, branchId.Value, ct).ConfigureAwait(false);
        if (!resolved.IsSuccess)
        {
            return (false, Guid.Empty, PosApiResults.Problem(
                resolved.ErrorCode!,
                resolved.ErrorMessage!,
                PosApiResults.MapStatusCode(resolved.ErrorCode!)));
        }

        return (true, resolved.Value!.BranchId, null);
    }

    private static bool TryAuthorize(
        HttpRequest request,
        IPosCommercialAccessAccessor access,
        UtangCapability capability,
        out Guid organizationId,
        out IResult? problem)
    {
        if (!PosOrganizationScope.TryGetOrganizationId(request, out organizationId, out problem))
        {
            return false;
        }

        return PosCommercialScope.TryAuthorize(access, capability, out problem);
    }
}
