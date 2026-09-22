using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;

public sealed record ConnectedCommerceCategoryDiscountRuleDto(Guid CategoryId, decimal DiscountPercent);

public sealed record ConnectedCommerceCategoryReturnRuleDto(
    Guid CategoryId,
    string Mode,
    bool? ReturnsAllowed = null,
    int? ReturnWindowDays = null);

public sealed record OrganizationConnectedCommerceSettingsDto(
    Guid OrganizationId,
    bool AllowPayBeforeFulfillment,
    bool AllowPayOnDeliveryOrReceipt,
    bool AllowSupplierCredit,
    string DefaultPaymentTiming,
    decimal DefaultB2bDiscountPercent,
    int ProposalReservationHoldHours,
    IReadOnlyList<ConnectedCommerceCategoryDiscountRuleDto> CategoryRules,
    bool ReturnsAllowed = true,
    int? ReturnWindowDays = null,
    int ReceivingIssueWindowDays = 2,
    bool RequireReturnApproval = true,
    IReadOnlyList<ConnectedCommerceCategoryReturnRuleDto>? CategoryReturnRules = null);

public sealed record UpdateOrganizationConnectedCommerceSettingsRequest(
    bool AllowPayBeforeFulfillment,
    bool AllowPayOnDeliveryOrReceipt,
    bool AllowSupplierCredit,
    string DefaultPaymentTiming,
    decimal DefaultB2bDiscountPercent,
    int ProposalReservationHoldHours,
    IReadOnlyList<ConnectedCommerceCategoryDiscountRuleDto> CategoryRules,
    bool ReturnsAllowed = true,
    int? ReturnWindowDays = null,
    int ReceivingIssueWindowDays = 2,
    bool RequireReturnApproval = true,
    IReadOnlyList<ConnectedCommerceCategoryReturnRuleDto>? CategoryReturnRules = null);

public sealed record ConnectedCommerceOverviewDto(
    OrganizationConnectedCommerceSettingsDto Settings,
    int ActiveBusinessCustomerCount,
    int PendingBusinessCustomerCount);

public sealed record BusinessCustomerPaymentTimingOverrideDto(
    Guid ConnectionId,
    bool UseOrganizationPaymentTimingDefaults,
    bool AllowPayBeforeFulfillment,
    bool AllowPayOnDeliveryOrReceipt,
    bool AllowSupplierCredit,
    string CustomerDefaultPaymentTiming,
    bool EffectiveAllowPayBeforeFulfillment,
    bool EffectiveAllowPayOnDeliveryOrReceipt,
    bool EffectiveAllowSupplierCredit,
    string EffectiveDefaultPaymentTiming);

public sealed record UpdateBusinessCustomerPaymentTimingOverrideRequest(
    bool UseOrganizationPaymentTimingDefaults,
    bool AllowPayBeforeFulfillment,
    bool AllowPayOnDeliveryOrReceipt,
    bool AllowSupplierCredit,
    string CustomerDefaultPaymentTiming);

public sealed record BusinessCustomerPricingOverridesDto(
    Guid ConnectionId,
    decimal? CustomerDiscountPercent,
    IReadOnlyList<ConnectedCommerceCategoryDiscountRuleDto> CategoryOverrides);

public sealed record UpdateBusinessCustomerPricingOverridesRequest(
    decimal? CustomerDiscountPercent,
    IReadOnlyList<ConnectedCommerceCategoryDiscountRuleDto> CategoryOverrides);

