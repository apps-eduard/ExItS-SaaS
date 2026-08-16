using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.CashierShifts;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.OperationalSetup;
using ExItS.PinoyBusinessPOS.Domain.Registers;

namespace ExItS.PinoyBusinessPOS.Application.OperationalSetup;

public sealed record PosOperationalSetupDto(
    Guid OrganizationId,
    string StoreDisplayName,
    string CurrencyCode,
    string TaxPricingMode,
    decimal TaxRatePercent,
    string? ReceiptHeader,
    string? ReceiptFooter,
    string? BusinessAddress,
    string? ContactPhone,
    Guid? DefaultRegisterId,
    string CashCountMode,
    bool IsCompleted,
    DateTimeOffset? CompletedAtUtc,
    DateTimeOffset CreatedAtUtc,
    Guid CreatedBy,
    DateTimeOffset UpdatedAtUtc,
    Guid UpdatedBy,
    bool TaxConfigurationEnabled = false);

public sealed record CompleteOperationalSetupRequest(
    string StoreDisplayName,
    string CurrencyCode,
    string TaxPricingMode,
    decimal TaxRatePercent,
    string? ReceiptHeader = null,
    string? ReceiptFooter = null,
    string? BusinessAddress = null,
    string? ContactPhone = null,
    string? CashCountMode = null);

public sealed record UpdateOperationalSetupRequest(
    string StoreDisplayName,
    string CurrencyCode,
    string TaxPricingMode,
    decimal TaxRatePercent,
    DateTimeOffset ExpectedUpdatedAtUtc,
    string? ReceiptHeader = null,
    string? ReceiptFooter = null,
    string? BusinessAddress = null,
    string? ContactPhone = null,
    string? CashCountMode = null);

public static class OperationalSetupMapper
{
    public static PosOperationalSetupDto Map(
        PosOperationalSetup setup,
        bool taxConfigurationEnabled = false) =>
        new(
            setup.OrganizationId.Value,
            setup.StoreDisplayName,
            setup.CurrencyCode,
            setup.TaxPricingMode.ToString(),
            setup.TaxRatePercent,
            setup.ReceiptHeader,
            setup.ReceiptFooter,
            setup.BusinessAddress,
            setup.ContactPhone,
            setup.DefaultRegisterId?.Value,
            setup.CashCountMode.ToString(),
            setup.IsCompleted,
            setup.CompletedAtUtc,
            setup.CreatedAtUtc,
            setup.CreatedBy,
            setup.UpdatedAtUtc,
            setup.UpdatedBy,
            taxConfigurationEnabled);

    public static PosOperationalSetupDto MapIncompleteDefaults(
        Guid organizationId,
        DateTimeOffset utcNow,
        Guid actorId,
        bool taxConfigurationEnabled = false) =>
        Map(
            PosOperationalSetup.CreateIncomplete(PosOrganizationId.From(organizationId), actorId, utcNow),
            taxConfigurationEnabled);
}

public sealed class GetOperationalSetupQuery
{
    private readonly IPosOperationalSetupRepository _setups;
    private readonly IOrganizationTaxConfigurationCapabilityReader _taxConfiguration;
    private readonly TimeProvider _clock;

    public GetOperationalSetupQuery(
        IPosOperationalSetupRepository setups,
        IOrganizationTaxConfigurationCapabilityReader taxConfiguration,
        TimeProvider? clock = null)
    {
        _setups = setups;
        _taxConfiguration = taxConfiguration;
        _clock = clock ?? TimeProvider.System;
    }

    public async Task<PosOperationalSetupDto> ExecuteAsync(
        Guid organizationId,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        var taxConfigurationEnabled = await _taxConfiguration
            .IsTaxConfigurationEnabledAsync(organizationId, cancellationToken)
            .ConfigureAwait(false);

        var org = PosOrganizationId.From(organizationId);
        var existing = await _setups
            .GetByOrganizationIdAsync(org, cancellationToken)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            return OperationalSetupMapper.Map(existing, taxConfigurationEnabled);
        }

        return OperationalSetupMapper.MapIncompleteDefaults(
            organizationId,
            _clock.GetUtcNow(),
            actorId == Guid.Empty ? Guid.Empty : actorId,
            taxConfigurationEnabled);
    }
}

public sealed class CompleteOperationalSetup
{
    private const string DefaultRegisterName = "Main Register";

    private readonly IPosOperationalSetupRepository _setups;
    private readonly IRegisterRepository _registers;
    private readonly IOrganizationCashDenominationRepository _denominations;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IPosCommercialAccessAccessor _access;
    private readonly IOrganizationTaxConfigurationCapabilityReader _taxConfiguration;
    private readonly TimeProvider _clock;

