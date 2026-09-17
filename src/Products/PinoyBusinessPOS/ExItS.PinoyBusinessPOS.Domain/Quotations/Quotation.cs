using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Domain.Quotations;

/// <summary>
/// Seller-owned quotation. Draft lines are editable; Issue freezes product/customer snapshots and
/// allocates a quotation number (Sent). After Sent, content is immutable except status transitions.
/// Quotations never create inventory movements, payments, or sales.
/// </summary>
public sealed class Quotation
{
    public const int ReferenceMaxLength = 128;
    public const int NotesMaxLength = 512;
    public const int TermsMaxLength = 2000;
    public const int MaxLineCount = 200;

    private readonly List<QuotationLine> _lines;

    public QuotationId Id { get; }
    public PosOrganizationId OrganizationId { get; }
    public string? QuotationNumber { get; private set; }
    public QuotationStatus Status { get; private set; }
    public POSCustomerId CustomerId { get; private set; }
    public string CustomerDisplayNameSnapshot { get; private set; }
    public string? CustomerMobileNumberSnapshot { get; private set; }
    public string? CustomerAddressSnapshot { get; private set; }
    public string? CustomerNotesSnapshot { get; private set; }
    public PosBranchId BranchId { get; private set; }
    public Guid PreparedBy { get; private set; }
    public DateOnly? ValidUntil { get; private set; }
    public string? Reference { get; private set; }
    public string? Notes { get; private set; }
    public string? Terms { get; private set; }
    public Guid? ConvertedSaleId { get; private set; }
    public bool IsCustomerVisible { get; private set; }
    public DateTimeOffset? IssuedAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public IReadOnlyList<QuotationLine> Lines => _lines;

    public decimal Subtotal => SaleMoney.RoundMoney(_lines.Sum(l => l.LineTotal));

    private Quotation(
        QuotationId id,
        PosOrganizationId organizationId,
        string? quotationNumber,
        QuotationStatus status,
        POSCustomerId customerId,
        string customerDisplayNameSnapshot,
        string? customerMobileNumberSnapshot,
        string? customerAddressSnapshot,
        string? customerNotesSnapshot,
        PosBranchId branchId,
        Guid preparedBy,
        DateOnly? validUntil,
        string? reference,
        string? notes,
        string? terms,
        Guid? convertedSaleId,
        bool isCustomerVisible,
        DateTimeOffset? issuedAtUtc,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        List<QuotationLine> lines)
    {
        Id = id;
        OrganizationId = organizationId;
        QuotationNumber = quotationNumber;
        Status = status;
        CustomerId = customerId;
        CustomerDisplayNameSnapshot = customerDisplayNameSnapshot;
        CustomerMobileNumberSnapshot = customerMobileNumberSnapshot;
        CustomerAddressSnapshot = customerAddressSnapshot;
        CustomerNotesSnapshot = customerNotesSnapshot;
        BranchId = branchId;
        PreparedBy = preparedBy;
        ValidUntil = validUntil;
        Reference = reference;
        Notes = notes;
        Terms = terms;
        ConvertedSaleId = convertedSaleId;
        IsCustomerVisible = isCustomerVisible;
        IssuedAtUtc = issuedAtUtc;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        _lines = lines;
    }

    public static Quotation CreateDraft(
        PosOrganizationId organizationId,
        POSCustomerId customerId,
        string customerDisplayName,
        PosBranchId branchId,
        Guid preparedBy,
        IReadOnlyList<QuotationLineDraft> lines,
        DateTimeOffset utcNow,
        string? customerMobileNumber = null,
        string? customerAddress = null,
        string? customerNotes = null,
        DateOnly? validUntil = null,
        string? reference = null,
        string? notes = null,
        string? terms = null,
        QuotationId? id = null,
        bool isCustomerVisible = false)
    {
        SaleMoney.EnsureUtc(utcNow);
        SaleMoney.EnsureActor(preparedBy);
        EnsureLines(lines);

        var quotationId = id ?? QuotationId.New();
        return new Quotation(
            quotationId,
            organizationId,
            quotationNumber: null,
            QuotationStatus.Draft,
            customerId,
            POSCustomer.NormalizeDisplayName(customerDisplayName),
            NormalizeOptionalCustomerMobile(customerMobileNumber),
            NormalizeOptionalText(
                customerAddress,
                POSCustomer.AddressMaxLength,
                DomainErrorCodes.InvalidQuotationCustomerSnapshot,
                "Customer address"),
            NormalizeOptionalText(
                customerNotes,
                POSCustomer.NotesMaxLength,
                DomainErrorCodes.InvalidQuotationCustomerSnapshot,
                "Customer notes"),
            branchId,
            preparedBy,
            validUntil,
            NormalizeReference(reference),
            NormalizeNotes(notes),
            NormalizeTerms(terms),
            convertedSaleId: null,
            isCustomerVisible,
            issuedAtUtc: null,
            utcNow,
            utcNow,
            BuildDraftLines(quotationId, organizationId, lines));
    }

