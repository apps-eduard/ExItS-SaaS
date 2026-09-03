"use client";

import type { ReactNode } from "react";
import { useReducedMotion } from "framer-motion";

import { MotionDiv } from "@/lib/motion";

type RevealProps = {
  children: ReactNode;
  className?: string;
  /** Mount animation (hero/above-fold). Avoids opacity:0 for LCP safety. */
  mode?: "mount" | "scroll";
  delay?: number;
  y?: number;
};

/**
 * Site-wide reveal primitive.
 * - mount: slight translate only (LCP-safe)
 * - scroll: fade + translate when entering viewport
 */
export function ExItsReveal({
  children,
  className,
  mode = "scroll",
  delay = 0,
  y = 22,
}: RevealProps) {
  const reducedMotion = useReducedMotion();

  if (reducedMotion) {
    return <div className={className}>{children}</div>;
  }

  if (mode === "mount") {
    return (
      <MotionDiv
        className={className}
        initial={{ y: Math.min(y, 12) }}
        animate={{ y: 0 }}
        transition={{ duration: 0.45, ease: "easeOut", delay }}
      >
        {children}
      </MotionDiv>
    );
  }

  return (
    <MotionDiv
      className={className}
      initial={{ opacity: 0, y }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true, amount: 0.2 }}
      transition={{ duration: 0.55, ease: "easeOut", delay }}
    >
      {children}
    </MotionDiv>
  );
}
