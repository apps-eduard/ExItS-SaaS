import { z } from "zod";
import { platformRequest } from "@/api/platform/platform-http";

const guidSchema = z
  .string()
  .regex(/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/);

function pick(raw: Record<string, unknown>, camel: string, pascal: string): unknown {
  return raw[camel] ?? raw[pascal];
}

export const subscriptionPaymentActivitySchema = z.object({
  eventType: z.string(),
  message: z.string(),
  occurredAtUtc: z.string(),
});

export type SubscriptionPaymentActivityDto = z.infer<typeof subscriptionPaymentActivitySchema>;

export const subscriptionPaymentTransactionSchema = z.object({
  id: guidSchema,
  referenceNumber: z.string(),
  organizationId: guidSchema.nullable().optional().default(null),
  subscriptionId: guidSchema.nullable().optional().default(null),
  planKey: z.string(),
  billingCycle: z.string(),
  baseAmount: z.number(),
  discountAmount: z.number(),
  discountPercent: z.number(),
  finalAmount: z.number(),
  currencyCode: z.string(),
  channel: z.string().nullable().optional().default(null),
  provider: z.string(),
  environment: z.string(),
  status: z.string(),
  providerReference: z.string().nullable().optional().default(null),
  cardBrand: z.string().nullable().optional().default(null),
  cardLast4: z.string().nullable().optional().default(null),
  failureCode: z.string().nullable().optional().default(null),
  failureReason: z.string().nullable().optional().default(null),
  createdAtUtc: z.string(),
  processingAtUtc: z.string().nullable().optional().default(null),
  paidAtUtc: z.string().nullable().optional().default(null),
  failedAtUtc: z.string().nullable().optional().default(null),
  cancelledAtUtc: z.string().nullable().optional().default(null),
  expiredAtUtc: z.string().nullable().optional().default(null),
  periodStartUtc: z.string().nullable().optional().default(null),
  periodEndUtc: z.string().nullable().optional().default(null),
  subscriptionActivated: z.boolean(),
  activities: z.array(subscriptionPaymentActivitySchema).default([]),
});

export type SubscriptionPaymentTransactionDto = z.infer<
  typeof subscriptionPaymentTransactionSchema
>;

export type SubscriptionPaymentChannel = "GCash" | "Maya" | "Card";

export type ProcessSubscriptionPaymentRequest = {
  channel: SubscriptionPaymentChannel;
  /** Card fields are submitted once; never persisted client-side after the request. */
  cardNumber?: string | null;
  cardExpiry?: string | null;
  cardName?: string | null;
  cardCvv?: string | null;
  simulationOutcome?: "succeed" | "fail" | "processing" | null;
};

function normalizeActivity(raw: unknown): unknown {
  if (!raw || typeof raw !== "object") return raw;
  const r = raw as Record<string, unknown>;
  const occurred = pick(r, "occurredAtUtc", "OccurredAtUtc");
  return {
    eventType: String(pick(r, "eventType", "EventType") ?? ""),
    message: String(pick(r, "message", "Message") ?? ""),
    occurredAtUtc:
      typeof occurred === "string"
        ? occurred
        : occurred instanceof Date
          ? occurred.toISOString()
          : String(occurred ?? ""),
  };
}

