using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Credit;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Application.Payments;
using ExItS.PinoyBusinessPOS.Application.Sales;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Sales;
using Microsoft.Extensions.Options;

namespace ExItS.PinoyBusinessPOS.Application.Statements;

/// <summary>
/// Privacy-safe Personal receipt for one linked-customer sale (lazy-loaded on explicit open).
/// Built only from sale-time snapshots — never live catalog price/name.
/// </summary>
public sealed record LinkedCustomerSaleReceiptLineDto(
    int LineNumber,
    string ProductNameSnapshot,
    decimal Quantity,
    string UnitOfMeasure,
    string SellingMode,
    decimal UnitPriceSnapshot,
    decimal LineTotal,
    decimal LineDiscountAmount = 0m);

/// <summary>Seller document identity for customer-facing purchase summary (public/business fields only).</summary>
public sealed record LinkedCustomerSellerDocumentIdentityDto(
    string? BusinessName,
    string? PublicOrganizationId,
    string? LogoUrl,
    string? Address,
    string? Phone,
    string? Email,
    string? BranchName,
    string? BranchAddress,
    bool ShowLogo,
    bool ShowBusinessName,
    bool ShowBusinessAddress,
    bool ShowBusinessPhone,
    bool ShowBusinessEmail,
    bool ShowBranchName,
    bool ShowBranchAddress,
    string IdentitySource);

public sealed record LinkedCustomerSaleReceiptDto(
    Guid OrganizationId,
    Guid PlatformBusinessCustomerId,
    Guid PosCustomerId,
    Guid SaleId,
    string ReceiptNumber,
    DateTimeOffset OccurredAtUtc,
    string Status,
    string PaymentMethod,
    string Currency,
    string? MerchantDisplayName,
    string? BranchDisplayName,
    string? CustomerDisplayName,
    decimal Subtotal,
    decimal? DiscountAmount,
    decimal TaxAmount,
    decimal Total,
    decimal? UtangAmount,
    decimal? PaidAmount,
    decimal? ChangeAmount,
    decimal? OutstandingEffect,
    IReadOnlyList<LinkedCustomerSaleReceiptLineDto> Lines,
    LinkedCustomerSellerDocumentIdentityDto? SellerDocumentIdentity = null);

/// <summary>
/// Lazy receipt detail: WP03 authorization → ownership → free-window / open-debt / entitlement.
/// </summary>
public sealed class GetLinkedCustomerSaleReceipt
{
    private const string NotFoundMessage = "Receipt was not found.";
    private const string ExtendedRequiredMessage =
        "Extended digital records entitlement is required to open this settled historical receipt.";

    private readonly AuthorizeLinkedCustomerStatementAccess _authorize;
    private readonly ISaleRepository _sales;
    private readonly ICreditEntryRepository _credits;
    private readonly IOutstandingBalanceService _outstanding;
    private readonly IPersonalFeatureEntitlementClient _entitlements;
    private readonly IOptions<PersonalStatementsOptions> _options;
    private readonly IClock _clock;
    private readonly IPosOperationalSetupRepository _operationalSetups;
    private readonly IOrganizationBranchDirectory? _branches;

    public GetLinkedCustomerSaleReceipt(
        AuthorizeLinkedCustomerStatementAccess authorize,
        ISaleRepository sales,
        ICreditEntryRepository credits,
        IOutstandingBalanceService outstanding,
        IPersonalFeatureEntitlementClient entitlements,
        IOptions<PersonalStatementsOptions> options,
        IClock clock,
        IPosOperationalSetupRepository operationalSetups,
        IOrganizationBranchDirectory? branches = null)
    {
        _authorize = authorize;
        _sales = sales;
        _credits = credits;
        _outstanding = outstanding;
        _entitlements = entitlements;
        _options = options;
        _clock = clock;
        _operationalSetups = operationalSetups;
        _branches = branches;
    }

