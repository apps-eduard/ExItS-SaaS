import type { Transition, Variants } from "motion/react";
import {
  DIALOG_DESKTOP_EXIT_TRANSITION,
  DIALOG_DESKTOP_TRANSITION,
  DIALOG_DESKTOP_VARIANTS,
  DIALOG_REDUCED_TRANSITION,
  DIALOG_REDUCED_VARIANTS,
  DIALOG_SHEET_EXIT_TRANSITION,
  DIALOG_SHEET_TRANSITION,
  DIALOG_SHEET_VARIANTS,
  MOTION_EASE,
} from "../../motion/motion";

export const DIALOG_TRANSITIONS = ["spring", "fade", "scale", "slide"] as const;

export type DialogTransition = (typeof DIALOG_TRANSITIONS)[number];

/** Locked default — Inspira spring (desktop 3D / sheet bounce). */
export const DIALOG_TRANSITION_DEFAULT: DialogTransition = "spring";

export function isDialogTransition(value: string): value is DialogTransition {
  return (DIALOG_TRANSITIONS as readonly string[]).includes(value);
}

const FADE_VARIANTS: Variants = {
  initial: { opacity: 0, scale: 1, rotateX: 0, y: 0 },
  open: { opacity: 1, scale: 1, rotateX: 0, y: 0 },
  exit: { opacity: 0, scale: 1, rotateX: 0, y: 0 },
};

const SCALE_DESKTOP_VARIANTS: Variants = {
  initial: { opacity: 0, scale: 0.92, rotateX: 0, y: 0 },
  open: { opacity: 1, scale: 1, rotateX: 0, y: 0 },
  exit: { opacity: 0, scale: 0.96, rotateX: 0, y: 0 },
};

const SCALE_SHEET_VARIANTS: Variants = {
  initial: { opacity: 1, scale: 0.98, rotateX: 0, y: "100%" },
  open: { opacity: 1, scale: 1, rotateX: 0, y: 0 },
  exit: { opacity: 1, scale: 0.98, rotateX: 0, y: "100%" },
};

const SLIDE_DESKTOP_VARIANTS: Variants = {
  initial: { opacity: 0, scale: 1, rotateX: 0, y: 28 },
  open: { opacity: 1, scale: 1, rotateX: 0, y: 0 },
  exit: { opacity: 0, scale: 1, rotateX: 0, y: 16 },
};

const EASE_IN_OUT: Transition = {
  duration: 0.22,
  ease: MOTION_EASE,
};

const EASE_OUT: Transition = {
  duration: 0.2,
  ease: "easeOut",
};

const EASE_IN: Transition = {
  duration: 0.16,
  ease: "easeIn",
};

const SCALE_SPRING: Transition = {
  type: "spring",
  stiffness: 380,
  damping: 28,
  mass: 0.85,
};

const SLIDE_SPRING: Transition = {
  type: "spring",
  stiffness: 360,
  damping: 30,
  mass: 0.9,
};

export function resolveDialogMotion(options: {
  transition: DialogTransition;
  sheet: boolean;
  reduced: boolean;
  closing: boolean;
}): { variants: Variants; transition: Transition } {
  if (options.reduced) {
    return { variants: DIALOG_REDUCED_VARIANTS, transition: DIALOG_REDUCED_TRANSITION };
  }

  const { transition, sheet, closing } = options;

  if (transition === "spring") {
    return {
      variants: sheet ? DIALOG_SHEET_VARIANTS : DIALOG_DESKTOP_VARIANTS,
      transition: closing
        ? sheet
          ? DIALOG_SHEET_EXIT_TRANSITION
          : DIALOG_DESKTOP_EXIT_TRANSITION
        : sheet
          ? DIALOG_SHEET_TRANSITION
          : DIALOG_DESKTOP_TRANSITION,
    };
  }

  if (transition === "fade") {
    return {
      variants: FADE_VARIANTS,
      transition: closing ? EASE_IN : EASE_OUT,
    };
  }

  if (transition === "scale") {
    return {
      variants: sheet ? SCALE_SHEET_VARIANTS : SCALE_DESKTOP_VARIANTS,
      transition: closing
        ? { opacity: EASE_IN, scale: SCALE_SPRING, y: SCALE_SPRING }
        : { opacity: EASE_OUT, scale: SCALE_SPRING, y: SCALE_SPRING },
    };
  }

  // slide
  return {
    variants: sheet ? DIALOG_SHEET_VARIANTS : SLIDE_DESKTOP_VARIANTS,
    transition: closing
      ? sheet
        ? { opacity: EASE_IN, y: SLIDE_SPRING }
        : { opacity: EASE_IN, y: EASE_IN_OUT }
      : sheet
        ? { opacity: EASE_OUT, y: SLIDE_SPRING }
        : { opacity: EASE_OUT, y: SLIDE_SPRING },
  };
}
