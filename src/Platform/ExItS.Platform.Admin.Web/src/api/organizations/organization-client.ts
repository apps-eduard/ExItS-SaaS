import { parsePagedResult, type PagedResult } from "@/api/platform/paged-result";
import { platformRequest } from "@/api/platform-http";
import { organizationsListPath } from "@/features/overview/dashboard-bounds";
import { organizationsListRequestPath } from "@/api/organizations/organization-list-query";
import {
  organizationInvitationsRequestPath,
  organizationMembersRequestPath,
} from "@/api/organizations/people-list-query";
import {
  organizationPaymentsRequestPath,
  type OrganizationBillingUrlState,
  type OrganizationPayment,
} from "@/api/organizations/billing-list-query";
import {
  organizationAuditRequestPath,
  type OrganizationAuditRecord,
  type OrganizationAuditUrlState,
} from "@/api/organizations/organization-audit-list-query";
import {
  organizationEntitlementSnapshotsRequestPath,
  type EntitlementGrant,
  type EntitlementSnapshot,
} from "@/api/organizations/entitlement-list-query";
import {
  organizationSubscriptionsRequestPath,
  type OrganizationSubscription,
  type OrganizationSubscriptionUrlState,
} from "@/api/organizations/subscription-list-query";
import type {
  OrganizationBranch,
  OrganizationBranding,
  OrganizationCommercialSummary,
  OrganizationDetail,
  OrganizationInvitation,
  OrganizationListItem,
  OrganizationListQuery,
  OrganizationMember,
  OrganizationProfile,
} from "@/api/organizations/organization-types";

function readString(record: Record<string, unknown>, ...keys: string[]): string | undefined {
  for (const key of keys) {
    const value = record[key];
    if (typeof value === "string" && value.length > 0) {
      return value;
    }
  }
  return undefined;
}

export function mapOrganizationListItem(payload: unknown): OrganizationListItem {
  if (typeof payload !== "object" || payload === null) {
    throw new Error("Invalid organization list item.");
  }
  const record = payload as Record<string, unknown>;
  const id = readString(record, "id", "Id");
  const displayName = readString(record, "displayName", "DisplayName");
  const slug = readString(record, "slug", "Slug");
  const status = readString(record, "status", "Status");
  if (!id || !displayName || !slug || !status) {
    throw new Error("Invalid organization list item.");
  }
  return {
    id,
    displayName,
    slug,
    status,
    createdAtUtc: readString(record, "createdAtUtc", "CreatedAtUtc"),
    updatedAtUtc: readString(record, "updatedAtUtc", "UpdatedAtUtc"),
  };
}

function asRecord(payload: unknown): Record<string, unknown> | null {
  if (typeof payload !== "object" || payload === null) {
    return null;
  }
  return payload as Record<string, unknown>;
}

function mapProfile(payload: unknown): OrganizationProfile {
  const record = asRecord(payload);
  if (!record) {
    return {};
  }
  return {
    legalName: readString(record, "legalName", "LegalName"),
    contactEmail: readString(record, "contactEmail", "ContactEmail"),
    contactPhone: readString(record, "contactPhone", "ContactPhone"),
    addressLine1: readString(record, "addressLine1", "AddressLine1"),
    addressLine2: readString(record, "addressLine2", "AddressLine2"),
    city: readString(record, "city", "City"),
    region: readString(record, "region", "Region"),
    postalCode: readString(record, "postalCode", "PostalCode"),
    countryCode: readString(record, "countryCode", "CountryCode"),
    timeZoneId: readString(record, "timeZoneId", "TimeZoneId"),
    locale: readString(record, "locale", "Locale"),
    currencyCode: readString(record, "currencyCode", "CurrencyCode"),
  };
}

function mapBranding(payload: unknown): OrganizationBranding {
  const record = asRecord(payload);
  if (!record) {
    return {};
  }
  return {
    brandDisplayName: readString(record, "brandDisplayName", "BrandDisplayName"),
    primaryColor: readString(record, "primaryColor", "PrimaryColor"),
    accentColor: readString(record, "accentColor", "AccentColor"),
  };
}

export function mapOrganizationDetail(payload: unknown): OrganizationDetail {
  const item = mapOrganizationListItem(payload);
  const record = asRecord(payload) ?? {};
  return {
    ...item,
    profile: mapProfile(record.profile ?? record.Profile),
    branding: mapBranding(record.branding ?? record.Branding),
  };
}

