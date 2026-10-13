using ExItS.Platform.Application.Access;
using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Application.Subscriptions;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;

namespace ExItS.Platform.Application.Personal;

public sealed record PersonalProductAffiliationDto(
    string ProductCode,
    string ProductDisplayName,
    Guid? OrganizationId,
    string? OrganizationDisplayName,
    string? MembershipRole,
    string? RoleDisplay,
    string? PlanKey,
    string? PlanDisplayName,
    string? SubscriptionStatus,
    DateTimeOffset? TrialEndUtc,
    bool CanManageBilling);

/// <summary>
/// Personal subscription hub: one row per catalog product and the caller's current affiliation.
/// </summary>
public sealed class ListPersonalProductAffiliations
{
    private readonly IProductRepository _products;
    private readonly IPlatformOrganizationRepository _organizations;
    private readonly IOrganizationMembershipRepository _memberships;
    private readonly ISubscriptionRepository _subscriptions;
    private readonly IPlanRepository _plans;
    private readonly IProductLocalRoleGrantRepository _roleGrants;
    private readonly IPlatformUserRepository _users;
    private readonly UserProductAffiliationGuard _affiliations;

    public ListPersonalProductAffiliations(
        IProductRepository products,
        IPlatformOrganizationRepository organizations,
        IOrganizationMembershipRepository memberships,
        ISubscriptionRepository subscriptions,
        IPlanRepository plans,
        IProductLocalRoleGrantRepository roleGrants,
        IPlatformUserRepository users,
        UserProductAffiliationGuard affiliations)
    {
        _products = products;
        _organizations = organizations;
        _memberships = memberships;
        _subscriptions = subscriptions;
        _plans = plans;
        _roleGrants = roleGrants;
        _users = users;
        _affiliations = affiliations;
    }

    public async Task<ApplicationResult<IReadOnlyList<PersonalProductAffiliationDto>>> ExecuteAsync(
        PlatformUserId personalUserId,
        CancellationToken cancellationToken = default)
    {
        var catalog = await _products
            .ListAsync(ProductStatus.Active, skip: 0, take: 50, cancellationToken)
            .ConfigureAwait(false);
        var staffUsers = await _users
            .ListStaffLinkedToPersonalUserAsync(personalUserId, cancellationToken)
            .ConfigureAwait(false);

        var rows = new List<PersonalProductAffiliationDto>();
        foreach (var product in catalog.Items.OrderBy(p => p.DisplayName, StringComparer.OrdinalIgnoreCase))
        {
            var (subjectUserId, affiliation) = await ResolveAffiliationAsync(
                personalUserId,
                staffUsers,
                product.Code,
                cancellationToken).ConfigureAwait(false);
            if (affiliation is null || subjectUserId is null)
            {
                rows.Add(new PersonalProductAffiliationDto(
                    product.Code.Value,
                    product.DisplayName,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    false));
                continue;
            }

            var organizationId = PlatformOrganizationId.From(affiliation.OrganizationId);
            var organization = await _organizations
                .GetByIdAsync(organizationId, cancellationToken)
                .ConfigureAwait(false);
            var membership = await _memberships
                .FindCurrentByUserAndOrganizationAsync(subjectUserId, organizationId, cancellationToken)
                .ConfigureAwait(false);
            var subscription = await _subscriptions
                .GetCurrentForOrganizationProductAsync(organizationId, product.Code, cancellationToken)
                .ConfigureAwait(false);
            var plan = subscription is null
                ? null
                : await _plans.GetByIdAsync(subscription.PlanId, cancellationToken).ConfigureAwait(false);
            var roleDisplay = await ResolveRoleDisplayAsync(
                membership,
                organizationId,
                subjectUserId,
                product.Code,
                cancellationToken).ConfigureAwait(false);

            rows.Add(new PersonalProductAffiliationDto(
                product.Code.Value,
                product.DisplayName,
                organization?.Id.Value,
                organization?.DisplayName,
                membership?.Role.ToString(),
                roleDisplay,
                plan?.PlanKey,
                plan?.DisplayName,
                subscription?.Status.ToString(),
                subscription?.TrialEndUtc,
                affiliation.CanManageBilling));
        }

        return ApplicationResult<IReadOnlyList<PersonalProductAffiliationDto>>.Success(rows);
    }

    private async Task<(PlatformUserId? UserId, UserProductAffiliation? Affiliation)> ResolveAffiliationAsync(
        PlatformUserId personalUserId,
        IReadOnlyList<PlatformUser> staffUsers,
        ProductCode productCode,
        CancellationToken cancellationToken)
    {
        var own = await _affiliations
            .FindAsync(personalUserId, productCode, cancellationToken)
            .ConfigureAwait(false);
        if (own is not null)
        {
            return (personalUserId, own);
        }

        foreach (var staffUser in staffUsers)
        {
            if (staffUser.Status != AccountStatus.Active)
            {
                continue;
            }

            var staffAffiliation = await _affiliations
                .FindAsync(staffUser.Id, productCode, cancellationToken)
                .ConfigureAwait(false);
            if (staffAffiliation is not null)
            {
                return (staffUser.Id, staffAffiliation);
            }
        }

        return (null, null);
    }

    private async Task<string?> ResolveRoleDisplayAsync(
        OrganizationMembership? membership,
        PlatformOrganizationId organizationId,
        PlatformUserId userId,
        ProductCode productCode,
        CancellationToken cancellationToken)
    {
        if (membership is null)
        {
            return null;
        }

        var grants = await _roleGrants
            .ListActiveByUserOrganizationAsync(organizationId, userId, cancellationToken)
            .ConfigureAwait(false);
        var productRole = grants
            .Where(g => g.ProductCode == productCode.Value)
            .Select(g => g.RoleCode)
            .OrderBy(code => code, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(productRole))
        {
            return ProductRoleDisplay.ToDisplayLabel(productRole);
        }

        return OrganizationRoleDisplay.ToDisplayLabel(membership.Role);
    }
}
