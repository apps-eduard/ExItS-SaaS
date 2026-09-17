using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Domain.Payments;

/// <summary>Per-organization configuration for a catalog payment method.</summary>
public sealed class OrganizationPaymentMethodSetting
{
    public const int DisplayNameMaxLength = 80;
    public const int InstructionsMaxLength = 500;
    public const int AccountHintMaxLength = 120;
    public const int MethodCodeMaxLength = 32;

    private OrganizationPaymentMethodSetting(
        Guid settingId,
        PosOrganizationId organizationId,
        string methodCode,
        bool isEnabled,
        string? displayName,
        bool requireReference,
        PaymentMethodBranchScope branchScope,
        IReadOnlyList<Guid> selectedBranchIds,
        string? instructions,
        string? accountHint,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        SettingId = settingId;
        OrganizationId = organizationId;
        MethodCode = methodCode;
        IsEnabled = isEnabled;
        DisplayName = displayName;
        RequireReference = requireReference;
        BranchScope = branchScope;
        SelectedBranchIds = selectedBranchIds;
        Instructions = instructions;
        AccountHint = accountHint;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public Guid SettingId { get; }
    public PosOrganizationId OrganizationId { get; }
    public string MethodCode { get; }
    public bool IsEnabled { get; private set; }
    public string? DisplayName { get; private set; }
    public bool RequireReference { get; private set; }
    public PaymentMethodBranchScope BranchScope { get; private set; }
    public IReadOnlyList<Guid> SelectedBranchIds { get; private set; }
    public string? Instructions { get; private set; }
    public string? AccountHint { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static OrganizationPaymentMethodSetting Create(
        PosOrganizationId organizationId,
        string methodCode,
        bool isEnabled,
        string? displayName,
        bool requireReference,
        PaymentMethodBranchScope branchScope,
        IReadOnlyList<Guid>? selectedBranchIds,
        string? instructions,
        string? accountHint,
        DateTimeOffset nowUtc)
    {
        var def = PaymentMethodCatalog.Find(methodCode)
            ?? throw new DomainException(
                DomainErrorCodes.InvalidSalePaymentMethod,
                $"Unknown payment method '{methodCode}'.");

        if (def.Availability == PaymentMethodAvailability.ComingSoon)
        {
            throw new DomainException(
                DomainErrorCodes.PaymentMethodNotConfigurable,
                "Online payment providers that are Coming soon cannot be configured yet.");
        }

        if (def.Availability == PaymentMethodAvailability.BuiltIn
            && def.RequiredCapability == PaymentCapability.BasicPayments
            && branchScope == PaymentMethodBranchScope.SelectedBranches)
        {
            branchScope = PaymentMethodBranchScope.AllBranches;
            selectedBranchIds = Array.Empty<Guid>();
        }

        var branches = NormalizeBranches(branchScope, selectedBranchIds);
        return new OrganizationPaymentMethodSetting(
            Guid.NewGuid(),
            organizationId,
            def.MethodCode,
            isEnabled,
            NormalizeOptional(displayName, DisplayNameMaxLength, DomainErrorCodes.InvalidPaymentMethodDisplayName),
            requireReference,
            branchScope,
            branches,
            NormalizeOptional(instructions, InstructionsMaxLength, DomainErrorCodes.InvalidPaymentMethodInstructions),
            NormalizeOptional(accountHint, AccountHintMaxLength, DomainErrorCodes.InvalidPaymentMethodAccountHint),
            nowUtc,
            nowUtc);
    }

    public static OrganizationPaymentMethodSetting Rehydrate(
        Guid settingId,
        PosOrganizationId organizationId,
        string methodCode,
        bool isEnabled,
        string? displayName,
        bool requireReference,
        PaymentMethodBranchScope branchScope,
        IReadOnlyList<Guid> selectedBranchIds,
        string? instructions,
        string? accountHint,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc) =>
        new(
            settingId,
            organizationId,
            methodCode,
            isEnabled,
            displayName,
            requireReference,
            branchScope,
            selectedBranchIds,
            instructions,
            accountHint,
            createdAtUtc,
            updatedAtUtc);

    public void Update(
        bool isEnabled,
        string? displayName,
        bool requireReference,
        PaymentMethodBranchScope branchScope,
        IReadOnlyList<Guid>? selectedBranchIds,
        string? instructions,
        string? accountHint,
        DateTimeOffset nowUtc)
    {
        var def = PaymentMethodCatalog.Find(MethodCode)
            ?? throw new DomainException(
                DomainErrorCodes.InvalidSalePaymentMethod,
                $"Unknown payment method '{MethodCode}'.");

        if (def.Availability == PaymentMethodAvailability.ComingSoon)
        {
            throw new DomainException(
                DomainErrorCodes.PaymentMethodNotConfigurable,
                "Online payment providers that are Coming soon cannot be configured yet.");
        }

        if (def.Availability == PaymentMethodAvailability.BuiltIn
            && def.RequiredCapability == PaymentCapability.BasicPayments)
        {
            branchScope = PaymentMethodBranchScope.AllBranches;
            selectedBranchIds = Array.Empty<Guid>();
        }

        IsEnabled = isEnabled;
        DisplayName = NormalizeOptional(displayName, DisplayNameMaxLength, DomainErrorCodes.InvalidPaymentMethodDisplayName);
        RequireReference = requireReference;
        BranchScope = branchScope;
        SelectedBranchIds = NormalizeBranches(branchScope, selectedBranchIds);
        Instructions = NormalizeOptional(instructions, InstructionsMaxLength, DomainErrorCodes.InvalidPaymentMethodInstructions);
        AccountHint = NormalizeOptional(accountHint, AccountHintMaxLength, DomainErrorCodes.InvalidPaymentMethodAccountHint);
        UpdatedAtUtc = nowUtc;
    }

    public bool IsAvailableForBranch(Guid branchId)
    {
        if (!IsEnabled)
        {
            return false;
        }

        if (BranchScope == PaymentMethodBranchScope.AllBranches)
        {
            return true;
        }

        return SelectedBranchIds.Any(id => id == branchId);
    }

    private static IReadOnlyList<Guid> NormalizeBranches(
        PaymentMethodBranchScope branchScope,
        IReadOnlyList<Guid>? selectedBranchIds)
    {
        if (branchScope == PaymentMethodBranchScope.AllBranches)
        {
            return Array.Empty<Guid>();
        }

        var distinct = (selectedBranchIds ?? Array.Empty<Guid>())
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();
        if (distinct.Length == 0)
        {
            throw new DomainException(
                DomainErrorCodes.PaymentMethodBranchesRequired,
                "Select at least one branch when branch scope is SelectedBranches.");
        }

        return distinct;
    }

    private static string? NormalizeOptional(string? value, int maxLength, string errorCode)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new DomainException(errorCode, $"Value must be at most {maxLength} characters.");
        }

        return trimmed;
    }
}
