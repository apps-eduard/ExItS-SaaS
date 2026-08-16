using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.CashierShifts;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.OperationalSetup;

namespace ExItS.PinoyBusinessPOS.Application.OperationalSetup;

public sealed record OrganizationCashDenominationDto(
    Guid DenominationId,
    Guid OrganizationId,
    decimal Value,
    string? DisplayLabel,
    bool IsEnabled,
    int SortOrder,
    DateTimeOffset UpdatedAtUtc);

public sealed record CashDenominationWriteDto(
    decimal Value,
    bool IsEnabled = true,
    int SortOrder = 0,
    string? DisplayLabel = null,
    Guid? DenominationId = null);

public sealed record ReplaceCashDenominationsRequest(IReadOnlyList<CashDenominationWriteDto> Items);

public sealed record CashCountDenominationLineDto(
    decimal DenominationValue,
    int Quantity,
    decimal? LineTotal = null);

public static class CashDenominationMapper
{
    public static OrganizationCashDenominationDto Map(OrganizationCashDenomination denomination) =>
        new(
            denomination.Id.Value,
            denomination.OrganizationId.Value,
            denomination.Value,
            denomination.DisplayLabel,
            denomination.IsEnabled,
            denomination.SortOrder,
            denomination.UpdatedAtUtc);

    public static CashCountDenominationLineDto Map(CashCountDenominationLine line) =>
        new(line.DenominationValue, line.Quantity, line.LineTotal);

    public static IReadOnlyList<CashCountDenominationLine> ParseSubmittedLines(
        IReadOnlyList<CashCountDenominationLineDto>? lines,
        decimal? submittedTotal)
    {
        if (lines is null || lines.Count == 0)
        {
            return Array.Empty<CashCountDenominationLine>();
        }

        if (submittedTotal is null)
        {
            throw new DomainException(
                DomainErrorCodes.CashCountDenominationTotalMismatch,
                "A denomination breakdown requires an authoritative cash count total.");
        }

        var parsed = lines
            .Select(line => CashCountDenominationLine.Create(line.DenominationValue, line.Quantity))
            .ToList();
        return CashCountDenominationBreakdown.EnsureMatchesSubmittedTotal(submittedTotal.Value, parsed);
    }
}

public static class DefaultCashDenominationSeeder
{
    public static async Task EnsureAsync(
        IOrganizationCashDenominationRepository repository,
        PosOrganizationId organizationId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default)
    {
        var existing = await repository.ListAsync(organizationId, cancellationToken).ConfigureAwait(false);
        if (existing.Count == 0)
        {
            var seeded = PhilippineCashDenominationDefaults.Values
                .Select((value, index) => OrganizationCashDenomination.Create(
                    organizationId,
                    value,
                    index,
                    utcNow))
                .ToList();
            await repository.ReplaceAsync(organizationId, seeded, cancellationToken).ConfigureAwait(false);
            return;
        }

        var existingValues = existing.Select(d => d.Value).ToHashSet();
        var missing = PhilippineCashDenominationDefaults.Values
            .Where(value => !existingValues.Contains(value))
            .ToList();
        if (missing.Count == 0)
        {
            return;
        }

        var nextSort = existing.Max(d => d.SortOrder) + 1;
        var next = existing.ToList();
        foreach (var value in missing)
        {
            next.Add(OrganizationCashDenomination.Create(
                organizationId,
                value,
                nextSort++,
                utcNow));
        }

        await repository.ReplaceAsync(organizationId, next, cancellationToken).ConfigureAwait(false);
    }
}

public sealed class ListCashDenominationsQuery
{
    private readonly IOrganizationCashDenominationRepository _denominations;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly TimeProvider _clock;

    public ListCashDenominationsQuery(
        IOrganizationCashDenominationRepository denominations,
        IPosUnitOfWork unitOfWork,
        TimeProvider? clock = null)
    {
        _denominations = denominations;
        _unitOfWork = unitOfWork;
        _clock = clock ?? TimeProvider.System;
    }

