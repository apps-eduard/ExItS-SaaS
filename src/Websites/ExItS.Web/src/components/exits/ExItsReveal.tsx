"use client";

import type { ReactNode } from "react";

import { MotionDiv } from "@/lib/motion";

export function ExItsReveal({ children }: { children: ReactNode }) {
  return (
    <MotionDiv
      initial={{ opacity: 0, y: 10 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.35, ease: "easeOut" }}
    >
      {children}
    </MotionDiv>
  );
}
