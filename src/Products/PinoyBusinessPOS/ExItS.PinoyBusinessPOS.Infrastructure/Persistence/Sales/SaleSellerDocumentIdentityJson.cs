using System.Text.Json;
using System.Text.Json.Serialization;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Sales;

internal static class SaleSellerDocumentIdentityJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string? Serialize(Domain.Sales.SaleSellerDocumentIdentity? identity)
    {
        if (identity is null || !identity.HasAnyIdentityField())
        {
            return null;
        }

        return JsonSerializer.Serialize(
            new Payload(
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
                identity.ShowBranchAddress),
            Options);
    }

    public static Domain.Sales.SaleSellerDocumentIdentity? Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            var payload = JsonSerializer.Deserialize<Payload>(json, Options);
            if (payload is null)
            {
                return null;
            }

            return Domain.Sales.SaleSellerDocumentIdentity.Rehydrate(
                payload.BusinessName,
                payload.PublicOrganizationId,
                payload.LogoUrl,
                payload.Address,
                payload.Phone,
                payload.Email,
                payload.BranchName,
                payload.BranchAddress,
                payload.ShowLogo,
                payload.ShowBusinessName,
                payload.ShowBusinessAddress,
                payload.ShowBusinessPhone,
                payload.ShowBusinessEmail,
                payload.ShowBranchName,
                payload.ShowBranchAddress);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private sealed record Payload(
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
        bool ShowBranchAddress);
}
