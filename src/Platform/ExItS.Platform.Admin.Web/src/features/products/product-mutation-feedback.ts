import type { CommercialMutationKind } from "@/api/commercial/commercial-errors";
import { classifyCommercialMutationFailure } from "@/api/commercial/commercial-errors";
import type { MessageKey } from "@/lib/i18n/messages";

const TITLE_KEYS: Record<CommercialMutationKind, MessageKey> = {
  validation: "products.mutation.error.validation",
  session_expired: "products.mutation.error.session",
  permission_denied: "products.mutation.error.permission",
  not_found: "products.mutation.error.notFound",
  conflict: "products.mutation.error.conflict",
  domain_rule: "products.mutation.error.domain",
  payment_required: "products.mutation.error.unknown",
  network: "products.mutation.error.network",
  unknown: "products.mutation.error.unknown",
};

export function productMutationFailureCopy(
  error: unknown,
  t: (key: MessageKey) => string,
): { title: string; detail: string } {
  const mapped = classifyCommercialMutationFailure(error);
  return {
    title: t(TITLE_KEYS[mapped.kind]),
    detail: mapped.message,
  };
}
