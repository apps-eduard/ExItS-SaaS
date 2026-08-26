const GUID_PATTERN =
  /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/;

export function parseProductId(value: string | undefined): string | null {
  if (!value || !GUID_PATTERN.test(value)) {
    return null;
  }
  return value;
}

export function productDetailHref(productId: string): string {
  return `/admin/products/${productId}`;
}

export function productsListHref(listSearch?: string): string {
  if (!listSearch) {
    return "/admin/products";
  }
  return listSearch.startsWith("?")
    ? `/admin/products${listSearch}`
    : `/admin/products?${listSearch}`;
}
