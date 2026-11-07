import { AnimatePresence, motion, useReducedMotion } from "motion/react";
import type { ReactNode } from "react";
import { listItemVariants, motionDuration, tweenPage } from "./presets";

function MotionList({
  className,
  children,
}: {
  className?: string;
  children: ReactNode;
}) {
  return <ul className={className}>{children}</ul>;
}

function MotionListItem({
  id,
  className,
  children,
  index = 0,
}: {
  id: string;
  className?: string;
  children: ReactNode;
  index?: number;
}) {
  const reduced = useReducedMotion();
  if (reduced) {
    return <li className={className}>{children}</li>;
  }
  return (
    <motion.li
      layout="position"
      className={className}
      variants={listItemVariants}
      initial="hidden"
      animate="visible"
      exit="exit"
      transition={{
        ...tweenPage,
        duration: motionDuration.list,
        delay: Math.min(index * 0.03, 0.12),
      }}
      key={id}
    >
      {children}
    </motion.li>
  );
}

function MotionListPresence({ children }: { children: ReactNode }) {
  const reduced = useReducedMotion();
  if (reduced) return children;
  return <AnimatePresence initial={false}>{children}</AnimatePresence>;
}

export { MotionList, MotionListItem, MotionListPresence };
