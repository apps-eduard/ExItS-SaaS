import type { SecureMutationIdResult } from "@/lib/secure-mutation-id";
import { createSecureMutationId } from "@/lib/secure-mutation-id";

/**
 * One logical return submit/retry reuses the same ReturnId.
 * A new id is created only when none is pending.
 */
export function resolveReturnMutationId(
  pendingReturnId: string | null,
  createId: () => SecureMutationIdResult = createSecureMutationId,
): SecureMutationIdResult & { reused: boolean } {
  if (pendingReturnId) {
    return { ok: true, id: pendingReturnId, reused: true };
  }
  const created = createId();
  if (!created.ok) {
    return { ...created, reused: false };
  }
  return { ok: true, id: created.id, reused: false };
}
