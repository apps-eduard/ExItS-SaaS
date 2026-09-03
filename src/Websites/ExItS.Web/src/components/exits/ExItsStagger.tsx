"use client";

import type { ReactNode } from "react";
import { Children } from "react";
import { useReducedMotion } from "framer-motion";

import { MotionDiv } from "@/lib/motion";

export function ExItsStagger({
  children,
  className,
  stagger = 0.08,
  y = 18,
}: {
  children: ReactNode;
  className?: string;
  stagger?: number;
  y?: number;
}) {
  const reducedMotion = useReducedMotion();
  const items = Children.toArray(children);

  if (reducedMotion) {
    return <div className={className}>{children}</div>;
  }

  return (
    <div className={className}>
      {items.map((child, index) => (
        <MotionDiv
          key={index}
          initial={{ opacity: 0, y }}
          whileInView={{ opacity: 1, y: 0 }}
          viewport={{ once: true, amount: 0.15 }}
          transition={{ duration: 0.5, ease: "easeOut", delay: index * stagger }}
        >
          {child}
        </MotionDiv>
      ))}
    </div>
  );
}
