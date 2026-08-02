using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Infrastructure.Persistence.Organizations;
using Microsoft.EntityFrameworkCore;

namespace ExItS.Platform.Infrastructure.Persistence.Repositories;

internal sealed class BusinessCustomerRepository(PlatformDbContext db) : IBusinessCustomerRepository
{
    public async Task<BusinessCustomer?> GetByIdAsync(
        BusinessCustomerId id,
        CancellationToken cancellationToken = default)
    {
        var record = await db.BusinessCustomers.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id.Value, cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : ToDomain(record);
    }

    public async Task<(IReadOnlyList<BusinessCustomer> Items, int TotalCount)> ListByOrganizationAsync(
        PlatformOrganizationId organizationId,
        string? owningProductCode,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = db.BusinessCustomers.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId.Value);
        if (!string.IsNullOrWhiteSpace(owningProductCode))
        {
            query = query.Where(x => x.OwningProductCode == owningProductCode);
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query
            .OrderBy(x => x.DisplayName)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return (records.Select(ToDomain).ToList(), total);
    }

    public Task AddAsync(BusinessCustomer customer, CancellationToken cancellationToken = default)
    {
        db.BusinessCustomers.Add(ToRecord(customer));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(BusinessCustomer customer, CancellationToken cancellationToken = default)
    {
        var record = await db.BusinessCustomers
            .FirstOrDefaultAsync(x => x.Id == customer.Id.Value, cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            return;
        }

        record.DisplayName = customer.DisplayName;
        record.NormalizedEmail = customer.NormalizedEmail;
        record.Phone = customer.Phone;
        record.Notes = customer.Notes;
        record.OwningProductCode = customer.OwningProductCode;
        record.Status = customer.Status.ToString();
        record.LinkedUserIdentityId = customer.LinkedUserIdentityId?.Value;
        record.UpdatedAtUtc = customer.UpdatedAtUtc;
    }

    private static BusinessCustomer ToDomain(BusinessCustomerRecord record) =>
        BusinessCustomer.Rehydrate(
            BusinessCustomerId.From(record.Id),
            PlatformOrganizationId.From(record.OrganizationId),
            record.DisplayName,
            record.NormalizedEmail,
            record.Phone,
            record.Notes,
            record.OwningProductCode,
            Enum.Parse<BusinessCustomerStatus>(record.Status),
            record.LinkedUserIdentityId is null ? null : PlatformUserId.From(record.LinkedUserIdentityId.Value),
            record.CreatedAtUtc,
            record.UpdatedAtUtc);

    private static BusinessCustomerRecord ToRecord(BusinessCustomer customer) =>
        new()
        {
            Id = customer.Id.Value,
            OrganizationId = customer.OrganizationId.Value,
            DisplayName = customer.DisplayName,
            NormalizedEmail = customer.NormalizedEmail,
            Phone = customer.Phone,
            Notes = customer.Notes,
            OwningProductCode = customer.OwningProductCode,
            Status = customer.Status.ToString(),
            LinkedUserIdentityId = customer.LinkedUserIdentityId?.Value,
            CreatedAtUtc = customer.CreatedAtUtc,
            UpdatedAtUtc = customer.UpdatedAtUtc
        };
}

internal sealed class CreditCustomerRepository(PlatformDbContext db) : ICreditCustomerRepository
{
    public async Task<CreditCustomer?> GetByIdAsync(
        CreditCustomerId id,
        CancellationToken cancellationToken = default)
    {
        var record = await db.CreditCustomers.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id.Value, cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : ToDomain(record);
    }

    public async Task<CreditCustomer?> FindActiveByBusinessCustomerAsync(
        BusinessCustomerId businessCustomerId,
        CancellationToken cancellationToken = default)
    {
        var active = nameof(CreditCustomerStatus.Active);
        var record = await db.CreditCustomers.AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.BusinessCustomerId == businessCustomerId.Value && x.Status == active,
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : ToDomain(record);
    }

    public async Task<(IReadOnlyList<CreditCustomer> Items, int TotalCount)> ListByOrganizationAsync(
        PlatformOrganizationId organizationId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = db.CreditCustomers.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId.Value);
        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return (records.Select(ToDomain).ToList(), total);
    }

