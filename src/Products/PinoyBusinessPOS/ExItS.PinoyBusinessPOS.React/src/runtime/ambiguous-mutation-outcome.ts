import { isLikelyNetworkFailure } from "@/connectivity/network-failure";
import type { MessageKey } from "@/i18n/messages";

/**
 * Shared Organization Web ambiguous money/stock outcome helper.
 * Used when a POST may have committed before the browser lost the response.
 * Never invites an unsafe duplicate retry while status is unknown.
 */

export type AmbiguousMutationOutcome<T> =
  | { kind: "confirmed"; value: T }
  | { kind: "not_found"; lookupError: unknown }
  | { kind: "still_unknown"; lookupError: unknown }
  | { kind: "not_network"; originalError: unknown };

export type AmbiguousMutationMessages = {
  confirmingKey: MessageKey;
  unknownKey: MessageKey;
};

export const DEFAULT_AMBIGUOUS_MUTATION_MESSAGES: AmbiguousMutationMessages = {
  confirmingKey: "checkout.confirmingTransaction",
  unknownKey: "checkout.transactionStatusUnknown",
};

/**
 * After a failed mutation POST: if the failure looks like a transport loss,
 * attempt a status lookup. Otherwise treat as a normal application error.
 */
export async function resolveAmbiguousMutationOutcome<T>(options: {
  error: unknown;
  lookup: () => Promise<T>;
}): Promise<AmbiguousMutationOutcome<T>> {
  if (!isLikelyNetworkFailure(options.error)) {
    return { kind: "not_network", originalError: options.error };
  }

  try {
    const value = await options.lookup();
    return { kind: "confirmed", value };
  } catch (lookupError) {
    if (isLikelyNetworkFailure(lookupError)) {
      return { kind: "still_unknown", lookupError };
    }
    return { kind: "not_found", lookupError };
  }
}

export function isNotFoundStatus(error: unknown): boolean {
  if (!error || typeof error !== "object") {
    return false;
  }
  const status = (error as { status?: number }).status;
  return status === 404;
}
