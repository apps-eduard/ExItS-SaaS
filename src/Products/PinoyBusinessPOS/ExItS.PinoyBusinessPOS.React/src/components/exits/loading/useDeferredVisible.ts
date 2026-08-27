import { useEffect, useRef, useState } from "react";

/**
 * UX-only deferred visibility. Never delays success/error propagation —
 * callers must keep authoritative async state separate from what is painted.
 */
export function useDeferredVisible(
  active: boolean,
  options?: { delayMs?: number; minVisibleMs?: number },
): boolean {
  const delayMs = options?.delayMs ?? 150;
  const minVisibleMs = options?.minVisibleMs ?? 180;
  const [visible, setVisible] = useState(false);
  const shownAtRef = useRef<number | null>(null);

  useEffect(() => {
    let showTimer: number | undefined;
    let hideTimer: number | undefined;

    if (active) {
      if (visible) {
        return;
      }
      showTimer = window.setTimeout(() => {
        shownAtRef.current = Date.now();
        setVisible(true);
      }, delayMs);
    } else if (visible) {
      const shownAt = shownAtRef.current;
      const elapsed = shownAt == null ? minVisibleMs : Date.now() - shownAt;
      const remaining = Math.max(0, minVisibleMs - elapsed);
      hideTimer = window.setTimeout(() => {
        shownAtRef.current = null;
        setVisible(false);
      }, remaining);
    }

    return () => {
      if (showTimer != null) window.clearTimeout(showTimer);
      if (hideTimer != null) window.clearTimeout(hideTimer);
    };
  }, [active, delayMs, minVisibleMs, visible]);

  return visible;
}
