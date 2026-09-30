using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Application.CustomerOrdering;

public sealed class GetStorefrontProductImage
{
    private readonly ISellerCustomerOrderingCapability _capability;
    private readonly GetCatalogProductImage _images;
    private readonly PersonalOnlineCommerceAuthorization? _shoppingAuth;

    public GetStorefrontProductImage(
        ISellerCustomerOrderingCapability capability,
        GetCatalogProductImage images,
        PersonalOnlineCommerceAuthorization? shoppingAuth = null)
    {
        _capability = capability;
        _images = images;
        _shoppingAuth = shoppingAuth;
    }

    public async Task<ApplicationResult<ProductImageBytes>> ExecuteAsync(
        Guid sellerOrganizationId,
        Guid productId,
        string variant,
        CancellationToken cancellationToken = default,
        Guid? personalPlatformUserId = null,
        Guid? platformBusinessCustomerId = null)
    {
        if (_shoppingAuth is not null)
        {
            if (personalPlatformUserId is not Guid personalUserId
                || personalUserId == Guid.Empty
                || platformBusinessCustomerId is not Guid pbcId
                || pbcId == Guid.Empty)
            {
                return ApplicationResult<ProductImageBytes>.Failure(
                    ApplicationErrorCodes.LinkedCustomerNotFound,
                    "Linked customer was not found.");
            }

            var shopping = await _shoppingAuth
                .AuthorizeShoppingAsync(
                    sellerOrganizationId,
                    personalUserId,
                    pbcId,
                    cancellationToken)
                .ConfigureAwait(false);
            if (!shopping.IsSuccess)
            {
                return ApplicationResult<ProductImageBytes>.Failure(
                    shopping.ErrorCode!,
                    shopping.ErrorMessage!);
            }
        }
        else
        {
            var capability = await _capability.ResolveAsync(sellerOrganizationId, cancellationToken).ConfigureAwait(false);
            if (!capability.CanCustomerOrder)
            {
                return ApplicationResult<ProductImageBytes>.Failure(
                    ApplicationErrorCodes.CustomerOrderOrderingUnavailable,
                    CustomerOnlineOrderingAccessRules.StoreNotAcceptingMessage);
            }
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