    public void UpdateDraft(
        POSCustomerId customerId,
        string customerDisplayName,
        PosBranchId branchId,
        IReadOnlyList<QuotationLineDraft> lines,
        DateTimeOffset utcNow,
        string? customerMobileNumber = null,
        string? customerAddress = null,
        string? customerNotes = null,
        DateOnly? validUntil = null,
        string? reference = null,
        string? notes = null,
        string? terms = null,
        bool? isCustomerVisible = null)
    {
        SaleMoney.EnsureUtc(utcNow);
        EnsureDraft();
        EnsureLines(lines);

        CustomerId = customerId;
        CustomerDisplayNameSnapshot = POSCustomer.NormalizeDisplayName(customerDisplayName);
        CustomerMobileNumberSnapshot = NormalizeOptionalCustomerMobile(customerMobileNumber);
        CustomerAddressSnapshot = NormalizeOptionalText(
            customerAddress,
            POSCustomer.AddressMaxLength,
            DomainErrorCodes.InvalidQuotationCustomerSnapshot,
            "Customer address");
        CustomerNotesSnapshot = NormalizeOptionalText(
            customerNotes,
            POSCustomer.NotesMaxLength,
            DomainErrorCodes.InvalidQuotationCustomerSnapshot,
            "Customer notes");
        BranchId = branchId;
        ValidUntil = validUntil;
        Reference = NormalizeReference(reference);
        Notes = NormalizeNotes(notes);
        Terms = NormalizeTerms(terms);
        if (isCustomerVisible is { } visible)
        {
            IsCustomerVisible = visible;
        }

        ReplaceDraftLines(lines);
        UpdatedAtUtc = utcNow;
    }

