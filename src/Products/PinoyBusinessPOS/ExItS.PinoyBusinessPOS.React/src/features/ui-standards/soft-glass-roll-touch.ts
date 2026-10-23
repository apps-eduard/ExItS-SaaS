import type { TouchEvent as ReactTouchEvent } from "react";

/** Minimum finger travel (px) before a vertical swipe changes the Soft Glass roll. */
export const SOFT_GLASS_ROLL_SWIPE_PX = 28;

/**
 * Touch swipe for Soft Glass roll columns (mobile has no wheel).
 * Swipe up → next; swipe down → previous (same direction as wheel).
 */
export function softGlassRollTouchHandlers(
  onPrevious: () => void,
  onNext: () => void,
  disabled: boolean,
  startYRef: { current: number | null },
): {
  onTouchStart: (event: ReactTouchEvent) => void;
  onTouchEnd: (event: ReactTouchEvent) => void;
} {
  return {
    onTouchStart(event) {
      if (disabled) return;
      startYRef.current = event.touches[0]?.clientY ?? null;
    },
    onTouchEnd(event) {
      if (disabled || startYRef.current == null) return;
      const y = event.changedTouches[0]?.clientY;
      const start = startYRef.current;
      startYRef.current = null;
      if (y == null) return;
      const dy = y - start;
      if (Math.abs(dy) < SOFT_GLASS_ROLL_SWIPE_PX) return;
      if (dy > 0) onPrevious();
      else onNext();
    },
  };
}
