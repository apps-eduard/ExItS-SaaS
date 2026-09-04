import { useEffect, useState } from "react";
import { prefersReducedMotion } from "@/lib/motion";

/** Subscribe to prefers-reduced-motion for chart isAnimationActive. */
export function usePrefersReducedMotion(): boolean {
  const [reduced, setReduced] = useState(() => prefersReducedMotion());

  useEffect(() => {
    if (typeof window === "undefined" || typeof window.matchMedia !== "function") {
      return;
    }
    const mq = window.matchMedia("(prefers-reduced-motion: reduce)");
    const onChange = () => setReduced(mq.matches);
    onChange();
    mq.addEventListener("change", onChange);
    return () => mq.removeEventListener("change", onChange);
  }, []);

  return reduced;
}

export function useChartAnimationActive(animationKey: string | number): {
  isAnimationActive: boolean;
  animationDuration: number;
} {
  const reduced = usePrefersReducedMotion();
  // Recharts re-animates when key changes via remount; parent should key charts.
  void animationKey;
  return {
    isAnimationActive: !reduced,
    animationDuration: reduced ? 0 : 700,
  };
}
