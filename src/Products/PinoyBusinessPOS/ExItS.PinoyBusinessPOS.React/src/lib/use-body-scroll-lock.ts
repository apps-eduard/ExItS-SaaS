import { useEffect } from "react";

let lockCount = 0;
let previousBodyOverflow = "";
let previousRootOverflow = "";

/**
 * Nested-safe body scroll lock. Multiple open overlays share one lock and only
 * restore overflow when the last locker releases — avoids stuck overflow:hidden
 * after stacked drawer/dialog dismissals.
 */
export function useBodyScrollLock(locked: boolean) {
  useEffect(() => {
    if (!locked || typeof document === "undefined") {
      return;
    }

    const { body, documentElement: root } = document;
    if (lockCount === 0) {
      previousBodyOverflow = body.style.overflow;
      previousRootOverflow = root.style.overflow;
      body.style.overflow = "hidden";
      root.style.overflow = "hidden";
    }
    lockCount += 1;

    return () => {
      lockCount = Math.max(0, lockCount - 1);
      if (lockCount === 0) {
        body.style.overflow = previousBodyOverflow;
        root.style.overflow = previousRootOverflow;
      }
    };
  }, [locked]);
}

/** Test helper — reset lock bookkeeping between unit tests. */
export function resetBodyScrollLockForTests() {
  lockCount = 0;
  previousBodyOverflow = "";
  previousRootOverflow = "";
  if (typeof document !== "undefined") {
    document.body.style.overflow = "";
    document.documentElement.style.overflow = "";
  }
}