function mapNamedRecords<T>(
  payload: unknown,
  mapItem: (record: Record<string, unknown>) => T | null,
): T[] {
  if (!Array.isArray(payload)) {
    return [];
  }
  return payload.flatMap((item) => {
    const record = asRecord(item);
    if (!record) {
      return [];
    }
    const mapped = mapItem(record);
    return mapped ? [mapped] : [];
  });
}

export function mapOrganizationCommercialSummary(payload: unknown): OrganizationCommercialSummary {
  const record = asRecord(payload);
  if (!record) {
    throw new Error("Invalid commercial summary.");
  }
  return {
    subscriptions: mapNamedRecords(record.subscriptions ?? record.Subscriptions, (item) => {
      const id = readString(item, "id", "Id");
      const productCode = readString(item, "productCode", "ProductCode");
      const status = readString(item, "status", "Status");
      if (!id || !productCode || !status) {
        return null;
      }
      return { id, productCode, status };
    }),
    payments: mapNamedRecords(record.payments ?? record.Payments, (item) => {
      const id = readString(item, "id", "Id");
      const productCode = readString(item, "productCode", "ProductCode");
      const status = readString(item, "status", "Status");
      if (!id || !productCode || !status) {
        return null;
      }
      return {
        id,
        productCode,
        status,
        paidAtUtc: readString(item, "paidAtUtc", "PaidAtUtc"),
      };
    }),
    latestEntitlements: mapNamedRecords(
      record.latestEntitlements ?? record.LatestEntitlements,
      (item) => {
        const id = readString(item, "id", "Id");
        const productCode = readString(item, "productCode", "ProductCode");
        const subscriptionStatus = readString(item, "subscriptionStatus", "SubscriptionStatus");
        if (!id || !productCode || !subscriptionStatus) {
          return null;
        }
        return {
          id,
          productCode,
          subscriptionStatus,
          generatedAtUtc: readString(item, "generatedAtUtc", "GeneratedAtUtc"),
          productDisplayName: readString(item, "productDisplayName", "ProductDisplayName"),
          snapshotVersion: readNumber(item, "snapshotVersion", "SnapshotVersion"),
          schemaVersion: readNumber(item, "schemaVersion", "SchemaVersion"),
        };
      },
    ),
  };
}

export function getOrganization(
  baseUrl: string,
  organizationId: string,
  signal?: AbortSignal,
): Promise<OrganizationDetail> {
  return platformRequest<unknown>(baseUrl, {
    path: `/api/v1/platform/organizations/${organizationId}`,
    signal,
  }).then(mapOrganizationDetail);
}

export function getOrganizationCommercialSummary(
  baseUrl: string,
  organizationId: string,
  signal?: AbortSignal,
): Promise<OrganizationCommercialSummary> {
  return platformRequest<unknown>(baseUrl, {
    path: `/api/v1/platform/admin/organizations/${organizationId}/commercial-summary`,
    signal,
  }).then(mapOrganizationCommercialSummary);
}

function readNumber(record: Record<string, unknown>, ...keys: string[]): number | undefined {
  for (const key of keys) {
    const value = record[key];
    if (typeof value === "number" && Number.isFinite(value)) {
      return value;
    }
  }
  return undefined;
}

function readBoolean(record: Record<string, unknown>, ...keys: string[]): boolean | undefined {
  for (const key of keys) {
    const value = record[key];
    if (typeof value === "boolean") {
      return value;
    }
  }
  return undefined;
}

