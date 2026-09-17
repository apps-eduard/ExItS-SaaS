import {
  platformRequest,
  PlatformApiError,
  type PlatformProblemDetails,
} from "@/api/platform/platform-http";

export type OrganizationSaasPaymentDto = {
  id: string | null;
  subscriptionId: string | null;
  amount: number | null;
  currencyCode: string | null;
  method: string | null;
  externalReference: string | null;
  status: string | null;
  paidAtUtc: string | null;
  confirmedAtUtc: string | null;
  createdAtUtc: string | null;
};

export type OrganizationSaasPaymentListDto = {
  items: OrganizationSaasPaymentDto[];
  totalCount: number;
};

type ClientResult<T> =
  | { ok: true; value: T }
  | { ok: false; status: number; body: PlatformProblemDetails | null };

function asRecord(value: unknown): Record<string, unknown> | null {
  return typeof value === "object" && value !== null ? (value as Record<string, unknown>) : null;
}

function pick(raw: Record<string, unknown>, camel: string, pascal: string): unknown {
  return raw[camel] ?? raw[pascal];
}

function pickString(raw: Record<string, unknown>, camel: string, pascal: string): string | null {
  const value = pick(raw, camel, pascal);
  if (typeof value !== "string") return null;
  const trimmed = value.trim();
  return trimmed.length > 0 ? trimmed : null;
}

function pickNumber(raw: Record<string, unknown>, camel: string, pascal: string): number | null {
  const value = pick(raw, camel, pascal);
  if (value == null || value === "") return null;
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : null;
}

function pickInstant(raw: Record<string, unknown>, camel: string, pascal: string): string | null {
  const value = pick(raw, camel, pascal);
  if (value == null) return null;
  if (typeof value === "string") {
    const trimmed = value.trim();
    return trimmed.length > 0 ? trimmed : null;
  }
  if (value instanceof Date) return value.toISOString();
  return null;
}

function normalizePayment(raw: unknown): OrganizationSaasPaymentDto | null {
  const r = asRecord(raw);
  if (!r) return null;
  return {
    id: pickString(r, "id", "Id"),
    subscriptionId: pickString(r, "subscriptionId", "SubscriptionId"),
    amount: pickNumber(r, "amount", "Amount"),
    currencyCode: pickString(r, "currencyCode", "CurrencyCode"),
    method: pickString(r, "method", "Method"),
    externalReference: pickString(r, "externalReference", "ExternalReference"),
    status: pickString(r, "status", "Status"),
    paidAtUtc: pickInstant(r, "paidAtUtc", "PaidAtUtc"),
    confirmedAtUtc: pickInstant(r, "confirmedAtUtc", "ConfirmedAtUtc"),
    createdAtUtc: pickInstant(r, "createdAtUtc", "CreatedAtUtc"),
  };
}

/**
 * Organization SaaS payment / invoice-like history for Subscription & Billing.
 * Owner commercial authority or Platform ManageManualPayments.
 */
export async function listOrganizationSaasPayments(
  organizationId: string,
  signal?: AbortSignal,
): Promise<ClientResult<OrganizationSaasPaymentListDto>> {
  try {
    const query = new URLSearchParams({ page: "1", pageSize: "25" });
    const payload = await platformRequest<Record<string, unknown>>({
      method: "GET",
      path: `/api/v1/platform/organizations/${organizationId}/payments?${query.toString()}`,
      signal,
    });
    const itemsRaw = pick(payload, "items", "Items");
    const items = Array.isArray(itemsRaw)
      ? itemsRaw
          .map(normalizePayment)
          .filter((item): item is OrganizationSaasPaymentDto => item != null)
      : [];
    return {
      ok: true,
      value: {
        items,
        totalCount: pickNumber(payload, "totalCount", "TotalCount") ?? items.length,
      },
    };
  } catch (error) {
    if (error instanceof PlatformApiError) {
      return { ok: false, status: error.status, body: error.problem };
    }
    throw error;
  }
}