    public async Task<ApplicationResult<LinkedCustomerSaleReceiptDto>> ExecuteAsync(
        Guid organizationId,
        Guid platformBusinessCustomerId,
        Guid saleId,
        string currencyCode = "PHP",
        CancellationToken cancellationToken = default)
    {
        if (saleId == Guid.Empty)
        {
            return NotFound();
        }

        var auth = await _authorize
            .ExecuteAsync(organizationId, platformBusinessCustomerId, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        if (!auth.IsSuccess)
        {
            return ApplicationResult<LinkedCustomerSaleReceiptDto>.Failure(auth.ErrorCode!, auth.ErrorMessage!);
        }

        var ctx = auth.Value!;
        var orgId = PosOrganizationId.From(ctx.OrganizationId);
        var sale = await _sales
            .GetByIdAsync(orgId, SaleId.From(saleId), cancellationToken)
            .ConfigureAwait(false);

        // Fail closed before revealing entitlement: missing / wrong customer → same 404.
        if (sale is null
            || sale.CustomerId is null
            || sale.CustomerId.Value != ctx.PosCustomerId)
        {
            return NotFound();
        }

        var asOfUtc = _clock.UtcNow;
        var freeStart = PersonalHistoryWindows.ComputeFreeWindowStart(
            asOfUtc,
            _options.Value.FreeRecentMonths);

        var openDebtAllows = false;
        if (sale.RecordedAtUtc < freeStart)
        {
            openDebtAllows = await IsOpenDebtEvidenceAsync(orgId, sale, cancellationToken)
                .ConfigureAwait(false);
        }

        var entitled = false;
        if (sale.RecordedAtUtc < freeStart && !openDebtAllows)
        {
            entitled = await _entitlements
                .HasActiveEntitlementAsync(PersonalSettledHistoryPolicy.ExtendedFeatureCode, cancellationToken)
                .ConfigureAwait(false);
        }

        var decision = PersonalSettledHistoryPolicy.EvaluateDetailAccess(
            sale.RecordedAtUtc,
            freeStart,
            openDebtAllows,
            entitled);
        if (decision == PersonalHistoryDetailAccessDecision.ExtendedHistoryRequired)
        {
            return ApplicationResult<LinkedCustomerSaleReceiptDto>.Failure(
                ApplicationErrorCodes.ExtendedHistoryRequired,
                ExtendedRequiredMessage);
        }

        var sellerIdentity = await ResolveSellerIdentityAsync(sale, cancellationToken).ConfigureAwait(false);
        return ApplicationResult<LinkedCustomerSaleReceiptDto>.Success(
            Map(ctx, sale, currencyCode, sellerIdentity));
    }

    private async Task<LinkedCustomerSellerDocumentIdentityDto> ResolveSellerIdentityAsync(
        Sale sale,
        CancellationToken cancellationToken)
    {
        string? branchName = null;
        if (sale.BranchId is not null && _branches is not null)
        {
            var branchGuid = sale.BranchId.Value;
            var names = await _branches
                .GetNamesAsync(sale.OrganizationId.Value, [branchGuid], cancellationToken)
                .ConfigureAwait(false);
            names.TryGetValue(branchGuid, out branchName);
        }

        var setup = await _operationalSetups
            .GetByOrganizationIdAsync(sale.OrganizationId, cancellationToken)
            .ConfigureAwait(false);

        // Prefer completed setup; still use any populated fields when setup is incomplete.
        var setupReady = setup is { IsCompleted: true };
        var fromSetup = SaleSellerDocumentIdentity.Create(
            businessName: FirstNonEmpty(
                setupReady ? setup!.StoreDisplayName : null,
                setup?.StoreDisplayName),
            address: FirstNonEmpty(
                setupReady ? setup!.BusinessAddress : null,
                setup?.BusinessAddress),
            phone: FirstNonEmpty(
                setupReady ? setup!.ContactPhone : null,
                setup?.ContactPhone),
            branchName: branchName);

        if (sale.SellerDocumentIdentity is { } snap)
        {
            // Always gap-fill empty snap fields from setup/branch. Branch-only snaps used to
            // short-circuit HasAnyIdentityField and hide merchant email/address forever.
            var merged = SaleSellerDocumentIdentity.Create(
                businessName: FirstNonEmpty(snap.BusinessName, fromSetup.BusinessName),
                publicOrganizationId: FirstNonEmpty(snap.PublicOrganizationId, fromSetup.PublicOrganizationId),
                logoUrl: FirstNonEmpty(snap.LogoUrl, fromSetup.LogoUrl),
                address: FirstNonEmpty(snap.Address, fromSetup.Address),
                phone: FirstNonEmpty(snap.Phone, fromSetup.Phone),
                email: FirstNonEmpty(snap.Email, fromSetup.Email),
                branchName: FirstNonEmpty(snap.BranchName, fromSetup.BranchName),
                branchAddress: FirstNonEmpty(snap.BranchAddress, fromSetup.BranchAddress),
                showLogo: snap.ShowLogo,
                showBusinessAddress: snap.ShowBusinessAddress,
                showBusinessPhone: snap.ShowBusinessPhone,
                showBusinessEmail: snap.ShowBusinessEmail,
                showBranchName: snap.ShowBranchName,
                showBranchAddress: snap.ShowBranchAddress);

            var source = snap.HasDurableBusinessIdentity()
                ? "saleSnapshot"
                : "saleSnapshotGapFilled";
            return ToDto(merged, source);
        }

        return ToDto(fromSetup, "operationalSetupFallback");
    }

    private static string? FirstNonEmpty(string? preferred, string? fallback)
    {
        if (!string.IsNullOrWhiteSpace(preferred))
        {
            return preferred.Trim();
        }

        return string.IsNullOrWhiteSpace(fallback) ? null : fallback.Trim();
    }

    private static LinkedCustomerSellerDocumentIdentityDto ToDto(
        SaleSellerDocumentIdentity identity,
        string source) =>
        new(
            identity.BusinessName,
            identity.PublicOrganizationId,
            identity.LogoUrl,
            identity.Address,
            identity.Phone,
            identity.Email,
            identity.BranchName,
            identity.BranchAddress,
            identity.ShowLogo,
            identity.ShowBusinessName,
            identity.ShowBusinessAddress,
            identity.ShowBusinessPhone,
            identity.ShowBusinessEmail,
            identity.ShowBranchName,
            identity.ShowBranchAddress,
            source);

    private async Task<bool> IsOpenDebtEvidenceAsync(
        PosOrganizationId orgId,
        Sale sale,
        CancellationToken cancellationToken)
    {
        var outstanding = await _outstanding
            .GetOutstandingAsync(orgId, sale.CustomerId!, cancellationToken)
            .ConfigureAwait(false);

        if (sale.PaymentMethod != SalePaymentMethod.Utang || sale.LinkedCreditEntryId is null)
        {
            return PersonalSettledHistoryPolicy.OpenDebtReceiptExceptionApplies(
                outstanding,
                isUtangSale: false,
                hasLinkedCredit: false,
                linkedCreditIsActive: false);
        }

        var credit = await _credits
            .GetByIdAsync(orgId, sale.CustomerId!, sale.LinkedCreditEntryId, cancellationToken)
            .ConfigureAwait(false);

        return PersonalSettledHistoryPolicy.OpenDebtReceiptExceptionApplies(
            outstanding,
            isUtangSale: true,
            hasLinkedCredit: true,
            linkedCreditIsActive: credit is not null && credit.Status == CreditEntryStatus.Active);
    }

    private static LinkedCustomerSaleReceiptDto Map(
        AuthorizedLinkedCustomerContext ctx,
        Sale sale,
        string currencyCode,
        LinkedCustomerSellerDocumentIdentityDto sellerIdentity)
    {
        var isUtang = sale.PaymentMethod == SalePaymentMethod.Utang;
        var isCompleted = sale.Status == SaleStatus.Completed;

        decimal? utangAmount = isUtang ? sale.Total : null;
        decimal? paidAmount = isUtang ? 0m : (sale.AmountTendered ?? sale.Total);
        decimal? changeAmount = isUtang ? null : sale.ChangeAmount;
        decimal? outstandingEffect = isUtang && isCompleted ? sale.Total : 0m;

        var lines = sale.Lines
            .OrderBy(l => l.LineNumber)
            .Select(l => new LinkedCustomerSaleReceiptLineDto(
                l.LineNumber,
                l.NameSnapshot,
                l.Quantity,
                UnitOfMeasures.ToCode(l.UnitOfMeasureSnapshot),
                SellingModes.ToCode(l.SellingModeSnapshot),
                l.UnitPrice,
                l.LineTotal,
                l.LineDiscountAmount + l.SaleDiscountAllocatedAmount))
            .ToList();

        var merchantName = sellerIdentity.BusinessName;
        var branchName = sellerIdentity.BranchName;
        var customerName = sale.BuyerParty.DisplayNameSnapshot;

        return new LinkedCustomerSaleReceiptDto(
            ctx.OrganizationId,
            ctx.PlatformBusinessCustomerId,
            ctx.PosCustomerId,
            sale.Id.Value,
            sale.SaleNumber,
            sale.RecordedAtUtc,
            sale.Status.ToString(),
            SalePaymentMethods.ToCode(sale.PaymentMethod),
            string.IsNullOrWhiteSpace(currencyCode) ? "PHP" : currencyCode.Trim().ToUpperInvariant(),
            MerchantDisplayName: merchantName,
            BranchDisplayName: branchName,
            CustomerDisplayName: customerName,
            sale.Subtotal,
            DiscountAmount: sale.DiscountTotal > 0 ? sale.DiscountTotal : null,
            sale.TaxAmount,
            sale.Total,
            utangAmount,
            paidAmount,
            changeAmount,
            outstandingEffect,
            lines,
            sellerIdentity);
    }

    private static ApplicationResult<LinkedCustomerSaleReceiptDto> NotFound() =>
        ApplicationResult<LinkedCustomerSaleReceiptDto>.Failure(
            ApplicationErrorCodes.ReceiptNotFound,
            NotFoundMessage);
}
