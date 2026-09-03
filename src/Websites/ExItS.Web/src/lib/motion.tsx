"use client";

import type { ReactNode } from "react";
import { motion, useReducedMotion } from "framer-motion";

/**
 * Minimal motion helper that respects `prefers-reduced-motion`.
 * When reduced motion is preferred, renders a plain div with no animation.
 */
export function MotionDiv({
  children,
  className,
  animate,
  initial,
  transition,
}: {
  children?: ReactNode;
  className?: string;
  animate?: { opacity?: number; y?: number };
  initial?: { opacity?: number; y?: number };
  transition?: { duration?: number; ease?: "easeOut" | "easeIn" | "easeInOut" | "linear" };
}) {
  const reducedMotion = useReducedMotion();

  if (reducedMotion) {
    return <div className={className}>{children}</div>;
  }

  return (
    <motion.div
      className={className}
      initial={initial}
      animate={animate}
      transition={transition}
      style={{ willChange: "transform" }}
    >
      {children}
    </motion.div>
  );
}