function normalizePayment(raw: unknown): unknown {
  if (!raw || typeof raw !== "object") return raw;
  const r = raw as Record<string, unknown>;
  const activitiesRaw = pick(r, "activities", "Activities");
  const activities = Array.isArray(activitiesRaw)
    ? activitiesRaw.map(normalizeActivity)
    : [];

  const toIso = (value: unknown): string | null => {
    if (value == null) return null;
    if (typeof value === "string") return value;
    if (value instanceof Date) return value.toISOString();
    return String(value);
  };

  return {
    id: String(pick(r, "id", "Id") ?? ""),
    referenceNumber: String(pick(r, "referenceNumber", "ReferenceNumber") ?? ""),
    organizationId: pick(r, "organizationId", "OrganizationId") ?? null,
    subscriptionId: pick(r, "subscriptionId", "SubscriptionId") ?? null,
    planKey: String(pick(r, "planKey", "PlanKey") ?? ""),
    billingCycle: String(pick(r, "billingCycle", "BillingCycle") ?? ""),
    baseAmount: Number(pick(r, "baseAmount", "BaseAmount") ?? 0),
    discountAmount: Number(pick(r, "discountAmount", "DiscountAmount") ?? 0),
    discountPercent: Number(pick(r, "discountPercent", "DiscountPercent") ?? 0),
    finalAmount: Number(pick(r, "finalAmount", "FinalAmount") ?? 0),
    currencyCode: String(pick(r, "currencyCode", "CurrencyCode") ?? "PHP"),
    channel: pick(r, "channel", "Channel") ?? null,
    provider: String(pick(r, "provider", "Provider") ?? ""),
    environment: String(pick(r, "environment", "Environment") ?? ""),
    status: String(pick(r, "status", "Status") ?? ""),
    providerReference: pick(r, "providerReference", "ProviderReference") ?? null,
    cardBrand: pick(r, "cardBrand", "CardBrand") ?? null,
    cardLast4: pick(r, "cardLast4", "CardLast4") ?? null,
    failureCode: pick(r, "failureCode", "FailureCode") ?? null,
    failureReason: pick(r, "failureReason", "FailureReason") ?? null,
    createdAtUtc: toIso(pick(r, "createdAtUtc", "CreatedAtUtc")) ?? "",
    processingAtUtc: toIso(pick(r, "processingAtUtc", "ProcessingAtUtc")),
    paidAtUtc: toIso(pick(r, "paidAtUtc", "PaidAtUtc")),
    failedAtUtc: toIso(pick(r, "failedAtUtc", "FailedAtUtc")),
    cancelledAtUtc: toIso(pick(r, "cancelledAtUtc", "CancelledAtUtc")),
    expiredAtUtc: toIso(pick(r, "expiredAtUtc", "ExpiredAtUtc")),
    periodStartUtc: toIso(pick(r, "periodStartUtc", "PeriodStartUtc")),
    periodEndUtc: toIso(pick(r, "periodEndUtc", "PeriodEndUtc")),
    subscriptionActivated: Boolean(pick(r, "subscriptionActivated", "SubscriptionActivated")),
    activities,
  };
}

function paymentPath(organizationId: string, paymentId: string, suffix = ""): string {
  return `/api/v1/platform/organizations/${organizationId}/subscription-payments/${paymentId}${suffix}`;
}

function personalPaymentPath(paymentId: string, suffix = ""): string {
  return `/api/v1/personal/subscription-payments/${paymentId}${suffix}`;
}

export async function createPersonalSubscriptionPayment(
  request: { planKey: string; billingCycle: string },
  signal?: AbortSignal,
): Promise<SubscriptionPaymentTransactionDto> {
  const raw = await platformRequest<unknown>({
    method: "POST",
    path: "/api/v1/personal/subscription-payments",
    body: {
      planKey: request.planKey,
      billingCycle: request.billingCycle,
    },
    signal,
  });
  return subscriptionPaymentTransactionSchema.parse(normalizePayment(raw));
}

export async function getPersonalSubscriptionPayment(
  paymentId: string,
  signal?: AbortSignal,
): Promise<SubscriptionPaymentTransactionDto> {
  const raw = await platformRequest<unknown>({
    path: personalPaymentPath(paymentId),
    signal,
  });
  return subscriptionPaymentTransactionSchema.parse(normalizePayment(raw));
}

