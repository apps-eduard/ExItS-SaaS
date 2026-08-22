import { mapOrganizationPayment } from "@/api/organizations/organization-client";
import type { OrganizationPayment } from "@/api/organizations/billing-list-query";
import { mapOrganizationSubscription } from "@/api/organizations/organization-client";
import type { OrganizationSubscription } from "@/api/organizations/subscription-list-query";
import { commercialMutationRequest } from "@/api/commercial/commercial-http";
import { PlatformApiError } from "@/api/platform-http";

function requirePayment(payload: unknown): OrganizationPayment {
  return mapOrganizationPayment(payload);
}

export type CreateManualPaymentBody = {
  organizationId: string;
  productCode: string;
  amount: number;
  currencyCode: string;
  method: string;
  externalReference: string;
  paidAtUtc: string;
};

export type ConfirmPaymentBody = { confirmedBy: string };
export type RejectPaymentBody = { rejectedBy: string; reason: string };
export type VoidPaymentBody = { voidedBy: string; reason: string };

export type ActivateSubscriptionFromPaymentBody = {
  confirmedBy: string;
  subscriptionId: string;
  periodStartUtc: string;
  periodEndUtc: string;
};

export type PaymentAndSubscriptionResult = {
  payment: OrganizationPayment;
  subscription: OrganizationSubscription;
};

export const LOCAL_VALIDATION_PAYMENT_SIMULATIONS = [
  "succeed",
  "success",
  "decline",
  "declined",
  "pending",
  "fail",
  "failed",
  "refund",
  "refunded",
  "renewal-succeed",
  "renewal-succeeded",
  "renewal-fail",
  "renewal-failed",
] as const;

export type LocalValidationPaymentSimulation =
  (typeof LOCAL_VALIDATION_PAYMENT_SIMULATIONS)[number];

export type SimulateLocalValidationPaymentBody = {
  simulation: string;
  organizationId: string;
  subscriptionId: string;
  amount: number;
  currencyCode: string;
  idempotencyKey: string;
  purpose?: string | null;
  billingCycle?: string | null;
};

export type LocalValidationPaymentSimulationResult = {
  status: string;
  provider: string;
  providerReference: string;
  amount: number;
  currencyCode: string;
  isTest: boolean;
  failureCode?: string;
  failureMessage?: string;
  idempotencyKey?: string;
};

export function createManualPayment(
  baseUrl: string,
  body: CreateManualPaymentBody,
  signal?: AbortSignal,
): Promise<OrganizationPayment> {
  return commercialMutationRequest<unknown>(baseUrl, {
    method: "POST",
    path: "/api/v1/platform/payments/manual",
    body,
    signal,
  }).then(requirePayment);
}

export function confirmManualPayment(
  baseUrl: string,
  paymentId: string,
  body: ConfirmPaymentBody,
  signal?: AbortSignal,
): Promise<OrganizationPayment> {
  return commercialMutationRequest<unknown>(baseUrl, {
    method: "POST",
    path: `/api/v1/platform/payments/${paymentId}/confirm`,
    body,
    signal,
  }).then(requirePayment);
}

export function rejectManualPayment(
  baseUrl: string,
  paymentId: string,
  body: RejectPaymentBody,
  signal?: AbortSignal,
): Promise<OrganizationPayment> {
  return commercialMutationRequest<unknown>(baseUrl, {
    method: "POST",
    path: `/api/v1/platform/payments/${paymentId}/reject`,
    body,
    signal,
  }).then(requirePayment);
}

export function voidManualPayment(
  baseUrl: string,
  paymentId: string,
  body: VoidPaymentBody,
  signal?: AbortSignal,
): Promise<OrganizationPayment> {
  return commercialMutationRequest<unknown>(baseUrl, {
    method: "POST",
    path: `/api/v1/platform/payments/${paymentId}/void`,
    body,
    signal,
  }).then(requirePayment);
}

export function activateSubscriptionFromPayment(
  baseUrl: string,
  paymentId: string,
  body: ActivateSubscriptionFromPaymentBody,
  signal?: AbortSignal,
): Promise<PaymentAndSubscriptionResult> {
  return commercialMutationRequest<unknown>(baseUrl, {
    method: "POST",
    path: `/api/v1/platform/payments/${paymentId}/activate-subscription`,
    body,
    signal,
  }).then((payload) => {
    if (typeof payload !== "object" || payload === null) {
      throw new Error("Invalid payment activation result.");
    }
    const record = payload as Record<string, unknown>;
    return {
      payment: mapOrganizationPayment(record.payment ?? record.Payment),
      subscription: mapOrganizationSubscription(record.subscription ?? record.Subscription),
    };
  });
}

/**
 * Local Validation / Development only. Production API returns 404.
 * Caller must pass localValidationToolsEnabled from existing runtime config (do not infer hostname).
 */
export function simulateLocalValidationPayment(
  baseUrl: string,
  body: SimulateLocalValidationPaymentBody,
  options: { localValidationToolsEnabled: boolean; signal?: AbortSignal },
): Promise<LocalValidationPaymentSimulationResult> {
  if (!options.localValidationToolsEnabled) {
    throw new PlatformApiError(404, {
      status: 404,
      title: "Not Found",
      detail: "Local Validation payment simulation is not available in this environment.",
      errorCode: "application.payment.not_configured",
    });
  }

  return commercialMutationRequest<unknown>(baseUrl, {
    method: "POST",
    path: "/api/v1/platform/local-validation/payments/simulate",
    body,
    signal: options.signal,
  }).then((payload) => {
    if (typeof payload !== "object" || payload === null) {
      throw new Error("Invalid local validation payment simulation result.");
    }
    const record = payload as Record<string, unknown>;
    const status = typeof record.status === "string" ? record.status : record.Status;
    const provider = typeof record.provider === "string" ? record.provider : record.Provider;
    const providerReference =
      typeof record.providerReference === "string"
        ? record.providerReference
        : record.ProviderReference;
    const amount = typeof record.amount === "number" ? record.amount : record.Amount;
    const currencyCode =
      typeof record.currencyCode === "string" ? record.currencyCode : record.CurrencyCode;
    const isTest = typeof record.isTest === "boolean" ? record.isTest : record.IsTest;
    if (
      typeof status !== "string" ||
      typeof provider !== "string" ||
      typeof providerReference !== "string" ||
      typeof amount !== "number" ||
      typeof currencyCode !== "string" ||
      typeof isTest !== "boolean"
    ) {
      throw new Error("Invalid local validation payment simulation result.");
    }
    return {
      status,
      provider,
      providerReference,
      amount,
      currencyCode,
      isTest,
      failureCode:
        typeof record.failureCode === "string"
          ? record.failureCode
          : typeof record.FailureCode === "string"
            ? record.FailureCode
            : undefined,
      failureMessage:
        typeof record.failureMessage === "string"
          ? record.failureMessage
          : typeof record.FailureMessage === "string"
            ? record.FailureMessage
            : undefined,
      idempotencyKey:
        typeof record.idempotencyKey === "string"
          ? record.idempotencyKey
          : typeof record.IdempotencyKey === "string"
            ? record.IdempotencyKey
            : undefined,
    };
  });
}
