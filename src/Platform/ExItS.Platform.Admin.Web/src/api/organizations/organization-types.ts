export const ORGANIZATION_STATUSES = ["Active", "Suspended", "Closed"] as const;
export type OrganizationStatus = (typeof ORGANIZATION_STATUSES)[number];

export const ORGANIZATION_LIST_SORT_BY = [
  "DisplayName",
  "Slug",
  "Status",
  "CreatedAtUtc",
  "UpdatedAtUtc",
] as const;
export type OrganizationListSortBy = (typeof ORGANIZATION_LIST_SORT_BY)[number];

export const ORGANIZATION_LIST_PAGE_SIZE = 20;

export type OrganizationListItem = {
  id: string;
  displayName: string;
  slug: string;
  status: string;
  createdAtUtc?: string;
  updatedAtUtc?: string;
};

export type OrganizationListQuery = {
  page?: number;
  pageSize?: number;
  status?: string;
  search?: string;
  sortBy?: string;
  sortDesc?: boolean;
};
