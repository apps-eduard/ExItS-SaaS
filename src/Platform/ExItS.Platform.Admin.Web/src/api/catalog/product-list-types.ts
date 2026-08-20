export const PRODUCT_STATUSES = ["Active", "Inactive", "Retired"] as const;
export type ProductStatus = (typeof PRODUCT_STATUSES)[number];

export const PRODUCT_LIST_SORT_BY = [
  "Code",
  "DisplayName",
  "Status",
  "CreatedAtUtc",
  "UpdatedAtUtc",
] as const;
export type ProductListSortBy = (typeof PRODUCT_LIST_SORT_BY)[number];

export const PRODUCT_LIST_PAGE_SIZE = 20;

export type ProductListQuery = {
  page?: number;
  pageSize?: number;
  status?: string;
  search?: string;
  sortBy?: string;
  sortDesc?: boolean;
};
