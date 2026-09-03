"use client";

import type { ReactNode } from "react";
import {
  motion,
  useReducedMotion,
  type HTMLMotionProps,
} from "framer-motion";

export function MotionDiv({
  children,
  className,
  animate,
  initial,
  transition,
  whileInView,
  viewport,
  ...rest
}: HTMLMotionProps<"div"> & {
  children?: ReactNode;
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
      whileInView={whileInView}
      viewport={viewport}
      transition={transition}
      {...rest}
    >
      {children}
    </motion.div>
  );
}

export { motion, useReducedMotion };