public sealed class GetOrganizationConnectedCommerceSettings(
    IOrganizationConnectedCommerceSettingsRepository settings,
    IPosUnitOfWork uow,
    IPosCommercialAccessAccessor access,
    TimeProvider? clock = null)
{
    private readonly TimeProvider _clock = clock ?? TimeProvider.System;

    public async Task<ApplicationResult<OrganizationConnectedCommerceSettingsDto>> ExecuteAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        var gate = ConnectedSupplierUseCaseGuard.Access(access, UtangCapability.ViewSuppliers);
        if (!gate.IsSuccess)
        {
            return ConnectedSupplierUseCaseGuard.Failure<OrganizationConnectedCommerceSettingsDto>(
                gate.ErrorCode!,
                gate.ErrorMessage!);
        }

        var row = await EnsureSettingsAsync(organizationId, cancellationToken).ConfigureAwait(false);
        return ApplicationResult<OrganizationConnectedCommerceSettingsDto>.Success(Map(row));
    }

    private async Task<OrganizationConnectedCommerceSettings> EnsureSettingsAsync(
        Guid organizationId,
        CancellationToken ct)
    {
        var org = PosOrganizationId.From(organizationId);
        var row = await settings.GetAsync(org, ct).ConfigureAwait(false);
        if (row is not null)
        {
            return row;
        }

        row = OrganizationConnectedCommerceSettings.CreateDefault(org, _clock.GetUtcNow());
        await settings.AddAsync(row, ct).ConfigureAwait(false);
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);
        return row;
    }

    internal static OrganizationConnectedCommerceSettingsDto Map(OrganizationConnectedCommerceSettings settingsRow) =>
        new(
            settingsRow.OrganizationId.Value,
            settingsRow.AllowPayBeforeFulfillment,
            settingsRow.AllowPayOnDeliveryOrReceipt,
            settingsRow.AllowSupplierCredit,
            settingsRow.DefaultPaymentTiming.ToString(),
            settingsRow.DefaultB2bDiscountPercent,
            settingsRow.ProposalReservationHoldHours,
            settingsRow.CategoryRules
                .Select(x => new ConnectedCommerceCategoryDiscountRuleDto(x.CategoryId, x.DiscountPercent))
                .ToList(),
            settingsRow.ReturnsAllowed,
            settingsRow.ReturnWindowDays,
            settingsRow.ReceivingIssueWindowDays,
            settingsRow.RequireReturnApproval,
            settingsRow.CategoryReturnRules
                .Select(x => new ConnectedCommerceCategoryReturnRuleDto(
                    x.CategoryId,
                    x.Mode.ToString(),
                    x.ReturnsAllowed,
                    x.ReturnWindowDays))
                .ToList());
}

public sealed class UpdateOrganizationConnectedCommerceSettings(
    IOrganizationConnectedCommerceSettingsRepository settings,
    IPosUnitOfWork uow,
    IPosCommercialAccessAccessor access,
    TimeProvider? clock = null)
{
    private readonly TimeProvider _clock = clock ?? TimeProvider.System;

    public async Task<ApplicationResult<OrganizationConnectedCommerceSettingsDto>> ExecuteAsync(
        Guid organizationId,
        UpdateOrganizationConnectedCommerceSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        var gate = ConnectedSupplierUseCaseGuard.Access(access, UtangCapability.ManageSuppliers);
        if (!gate.IsSuccess)
        {
            return ConnectedSupplierUseCaseGuard.Failure<OrganizationConnectedCommerceSettingsDto>(
                gate.ErrorCode!,
                gate.ErrorMessage!);
        }

        var org = PosOrganizationId.From(organizationId);
        var now = _clock.GetUtcNow();
        var row = await settings.GetAsync(org, cancellationToken).ConfigureAwait(false);
        var isNew = row is null;
        row ??= OrganizationConnectedCommerceSettings.CreateDefault(org, now);

        try
        {
            row.ConfigurePaymentTiming(
                request.AllowPayBeforeFulfillment,
                request.AllowPayOnDeliveryOrReceipt,
                request.AllowSupplierCredit,
                ParseTiming(request.DefaultPaymentTiming),
                now);
            row.ConfigurePricing(
                request.DefaultB2bDiscountPercent,
                request.CategoryRules?.Select(ToDomainRule).ToList() ?? [],
                now);
            row.SetProposalReservationHoldHours(request.ProposalReservationHoldHours, now);
            row.ConfigureReturnPolicy(
                request.ReturnsAllowed,
                request.ReturnWindowDays,
                request.ReceivingIssueWindowDays,
                request.RequireReturnApproval,
                request.CategoryReturnRules?.Select(ToDomainReturnRule).ToList() ?? [],
                now);
        }
        catch (DomainException ex)
        {
            return ConnectedSupplierUseCaseGuard.Failure<OrganizationConnectedCommerceSettingsDto>(ex.ErrorCode, ex.Message);
        }

        if (isNew)
        {
            await settings.AddAsync(row, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await settings.UpdateAsync(row, cancellationToken).ConfigureAwait(false);
        }

        await uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return ApplicationResult<OrganizationConnectedCommerceSettingsDto>.Success(
            GetOrganizationConnectedCommerceSettings.Map(row));
    }

    private static ConnectedPoPaymentTiming ParseTiming(string raw)
    {
        if (!Enum.TryParse<ConnectedPoPaymentTiming>(raw?.Trim(), ignoreCase: true, out var value))
        {
            throw new DomainException(
                ConnectedSupplierDomainErrorCodes.InvalidPaymentTiming,
                "Invalid connected PO payment timing.");
        }

        return value;
    }

    private static OrganizationConnectedCommerceCategoryRule ToDomainRule(ConnectedCommerceCategoryDiscountRuleDto dto) =>
        new(dto.CategoryId, dto.DiscountPercent);

    private static OrganizationConnectedCommerceCategoryReturnRule ToDomainReturnRule(
        ConnectedCommerceCategoryReturnRuleDto dto)
    {
        if (!Enum.TryParse<ConnectedPoReturnPolicyMode>(dto.Mode?.Trim(), ignoreCase: true, out var mode))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidConnectedPoReturnPolicyMode,
                "Invalid category return policy mode.");
        }

        return new OrganizationConnectedCommerceCategoryReturnRule(
            dto.CategoryId,
            mode,
            dto.ReturnsAllowed,
            dto.ReturnWindowDays);
    }
}

