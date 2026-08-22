import type { GlobalCatalogMutationFailure } from "@/api/global-catalog/global-catalog-errors";
import type { MessageKey } from "@/lib/i18n/messages";

const MESSAGE_KEYS: Partial<Record<GlobalCatalogMutationFailure["kind"], MessageKey>> = {
  conflict: "globalCatalog.mutation.error.conflict",
  permission_denied: "globalCatalog.mutation.error.permissionDenied",
  session_expired: "globalCatalog.mutation.error.sessionExpired",
  not_found: "globalCatalog.mutation.error.notFound",
  validation: "globalCatalog.mutation.error.validation",
  domain_rule: "globalCatalog.mutation.error.domainRule",
  network: "globalCatalog.mutation.error.network",
  unknown: "globalCatalog.mutation.error.unknown",
};

export function globalCatalogMutationMessageKey(
  failure: GlobalCatalogMutationFailure,
): MessageKey {
  return MESSAGE_KEYS[failure.kind] ?? "globalCatalog.mutation.error.unknown";
}

export function globalCatalogMutationDetail(
  failure: GlobalCatalogMutationFailure,
): string | undefined {
  if (failure.kind === "conflict" || failure.kind === "validation" || failure.kind === "domain_rule") {
    return failure.message;
  }
  return undefined;
}