    public CompleteOperationalSetup(
        IPosOperationalSetupRepository setups,
        IRegisterRepository registers,
        IOrganizationCashDenominationRepository denominations,
        IPosUnitOfWork unitOfWork,
        IPosCommercialAccessAccessor access,
        IOrganizationTaxConfigurationCapabilityReader taxConfiguration,
        TimeProvider? clock = null)
    {
        _setups = setups;
        _registers = registers;
        _denominations = denominations;
        _unitOfWork = unitOfWork;
        _access = access;
        _taxConfiguration = taxConfiguration;
        _clock = clock ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<PosOperationalSetupDto>> ExecuteAsync(
        Guid organizationId,
        Guid actorId,
        CompleteOperationalSetupRequest request,
        CancellationToken cancellationToken = default)
    {
        var gate = CommercialAccessGuard.Require(_access, UtangCapability.ManageOperationalSetup);
        if (!gate.IsSuccess)
        {
            return ApplicationResult<PosOperationalSetupDto>.Failure(gate.ErrorCode!, gate.ErrorMessage!);
        }

        if (actorId == Guid.Empty)
        {
            return ApplicationResult<PosOperationalSetupDto>.Failure(
                ApplicationErrorCodes.ActorRequired,
                "An actor identifier is required to complete operational setup.");
        }

        try
        {
            var taxConfigurationEnabled = await _taxConfiguration
                .IsTaxConfigurationEnabledAsync(organizationId, cancellationToken)
                .ConfigureAwait(false);

            var org = PosOrganizationId.From(organizationId);
            var utcNow = _clock.GetUtcNow();
            var taxMode = ParseTaxPricingMode(request.TaxPricingMode);
            var taxRate = request.TaxRatePercent;

            var existing = await _setups.GetByOrganizationIdAsync(org, cancellationToken).ConfigureAwait(false);
            var setup = existing ?? PosOperationalSetup.CreateIncomplete(org, actorId, utcNow);
            var isNew = existing is null;

            if (setup.IsCompleted)
            {
                return ApplicationResult<PosOperationalSetupDto>.Success(
                    OperationalSetupMapper.Map(setup, taxConfigurationEnabled));
            }

            if (!taxConfigurationEnabled)
            {
                if (OperationalSetupTaxWriteGuard.TaxSettingsDiffer(
                        taxMode,
                        taxRate,
                        TaxPricingMode.TaxExclusive,
                        0m))
                {
                    return OperationalSetupTaxWriteGuard.TaxConfigurationNotEnabledResult();
                }

                taxMode = TaxPricingMode.TaxExclusive;
                taxRate = 0m;
            }

            var defaultRegister = await EnsureDefaultRegisterAsync(org, actorId, utcNow, cancellationToken)
                .ConfigureAwait(false);

            setup.Complete(
                request.StoreDisplayName,
                request.CurrencyCode,
                taxMode,
                taxRate,
                request.ReceiptHeader,
                request.ReceiptFooter,
                request.BusinessAddress,
                request.ContactPhone,
                defaultRegister.Id,
                actorId,
                utcNow,
                CashCountModes.ParseConfigurable(request.CashCountMode));

            if (isNew)
            {
                await _setups.AddAsync(setup, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await _setups.UpdateAsync(setup, cancellationToken).ConfigureAwait(false);
            }

            await DefaultCashDenominationSeeder
                .EnsureAsync(_denominations, org, utcNow, cancellationToken)
                .ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<PosOperationalSetupDto>.Success(
                OperationalSetupMapper.Map(setup, taxConfigurationEnabled));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PosOperationalSetupDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<PosOperationalSetupDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }

    private async Task<Register> EnsureDefaultRegisterAsync(
        PosOrganizationId org,
        Guid actorId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        var (items, _) = await _registers
            .ListAsync(org, new RegisterFilter(), skip: 0, take: 1, cancellationToken)
            .ConfigureAwait(false);

        if (items.Count > 0)
        {
            return items[0];
        }

        var code = await _registers.AllocateNextRegisterCodeAsync(org, cancellationToken).ConfigureAwait(false);
        var register = Register.Create(org, code, DefaultRegisterName, actorId, utcNow);
        await _registers.AddAsync(register, cancellationToken).ConfigureAwait(false);
        return register;
    }

    private static TaxPricingMode ParseTaxPricingMode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)
            || !Enum.TryParse<TaxPricingMode>(value.Trim(), ignoreCase: true, out var parsed))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidOperationalSetupTaxRate,
                "Tax pricing mode must be TaxExclusive or TaxInclusive.");
        }

        return parsed;
    }
}

