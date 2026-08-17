using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Application.CustomerOrdering;

public sealed class GetStorefrontProductImage
{
    private readonly ISellerCustomerOrderingCapability _capability;
    private readonly GetCatalogProductImage _images;

    public GetStorefrontProductImage(
        ISellerCustomerOrderingCapability capability,
        GetCatalogProductImage images)
    {
        _capability = capability;
        _images = images;
    }

    public async Task<ApplicationResult<ProductImageBytes>> ExecuteAsync(
        Guid sellerOrganizationId,
        Guid productId,
        string variant,
        CancellationToken cancellationToken = default)
    {
        var capability = await _capability.ResolveAsync(sellerOrganizationId, cancellationToken).ConfigureAwait(false);
        if (!capability.CanCustomerOrder)
        {
            return ApplicationResult<ProductImageBytes>.Failure(
                ApplicationErrorCodes.CustomerOrderOrderingUnavailable,
                "This merchant is not accepting customer orders.");
        }

        return await _images
            .ReadAsync(
                PosOrganizationId.From(sellerOrganizationId),
                CatalogProductId.From(productId),
                variant,
                cancellationToken)
            .ConfigureAwait(false);
    }
}