    public Task AddAsync(CreditCustomer creditCustomer, CancellationToken cancellationToken = default)
    {
        db.CreditCustomers.Add(ToRecord(creditCustomer));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(CreditCustomer creditCustomer, CancellationToken cancellationToken = default)
    {
        var record = await db.CreditCustomers
            .FirstOrDefaultAsync(x => x.Id == creditCustomer.Id.Value, cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            return;
        }

        record.CurrencyCode = creditCustomer.CurrencyCode;
        record.Status = creditCustomer.Status.ToString();
        record.UpdatedAtUtc = creditCustomer.UpdatedAtUtc;
    }

    private static CreditCustomer ToDomain(CreditCustomerRecord record) =>
        CreditCustomer.Rehydrate(
            CreditCustomerId.From(record.Id),
            PlatformOrganizationId.From(record.OrganizationId),
            BusinessCustomerId.From(record.BusinessCustomerId),
            record.CurrencyCode,
            Enum.Parse<CreditCustomerStatus>(record.Status),
            record.CreatedAtUtc,
            record.UpdatedAtUtc);

    private static CreditCustomerRecord ToRecord(CreditCustomer credit) =>
        new()
        {
            Id = credit.Id.Value,
            OrganizationId = credit.OrganizationId.Value,
            BusinessCustomerId = credit.BusinessCustomerId.Value,
            CurrencyCode = credit.CurrencyCode,
            Status = credit.Status.ToString(),
            CreatedAtUtc = credit.CreatedAtUtc,
            UpdatedAtUtc = credit.UpdatedAtUtc
        };
}

internal sealed class CustomerLinkRequestRepository(PlatformDbContext db) : ICustomerLinkRequestRepository
{
    public async Task<CustomerLinkRequest?> GetByIdAsync(
        CustomerLinkRequestId id,
        CancellationToken cancellationToken = default)
    {
        var record = await db.CustomerLinkRequests.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id.Value, cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : ToDomain(record);
    }

    public async Task<CustomerLinkRequest?> FindPendingByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        var pending = nameof(CustomerLinkRequestStatus.Pending);
        var record = await db.CustomerLinkRequests.AsNoTracking()
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash && x.Status == pending, cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : ToDomain(record);
    }

    public async Task<CustomerLinkRequest?> FindPendingByBusinessCustomerAsync(
        BusinessCustomerId businessCustomerId,
        CancellationToken cancellationToken = default)
    {
        var pending = nameof(CustomerLinkRequestStatus.Pending);
        var record = await db.CustomerLinkRequests.AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.BusinessCustomerId == businessCustomerId.Value && x.Status == pending,
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : ToDomain(record);
    }

    public async Task<(IReadOnlyList<CustomerLinkRequest> Items, int TotalCount)> ListByOrganizationAsync(
        PlatformOrganizationId organizationId,
        CustomerLinkRequestStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = db.CustomerLinkRequests.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId.Value);
        if (status is not null)
        {
            var statusName = status.Value.ToString();
            query = query.Where(x => x.Status == statusName);
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return (records.Select(ToDomain).ToList(), total);
    }

    public Task AddAsync(CustomerLinkRequest request, CancellationToken cancellationToken = default)
    {
        db.CustomerLinkRequests.Add(ToRecord(request));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(CustomerLinkRequest request, CancellationToken cancellationToken = default)
    {
        var record = await db.CustomerLinkRequests
            .FirstOrDefaultAsync(x => x.Id == request.Id.Value, cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            return;
        }

        record.NormalizedEmail = request.NormalizedEmail;
        record.Status = request.Status.ToString();
        record.TokenHash = request.TokenHash;
        record.UpdatedAtUtc = request.UpdatedAtUtc;
        record.ExpiresAtUtc = request.ExpiresAtUtc;
        record.AcceptedAtUtc = request.AcceptedAtUtc;
        record.DeclinedAtUtc = request.DeclinedAtUtc;
        record.RevokedAtUtc = request.RevokedAtUtc;
        record.AcceptedByUserId = request.AcceptedByUserId?.Value;
    }

    private static CustomerLinkRequest ToDomain(CustomerLinkRequestRecord record) =>
        CustomerLinkRequest.Rehydrate(
            CustomerLinkRequestId.From(record.Id),
            PlatformOrganizationId.From(record.OrganizationId),
            BusinessCustomerId.From(record.BusinessCustomerId),
            record.NormalizedEmail,
            Enum.Parse<CustomerLinkRequestStatus>(record.Status),
            record.TokenHash,
            record.InvitedByUserId is null ? null : PlatformUserId.From(record.InvitedByUserId.Value),
            record.CreatedAtUtc,
            record.UpdatedAtUtc,
            record.ExpiresAtUtc,
            record.AcceptedAtUtc,
            record.DeclinedAtUtc,
            record.RevokedAtUtc,
            record.AcceptedByUserId is null ? null : PlatformUserId.From(record.AcceptedByUserId.Value));

    private static CustomerLinkRequestRecord ToRecord(CustomerLinkRequest request) =>
        new()
        {
            Id = request.Id.Value,
            OrganizationId = request.OrganizationId.Value,
            BusinessCustomerId = request.BusinessCustomerId.Value,
            NormalizedEmail = request.NormalizedEmail,
            Status = request.Status.ToString(),
            TokenHash = request.TokenHash,
            InvitedByUserId = request.InvitedByUserId?.Value,
            CreatedAtUtc = request.CreatedAtUtc,
            UpdatedAtUtc = request.UpdatedAtUtc,
            ExpiresAtUtc = request.ExpiresAtUtc,
            AcceptedAtUtc = request.AcceptedAtUtc,
            DeclinedAtUtc = request.DeclinedAtUtc,
            RevokedAtUtc = request.RevokedAtUtc,
            AcceptedByUserId = request.AcceptedByUserId?.Value
        };
}

internal sealed class LinkedCustomerAppUserRepository(PlatformDbContext db) : ILinkedCustomerAppUserRepository
{
    public async Task<LinkedCustomerAppUser?> GetByIdAsync(
        LinkedCustomerAppUserId id,
        CancellationToken cancellationToken = default)
    {
        var record = await db.LinkedCustomerAppUsers.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id.Value, cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : ToDomain(record);
    }

