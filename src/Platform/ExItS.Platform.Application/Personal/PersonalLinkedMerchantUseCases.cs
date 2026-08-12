using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Application.Personal;

public sealed record LinkedMerchantDto(
    Guid LinkedCustomerId,
    Guid BusinessCustomerId,
    Guid OrganizationId,
    string OrganizationDisplayName,
    string CustomerDisplayName,
    string LinkStatus,
    DateTimeOffset LinkedAtUtc);

public sealed class ListLinkedMerchantsForPersonalUser
{
    private readonly ILinkedCustomerAppUserRepository _links;
    private readonly IBusinessCustomerRepository _customers;
    private readonly IPlatformOrganizationRepository _organizations;

    public ListLinkedMerchantsForPersonalUser(
        ILinkedCustomerAppUserRepository links,
        IBusinessCustomerRepository customers,
        IPlatformOrganizationRepository organizations)
    {
        _links = links;
        _customers = customers;
        _organizations = organizations;
    }

    public async Task<PagedResult<LinkedMerchantDto>> ExecuteAsync(
        PlatformUserId userIdentityId,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var (skip, take) = CatalogPagination.Normalize(page, pageSize);
        var (items, total) = await _links
            .ListActiveByUserAsync(userIdentityId, skip, take, cancellationToken)
            .ConfigureAwait(false);

        var customerIds = items.Select(i => i.BusinessCustomerId).Distinct().ToList();
        var customers = (await _customers.ListByIdsAsync(customerIds, cancellationToken).ConfigureAwait(false))
            .ToDictionary(c => c.Id.Value);

        var orgIds = items.Select(i => i.OrganizationId).Distinct().ToList();
        var orgNames = new Dictionary<Guid, string>();
        foreach (var orgId in orgIds)
        {
            var org = await _organizations.GetByIdAsync(orgId, cancellationToken).ConfigureAwait(false);
            if (org is not null)
            {
                orgNames[org.Id.Value] = org.DisplayName;
            }
        }

        var mapped = new List<LinkedMerchantDto>(items.Count);
        foreach (var link in items)
        {
            if (!customers.TryGetValue(link.BusinessCustomerId.Value, out var customer)
                || customer.OrganizationId != link.OrganizationId)
            {
                continue;
            }

            mapped.Add(new LinkedMerchantDto(
                link.Id.Value,
                link.BusinessCustomerId.Value,
                link.OrganizationId.Value,
                orgNames.GetValueOrDefault(link.OrganizationId.Value, string.Empty),
                customer.DisplayName,
                link.Status.ToString(),
                link.LinkedAtUtc));
        }

        return new PagedResult<LinkedMerchantDto>(
            mapped,
            total,
            Math.Max(page ?? 1, 1),
            take);
    }
}