export async function processPersonalSubscriptionPaymentSimulator(
  paymentId: string,
  request: ProcessSubscriptionPaymentRequest,
  signal?: AbortSignal,
): Promise<SubscriptionPaymentTransactionDto> {
  const body = {
    channel: request.channel,
    cardNumber: request.cardNumber ?? null,
    cardExpiry: request.cardExpiry ?? null,
    cardName: request.cardName ?? null,
    cardCvv: request.cardCvv ?? null,
    simulationOutcome: request.simulationOutcome ?? null,
  };

  const raw = await platformRequest<unknown>({
    method: "POST",
    path: personalPaymentPath(paymentId, "/process"),
    body,
    signal,
  });
  return subscriptionPaymentTransactionSchema.parse(normalizePayment(raw));
}

export async function retryPersonalSubscriptionPayment(
  paymentId: string,
  signal?: AbortSignal,
): Promise<SubscriptionPaymentTransactionDto> {
  const raw = await platformRequest<unknown>({
    method: "POST",
    path: personalPaymentPath(paymentId, "/retry"),
    signal,
  });
  return subscriptionPaymentTransactionSchema.parse(normalizePayment(raw));
}

export async function selectPersonalSubscriptionPaymentChannel(
  paymentId: string,
  channel: SubscriptionPaymentChannel,
  signal?: AbortSignal,
): Promise<SubscriptionPaymentTransactionDto> {
  const raw = await platformRequest<unknown>({
    method: "POST",
    path: personalPaymentPath(paymentId, "/select-channel"),
    body: { channel },
    signal,
  });
  return subscriptionPaymentTransactionSchema.parse(normalizePayment(raw));
}

/** @deprecated Prefer personal pre-org APIs for new checkout. Kept for org-attached payments. */
export async function getSubscriptionPayment(
  organizationId: string,
  paymentId: string,
  signal?: AbortSignal,
): Promise<SubscriptionPaymentTransactionDto> {
  if (!organizationId) {
    return getPersonalSubscriptionPayment(paymentId, signal);
  }
  const raw = await platformRequest<unknown>({
    path: paymentPath(organizationId, paymentId),
    signal,
  });
  return subscriptionPaymentTransactionSchema.parse(normalizePayment(raw));
}

/** @deprecated Prefer personal pre-org APIs for new checkout. Kept for org-attached payments. */
export async function processSubscriptionPaymentSimulator(
  organizationId: string,
  paymentId: string,
  request: ProcessSubscriptionPaymentRequest,
  signal?: AbortSignal,
): Promise<SubscriptionPaymentTransactionDto> {
  if (!organizationId) {
    return processPersonalSubscriptionPaymentSimulator(paymentId, request, signal);
  }
  const body = {
    channel: request.channel,
    cardNumber: request.cardNumber ?? null,
    cardExpiry: request.cardExpiry ?? null,
    cardName: request.cardName ?? null,
    cardCvv: request.cardCvv ?? null,
    simulationOutcome: request.simulationOutcome ?? null,
  };

  const raw = await platformRequest<unknown>({
    method: "POST",
    path: paymentPath(organizationId, paymentId, "/process"),
    body,
    signal,
  });
  return subscriptionPaymentTransactionSchema.parse(normalizePayment(raw));
}

/** Display helpers for simulator test cards (never validate real PANs). */
export const SUBSCRIPTION_CARD_SIMULATOR = {
  successPan: "4242424242424242",
  declinePan: "4000000000000002",
  pendingPan: "4000000000009995",
  autofillName: "Test Cardholder",
  autofillExpiry: "12/30",
  autofillCvv: "123",
} as const;

export function describeCardSimulatorPan(panDigits: string): "success" | "decline" | "processing" | "other" {
  const digits = panDigits.replace(/\D/g, "");
  if (digits === SUBSCRIPTION_CARD_SIMULATOR.successPan) return "success";
  if (digits === SUBSCRIPTION_CARD_SIMULATOR.declinePan) return "decline";
  if (digits === SUBSCRIPTION_CARD_SIMULATOR.pendingPan) return "processing";
  return "other";
}
