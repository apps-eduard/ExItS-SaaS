import type { CommercialMutationKind } from "@/api/commercial/commercial-errors";
import { classifyCommercialMutationFailure } from "@/api/commercial/commercial-errors";
import type { MessageKey } from "@/lib/i18n/messages";

const TITLE_KEYS: Record<CommercialMutationKind, MessageKey> = {
  validation: "organization.admin.mutation.error.validation",
  session_expired: "organization.admin.mutation.error.session",
  permission_denied: "organization.admin.mutation.error.permission",
  not_found: "organization.admin.mutation.error.notFound",
  conflict: "organization.admin.mutation.error.conflict",
  domain_rule: "organization.admin.mutation.error.domain",
  payment_required: "organization.admin.mutation.error.payment",
  network: "organization.admin.mutation.error.network",
  unknown: "organization.admin.mutation.error.unknown",
};

export function organizationMutationFailureCopy(
  error: unknown,
  t: (key: MessageKey) => string,
): { title: string; detail: string } {
  const mapped = classifyCommercialMutationFailure(error);
  return {
    title: t(TITLE_KEYS[mapped.kind]),
    detail: mapped.message,
  };
}
