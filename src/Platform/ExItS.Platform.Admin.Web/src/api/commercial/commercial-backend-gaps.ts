/**
 * Reconfirmed PA-COM-01 backend gap register. Typed clients are not created for missing routes.
 * Do not invent HTTP. Do not treat this file as a UI surface.
 */
export const COMMERCIAL_BACKEND_GAPS = {
  planVersionRetireHttp: {
    id: "plan-version-retire-http",
    available: false,
    notes: "Domain PlanVersion.Retire exists; CatalogEndpoints has no version retire MapPost.",
  },
  draftBusinessTypeGrants: {
    id: "draft-business-type-grants",
    available: false,
    notes: "POST .../versions/draft forces businessTypeGrants: null.",
  },
  subscriptionRenewHttp: {
    id: "subscription-renew-http",
    available: false,
    notes: "No Admin POST .../renew. Local Validation can simulate renewal-succeed/fail.",
  },
  subscriptionActivateWithoutPayment: {
    id: "subscription-activate-payment-block",
    available: false,
    notes:
      "POST /api/v1/platform/subscriptions/{id}/activate exists but ActivateSubscription always returns PaymentRequiredForPaidActivation.",
  },
  maxActiveStaffInviteEnforcement: {
    id: "max-active-staff-invite-enforcement",
    available: false,
    notes: "Plan.MaxActiveStaff and plan-change preview exist; InvitationUseCases has no MaxActiveStaff check.",
  },
  entitlementGenerateReconcileOverride: {
    id: "entitlement-generate-reconcile-override",
    available: true,
    notes:
      "POST snapshots, POST reconcile, POST feature-overrides, POST feature-overrides/{id}/revoke exist.",
  },
} as const;

export const COMMERCIAL_OPERATOR_ENDPOINTS = {
  updatePlanCommercial: "PATCH /api/v1/platform/catalog/products/{productCode}/plans/{planId}/commercial",
  activatePlan: "POST /api/v1/platform/catalog/products/{productCode}/plans/{planId}/activate",
  deactivatePlan: "POST /api/v1/platform/catalog/products/{productCode}/plans/{planId}/deactivate",
  retirePlan: "POST /api/v1/platform/catalog/products/{productCode}/plans/{planId}/retire",
  createDraftPlanVersion:
    "POST /api/v1/platform/catalog/products/{productCode}/plans/{planId}/versions/draft",
  publishPlanVersion:
    "POST /api/v1/platform/catalog/products/{productCode}/plans/{planId}/versions/{versionNumber}/publish",
  upsertDraftFeatureGrant:
    "PUT /api/v1/platform/catalog/products/{productCode}/plans/{planId}/versions/{versionNumber}/feature-grants/{featureCode}",
  startTrial: "POST /api/v1/platform/organizations/{organizationId}/subscriptions/trials",
  createPaidSubscription: "POST /api/v1/platform/organizations/{organizationId}/subscriptions",
  fromCatalog: "POST /api/v1/platform/organizations/{organizationId}/subscriptions/from-catalog",
  upgrade: "POST /api/v1/platform/organizations/{organizationId}/subscriptions/{id}/upgrade",
  downgrade: "POST /api/v1/platform/organizations/{organizationId}/subscriptions/{id}/downgrade",
  convertTrial: "POST /api/v1/platform/organizations/{organizationId}/subscriptions/{id}/convert-trial",
  applyPendingPlan:
    "POST /api/v1/platform/organizations/{organizationId}/subscriptions/{id}/apply-pending-plan",
  suspend: "POST /api/v1/platform/subscriptions/{id}/suspend",
  reactivate: "POST /api/v1/platform/subscriptions/{id}/reactivate",
  cancel: "POST /api/v1/platform/subscriptions/{id}/cancel",
  gracePeriod: "POST /api/v1/platform/subscriptions/{id}/grace-period",
  pastDue: "POST /api/v1/platform/subscriptions/{id}/past-due",
  expire: "POST /api/v1/platform/subscriptions/{id}/expire",
  createManualPayment: "POST /api/v1/platform/payments/manual",
  confirmPayment: "POST /api/v1/platform/payments/{id}/confirm",
  rejectPayment: "POST /api/v1/platform/payments/{id}/reject",
  voidPayment: "POST /api/v1/platform/payments/{id}/void",
  activateSubscriptionFromPayment: "POST /api/v1/platform/payments/{id}/activate-subscription",
  simulateLocalValidationPayment: "POST /api/v1/platform/local-validation/payments/simulate",
  generateEntitlementSnapshot:
    "POST /api/v1/platform/organizations/{orgId}/products/{productCode}/entitlements/snapshots",
  reconcileEntitlement:
    "POST /api/v1/platform/organizations/{orgId}/products/{productCode}/entitlements/reconcile",
  createFeatureOverride:
    "POST /api/v1/platform/organizations/{orgId}/products/{productCode}/feature-overrides",
  revokeFeatureOverride: "POST /api/v1/platform/feature-overrides/{id}/revoke",
} as const;