    public async Task<LinkedCustomerAppUser?> FindActiveByBusinessCustomerAsync(
        BusinessCustomerId businessCustomerId,
        CancellationToken cancellationToken = default)
    {
        var active = nameof(LinkedCustomerAppUserStatus.Active);
        var record = await db.LinkedCustomerAppUsers.AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.BusinessCustomerId == businessCustomerId.Value && x.Status == active,
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : ToDomain(record);
    }

    public async Task<LinkedCustomerAppUser?> FindActiveByUserAndOrganizationAsync(
        PlatformUserId userIdentityId,
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var active = nameof(LinkedCustomerAppUserStatus.Active);
        var record = await db.LinkedCustomerAppUsers.AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.UserIdentityId == userIdentityId.Value
                     && x.OrganizationId == organizationId.Value
                     && x.Status == active,
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : ToDomain(record);
    }

    public async Task<(IReadOnlyList<LinkedCustomerAppUser> Items, int TotalCount)> ListByOrganizationAsync(
        PlatformOrganizationId organizationId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = db.LinkedCustomerAppUsers.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId.Value);
        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query
            .OrderByDescending(x => x.LinkedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return (records.Select(ToDomain).ToList(), total);
    }

    public Task AddAsync(LinkedCustomerAppUser link, CancellationToken cancellationToken = default)
    {
        db.LinkedCustomerAppUsers.Add(ToRecord(link));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(LinkedCustomerAppUser link, CancellationToken cancellationToken = default)
    {
        var record = await db.LinkedCustomerAppUsers
            .FirstOrDefaultAsync(x => x.Id == link.Id.Value, cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            return;
        }

        record.Status = link.Status.ToString();
        record.UpdatedAtUtc = link.UpdatedAtUtc;
        record.RevokedAtUtc = link.RevokedAtUtc;
    }

    private static LinkedCustomerAppUser ToDomain(LinkedCustomerAppUserRecord record) =>
        LinkedCustomerAppUser.Rehydrate(
            LinkedCustomerAppUserId.From(record.Id),
            PlatformOrganizationId.From(record.OrganizationId),
            BusinessCustomerId.From(record.BusinessCustomerId),
            PlatformUserId.From(record.UserIdentityId),
            CustomerLinkRequestId.From(record.SourceLinkRequestId),
            Enum.Parse<LinkedCustomerAppUserStatus>(record.Status),
            record.LinkedAtUtc,
            record.UpdatedAtUtc,
            record.RevokedAtUtc);

    private static LinkedCustomerAppUserRecord ToRecord(LinkedCustomerAppUser link) =>
        new()
        {
            Id = link.Id.Value,
            OrganizationId = link.OrganizationId.Value,
            BusinessCustomerId = link.BusinessCustomerId.Value,
            UserIdentityId = link.UserIdentityId.Value,
            SourceLinkRequestId = link.SourceLinkRequestId.Value,
            Status = link.Status.ToString(),
            LinkedAtUtc = link.LinkedAtUtc,
            UpdatedAtUtc = link.UpdatedAtUtc,
            RevokedAtUtc = link.RevokedAtUtc
        };
}