public sealed class GetConnectedCommerceOverview(
    IOrganizationConnectedCommerceSettingsRepository settings,
    IConnectedSupplierRelationshipRepository relationships,
    IPosCommercialAccessAccessor access,
    IPosUnitOfWork uow,
    TimeProvider? clock = null)
{
    private readonly TimeProvider _clock = clock ?? TimeProvider.System;

    public async Task<ApplicationResult<ConnectedCommerceOverviewDto>> ExecuteAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        var gate = ConnectedSupplierUseCaseGuard.Access(access, UtangCapability.ViewSuppliers);
        if (!gate.IsSuccess)
        {
            return ConnectedSupplierUseCaseGuard.Failure<ConnectedCommerceOverviewDto>(gate.ErrorCode!, gate.ErrorMessage!);
        }

        var org = PosOrganizationId.From(organizationId);
        var row = await settings.GetAsync(org, cancellationToken).ConfigureAwait(false);
        if (row is null)
        {
            row = OrganizationConnectedCommerceSettings.CreateDefault(org, _clock.GetUtcNow());
            await settings.AddAsync(row, cancellationToken).ConfigureAwait(false);
            await uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        var linked = await relationships.ListAsync(org, supplierView: true, cancellationToken).ConfigureAwait(false);
        var active = linked.Count(x => x.Status == ConnectedSupplierRelationshipStatus.Active);
        var pending = linked.Count(x => x.Status == ConnectedSupplierRelationshipStatus.Pending);
        return ApplicationResult<ConnectedCommerceOverviewDto>.Success(
            new ConnectedCommerceOverviewDto(
                GetOrganizationConnectedCommerceSettings.Map(row),
                active,
                pending));
    }
}

