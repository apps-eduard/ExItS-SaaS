import { useEffect, useState } from "react";

export function prefersReducedMotion(): boolean {
  if (typeof document !== "undefined" && document.documentElement.dataset.motion === "reduced") {
    return true;
  }
  if (typeof window === "undefined" || typeof window.matchMedia !== "function") {
    return false;
  }
  return window.matchMedia("(prefers-reduced-motion: reduce)").matches;
}

export function motionDurationMs(baseMs: number): number {
  return prefersReducedMotion() ? 0 : baseMs;
}

/**
 * Reactive reduced-motion: ExItS Motion=Reduced (`data-motion`) and OS preference.
 */
export function usePrefersReducedMotion(): boolean {
  const [reduced, setReduced] = useState(() => prefersReducedMotion());

  useEffect(() => {
    const sync = () => setReduced(prefersReducedMotion());
    sync();

    const mq =
      typeof window !== "undefined" && typeof window.matchMedia === "function"
        ? window.matchMedia("(prefers-reduced-motion: reduce)")
        : null;
    mq?.addEventListener("change", sync);

    const root = typeof document !== "undefined" ? document.documentElement : null;
    const observer =
      root && typeof MutationObserver !== "undefined"
        ? new MutationObserver(sync)
        : null;
    observer?.observe(root!, { attributes: true, attributeFilter: ["data-motion"] });

    return () => {
      mq?.removeEventListener("change", sync);
      observer?.disconnect();
    };
  }, []);

  return reduced;
}
