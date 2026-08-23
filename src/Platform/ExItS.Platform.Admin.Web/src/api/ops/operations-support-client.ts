import { platformRequest } from "@/api/platform-http";
import type { PlatformAuditRecord } from "@/api/audit/audit-list-query";
import type { OrganizationCommercialSummary } from "@/api/organizations/organization-types";
import type { OrganizationDetail } from "@/api/organizations/organization-types";
import type { OrganizationPayment } from "@/api/organizations/billing-list-query";
import type { OrganizationSubscription } from "@/api/organizations/subscription-list-query";
import type { PlatformUserDetail } from "@/api/users/user-types";

export type OperationsSupportLookupMode =
  | "organization"
  | "publicOrganizationId"
  | "userEmail"
  | "publicUserId"
  | "subscriptionId"
  | "paymentId"
  | "paymentReference"
  | "deviceId";

export type OperationsSupportLookupRequest = {
  mode: OperationsSupportLookupMode;
  query: string;
  paymentMethod?: string;
};

export type OperationsSupportPosDevice = {
  id: string;
  organizationId: string;
  branchId: string;
  installationDeviceId: string;
  friendlyName: string;
  platform?: string | null;
  model?: string | null;
  appVersion?: string | null;
  status: string;
  registeredAtUtc: string;
  lastSeenAtUtc: string;
  revokedAtUtc?: string | null;
};

export type OperationsSupportLookupPayload = {
  organization?: OrganizationDetail | null;
  user?: PlatformUserDetail | null;
  subscription?: OrganizationSubscription | null;
  payment?: OrganizationPayment | null;
  device?: OperationsSupportPosDevice | null;
  subscriptions: OrganizationCommercialSummary["subscriptions"];
  latestEntitlements: OrganizationCommercialSummary["latestEntitlements"];
  payments: OrganizationCommercialSummary["payments"];
  devices: OperationsSupportPosDevice[];
  recentAudit: PlatformAuditRecord[];
};

export const OPERATIONS_SUPPORT_LOOKUP_PATH = "/api/v1/platform/operations/support/lookup";

export function postOperationsSupportLookup(
  baseUrl: string,
  body: OperationsSupportLookupRequest,
  signal?: AbortSignal,
): Promise<OperationsSupportLookupPayload> {
  return platformRequest<OperationsSupportLookupPayload>(baseUrl, {
    path: OPERATIONS_SUPPORT_LOOKUP_PATH,
    method: "POST",
    body,
    signal,
  });
}
