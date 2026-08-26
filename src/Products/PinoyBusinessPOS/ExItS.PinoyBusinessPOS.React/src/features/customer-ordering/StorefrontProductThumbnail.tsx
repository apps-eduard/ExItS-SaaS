import type { CustomerStorefrontProductDto } from "@/api/pos/pos-customer-orders-client";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import { useStorefrontProductImageUrl } from "@/features/customer-ordering/use-storefront-product-image";

function productInitial(name: string): string {
  const trimmed = name.trim();
  return trimmed ? trimmed.charAt(0).toUpperCase() : "?";
}

type StorefrontProductThumbnailProps = {
  workspace: PosWorkspaceScope | null;
  sellerOrganizationId: string;
  product: CustomerStorefrontProductDto;
};

export function StorefrontProductThumbnail({
  workspace,
  sellerOrganizationId,
  product,
}: StorefrontProductThumbnailProps) {
  const imageUrl = useStorefrontProductImageUrl(
    workspace,
    sellerOrganizationId,
    product.productId,
    product.hasImage,
    product.imageVersion,
  );

  return (
    <div className="storefront-product-thumb" aria-hidden>
      {imageUrl ? (
        <img
          src={imageUrl}
          alt=""
          className="storefront-product-thumb__image"
          loading="lazy"
          decoding="async"
        />
      ) : (
        <span className="storefront-product-thumb__initial">{productInitial(product.name)}</span>
      )}
    </div>
  );
}
