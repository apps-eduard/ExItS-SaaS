"use client";

import type { ReactNode } from "react";

import { MotionDiv } from "@/lib/motion";

/**
 * Subtle entrance motion that must not delay LCP.
 * Avoids initial opacity:0 so text remains paint-eligible immediately.
 */
export function ExItsReveal({ children }: { children: ReactNode }) {
  return (
    <MotionDiv
      initial={{ y: 8 }}
      animate={{ y: 0 }}
      transition={{ duration: 0.35, ease: "easeOut" }}
    >
      {children}
    </MotionDiv>
  );
}
