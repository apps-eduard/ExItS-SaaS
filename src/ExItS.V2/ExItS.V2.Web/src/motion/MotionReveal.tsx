import { motion, useReducedMotion } from "motion/react";
import type { ReactNode } from "react";
import {
  fadeUpVariants,
  motionDuration,
  tweenPage,
} from "./presets";

type MotionRevealProps = {
  children: ReactNode;
  className?: string;
  /** Stagger index for section reveal (0-based). */
  index?: number;
  /** When false, render static (e.g. after initial reveal / soft refresh). */
  animate?: boolean;
};

/**
 * One-shot section reveal. Disabled after first paint when `animate` is false
 * so soft Home refreshes do not replay the whole dashboard.
 */
function MotionReveal({
  children,
  className,
  index = 0,
  animate = true,
}: MotionRevealProps) {
  const reduced = useReducedMotion();
  if (!animate || reduced) {
    return <div className={className}>{children}</div>;
  }

  return (
    <motion.div
      className={className}
      variants={fadeUpVariants}
      initial="hidden"
      animate="visible"
      transition={{
        ...tweenPage,
        delay: Math.min(index * 0.045, 0.22),
        duration: motionDuration.card,
      }}
    >
      {children}
    </motion.div>
  );
}

export { MotionReveal };
