import type { CatalogProduct } from "@/api/catalog/product-catalog-client";

export function productLifecycleActions(status: string): {
  canActivate: boolean;
  canDeactivate: boolean;
  canRetire: boolean;
} {
  if (status === "Retired") {
    return { canActivate: false, canDeactivate: false, canRetire: false };
  }
  if (status === "Active") {
    return { canActivate: false, canDeactivate: true, canRetire: true };
  }
  return { canActivate: true, canDeactivate: false, canRetire: true };
}

export function productRenameValues(product: CatalogProduct) {
  return { displayName: product.displayName };
}
