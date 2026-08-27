import { useCallback, useEffect, useState } from "react";

import { PlatformApiError } from "@/api/platform/platform-http";

export const CONSENT_ACTION_COOLDOWN_MS = 2000;
export const CONSENT_ACTION_RATE_LIMIT_COOLDOWN_MS = 8000;

export function isConsentActionRateLimited(error: unknown): boolean {
  return error instanceof PlatformApiError && error.status === 429;
}

export function useConsentActionGuard(busy: boolean, cooldownMs = CONSENT_ACTION_COOLDOWN_MS) {
  const [cooldownUntil, setCooldownUntil] = useState(0);
  const [, refreshCooldown] = useState(0);

  useEffect(() => {
    const remainingMs = cooldownUntil - Date.now();
    if (remainingMs <= 0) {
      return;
    }

    const timerId = window.setTimeout(() => refreshCooldown((value) => value + 1), remainingMs);
    return () => window.clearTimeout(timerId);
  }, [cooldownUntil]);

  const cooledDown = Date.now() >= cooldownUntil;
  const actionsDisabled = busy || !cooledDown;

  const armCooldown = useCallback(
    (durationMs = cooldownMs) => {
      setCooldownUntil(Date.now() + durationMs);
    },
    [cooldownMs],
  );

  const noteActionError = useCallback(
    (error: unknown) => {
      if (isConsentActionRateLimited(error)) {
        armCooldown(CONSENT_ACTION_RATE_LIMIT_COOLDOWN_MS);
      }
    },
    [armCooldown],
  );

  const noteActionSuccess = useCallback(() => {
    armCooldown();
  }, [armCooldown]);

  return {
    actionsDisabled,
    cooledDown,
    noteActionError,
    noteActionSuccess,
  };
}