    public void Issue(
        string quotationNumber,
        IReadOnlyList<QuotationLineSnapshotInput> snapshots,
        DateTimeOffset utcNow)
    {
        SaleMoney.EnsureUtc(utcNow);
        EnsureDraft();
        EnsureLines(snapshots);

        if (snapshots.Count != _lines.Count)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidQuotationLine,
                "Line snapshot count must match draft lines.");
        }

        var snapshotByProduct = snapshots.ToDictionary(s => s.ProductId.Value);
        foreach (var line in _lines.OrderBy(l => l.LineNumber))
        {
            if (!snapshotByProduct.TryGetValue(line.ProductId.Value, out var snapshot))
            {
                throw new DomainException(
                    DomainErrorCodes.InvalidQuotationLine,
                    "Each draft line must have a matching snapshot on issue.");
            }

            line.FreezeSnapshot(snapshot);
        }

        QuotationNumber = QuotationNumbers.Normalize(quotationNumber);
        Status = QuotationStatus.Sent;
        IssuedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    public void Cancel(DateTimeOffset utcNow)
    {
        SaleMoney.EnsureUtc(utcNow);
        if (Status is QuotationStatus.Cancelled)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidQuotationStatusTransition,
                "Quotation is already cancelled.");
        }

        if (Status is QuotationStatus.Converted)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidQuotationStatusTransition,
                "Converted quotations cannot be cancelled.");
        }

        if (Status is not (QuotationStatus.Draft or QuotationStatus.Sent or QuotationStatus.Accepted))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidQuotationStatusTransition,
                "Only draft, sent, or accepted quotations can be cancelled.");
        }

        Status = QuotationStatus.Cancelled;
        UpdatedAtUtc = utcNow;
    }

    public void Accept(DateTimeOffset utcNow)
    {
        SaleMoney.EnsureUtc(utcNow);
        EnsureSent();
        Status = QuotationStatus.Accepted;
        UpdatedAtUtc = utcNow;
    }

    public void Decline(DateTimeOffset utcNow)
    {
        SaleMoney.EnsureUtc(utcNow);
        EnsureSent();
        Status = QuotationStatus.Declined;
        UpdatedAtUtc = utcNow;
    }

    public void Expire(DateTimeOffset utcNow)
    {
        SaleMoney.EnsureUtc(utcNow);
        if (Status is not (QuotationStatus.Sent or QuotationStatus.Accepted))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidQuotationStatusTransition,
                "Only sent or accepted quotations can expire.");
        }

        Status = QuotationStatus.Expired;
        UpdatedAtUtc = utcNow;
    }

    /// <summary>
    /// Links this quotation to a completed sale. Idempotent when already Converted with the same sale id.
    /// Does not create the sale.
    /// </summary>
    public void MarkConverted(Guid saleId, DateTimeOffset utcNow)
    {
        SaleMoney.EnsureUtc(utcNow);
        if (saleId == Guid.Empty)
        {
            throw new DomainException(DomainErrorCodes.InvalidQuotationConvertedSaleId, "Sale id cannot be empty.");
        }

        if (Status is QuotationStatus.Converted)
        {
            if (ConvertedSaleId == saleId)
            {
                return;
            }

            throw new DomainException(
                DomainErrorCodes.InvalidQuotationStatusTransition,
                "Quotation is already converted to a different sale.");
        }

        if (Status is not (QuotationStatus.Sent or QuotationStatus.Accepted))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidQuotationStatusTransition,
                "Only sent or accepted quotations can be converted.");
        }

        Status = QuotationStatus.Converted;
        ConvertedSaleId = saleId;
        UpdatedAtUtc = utcNow;
    }

    public static Quotation Rehydrate(
        QuotationId id,
        PosOrganizationId organizationId,
        string? quotationNumber,
        QuotationStatus status,
        POSCustomerId customerId,
        string customerDisplayNameSnapshot,
        string? customerMobileNumberSnapshot,
        string? customerAddressSnapshot,
        string? customerNotesSnapshot,
        PosBranchId branchId,
        Guid preparedBy,
        DateOnly? validUntil,
        string? reference,
        string? notes,
        string? terms,
        Guid? convertedSaleId,
        bool isCustomerVisible,
        DateTimeOffset? issuedAtUtc,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        IReadOnlyList<QuotationLine> lines) =>
        new(
            id,
            organizationId,
            quotationNumber,
            status,
            customerId,
            customerDisplayNameSnapshot,
            customerMobileNumberSnapshot,
            customerAddressSnapshot,
            customerNotesSnapshot,
            branchId,
            preparedBy,
            validUntil,
            reference,
            notes,
            terms,
            convertedSaleId,
            isCustomerVisible,
            issuedAtUtc,
            createdAtUtc,
            updatedAtUtc,
            lines.ToList());

    private void EnsureDraft()
    {
        if (Status != QuotationStatus.Draft)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidQuotationStatusTransition,
                "Only draft quotations can be edited.");
        }
    }

    private void EnsureSent()
    {
        if (Status != QuotationStatus.Sent)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidQuotationStatusTransition,
                "Only sent quotations support this status transition.");
        }
    }

    private void ReplaceDraftLines(IReadOnlyList<QuotationLineDraft> lines)
    {
        _lines.Clear();
        _lines.AddRange(BuildDraftLines(Id, OrganizationId, lines));
    }

    private static List<QuotationLine> BuildDraftLines(
        QuotationId quotationId,
        PosOrganizationId organizationId,
        IReadOnlyList<QuotationLineDraft> lines)
    {
        EnsureNoDuplicateProducts(lines.Select(l => l.ProductId.Value).ToList());
        var result = new List<QuotationLine>(lines.Count);
        for (var i = 0; i < lines.Count; i++)
        {
            result.Add(QuotationLine.CreateDraft(quotationId, organizationId, i + 1, lines[i]));
        }

        return result;
    }

    private static void EnsureLines(IReadOnlyList<QuotationLineDraft> lines)
    {
        if (lines is null || lines.Count == 0)
        {
            throw new DomainException(
                DomainErrorCodes.QuotationRequiresLines,
                "A quotation must contain at least one line.");
        }

        if (lines.Count > MaxLineCount)
        {
            throw new DomainException(
                DomainErrorCodes.QuotationRequiresLines,
                $"A quotation may contain at most {MaxLineCount} lines.");
        }

        EnsureNoDuplicateProducts(lines.Select(l => l.ProductId.Value).ToList());
    }

    private static void EnsureLines(IReadOnlyList<QuotationLineSnapshotInput> lines)
    {
        if (lines is null || lines.Count == 0)
        {
            throw new DomainException(
                DomainErrorCodes.QuotationRequiresLines,
                "A quotation must contain at least one line.");
        }

        if (lines.Count > MaxLineCount)
        {
            throw new DomainException(
                DomainErrorCodes.QuotationRequiresLines,
                $"A quotation may contain at most {MaxLineCount} lines.");
        }

        EnsureNoDuplicateProducts(lines.Select(l => l.ProductId.Value).ToList());
    }

    private static void EnsureNoDuplicateProducts(IReadOnlyList<Guid> productIds)
    {
        if (productIds.Count != productIds.Distinct().Count())
        {
            throw new DomainException(
                DomainErrorCodes.QuotationDuplicateProduct,
                "Duplicate products are not allowed on a quotation.");
        }
    }

    private static string? NormalizeOptionalCustomerMobile(string? mobile)
    {
        var (display, _) = POSCustomer.NormalizeOptionalMobile(mobile);
        return display;
    }

    private static string? NormalizeReference(string? value) =>
        NormalizeOptionalText(value, ReferenceMaxLength, DomainErrorCodes.InvalidQuotationReference, "Reference");

    private static string? NormalizeNotes(string? value) =>
        NormalizeOptionalText(value, NotesMaxLength, DomainErrorCodes.InvalidQuotationNotes, "Notes");

    private static string? NormalizeTerms(string? value) =>
        NormalizeOptionalText(value, TermsMaxLength, DomainErrorCodes.InvalidQuotationTerms, "Terms");

    private static string? NormalizeOptionalText(string? value, int maxLength, string errorCode, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new DomainException(errorCode, $"{label} must be at most {maxLength} characters.");
        }

        return trimmed;
    }
}