export function mapOrganizationBranch(payload: unknown): OrganizationBranch {
  const record = asRecord(payload);
  if (!record) {
    throw new Error("Invalid organization branch.");
  }
  const id = readString(record, "id", "Id");
  const organizationId = readString(record, "organizationId", "OrganizationId");
  const code = readString(record, "code", "Code");
  const name = readString(record, "name", "Name");
  const status = readString(record, "status", "Status");
  const isPrimary = readBoolean(record, "isPrimary", "IsPrimary");
  if (!id || !organizationId || !code || !name || !status || isPrimary === undefined) {
    throw new Error("Invalid organization branch.");
  }
  return {
    id,
    organizationId,
    code,
    name,
    status,
    isPrimary,
    addressLine1: readString(record, "addressLine1", "AddressLine1"),
    addressLine2: readString(record, "addressLine2", "AddressLine2"),
    city: readString(record, "city", "City"),
    region: readString(record, "region", "Region"),
    postalCode: readString(record, "postalCode", "PostalCode"),
    countryCode: readString(record, "countryCode", "CountryCode"),
    contactPhone: readString(record, "contactPhone", "ContactPhone"),
    timeZoneId: readString(record, "timeZoneId", "TimeZoneId"),
    createdAtUtc: readString(record, "createdAtUtc", "CreatedAtUtc"),
    updatedAtUtc: readString(record, "updatedAtUtc", "UpdatedAtUtc"),
  };
}

export function mapOrganizationBranches(payload: unknown): OrganizationBranch[] {
  if (!Array.isArray(payload)) {
    throw new Error("Invalid organization branch list.");
  }
  return payload.map(mapOrganizationBranch);
}

export function listOrganizationBranches(
  baseUrl: string,
  organizationId: string,
  signal?: AbortSignal,
): Promise<OrganizationBranch[]> {
  return platformRequest<unknown>(baseUrl, {
    path: `/api/v1/platform/organizations/${organizationId}/branches`,
    signal,
  }).then(mapOrganizationBranches);
}

export function mapOrganizationMember(payload: unknown): OrganizationMember {
  const record = asRecord(payload);
  if (!record) {
    throw new Error("Invalid organization member.");
  }
  const id = readString(record, "id", "Id");
  const organizationId = readString(record, "organizationId", "OrganizationId");
  const userId = readString(record, "userId", "UserId");
  const role = readString(record, "role", "Role");
  const status = readString(record, "status", "Status");
  if (!id || !organizationId || !userId || !role || !status) {
    throw new Error("Invalid organization member.");
  }
  return {
    id,
    organizationId,
    userId,
    role,
    status,
    createdAtUtc: readString(record, "createdAtUtc", "CreatedAtUtc"),
    updatedAtUtc: readString(record, "updatedAtUtc", "UpdatedAtUtc"),
    suspendedAtUtc: readString(record, "suspendedAtUtc", "SuspendedAtUtc"),
    removedAtUtc: readString(record, "removedAtUtc", "RemovedAtUtc"),
    username: readString(record, "username", "Username"),
    displayName: readString(record, "displayName", "DisplayName"),
    email: readString(record, "email", "Email"),
    roleDisplay: readString(record, "roleDisplay", "RoleDisplay"),
    accountStatus: readString(record, "accountStatus", "AccountStatus"),
    employeeCode: readString(record, "employeeCode", "EmployeeCode"),
  };
}

export function mapOrganizationInvitation(payload: unknown): OrganizationInvitation {
  const record = asRecord(payload);
  if (!record) {
    throw new Error("Invalid organization invitation.");
  }
  const id = readString(record, "id", "Id");
  const organizationId = readString(record, "organizationId", "OrganizationId");
  const email = readString(record, "email", "Email");
  const role = readString(record, "role", "Role");
  const status = readString(record, "status", "Status");
  if (!id || !organizationId || !email || !role || !status) {
    throw new Error("Invalid organization invitation.");
  }
  return {
    id,
    organizationId,
    email,
    role,
    status,
    createdAtUtc: readString(record, "createdAtUtc", "CreatedAtUtc"),
    updatedAtUtc: readString(record, "updatedAtUtc", "UpdatedAtUtc"),
    expiresAtUtc: readString(record, "expiresAtUtc", "ExpiresAtUtc"),
    acceptedAtUtc: readString(record, "acceptedAtUtc", "AcceptedAtUtc"),
    revokedAtUtc: readString(record, "revokedAtUtc", "RevokedAtUtc"),
    roleDisplay: readString(record, "roleDisplay", "RoleDisplay"),
    inviteeDisplayName: readString(record, "inviteeDisplayName", "InviteeDisplayName"),
    invitationStatus: readString(record, "invitationStatus", "InvitationStatus"),
  };
}

