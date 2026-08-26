import type { CommercialMutationKind } from "@/api/commercial/commercial-errors";
import { classifyCommercialMutationFailure } from "@/api/commercial/commercial-errors";
import type { MessageKey } from "@/lib/i18n/messages";

const TITLE_KEYS: Record<CommercialMutationKind, MessageKey> = {
  validation: "organization.subscriptions.mutation.error.validation",
  session_expired: "organization.subscriptions.mutation.error.session",
  permission_denied: "organization.subscriptions.mutation.error.permission",
  not_found: "organization.subscriptions.mutation.error.notFound",
  conflict: "organization.subscriptions.mutation.error.conflict",
  domain_rule: "organization.subscriptions.mutation.error.domain",
  payment_required: "organization.subscriptions.mutation.error.payment",
  network: "organization.subscriptions.mutation.error.network",
  unknown: "organization.subscriptions.mutation.error.unknown",
};

export function commercialMutationFailureCopy(
  error: unknown,
  t: (key: MessageKey) => string,
): { title: string; detail: string } {
  const mapped = classifyCommercialMutationFailure(error);
  return {
    title: t(TITLE_KEYS[mapped.kind]),
    detail: mapped.message,
  };
}
