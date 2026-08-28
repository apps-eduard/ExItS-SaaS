import {
  ensureOnboardingProgress,
  getOnboardingProgress,
  type OrganizationOnboardingProgressDto,
} from "@/api/pos/pos-onboarding-client";
import { PosApiError, type PosWorkspaceScope } from "@/api/pos/pos-http";
import { isAbortError } from "@/diagnostics/global-error-reporter";

export const POST_SUBSCRIPTION_ONBOARDING_STORAGE_KEY = "exits.postSubscriptionOnboarding";

export type PendingPostSubscriptionOnboarding = {
  organizationId: string;
  primaryBusinessTypeId?: string | null;
  businessTypeCode?: string | null;
  businessTypeName?: string | null;
  businessTypeDescription?: string | null;
};

export function sameOrganizationId(left: string | null | undefined, right: string | null | undefined): boolean {
  if (!left || !right) {
    return false;
  }
  return left.trim().toLowerCase() === right.trim().toLowerCase();
}

export function readPendingPostSubscriptionOnboarding(): PendingPostSubscriptionOnboarding | null {
  try {
    const raw = sessionStorage.getItem(POST_SUBSCRIPTION_ONBOARDING_STORAGE_KEY);
    if (!raw) {
      return null;
    }
    const pending = JSON.parse(raw) as PendingPostSubscriptionOnboarding;
    if (!pending?.organizationId?.trim()) {
      return null;
    }
    return pending;
  } catch {
    return null;
  }
}

export function writePendingPostSubscriptionOnboarding(
  pending: PendingPostSubscriptionOnboarding,
): void {
  sessionStorage.setItem(POST_SUBSCRIPTION_ONBOARDING_STORAGE_KEY, JSON.stringify(pending));
}

export function clearPendingPostSubscriptionOnboarding(): void {
  sessionStorage.removeItem(POST_SUBSCRIPTION_ONBOARDING_STORAGE_KEY);
}

export function hasPendingPostSubscriptionOnboarding(): boolean {
  return readPendingPostSubscriptionOnboarding() !== null;
}

export function pendingPostSubscriptionForOrganization(
  organizationId: string | null | undefined,
): PendingPostSubscriptionOnboarding | null {
  const pending = readPendingPostSubscriptionOnboarding();
  if (!pending || !sameOrganizationId(pending.organizationId, organizationId)) {
    return null;
  }
  return pending;
}

export function isTransientOnboardingLoadError(error: unknown): boolean {
  if (!(error instanceof PosApiError)) {
    return false;
  }
  if (error.status === 403 || error.status === 409 || error.status === 429 || error.status === 503) {
    return true;
  }
  const code = (error.errorCode ?? "").toLowerCase();
  return (
    code.includes("development_headers") ||
    code.includes("unavailable") ||
    code.includes("concurrency")
  );
}

async function delay(ms: number, signal?: AbortSignal): Promise<void> {
  if (ms <= 0) {
    return;
  }
  await new Promise<void>((resolve, reject) => {
    const timer = window.setTimeout(resolve, ms);
    const onAbort = () => {
      window.clearTimeout(timer);
      const abort = Object.assign(new Error("aborted"), { name: "AbortError" });
      reject(abort);
    };
    if (signal?.aborted) {
      onAbort();
      return;
    }
    signal?.addEventListener("abort", onAbort, { once: true });
  });
}

/**
 * New orgs from Start a Business must ensure a progress row.
 * Casual visits to /onboarding for older orgs stay 404 → no checklist.
 * Right after subscribe, POS grant/provision can 403 briefly — retry those.
 */
export async function loadPostSubscriptionOnboardingProgress(
  workspace: PosWorkspaceScope,
  signal?: AbortSignal,
): Promise<OrganizationOnboardingProgressDto | null> {
  const pending = pendingPostSubscriptionForOrganization(workspace.organizationId);
  const maxAttempts = pending ? 4 : 1;
  let lastError: unknown;

  for (let attempt = 0; attempt < maxAttempts; attempt++) {
    if (signal?.aborted) {
      throw Object.assign(new Error("aborted"), { name: "AbortError" });
    }
    if (attempt > 0) {
      await delay(Math.min(250 * 2 ** (attempt - 1), 2000), signal);
    }

    try {
      return await getOnboardingProgress(workspace, signal);
    } catch (error) {
      if (isAbortError(error)) {
        throw error;
      }
      if (error instanceof PosApiError && error.status === 404) {
        if (!pending) {
          return null;
        }
        try {
          return await ensureOnboardingProgress(
            workspace,
            { primaryBusinessTypeId: pending.primaryBusinessTypeId ?? null },
            signal,
          );
        } catch (ensureError) {
          if (isAbortError(ensureError)) {
            throw ensureError;
          }
          lastError = ensureError;
          if (isTransientOnboardingLoadError(ensureError) && attempt < maxAttempts - 1) {
            continue;
          }
          throw ensureError;
        }
      }

      lastError = error;
      if (isTransientOnboardingLoadError(error) && attempt < maxAttempts - 1) {
        continue;
      }
      throw error;
    }
  }

  throw lastError;
}