export function listOrganizationMembers(
  baseUrl: string,
  organizationId: string,
  options: { status?: string; page: number; pageSize?: number; signal?: AbortSignal },
): Promise<PagedResult<OrganizationMember>> {
  return platformRequest<unknown>(baseUrl, {
    path: organizationMembersRequestPath(organizationId, options),
    signal: options.signal,
  }).then((payload) => {
    const page = parsePagedResult<unknown>(payload);
    return {
      ...page,
      items: page.items.map(mapOrganizationMember),
    };
  });
}

export function listOrganizationInvitations(
  baseUrl: string,
  organizationId: string,
  options: { status?: string; page: number; pageSize?: number; signal?: AbortSignal },
): Promise<PagedResult<OrganizationInvitation>> {
  return platformRequest<unknown>(baseUrl, {
    path: organizationInvitationsRequestPath(organizationId, options),
    signal: options.signal,
  }).then((payload) => {
    const page = parsePagedResult<unknown>(payload);
    return {
      ...page,
      items: page.items.map(mapOrganizationInvitation),
    };
  });
}

export function mapOrganizationSubscription(payload: unknown): OrganizationSubscription {
  const record = asRecord(payload);
  if (!record) {
    throw new Error("Invalid organization subscription.");
  }
  const id = readString(record, "id", "Id");
  const organizationId = readString(record, "organizationId", "OrganizationId");
  const productCode = readString(record, "productCode", "ProductCode");
  const planId = readString(record, "planId", "PlanId");
  const status = readString(record, "status", "Status");
  if (!id || !organizationId || !productCode || !planId || !status) {
    throw new Error("Invalid organization subscription.");
  }
  return {
    id,
    organizationId,
    productCode,
    planId,
    status,
    createdAtUtc: readString(record, "createdAtUtc", "CreatedAtUtc"),
    updatedAtUtc: readString(record, "updatedAtUtc", "UpdatedAtUtc"),
    trialStartUtc: readString(record, "trialStartUtc", "TrialStartUtc"),
    trialEndUtc: readString(record, "trialEndUtc", "TrialEndUtc"),
    paidPeriodStartUtc: readString(record, "paidPeriodStartUtc", "PaidPeriodStartUtc"),
    paidPeriodEndUtc: readString(record, "paidPeriodEndUtc", "PaidPeriodEndUtc"),
    currentPeriodStartUtc: readString(record, "currentPeriodStartUtc", "CurrentPeriodStartUtc"),
    currentPeriodEndUtc: readString(record, "currentPeriodEndUtc", "CurrentPeriodEndUtc"),
    productDisplayName: readString(record, "productDisplayName", "ProductDisplayName"),
    planDisplayName: readString(record, "planDisplayName", "PlanDisplayName"),
    planKey: readString(record, "planKey", "PlanKey"),
    planVersionId: readString(record, "planVersionId", "PlanVersionId"),
    trialDefinitionId: readString(record, "trialDefinitionId", "TrialDefinitionId"),
    billingCycle: readString(record, "billingCycle", "BillingCycle"),
    agreedPrice: readNumber(record, "agreedPrice", "AgreedPrice"),
    currencyCode: readString(record, "currencyCode", "CurrencyCode"),
    pendingPlanId: readString(record, "pendingPlanId", "PendingPlanId"),
    pendingPlanEffectiveAtUtc: readString(
      record,
      "pendingPlanEffectiveAtUtc",
      "PendingPlanEffectiveAtUtc",
    ),
    gracePeriodEndUtc: readString(record, "gracePeriodEndUtc", "GracePeriodEndUtc"),
    suspendedAtUtc: readString(record, "suspendedAtUtc", "SuspendedAtUtc"),
    pastDueAtUtc: readString(record, "pastDueAtUtc", "PastDueAtUtc"),
    cancelledAtUtc: readString(record, "cancelledAtUtc", "CancelledAtUtc"),
    expiredAtUtc: readString(record, "expiredAtUtc", "ExpiredAtUtc"),
    version: readNumber(record, "version", "Version"),
  };
}

export function listOrganizationSubscriptions(
  baseUrl: string,
  organizationId: string,
  state: OrganizationSubscriptionUrlState,
  signal?: AbortSignal,
): Promise<PagedResult<OrganizationSubscription>> {
  return platformRequest<unknown>(baseUrl, {
    path: organizationSubscriptionsRequestPath(organizationId, state),
    signal,
  }).then((payload) => {
    const page = parsePagedResult<unknown>(payload);
    return {
      ...page,
      items: page.items.map(mapOrganizationSubscription),
    };
  });
}