public sealed class UpdateOperationalSetup
{
    private readonly IPosOperationalSetupRepository _setups;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IPosCommercialAccessAccessor _access;
    private readonly IOrganizationTaxConfigurationCapabilityReader _taxConfiguration;
    private readonly TimeProvider _clock;

    public UpdateOperationalSetup(
        IPosOperationalSetupRepository setups,
        IPosUnitOfWork unitOfWork,
        IPosCommercialAccessAccessor access,
        IOrganizationTaxConfigurationCapabilityReader taxConfiguration,
        TimeProvider? clock = null)
    {
        _setups = setups;
        _unitOfWork = unitOfWork;
        _access = access;
        _taxConfiguration = taxConfiguration;
        _clock = clock ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<PosOperationalSetupDto>> ExecuteAsync(
        Guid organizationId,
        Guid actorId,
        UpdateOperationalSetupRequest request,
        CancellationToken cancellationToken = default)
    {
        var gate = CommercialAccessGuard.Require(_access, UtangCapability.ManageOperationalSetup);
        if (!gate.IsSuccess)
        {
            return ApplicationResult<PosOperationalSetupDto>.Failure(gate.ErrorCode!, gate.ErrorMessage!);
        }

        if (actorId == Guid.Empty)
        {
            return ApplicationResult<PosOperationalSetupDto>.Failure(
                ApplicationErrorCodes.ActorRequired,
                "An actor identifier is required to update operational setup.");
        }

        try
        {
            var taxConfigurationEnabled = await _taxConfiguration
                .IsTaxConfigurationEnabledAsync(organizationId, cancellationToken)
                .ConfigureAwait(false);

            var org = PosOrganizationId.From(organizationId);
            var setup = await _setups.GetByOrganizationIdAsync(org, cancellationToken).ConfigureAwait(false);
            if (setup is null || !setup.IsCompleted)
            {
                return ApplicationResult<PosOperationalSetupDto>.Failure(
                    DomainErrorCodes.OperationalSetupIncomplete,
                    "Operational setup must be completed before it can be updated.");
            }

            if (setup.UpdatedAtUtc != request.ExpectedUpdatedAtUtc)
            {
                return ApplicationResult<PosOperationalSetupDto>.Failure(
                    ApplicationErrorCodes.OperationalSetupConcurrencyConflict,
                    "Operational setup was modified by another session.");
            }

            var taxMode = ParseTaxPricingMode(request.TaxPricingMode);
            var taxRate = request.TaxRatePercent;

            if (!taxConfigurationEnabled)
            {
                if (OperationalSetupTaxWriteGuard.TaxSettingsDiffer(
                        taxMode,
                        taxRate,
                        setup.TaxPricingMode,
                        setup.TaxRatePercent))
                {
                    return OperationalSetupTaxWriteGuard.TaxConfigurationNotEnabledResult();
                }

                taxMode = setup.TaxPricingMode;
                taxRate = setup.TaxRatePercent;
            }

            setup.Update(
                request.StoreDisplayName,
                request.CurrencyCode,
                taxMode,
                taxRate,
                request.ReceiptHeader,
                request.ReceiptFooter,
                request.BusinessAddress,
                request.ContactPhone,
                actorId,
                _clock.GetUtcNow(),
                CashCountModes.ParseConfigurable(request.CashCountMode, CashCountModes.ForNewShift(setup.CashCountMode)));

            await _setups.UpdateAsync(setup, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<PosOperationalSetupDto>.Success(
                OperationalSetupMapper.Map(setup, taxConfigurationEnabled));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PosOperationalSetupDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<PosOperationalSetupDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }

    private static TaxPricingMode ParseTaxPricingMode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)
            || !Enum.TryParse<TaxPricingMode>(value.Trim(), ignoreCase: true, out var parsed))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidOperationalSetupTaxRate,
                "Tax pricing mode must be TaxExclusive or TaxInclusive.");
        }

        return parsed;
    }
}

file static class OperationalSetupTaxWriteGuard
{
    public static bool TaxSettingsDiffer(
        TaxPricingMode mode,
        decimal rate,
        TaxPricingMode baselineMode,
        decimal baselineRate) =>
        mode != baselineMode || rate != baselineRate;

    public static ApplicationResult<PosOperationalSetupDto> TaxConfigurationNotEnabledResult() =>
        ApplicationResult<PosOperationalSetupDto>.Failure(
            ApplicationErrorCodes.TaxConfigurationNotEnabled,
            "Tax configuration is not enabled for this organization.");
}
