import { prefersReducedMotion, usePrefersReducedMotion } from "@/lib/motion";

export { prefersReducedMotion, usePrefersReducedMotion };

/** Subscribe to prefers-reduced-motion for chart isAnimationActive. */
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
