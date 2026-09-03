"use client";

import { motion, useReducedMotion, type MotionProps } from "framer-motion";

/**
 * Minimal motion helper that respects `prefers-reduced-motion`.
 * Later components should prefer these wrappers over raw `motion.*` usage.
 */
export function MotionDiv({
  animate,
  initial,
  ...props
}: MotionProps & { animate?: MotionProps["animate"]; initial?: MotionProps["initial"] }) {
  const reducedMotion = useReducedMotion();

  return (
    <motion.div
      initial={reducedMotion ? undefined : initial}
      animate={reducedMotion ? undefined : animate}
      {...props}
    />
  );
}

export { motion };

