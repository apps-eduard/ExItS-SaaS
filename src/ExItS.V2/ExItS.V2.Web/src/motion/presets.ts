import type { Transition, Variants } from "motion/react";

/** Shared ExItS V2 motion language — keep values restrained and reusable. */
export const motionDuration = {
  press: 0.12,
  hover: 0.15,
  tabs: 0.22,
  card: 0.24,
  dialog: 0.26,
  sheet: 0.32,
  page: 0.24,
  list: 0.2,
} as const;

export const motionEase = [0.22, 1, 0.36, 1] as const;

export const springSnappy: Transition = {
  type: "spring",
  stiffness: 520,
  damping: 38,
  mass: 0.8,
};

export const springSoft: Transition = {
  type: "spring",
  stiffness: 380,
  damping: 34,
  mass: 0.9,
};

export const springSheet: Transition = {
  type: "spring",
  stiffness: 420,
  damping: 36,
  mass: 0.95,
};

export const tweenFast: Transition = {
  type: "tween",
  duration: motionDuration.hover,
  ease: motionEase,
};

export const tweenDialog: Transition = {
  type: "tween",
  duration: motionDuration.dialog,
  ease: motionEase,
};

export const tweenPage: Transition = {
  type: "tween",
  duration: motionDuration.page,
  ease: motionEase,
};

export const fadeUpVariants: Variants = {
  hidden: { opacity: 0, y: 5 },
  visible: { opacity: 1, y: 0 },
};

export const fadeVariants: Variants = {
  hidden: { opacity: 0 },
  visible: { opacity: 1 },
};

export const scaleInVariants: Variants = {
  hidden: { opacity: 0, scale: 0.97 },
  visible: { opacity: 1, scale: 1 },
};

export const sheetVariants: Variants = {
  hidden: { opacity: 0, y: "100%" },
  visible: { opacity: 1, y: 0 },
};

export const menuVariants: Variants = {
  hidden: { opacity: 0, y: -4, scale: 0.98 },
  visible: { opacity: 1, y: 0, scale: 1 },
};

export const listItemVariants: Variants = {
  hidden: { opacity: 0, y: 6 },
  visible: { opacity: 1, y: 0 },
  exit: { opacity: 0, y: -4 },
};

export const buttonTap = { scale: 0.97 };
export const buttonHover = { scale: 1.01 };
