export const PLATFORM_PERMISSIONS = {
  viewPortfolio: "platform.permission.view_portfolio",
  manageOrganizations: "platform.permission.manage_organizations",
  manageCatalog: "platform.permission.manage_catalog",
  managePlatformUsers: "platform.permission.manage_platform_users",
  manageMemberships: "platform.permission.manage_memberships",
  manageProductAccess: "platform.permission.manage_product_access",
  manageSubscriptions: "platform.permission.manage_subscriptions",
  manageManualPayments: "platform.permission.manage_manual_payments",
  manageEntitlementOverrides: "platform.permission.manage_entitlement_overrides",
  viewAuditRecords: "platform.permission.view_audit_records",
  viewGlobalCatalog: "platform.permission.view_global_catalog",
  manageGlobalCategories: "platform.permission.manage_global_categories",
  manageGlobalProducts: "platform.permission.manage_global_products",
  importGlobalProducts: "platform.permission.import_global_products",
  viewPrivacyCompliance: "platform.permission.view_privacy_compliance",
} as const;

export type PlatformPermissionCode =
  (typeof PLATFORM_PERMISSIONS)[keyof typeof PLATFORM_PERMISSIONS];

export type ResolvedPermissionsDto = {
  actorIdentifier: string;
  actorType: string;
  platformUserId: string | null;
  organizationId: string | null;
  permissions: string[];
};
