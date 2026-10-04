using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Products;

namespace ExItS.Platform.Application.LocalValidation;

/// <summary>
/// Idempotent Local Validation catalog registration for Pinoy Service Pro.
/// Registers the product only. It does not create plans, trials, subscriptions, or product operations.
/// </summary>
public sealed class EnsurePspLocalValidationCatalog
{
    public const string ProductDisplayName = "Pinoy Service Pro";

    private readonly CreateProduct _createProduct;
    private readonly IProductRepository _products;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public EnsurePspLocalValidationCatalog(
        CreateProduct createProduct,
        IProductRepository products,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _createProduct = createProduct;
        _products = products;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task EnsureReferenceAsync(CancellationToken cancellationToken = default)
    {
        var code = ProductCode.Create(ProductCode.PinoyServicePro);
        var product = await _products.GetByCodeAsync(code, cancellationToken).ConfigureAwait(false);
        if (product is null)
        {
            var created = await _createProduct
                .ExecuteAsync(ProductCode.PinoyServicePro, ProductDisplayName, cancellationToken)
                .ConfigureAwait(false);
            if (!created.IsSuccess)
            {
                product = await _products.GetByCodeAsync(code, cancellationToken).ConfigureAwait(false);
                if (product is null)
                {
                    throw new InvalidOperationException(
                        $"Local validation PSP product create failed: {created.ErrorCode} {created.ErrorMessage}");
                }
            }
            else
            {
                product = created.Value;
            }
        }

        if (product is not null
            && !string.Equals(product.DisplayName, ProductDisplayName, StringComparison.Ordinal))
        {
            product.Rename(ProductDisplayName, _clock.UtcNow);
            await _products.UpdateAsync(product, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
