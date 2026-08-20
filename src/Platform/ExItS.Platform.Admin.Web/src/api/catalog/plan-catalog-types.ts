export const PLAN_STATUSES = ["Active", "Inactive", "Retired"] as const;
export type PlanStatus = (typeof PLAN_STATUSES)[number];

export const PLAN_LIST_SORT_BY = [
  "Code",
  "DisplayName",
  "Status",
  "CreatedAtUtc",
  "UpdatedAtUtc",
] as const;
export type PlanListSortBy = (typeof PLAN_LIST_SORT_BY)[number];

export const PLAN_LIST_PAGE_SIZE = 20;

export type CatalogPlan = {
  id: string;
  productCode: string;
  code: string;
  displayName: string;
  status: string;
  createdAtUtc?: string;
  updatedAtUtc?: string;
  productId?: string;
  productDisplayName?: string;
  planKey?: string;
  description?: string;
  maxBranches?: number;
  maxActiveStaff?: number;
  maxActivePosDevices?: number;
  maxActiveBusinessTypes?: number;
  customerCreditEnabled?: boolean;
  advancedReportsEnabled?: boolean;
  exportEnabled?: boolean;
  trialAllowed?: boolean;
  defaultTrialDays?: number;
  sortOrder?: number;
  monthlyPrice?: number;
  annualPrice?: number;
  currencyCode?: string;
};

export type PlanListQuery = {
  page?: number;
  pageSize?: number;
  productCode?: string;
  status?: string;
  search?: string;
  sortBy?: string;
  sortDesc?: boolean;
};
