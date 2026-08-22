import type { CommercialMutationKind } from "@/api/commercial/commercial-errors";
import { classifyCommercialMutationFailure } from "@/api/commercial/commercial-errors";
import type { MessageKey } from "@/lib/i18n/messages";

const TITLE_KEYS: Record<CommercialMutationKind, MessageKey> = {
  validation: "plans.mutation.error.validation",
  session_expired: "plans.mutation.error.session",
  permission_denied: "plans.mutation.error.permission",
  not_found: "plans.mutation.error.notFound",
  conflict: "plans.mutation.error.conflict",
  domain_rule: "plans.mutation.error.domain",
  payment_required: "plans.mutation.error.unknown",
  network: "plans.mutation.error.network",
  unknown: "plans.mutation.error.unknown",
};

export function planMutationFailureCopy(
  error: unknown,
  t: (key: MessageKey) => string,
): { title: string; detail: string } {
  const mapped = classifyCommercialMutationFailure(error);
  return {
    title: t(TITLE_KEYS[mapped.kind]),
    detail: mapped.message,
  };
}
