using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Payments;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Application.Payments;

public sealed record PaymentMethodSettingDto(
    string MethodCode,
    string RequiredCapability,
    string IntegrationMode,
    string SettlementMode,
    string Availability,
    bool Entitled,
    bool IsEnabled,
    bool IsCheckoutEligible,
    bool ComingSoon,
    string? DisplayName,
    bool RequireReference,
    string BranchScope,
    IReadOnlyList<Guid> SelectedBranchIds,
    string? Instructions,
    string? AccountHint,
    bool CanConfigure);

public sealed record UpsertPaymentMethodSettingRequest(
    string MethodCode,
    bool IsEnabled,
    string? DisplayName,
    bool RequireReference,
    string BranchScope,
    IReadOnlyList<Guid>? SelectedBranchIds,
    string? Instructions,
    string? AccountHint);

public static class PaymentMethodAccessGuard
{
    public static ApplicationResult EnsureCheckoutMethodAllowed(
        SalePaymentMethod method,
        Guid branchId,
        string? subscriptionStatus,
        IReadOnlyCollection<string>? featureCodes,
        IReadOnlyList<OrganizationPaymentMethodSetting> settings)
    {
        if (!PaymentCapabilityPolicy.IsCheckoutMethodAllowedByEntitlement(method, subscriptionStatus, featureCodes))
        {
            return ApplicationResult.Failure(
                DomainErrorCodes.PaymentMethodNotEntitled,
                "This payment method is not included in the organization's plan.");
        }

        var code = SalePaymentMethods.ToCode(method);
        var setting = settings.FirstOrDefault(s =>
            string.Equals(s.MethodCode, code, StringComparison.OrdinalIgnoreCase));

        if (setting is null)
        {
            return ApplicationResult.Success();
        }

        if (!setting.IsEnabled)
        {
            return ApplicationResult.Failure(
                DomainErrorCodes.PaymentMethodDisabled,
                "This payment method is disabled for the organization.");
        }

        if (!setting.IsAvailableForBranch(branchId))
        {
            return ApplicationResult.Failure(
                DomainErrorCodes.PaymentMethodNotAvailableForBranch,
                "This payment method is not available at the current branch.");
        }

        return ApplicationResult.Success();
    }
}

public sealed class ListOrganizationPaymentMethods(
    IOrganizationPaymentMethodSettingRepository settings,
    IPosCommercialAccessAccessor access)
{
    public async Task<IReadOnlyList<PaymentMethodSettingDto>> ExecuteAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var stored = await settings.ListByOrganizationAsync(orgId, cancellationToken).ConfigureAwait(false);
        var byCode = stored.ToDictionary(s => s.MethodCode, StringComparer.OrdinalIgnoreCase);
        var featureCodes = access.Current.EnabledFeatureCodes;
        var status = access.Current.SubscriptionStatus;

        return PaymentMethodCatalog.All.Select(def =>
        {
            byCode.TryGetValue(def.MethodCode, out var setting);
            var entitled = PaymentCapabilityPolicy.HasCapabilityOrDefaultBasic(
                def.RequiredCapability,
                status,
                featureCodes);
            var comingSoon = def.Availability == PaymentMethodAvailability.ComingSoon;
            var isEnabled = setting?.IsEnabled ?? (!comingSoon && entitled);
            var canConfigure = entitled
                && !comingSoon
                && def.Availability == PaymentMethodAvailability.Configurable
                && PaymentCapabilityPolicy.HasCapabilityOrDefaultBasic(
                    PaymentCapability.PaymentManagement,
                    status,
                    featureCodes);
            var checkoutEligible = entitled
                && !comingSoon
                && def.IsCheckoutSaleMethod
                && isEnabled;

            return new PaymentMethodSettingDto(
                def.MethodCode,
                def.RequiredCapability.ToString(),
                def.IntegrationMode.ToString(),
                def.SettlementMode.ToString(),
                def.Availability.ToString(),
                entitled,
                isEnabled,
                checkoutEligible,
                comingSoon,
                setting?.DisplayName,
                setting?.RequireReference
                    ?? def.MethodCode is PaymentMethodCatalog.ManualGCash
                        or PaymentMethodCatalog.BankTransfer
                        or PaymentMethodCatalog.Check,
                (setting?.BranchScope ?? PaymentMethodBranchScope.AllBranches).ToString(),
                setting?.SelectedBranchIds ?? Array.Empty<Guid>(),
                setting?.Instructions,
                setting?.AccountHint,
                canConfigure);
        }).ToArray();
    }
}