public sealed class GetBusinessCustomerPaymentTimingOverride(
    IConnectedSupplierRelationshipRepository relationships,
    IOrganizationConnectedCommerceSettingsRepository settings,
    IPosCommercialAccessAccessor access,
    IPosUnitOfWork uow,
    TimeProvider? clock = null)
{
    private readonly TimeProvider _clock = clock ?? TimeProvider.System;

    public async Task<ApplicationResult<BusinessCustomerPaymentTimingOverrideDto>> ExecuteAsync(
        Guid organizationId,
        Guid connectionId,
        CancellationToken cancellationToken = default)
    {
        var gate = ConnectedSupplierUseCaseGuard.Access(access, UtangCapability.ViewSuppliers);
        if (!gate.IsSuccess)
        {
            return ConnectedSupplierUseCaseGuard.Failure<BusinessCustomerPaymentTimingOverrideDto>(gate.ErrorCode!, gate.ErrorMessage!);
        }

        var org = PosOrganizationId.From(organizationId);
        var relationship = await relationships.GetAsync(ConnectedSupplierRelationshipId.From(connectionId), cancellationToken)
            .ConfigureAwait(false);
        if (relationship is null || relationship.SupplierOrganizationId != org)
        {
            return ConnectedSupplierUseCaseGuard.Failure<BusinessCustomerPaymentTimingOverrideDto>(
                ConnectedSupplierErrorCodes.NotFound,
                "Business customer relationship was not found.");
        }

        var settingsRow = await settings.GetAsync(org, cancellationToken).ConfigureAwait(false);
        if (settingsRow is null)
        {
            settingsRow = OrganizationConnectedCommerceSettings.CreateDefault(org, _clock.GetUtcNow());
            await settings.AddAsync(settingsRow, cancellationToken).ConfigureAwait(false);
            await uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return ApplicationResult<BusinessCustomerPaymentTimingOverrideDto>.Success(MapTiming(settingsRow, relationship));
    }

    internal static BusinessCustomerPaymentTimingOverrideDto MapTiming(
        OrganizationConnectedCommerceSettings organizationSettings,
        ConnectedSupplierRelationship relationship)
    {
        var effective = ConnectedPoPaymentTimingResolver.Resolve(organizationSettings, relationship);
        return new BusinessCustomerPaymentTimingOverrideDto(
            relationship.Id.Value,
            relationship.UseOrganizationPaymentTimingDefaults,
            relationship.AllowPayBeforeFulfillment,
            relationship.AllowPayOnDeliveryOrReceipt,
            relationship.AllowSupplierCredit,
            relationship.CustomerDefaultPaymentTiming.ToString(),
            effective.AllowPayBeforeFulfillment,
            effective.AllowPayOnDeliveryOrReceipt,
            effective.AllowSupplierCredit,
            effective.DefaultPaymentTiming.ToString());
    }
}

public sealed class UpdateBusinessCustomerPaymentTimingOverride(
    IConnectedSupplierRelationshipRepository relationships,
    IOrganizationConnectedCommerceSettingsRepository settings,
    IPosCommercialAccessAccessor access,
    IPosUnitOfWork uow,
    TimeProvider? clock = null)
{
    private readonly TimeProvider _clock = clock ?? TimeProvider.System;

    public async Task<ApplicationResult<BusinessCustomerPaymentTimingOverrideDto>> ExecuteAsync(
        Guid organizationId,
        Guid connectionId,
        UpdateBusinessCustomerPaymentTimingOverrideRequest request,
        CancellationToken cancellationToken = default)
    {
        var gate = ConnectedSupplierUseCaseGuard.Access(access, UtangCapability.ManageSuppliers);
        if (!gate.IsSuccess)
        {
            return ConnectedSupplierUseCaseGuard.Failure<BusinessCustomerPaymentTimingOverrideDto>(gate.ErrorCode!, gate.ErrorMessage!);
        }

        var org = PosOrganizationId.From(organizationId);
        var relationship = await relationships.GetAsync(ConnectedSupplierRelationshipId.From(connectionId), cancellationToken)
            .ConfigureAwait(false);
        if (relationship is null || relationship.SupplierOrganizationId != org)
        {
            return ConnectedSupplierUseCaseGuard.Failure<BusinessCustomerPaymentTimingOverrideDto>(
                ConnectedSupplierErrorCodes.NotFound,
                "Business customer relationship was not found.");
        }

        var settingsRow = await settings.GetAsync(org, cancellationToken).ConfigureAwait(false)
            ?? OrganizationConnectedCommerceSettings.CreateDefault(org, _clock.GetUtcNow());
        if (settingsRow.OrganizationId != org)
        {
            await settings.AddAsync(settingsRow, cancellationToken).ConfigureAwait(false);
        }

        try
        {
            relationship.ConfigurePaymentTimingOverrides(
                request.UseOrganizationPaymentTimingDefaults,
                request.AllowPayBeforeFulfillment,
                request.AllowPayOnDeliveryOrReceipt,
                request.AllowSupplierCredit,
                ParseTiming(request.CustomerDefaultPaymentTiming),
                settingsRow,
                _clock.GetUtcNow());
        }
        catch (DomainException ex)
        {
            return ConnectedSupplierUseCaseGuard.Failure<BusinessCustomerPaymentTimingOverrideDto>(ex.ErrorCode, ex.Message);
        }

        await relationships.UpdateAsync(relationship, cancellationToken).ConfigureAwait(false);
        await uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return ApplicationResult<BusinessCustomerPaymentTimingOverrideDto>.Success(
            GetBusinessCustomerPaymentTimingOverride.MapTiming(settingsRow, relationship));
    }

    private static ConnectedPoPaymentTiming ParseTiming(string raw)
    {
        if (!Enum.TryParse<ConnectedPoPaymentTiming>(raw?.Trim(), ignoreCase: true, out var value))
        {
            throw new DomainException(
                ConnectedSupplierDomainErrorCodes.InvalidPaymentTiming,
                "Invalid connected PO payment timing.");
        }

        return value;
    }
}

public sealed class GetBusinessCustomerPricingOverrides(
    IConnectedSupplierRelationshipRepository relationships,
    IPosCommercialAccessAccessor access)
{
    public async Task<ApplicationResult<BusinessCustomerPricingOverridesDto>> ExecuteAsync(
        Guid organizationId,
        Guid connectionId,
        CancellationToken cancellationToken = default)
    {
        var gate = ConnectedSupplierUseCaseGuard.Access(access, UtangCapability.ViewSuppliers);
        if (!gate.IsSuccess)
        {
            return ConnectedSupplierUseCaseGuard.Failure<BusinessCustomerPricingOverridesDto>(gate.ErrorCode!, gate.ErrorMessage!);
        }

        var org = PosOrganizationId.From(organizationId);
        var relationship = await relationships.GetAsync(ConnectedSupplierRelationshipId.From(connectionId), cancellationToken)
            .ConfigureAwait(false);
        if (relationship is null || relationship.SupplierOrganizationId != org)
        {
            return ConnectedSupplierUseCaseGuard.Failure<BusinessCustomerPricingOverridesDto>(
                ConnectedSupplierErrorCodes.NotFound,
                "Business customer relationship was not found.");
        }

        return ApplicationResult<BusinessCustomerPricingOverridesDto>.Success(
            new BusinessCustomerPricingOverridesDto(
                relationship.Id.Value,
                relationship.CustomerDiscountPercent,
                relationship.CustomerCategoryDiscountOverrides
                    .Select(x => new ConnectedCommerceCategoryDiscountRuleDto(x.CategoryId, x.DiscountPercent))
                    .ToList()));
    }
}

public sealed class UpdateBusinessCustomerPricingOverrides(
    IConnectedSupplierRelationshipRepository relationships,
    IPosCommercialAccessAccessor access,
    IPosUnitOfWork uow,
    TimeProvider? clock = null)
{
    private readonly TimeProvider _clock = clock ?? TimeProvider.System;

    public async Task<ApplicationResult<BusinessCustomerPricingOverridesDto>> ExecuteAsync(
        Guid organizationId,
        Guid connectionId,
        UpdateBusinessCustomerPricingOverridesRequest request,
        CancellationToken cancellationToken = default)
    {
        var gate = ConnectedSupplierUseCaseGuard.Access(access, UtangCapability.ManageSuppliers);
        if (!gate.IsSuccess)
        {
            return ConnectedSupplierUseCaseGuard.Failure<BusinessCustomerPricingOverridesDto>(gate.ErrorCode!, gate.ErrorMessage!);
        }

        var org = PosOrganizationId.From(organizationId);
        var relationship = await relationships.GetAsync(ConnectedSupplierRelationshipId.From(connectionId), cancellationToken)
            .ConfigureAwait(false);
        if (relationship is null || relationship.SupplierOrganizationId != org)
        {
            return ConnectedSupplierUseCaseGuard.Failure<BusinessCustomerPricingOverridesDto>(
                ConnectedSupplierErrorCodes.NotFound,
                "Business customer relationship was not found.");
        }

        try
        {
            relationship.ConfigureCustomerPricing(
                request.CustomerDiscountPercent,
                request.CategoryOverrides?.Select(x => new ConnectedCustomerCategoryDiscountOverride(x.CategoryId, x.DiscountPercent)).ToList() ?? [],
                _clock.GetUtcNow());
        }
        catch (DomainException ex)
        {
            return ConnectedSupplierUseCaseGuard.Failure<BusinessCustomerPricingOverridesDto>(ex.ErrorCode, ex.Message);
        }

        await relationships.UpdateAsync(relationship, cancellationToken).ConfigureAwait(false);
        await uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return ApplicationResult<BusinessCustomerPricingOverridesDto>.Success(
            new BusinessCustomerPricingOverridesDto(
                relationship.Id.Value,
                relationship.CustomerDiscountPercent,
                relationship.CustomerCategoryDiscountOverrides
                    .Select(x => new ConnectedCommerceCategoryDiscountRuleDto(x.CategoryId, x.DiscountPercent))
                    .ToList()));
    }
}