function mapEntitlementGrant(payload: unknown): EntitlementGrant | null {
  const record = asRecord(payload);
  if (!record) {
    return null;
  }
  const featureCode = readString(record, "featureCode", "FeatureCode");
  const enabled = readBoolean(record, "enabled", "Enabled");
  if (!featureCode || enabled === undefined) {
    return null;
  }
  return {
    featureCode,
    enabled,
    numericLimit: readNumber(record, "numericLimit", "NumericLimit"),
    source: readString(record, "source", "Source"),
    effectiveAtUtc: readString(record, "effectiveAtUtc", "EffectiveAtUtc"),
    expiresAtUtc: readString(record, "expiresAtUtc", "ExpiresAtUtc"),
  };
}

export function mapEntitlementSnapshot(payload: unknown): EntitlementSnapshot {
  const record = asRecord(payload);
  if (!record) {
    throw new Error("Invalid entitlement snapshot.");
  }
  const id = readString(record, "id", "Id");
  const organizationId = readString(record, "organizationId", "OrganizationId");
  const productCode = readString(record, "productCode", "ProductCode");
  const subscriptionId = readString(record, "subscriptionId", "SubscriptionId");
  const planCode = readString(record, "planCode", "PlanCode");
  const snapshotVersion = readNumber(record, "snapshotVersion", "SnapshotVersion");
  const subscriptionStatus = readString(record, "subscriptionStatus", "SubscriptionStatus");
  const inGracePeriod = readBoolean(record, "inGracePeriod", "InGracePeriod");
  if (
    !id ||
    !organizationId ||
    !productCode ||
    !subscriptionId ||
    !planCode ||
    snapshotVersion === undefined ||
    !subscriptionStatus ||
    inGracePeriod === undefined
  ) {
    throw new Error("Invalid entitlement snapshot.");
  }
  const grantsPayload = record.grants ?? record.Grants;
  return {
    id,
    organizationId,
    productCode,
    subscriptionId,
    planCode,
    planVersionNumber: readNumber(record, "planVersionNumber", "PlanVersionNumber"),
    snapshotVersion,
    schemaVersion: readNumber(record, "schemaVersion", "SchemaVersion"),
    subscriptionStatus,
    inGracePeriod,
    generatedAtUtc: readString(record, "generatedAtUtc", "GeneratedAtUtc"),
    effectiveAtUtc: readString(record, "effectiveAtUtc", "EffectiveAtUtc"),
    refreshByUtc: readString(record, "refreshByUtc", "RefreshByUtc"),
    expiresAtUtc: readString(record, "expiresAtUtc", "ExpiresAtUtc"),
    grants: Array.isArray(grantsPayload)
      ? grantsPayload.flatMap((item) => {
          const mapped = mapEntitlementGrant(item);
          return mapped ? [mapped] : [];
        })
      : [],
  };
}

export function listOrganizationEntitlementSnapshots(
  baseUrl: string,
  organizationId: string,
  productCode: string,
  options: { page: number; pageSize?: number; signal?: AbortSignal },
): Promise<PagedResult<EntitlementSnapshot>> {
  return platformRequest<unknown>(baseUrl, {
    path: organizationEntitlementSnapshotsRequestPath(organizationId, productCode, options),
    signal: options.signal,
  }).then((payload) => {
    const page = parsePagedResult<unknown>(payload);
    return {
      ...page,
      items: page.items.map(mapEntitlementSnapshot),
    };
  });
}

