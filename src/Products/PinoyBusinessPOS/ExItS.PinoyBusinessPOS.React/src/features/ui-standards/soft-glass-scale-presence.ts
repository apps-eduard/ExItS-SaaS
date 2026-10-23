import { useEffect, useState } from "react";

/** Soft Glass / dropdown scale open-close duration (matches `0.3s` menu scale). */
export const SOFT_GLASS_SCALE_MS = 300;

export function softGlassPrefersReducedMotion(): boolean {
  return (
    typeof window !== "undefined" &&
    typeof window.matchMedia === "function" &&
    window.matchMedia("(prefers-reduced-motion: reduce)").matches
  );
}

/**
 * Keep a Soft Glass scale surface mounted after `open` becomes false so the
 * scale-out animation can finish (same pattern as Soft Glass dropdown).
 */
export function useScalePresence(open: boolean, durationMs = SOFT_GLASS_SCALE_MS): boolean {
  const [mounted, setMounted] = useState(open);

  useEffect(() => {
    if (open) {
      setMounted(true);
      return;
    }
    if (!mounted) return;
    if (softGlassPrefersReducedMotion()) {
      setMounted(false);
      return;
    }
    const timeout = window.setTimeout(() => setMounted(false), durationMs);
    return () => window.clearTimeout(timeout);
  }, [open, mounted, durationMs]);

  return mounted;
}
