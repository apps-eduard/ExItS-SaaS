import {
  PRODUCT_LIST_PAGE_SIZE,
  PRODUCT_LIST_SORT_BY,
  PRODUCT_STATUSES,
  type ProductListQuery,
  type ProductListSortBy,
  type ProductStatus,
} from "@/api/catalog/product-list-types";
import { withQuery } from "@/lib/http/query-string";

export type ProductListUrlState = {
  page: number;
  search: string;
  status: ProductStatus | "";
  sortBy: ProductListSortBy;
  sortDesc: boolean;
};

const DEFAULT_SORT: ProductListSortBy = "DisplayName";

export function isProductStatus(value: string): value is ProductStatus {
  return (PRODUCT_STATUSES as readonly string[]).includes(value);
}

export function isProductListSortBy(value: string): value is ProductListSortBy {
  return (PRODUCT_LIST_SORT_BY as readonly string[]).includes(value);
}

export function parseProductListSearchParams(params: URLSearchParams): ProductListUrlState {
  const pageRaw = Number(params.get("page") ?? "1");
  const page = Number.isFinite(pageRaw) && pageRaw >= 1 ? Math.floor(pageRaw) : 1;
  const statusRaw = params.get("status") ?? "";
  const sortRaw = params.get("sortBy") ?? DEFAULT_SORT;
  return {
    page,
    search: params.get("search")?.trim() ?? "",
    status: isProductStatus(statusRaw) ? statusRaw : "",
    sortBy: isProductListSortBy(sortRaw) ? sortRaw : DEFAULT_SORT,
    sortDesc: params.get("sortDesc") === "true",
  };
}

export function productListSearchParams(state: ProductListUrlState): URLSearchParams {
  const params = new URLSearchParams();
  if (state.search) {
    params.set("search", state.search);
  }
  if (state.status) {
    params.set("status", state.status);
  }
  if (state.sortBy !== DEFAULT_SORT) {
    params.set("sortBy", state.sortBy);
  }
  if (state.sortDesc) {
    params.set("sortDesc", "true");
  }
  if (state.page > 1) {
    params.set("page", String(state.page));
  }
  return params;
}

export function catalogProductsListRequestPath(query: ProductListQuery): string {
  return withQuery("/api/v1/platform/catalog/products", {
    page: query.page ?? 1,
    pageSize: query.pageSize ?? PRODUCT_LIST_PAGE_SIZE,
    status: query.status,
    search: query.search,
    sortBy: query.sortBy,
    sortDesc: query.sortDesc === true ? true : undefined,
  });
}

export function hasActiveProductFilters(state: ProductListUrlState): boolean {
  return Boolean(state.search || state.status);
}