export function mapOrganizationPayment(payload: unknown): OrganizationPayment {
  const record = asRecord(payload);
  if (!record) {
    throw new Error("Invalid organization payment.");
  }
  const id = readString(record, "id", "Id");
  const organizationId = readString(record, "organizationId", "OrganizationId");
  const productCode = readString(record, "productCode", "ProductCode");
  const amount = readNumber(record, "amount", "Amount");
  const currencyCode = readString(record, "currencyCode", "CurrencyCode");
  const method = readString(record, "method", "Method");
  const status = readString(record, "status", "Status");
  if (
    !id ||
    !organizationId ||
    !productCode ||
    amount === undefined ||
    !currencyCode ||
    !method ||
    !status
  ) {
    throw new Error("Invalid organization payment.");
  }
  return {
    id,
    organizationId,
    productCode,
    amount,
    currencyCode,
    method,
    status,
    subscriptionId: readString(record, "subscriptionId", "SubscriptionId"),
    externalReference: readString(record, "externalReference", "ExternalReference"),
    paidAtUtc: readString(record, "paidAtUtc", "PaidAtUtc"),
    confirmedAtUtc: readString(record, "confirmedAtUtc", "ConfirmedAtUtc"),
    rejectedAtUtc: readString(record, "rejectedAtUtc", "RejectedAtUtc"),
    voidedAtUtc: readString(record, "voidedAtUtc", "VoidedAtUtc"),
    createdAtUtc: readString(record, "createdAtUtc", "CreatedAtUtc"),
    updatedAtUtc: readString(record, "updatedAtUtc", "UpdatedAtUtc"),
    confirmedBy: readString(record, "confirmedBy", "ConfirmedBy"),
    rejectedBy: readString(record, "rejectedBy", "RejectedBy"),
    rejectionReason: readString(record, "rejectionReason", "RejectionReason"),
    voidedBy: readString(record, "voidedBy", "VoidedBy"),
    voidReason: readString(record, "voidReason", "VoidReason"),
    version: readNumber(record, "version", "Version"),
  };
}

export function listOrganizationPayments(
  baseUrl: string,
  organizationId: string,
  state: OrganizationBillingUrlState,
  signal?: AbortSignal,
): Promise<PagedResult<OrganizationPayment>> {
  return platformRequest<unknown>(baseUrl, {
    path: organizationPaymentsRequestPath(organizationId, state),
    signal,
  }).then((payload) => {
    const page = parsePagedResult<unknown>(payload);
    return {
      ...page,
      items: page.items.map(mapOrganizationPayment),
    };
  });
}

function mapOrganizationAuditRecord(payload: unknown): OrganizationAuditRecord {
  const record = asRecord(payload) ?? {};
  return {
    id: readString(record, "id", "Id") ?? "",
    occurredAtUtc: readString(record, "occurredAtUtc", "OccurredAtUtc") ?? "",
    actorIdentifier: readString(record, "actorIdentifier", "ActorIdentifier") ?? "",
    actorType: readString(record, "actorType", "ActorType") ?? "",
    actionCode: readString(record, "actionCode", "ActionCode") ?? "",
    targetType: readString(record, "targetType", "TargetType") ?? "",
    targetId: readString(record, "targetId", "TargetId") ?? "",
    organizationId: readString(record, "organizationId", "OrganizationId"),
    productCode: readString(record, "productCode", "ProductCode"),
    correlationId: readString(record, "correlationId", "CorrelationId"),
    outcome: readString(record, "outcome", "Outcome") ?? "",
    reason: readString(record, "reason", "Reason"),
    summary: readString(record, "summary", "Summary"),
  };
}

export function listOrganizationAuditRecords(
  baseUrl: string,
  organizationId: string,
  state: OrganizationAuditUrlState,
  signal?: AbortSignal,
): Promise<PagedResult<OrganizationAuditRecord>> {
  return platformRequest<unknown>(baseUrl, {
    path: organizationAuditRequestPath(organizationId, state),
    signal,
  }).then((payload) => {
    const page = parsePagedResult<unknown>(payload);
    return {
      ...page,
      items: page.items.map(mapOrganizationAuditRecord).filter((item) => item.id.length > 0),
    };
  });
}

export function listOrganizations(
  baseUrl: string,
  options: OrganizationListQuery & { signal?: AbortSignal },
): Promise<PagedResult<OrganizationListItem>> {
  const dashboardShaped =
    options.search == null &&
    options.sortBy == null &&
    options.sortDesc == null &&
    options.productCode == null &&
    (options.page == null || options.page === 1);
  const path = dashboardShaped
    ? organizationsListPath({
        status: options.status,
        pageSize: options.pageSize ?? 1,
      })
    : organizationsListRequestPath(options);

  return platformRequest<unknown>(baseUrl, {
    path,
    signal: options.signal,
  }).then((payload) => {
    const page = parsePagedResult<unknown>(payload);
    return {
      ...page,
      items: page.items.map(mapOrganizationListItem),
    };
  });
}