public sealed class UpsertOrganizationPaymentMethodSetting(
    IOrganizationPaymentMethodSettingRepository settings,
    IPosUnitOfWork unitOfWork,
    IClock clock,
    IPosCommercialAccessAccessor access)
{
    public async Task<ApplicationResult<PaymentMethodSettingDto>> ExecuteAsync(
        Guid organizationId,
        UpsertPaymentMethodSettingRequest request,
        CancellationToken cancellationToken = default)
    {
        var def = PaymentMethodCatalog.Find(request.MethodCode);
        if (def is null)
        {
            return ApplicationResult<PaymentMethodSettingDto>.Failure(
                DomainErrorCodes.InvalidSalePaymentMethod,
                "Unknown payment method.");
        }

        if (def.Availability == PaymentMethodAvailability.ComingSoon)
        {
            return ApplicationResult<PaymentMethodSettingDto>.Failure(
                DomainErrorCodes.PaymentMethodNotConfigurable,
                "Online payment providers are Coming soon and cannot be configured yet.");
        }

        if (def.Availability != PaymentMethodAvailability.Configurable)
        {
            return ApplicationResult<PaymentMethodSettingDto>.Failure(
                DomainErrorCodes.PaymentMethodNotConfigurable,
                "Built-in payment methods cannot be reconfigured in this release.");
        }

        if (!PaymentCapabilityPolicy.HasCapabilityOrDefaultBasic(
                PaymentCapability.PaymentManagement,
                access.Current.SubscriptionStatus,
                access.Current.EnabledFeatureCodes))
        {
            return ApplicationResult<PaymentMethodSettingDto>.Failure(
                DomainErrorCodes.PaymentMethodNotEntitled,
                "Payment Management is not included in the organization's plan.");
        }

        if (!Enum.TryParse<PaymentMethodBranchScope>(request.BranchScope, ignoreCase: true, out var branchScope))
        {
            branchScope = PaymentMethodBranchScope.AllBranches;
        }

        var orgId = PosOrganizationId.From(organizationId);
        var now = clock.UtcNow;
        var existing = await settings.GetAsync(orgId, def.MethodCode, cancellationToken).ConfigureAwait(false);
        try
        {
            if (existing is null)
            {
                var created = OrganizationPaymentMethodSetting.Create(
                    orgId,
                    def.MethodCode,
                    request.IsEnabled,
                    request.DisplayName,
                    request.RequireReference,
                    branchScope,
                    request.SelectedBranchIds,
                    request.Instructions,
                    request.AccountHint,
                    now);
                await settings.AddAsync(created, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                existing.Update(
                    request.IsEnabled,
                    request.DisplayName,
                    request.RequireReference,
                    branchScope,
                    request.SelectedBranchIds,
                    request.Instructions,
                    request.AccountHint,
                    now);
                await settings.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PaymentMethodSettingDto>.Failure(ex.ErrorCode, ex.Message);
        }

        var list = new ListOrganizationPaymentMethods(settings, access);
        var dto = (await list.ExecuteAsync(organizationId, cancellationToken).ConfigureAwait(false))
            .First(d => string.Equals(d.MethodCode, def.MethodCode, StringComparison.OrdinalIgnoreCase));
        return ApplicationResult<PaymentMethodSettingDto>.Success(dto);
    }
}

public sealed class ResolveCheckoutPaymentMethods(
    IOrganizationPaymentMethodSettingRepository settings,
    IPosCommercialAccessAccessor access)
{
    public async Task<IReadOnlyList<string>> ExecuteAsync(
        Guid organizationId,
        Guid branchId,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var stored = await settings.ListByOrganizationAsync(orgId, cancellationToken).ConfigureAwait(false);
        var status = access.Current.SubscriptionStatus;
        var features = access.Current.EnabledFeatureCodes;

        var allowed = new List<string>();
        foreach (var def in PaymentMethodCatalog.All.Where(d => d.IsCheckoutSaleMethod))
        {
            if (!PaymentMethodCatalog.TryGetSalePaymentMethod(def.MethodCode, out var method))
            {
                continue;
            }

            var gate = PaymentMethodAccessGuard.EnsureCheckoutMethodAllowed(
                method,
                branchId,
                status,
                features,
                stored);
            if (gate.IsSuccess)
            {
                allowed.Add(def.MethodCode);
            }
        }

        return allowed;
    }
}
