namespace ExItS.PinoyBusinessPOS.Domain.Sales;

/// <summary>
/// Immutable seller branding/identity captured at checkout for customer-facing documents.
/// Public/business fields only — never staff directory or private admin contacts.
/// </summary>
public sealed class SaleSellerDocumentIdentity
{
    public const int BusinessNameMaxLength = 200;
    public const int PublicOrganizationIdMaxLength = 32;
    public const int LogoUrlMaxLength = 2048;
    public const int AddressMaxLength = 512;
    public const int PhoneMaxLength = 64;
    public const int EmailMaxLength = 256;
    public const int BranchNameMaxLength = 200;
    public const int BranchAddressMaxLength = 512;

    public string? BusinessName { get; }
    public string? PublicOrganizationId { get; }
    public string? LogoUrl { get; }
    public string? Address { get; }
    public string? Phone { get; }
    public string? Email { get; }
    public string? BranchName { get; }
    public string? BranchAddress { get; }

    public bool ShowLogo { get; }
    public bool ShowBusinessName { get; }
    public bool ShowBusinessAddress { get; }
    public bool ShowBusinessPhone { get; }
    public bool ShowBusinessEmail { get; }
    public bool ShowBranchName { get; }
    public bool ShowBranchAddress { get; }

    private SaleSellerDocumentIdentity(
        string? businessName,
        string? publicOrganizationId,
        string? logoUrl,
        string? address,
        string? phone,
        string? email,
        string? branchName,
        string? branchAddress,
        bool showLogo,
        bool showBusinessName,
        bool showBusinessAddress,
        bool showBusinessPhone,
        bool showBusinessEmail,
        bool showBranchName,
        bool showBranchAddress)
    {
        BusinessName = businessName;
        PublicOrganizationId = publicOrganizationId;
        LogoUrl = logoUrl;
        Address = address;
        Phone = phone;
        Email = email;
        BranchName = branchName;
        BranchAddress = branchAddress;
        ShowLogo = showLogo;
        ShowBusinessName = showBusinessName;
        ShowBusinessAddress = showBusinessAddress;
        ShowBusinessPhone = showBusinessPhone;
        ShowBusinessEmail = showBusinessEmail;
        ShowBranchName = showBranchName;
        ShowBranchAddress = showBranchAddress;
    }

    public static SaleSellerDocumentIdentity Create(
        string? businessName,
        string? publicOrganizationId = null,
        string? logoUrl = null,
        string? address = null,
        string? phone = null,
        string? email = null,
        string? branchName = null,
        string? branchAddress = null,
        bool showLogo = true,
        bool showBusinessName = true,
        bool showBusinessAddress = true,
        bool showBusinessPhone = true,
        bool showBusinessEmail = true,
        bool showBranchName = true,
        bool showBranchAddress = false) =>
        new(
            Truncate(Normalize(businessName), BusinessNameMaxLength),
            Truncate(Normalize(publicOrganizationId), PublicOrganizationIdMaxLength),
            Truncate(Normalize(logoUrl), LogoUrlMaxLength),
            Truncate(Normalize(address), AddressMaxLength),
            Truncate(Normalize(phone), PhoneMaxLength),
            Truncate(Normalize(email), EmailMaxLength),
            Truncate(Normalize(branchName), BranchNameMaxLength),
            Truncate(Normalize(branchAddress), BranchAddressMaxLength),
            showLogo,
            showBusinessName: true, // business name remains mandatory on documents
            showBusinessAddress,
            showBusinessPhone,
            showBusinessEmail,
            showBranchName,
            showBranchAddress);

    public static SaleSellerDocumentIdentity Rehydrate(
        string? businessName,
        string? publicOrganizationId,
        string? logoUrl,
        string? address,
        string? phone,
        string? email,
        string? branchName,
        string? branchAddress,
        bool showLogo,
        bool showBusinessName,
        bool showBusinessAddress,
        bool showBusinessPhone,
        bool showBusinessEmail,
        bool showBranchName,
        bool showBranchAddress) =>
        new(
            businessName,
            publicOrganizationId,
            logoUrl,
            address,
            phone,
            email,
            branchName,
            branchAddress,
            showLogo,
            showBusinessName || true,
            showBusinessAddress,
            showBusinessPhone,
            showBusinessEmail,
            showBranchName,
            showBranchAddress);

    public bool HasAnyIdentityField() =>
        !string.IsNullOrWhiteSpace(BusinessName)
        || !string.IsNullOrWhiteSpace(PublicOrganizationId)
        || !string.IsNullOrWhiteSpace(LogoUrl)
        || !string.IsNullOrWhiteSpace(Address)
        || !string.IsNullOrWhiteSpace(Phone)
        || !string.IsNullOrWhiteSpace(Email)
        || !string.IsNullOrWhiteSpace(BranchName)
        || !string.IsNullOrWhiteSpace(BranchAddress);

    private static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (value is null)
        {
            return null;
        }

        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
