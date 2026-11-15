import type { Transition, Variants } from "motion/react";

export const MOTION_SPRING = { type: "spring" as const, stiffness: 420, damping: 32 };

export const MOTION_EASE = [0.2, 0.8, 0.2, 1] as const;

/**
 * Inspira UI Animated Modal desktop spring (stiffness 260 / damping 15).
 * Used for scale, rotateX, and y on the panel inside a perspective stage.
 */
export const DIALOG_DESKTOP_SPRING = {
  type: "spring" as const,
  stiffness: 260,
  damping: 15,
};

/**
 * Mobile sheet: gentle Inspira-like bounce (subtle overshoot, then settle).
 */
export const DIALOG_SHEET_SPRING = {
  type: "spring" as const,
  stiffness: 270,
  damping: 15,
  mass: 0.92,
  restDelta: 0.5,
  restSpeed: 0.5,
};

/** Inspira: opacity eases; transform springs. */
export const DIALOG_DESKTOP_TRANSITION: Transition = {
  opacity: { duration: 0.2, ease: "easeOut" },
  scale: DIALOG_DESKTOP_SPRING,
  rotateX: DIALOG_DESKTOP_SPRING,
  y: DIALOG_DESKTOP_SPRING,
};

export const DIALOG_DESKTOP_EXIT_TRANSITION: Transition = {
  opacity: { duration: 0.18, ease: "easeIn" },
  scale: DIALOG_DESKTOP_SPRING,
  rotateX: DIALOG_DESKTOP_SPRING,
  y: DIALOG_DESKTOP_SPRING,
};

export const DIALOG_SHEET_TRANSITION: Transition = {
  opacity: { duration: 0.16, ease: "easeOut" },
  y: DIALOG_SHEET_SPRING,
};

export const DIALOG_SHEET_EXIT_TRANSITION: Transition = {
  opacity: { duration: 0.14, ease: "easeIn" },
  // Exit stays snappy — bounce on the way out feels like a stuck overlay.
  y: { type: "spring", stiffness: 420, damping: 36, mass: 0.85, restDelta: 0.2, restSpeed: 0.2 },
};

/** Exact Inspira Animated Modal panel variants (perspective lives on the stage). */
export const DIALOG_DESKTOP_VARIANTS: Variants = {
  initial: {
    opacity: 0,
    scale: 0.5,
    rotateX: 80,
    y: 40,
  },
  open: {
    opacity: 1,
    scale: 1,
    rotateX: 0,
    y: 0,
  },
  exit: {
    opacity: 0,
    scale: 0.8,
    rotateX: 10,
  },
};

/**
 * Bottom sheet: slide in percent of the panel height.
 * Explicit scale/rotateX keep desktop 3D leftovers from sticking on mobile.
 */
export const DIALOG_SHEET_VARIANTS: Variants = {
  initial: {
    opacity: 1,
    scale: 1,
    rotateX: 0,
    y: "100%",
  },
  open: {
    opacity: 1,
    scale: 1,
    rotateX: 0,
    // Numeric 0 — percent "0%" can fail to leave a percent initial in some Motion paths.
    y: 0,
  },
  exit: {
    opacity: 1,
    scale: 1,
    rotateX: 0,
    y: "100%",
  },
};

export const DIALOG_REDUCED_VARIANTS: Variants = {
  initial: { opacity: 0, y: 0, scale: 1, rotateX: 0 },
  open: { opacity: 1, y: 0, scale: 1, rotateX: 0 },
  exit: { opacity: 0, y: 0, scale: 1, rotateX: 0 },
};

export const DIALOG_REDUCED_TRANSITION: Transition = {
  duration: 0.01,
  ease: "linear",
};