    public async Task<IReadOnlyList<OrganizationCashDenominationDto>> ExecuteAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        var org = PosOrganizationId.From(organizationId);
        await DefaultCashDenominationSeeder
            .EnsureAsync(_denominations, org, _clock.GetUtcNow(), cancellationToken)
            .ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        var items = await _denominations.ListAsync(org, cancellationToken).ConfigureAwait(false);
        return items
            .OrderBy(d => d.SortOrder)
            .ThenBy(d => d.Value)
            .Select(CashDenominationMapper.Map)
            .ToList();
    }
}

public sealed class ReplaceCashDenominations
{
    private readonly IOrganizationCashDenominationRepository _denominations;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IPosCommercialAccessAccessor _access;
    private readonly TimeProvider _clock;

    public ReplaceCashDenominations(
        IOrganizationCashDenominationRepository denominations,
        IPosUnitOfWork unitOfWork,
        IPosCommercialAccessAccessor access,
        TimeProvider? clock = null)
    {
        _denominations = denominations;
        _unitOfWork = unitOfWork;
        _access = access;
        _clock = clock ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<IReadOnlyList<OrganizationCashDenominationDto>>> ExecuteAsync(
        Guid organizationId,
        ReplaceCashDenominationsRequest request,
        CancellationToken cancellationToken = default)
    {
        var gate = CommercialAccessGuard.Require(_access, UtangCapability.ManageOperationalSetup);
        if (!gate.IsSuccess)
        {
            return ApplicationResult<IReadOnlyList<OrganizationCashDenominationDto>>.Failure(
                gate.ErrorCode!,
                gate.ErrorMessage!);
        }

        try
        {
            var org = PosOrganizationId.From(organizationId);
            var utcNow = _clock.GetUtcNow();
            var existing = await _denominations.ListAsync(org, cancellationToken).ConfigureAwait(false);
            var byId = existing.ToDictionary(d => d.Id.Value);
            var seenValues = new HashSet<decimal>();
            var next = new List<OrganizationCashDenomination>();

            foreach (var item in request.Items ?? Array.Empty<CashDenominationWriteDto>())
            {
                var value = OrganizationCashDenomination.NormalizeValue(item.Value);
                if (!seenValues.Add(value))
                {
                    throw new DomainException(
                        DomainErrorCodes.DuplicateCashDenomination,
                        "Each denomination value can appear only once for an organization.");
                }

                if (item.DenominationId is Guid id && byId.TryGetValue(id, out var current))
                {
                    if (current.Value != value)
                    {
                        current = OrganizationCashDenomination.Create(
                            org,
                            value,
                            item.SortOrder,
                            utcNow,
                            item.IsEnabled,
                            item.DisplayLabel,
                            current.Id);
                    }
                    else
                    {
                        current.SetEnabled(item.IsEnabled, utcNow);
                        current.Reorder(item.SortOrder, utcNow);
                        current.SetDisplayLabel(item.DisplayLabel, utcNow);
                    }

                    next.Add(current);
                    continue;
                }

                next.Add(OrganizationCashDenomination.Create(
                    org,
                    value,
                    item.SortOrder,
                    utcNow,
                    item.IsEnabled,
                    item.DisplayLabel));
            }

            await _denominations.ReplaceAsync(org, next, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            var saved = await _denominations.ListAsync(org, cancellationToken).ConfigureAwait(false);
            return ApplicationResult<IReadOnlyList<OrganizationCashDenominationDto>>.Success(
                saved.OrderBy(d => d.SortOrder).ThenBy(d => d.Value).Select(CashDenominationMapper.Map).ToList());
        }
        catch (DomainException ex)
        {
            return ApplicationResult<IReadOnlyList<OrganizationCashDenominationDto>>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<IReadOnlyList<OrganizationCashDenominationDto>>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}
