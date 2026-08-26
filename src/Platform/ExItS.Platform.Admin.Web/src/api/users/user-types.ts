export const ACCOUNT_STATUSES = [
  "Active",
  "Suspended",
  "Deactivated",
  "PendingVerification",
] as const;
export type AccountStatus = (typeof ACCOUNT_STATUSES)[number];

export const USER_DIRECTORY_FILTERS = [
  "PlatformStaff",
  "Organization",
  "Personal",
  "Unassigned",
] as const;
export type UserDirectoryFilter = (typeof USER_DIRECTORY_FILTERS)[number];

export const USER_LIST_SORT_BY = [
  "DisplayName",
  "Username",
  "Email",
  "Status",
  "UpdatedUtc",
  "AccountType",
  "Organization",
] as const;
export type UserListSortBy = (typeof USER_LIST_SORT_BY)[number];

export const USER_LIST_PAGE_SIZE = 20;

export type PlatformUserListItem = {
  id: string;
  displayName: string;
  username: string;
  email: string;
  status: string;
  createdAtUtc?: string;
  updatedAtUtc?: string;
  accountClasses: string[];
  organizationNames: string[];
};

export type PlatformUserOrganizationItem = {
  name: string;
  role?: string;
  roleDisplay?: string;
};

export type PlatformUserDetail = {
  id: string;
  username: string;
  displayName: string;
  email: string;
  status: string;
  createdAtUtc?: string;
  updatedAtUtc?: string;
  suspendedAtUtc?: string;
  suspensionReason?: string;
  accountClasses: string[];
  organizationNames: string[];
  organizations?: PlatformUserOrganizationItem[];
  firstName?: string;
  lastName?: string;
  phone?: string;
  employeeCode?: string;
  staffNumber?: string;
  createdByUserId?: string;
};

export type UserListQuery = {
  page?: number;
  pageSize?: number;
  status?: string;
  search?: string;
  directory?: string;
  sortBy?: string;
  sortDesc?: boolean;
};
