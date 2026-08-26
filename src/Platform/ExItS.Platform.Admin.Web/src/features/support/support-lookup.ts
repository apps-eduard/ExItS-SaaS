import { PlatformApiError } from "@/api/platform-http";
import {
  postOperationsSupportLookup,
  type OperationsSupportLookupMode,
  type OperationsSupportLookupPayload,
} from "@/api/ops/operations-support-client";
import type { PlatformAuditRecord } from "@/api/audit/audit-list-query";
import type { OrganizationCommercialSummary } from "@/api/organizations/organization-types";
import type { OrganizationDetail } from "@/api/organizations/organization-types";
import type { OrganizationPayment } from "@/api/organizations/billing-list-query";
import type { OrganizationSubscription } from "@/api/organizations/subscription-list-query";
import type { PlatformUserDetail } from "@/api/users/user-types";

export type SupportLookupMode = OperationsSupportLookupMode;

export type SupportLookupResult = {
  organization?: OrganizationDetail;
  commercialSummary?: OrganizationCommercialSummary;
  subscription?: OrganizationSubscription;
  payment?: OrganizationPayment;
  user?: PlatformUserDetail;
  device?: OperationsSupportLookupPayload["device"];
  devices: OperationsSupportLookupPayload["devices"];
  auditRecords: PlatformAuditRecord[];
};

export type SupportLookupResolution =
  | { kind: "success"; result: SupportLookupResult }
  | { kind: "notFound" }
  | { kind: "error"; message: string };

function mapCommercialSummary(payload: OperationsSupportLookupPayload): OrganizationCommercialSummary | undefined {
  if (
    payload.subscriptions.length === 0
    && payload.latestEntitlements.length === 0
    && payload.payments.length === 0
  ) {
    return undefined;
  }

  return {
    subscriptions: payload.subscriptions,
    latestEntitlements: payload.latestEntitlements,
    payments: payload.payments,
  };
}

export async function resolveSupportLookup(
  baseUrl: string,
  mode: SupportLookupMode,
  query: string,
  signal?: AbortSignal,
  paymentMethod?: string,
): Promise<SupportLookupResolution> {
  const trimmed = query.trim();
  if (!trimmed) {
    return { kind: "notFound" };
  }

  try {
    const payload = await postOperationsSupportLookup(
      baseUrl,
      {
        mode,
        query: trimmed,
        paymentMethod: paymentMethod?.trim() || undefined,
      },
      signal,
    );

    return {
      kind: "success",
      result: {
        organization: payload.organization ?? undefined,
        commercialSummary: mapCommercialSummary(payload),
        subscription: payload.subscription ?? undefined,
        payment: payload.payment ?? undefined,
        user: payload.user ?? undefined,
        device: payload.device ?? undefined,
        devices: payload.devices,
        auditRecords: payload.recentAudit,
      },
    };
  } catch (error) {
    if (error instanceof PlatformApiError && (error.status === 404 || error.status === 400)) {
      return { kind: "notFound" };
    }
    const message = error instanceof Error ? error.message : "Support lookup failed.";
    return { kind: "error", message };
  }
}
