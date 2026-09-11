import { Slot } from "@radix-ui/react-slot";
import { cva, type VariantProps } from "class-variance-authority";
import type { ButtonHTMLAttributes } from "react";
import { cn } from "@/lib/cn";

/**
 * Shared ExItS Button — canonical control (APPROVED / LOCKED).
 * See `Docs/UI/exits-button-standard.md` and `/ui-standards` → Buttons.
 * Defaults (standard + flat) preserve historical appearance.
 * Contextual icon motion: `group/button` (on Button) + `buttonIconMotion.*` on Lucide children.
 */
export const buttonVariants = cva(
  [
    "group/button inline-flex items-center justify-center gap-2 text-[length:var(--exits-text-sm)] font-medium",
    "transition-[background-color,color,box-shadow,border-color,transform,filter] duration-[var(--exits-motion-fast)] ease-[var(--exits-ease-standard)]",
    "active:scale-[0.985] motion-reduce:active:scale-100",
    "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--exits-ring)] focus-visible:ring-offset-2 focus-visible:ring-offset-[var(--exits-bg)]",
    "disabled:pointer-events-none disabled:opacity-50 disabled:translate-y-0 disabled:scale-100 disabled:shadow-none",
  ].join(" "),
  {
    variants: {
      variant: {
        default: "bg-primary text-primary-foreground hover:bg-[var(--exits-primary-hover)]",
        secondary:
          "border border-border bg-[var(--exits-surface-muted)] text-foreground hover:border-[var(--exits-border-strong)]",
        ghost: "bg-transparent text-foreground hover:bg-[var(--exits-surface-muted)]",
        outline:
          "border border-border bg-surface text-foreground hover:bg-[var(--exits-surface-muted)] hover:border-[var(--exits-border-strong)]",
        destructive:
          "border border-destructive/35 bg-[var(--exits-danger-soft)] text-destructive hover:border-destructive/50",
        success:
          "border border-border bg-[var(--exits-success-soft)] text-[var(--exits-success)] hover:border-[var(--exits-success)]",
        info:
          "border border-border bg-[color-mix(in_srgb,var(--exits-info)_10%,var(--exits-surface))] text-[var(--exits-info)] hover:border-[var(--exits-info)]",
        warning:
          "border border-border bg-[var(--exits-warning-soft)] text-[var(--exits-warning)] hover:border-[var(--exits-warning)]",
        /** High-risk destructive confirmation only. */
        dangerStrong:
          "bg-[var(--exits-danger)] text-white hover:brightness-95 focus-visible:ring-[var(--exits-danger)]",
      },
      size: {
        /** Follows Preferences density (compact 32 / balance 36 / comfort 44). */
        default:
          "h-[var(--exits-control-height)] min-h-[var(--exits-control-height)] px-[var(--exits-control-padding-x)]",
        /** Square control matching density height. */
        icon: "size-[var(--exits-control-height)] min-h-[var(--exits-control-height)] min-w-[var(--exits-control-height)] p-0",
        /** Exceptional CTA only — not for routine CRUD / toolbar actions. */
        large:
          "h-[var(--exits-control-height-lg)] min-h-[var(--exits-control-height-lg)] px-5 text-[length:var(--exits-text-md)]",
      },
      /** Default matches historical ExItS button radius. */
      shape: {
        standard: "rounded-[var(--exits-radius-md)]",
        soft: "rounded-[var(--exits-radius-soft)]",
        pill: "rounded-full",
        /** Circular icon-only control (pair with size="icon"). */
        round: "rounded-full",
      },
      /** Default flat preserves historical treatment. */
      treatment: {
        flat: "",
        elevated:
          "shadow-[var(--exits-shadow-sm)] hover:-translate-y-px hover:shadow-[var(--exits-shadow-md)] active:translate-y-0 active:scale-[0.99] motion-reduce:hover:translate-y-0 motion-reduce:hover:shadow-[var(--exits-shadow-sm)] motion-reduce:active:scale-100",
        gradient:
          "shadow-[var(--exits-shadow-sm)] hover:-translate-y-px active:translate-y-0 active:scale-[0.99] motion-reduce:hover:translate-y-0 motion-reduce:active:scale-100",
      },
    },
    compoundVariants: [
      {
        variant: "default",
        treatment: "gradient",
        class:
          "bg-gradient-to-b from-[var(--exits-primary)] to-[var(--exits-primary-hover)] hover:from-[var(--exits-primary)] hover:to-[var(--exits-primary-hover)] hover:brightness-[1.02]",
      },
      {
        variant: "dangerStrong",
        treatment: "gradient",
        class:
          "bg-gradient-to-b from-[var(--exits-danger)] to-[color-mix(in_srgb,var(--exits-danger)_78%,#000)] hover:brightness-100",
      },
      {
        variant: "success",
        treatment: "gradient",
        class:
          "bg-gradient-to-b from-[var(--exits-success-soft)] to-[color-mix(in_srgb,var(--exits-success)_18%,var(--exits-success-soft))] hover:brightness-100",
      },
    ],
    defaultVariants: {
      variant: "default",
      size: "default",
      shape: "standard",
      treatment: "flat",
    },
  },
);

/**
 * Contextual icon micro-motion for Lucide children inside Button.
 * Use only when movement communicates intent — not on every icon.
 */
export const buttonIconMotion = {
  continue:
    "transition-transform duration-[var(--exits-motion-fast)] ease-[var(--exits-ease-standard)] group-hover/button:translate-x-0.5 motion-reduce:transition-none motion-reduce:group-hover/button:translate-x-0",
  back: "transition-transform duration-[var(--exits-motion-fast)] ease-[var(--exits-ease-standard)] group-hover/button:-translate-x-0.5 motion-reduce:transition-none motion-reduce:group-hover/button:translate-x-0",
  open: "transition-transform duration-[var(--exits-motion-fast)] ease-[var(--exits-ease-standard)] group-hover/button:translate-x-px group-hover/button:-translate-y-px motion-reduce:transition-none motion-reduce:group-hover/button:translate-x-0 motion-reduce:group-hover/button:translate-y-0",
  add: "transition-transform duration-[var(--exits-motion-fast)] ease-[var(--exits-ease-standard)] group-hover/button:scale-110 motion-reduce:transition-none motion-reduce:group-hover/button:scale-100",
  view: "transition-transform duration-[var(--exits-motion-fast)] ease-[var(--exits-ease-standard)] group-hover/button:scale-105 motion-reduce:transition-none motion-reduce:group-hover/button:scale-100",
  delete:
    "transition-transform duration-[var(--exits-motion-fast)] ease-[var(--exits-ease-standard)] group-hover/button:-translate-y-px group-hover/button:scale-105 motion-reduce:transition-none motion-reduce:group-hover/button:translate-y-0 motion-reduce:group-hover/button:scale-100",
  more: "transition-transform duration-[var(--exits-motion-fast)] ease-[var(--exits-ease-standard)] group-hover/button:scale-105 group-hover/button:opacity-90 motion-reduce:transition-none motion-reduce:group-hover/button:scale-100",
  refresh:
    "transition-transform duration-[var(--exits-motion-fast)] ease-[var(--exits-ease-standard)] group-hover/button:rotate-[20deg] motion-reduce:transition-none motion-reduce:group-hover/button:rotate-0",
  warning:
    "transition-transform duration-[var(--exits-motion-fast)] ease-[var(--exits-ease-standard)] group-hover/button:scale-105 motion-reduce:transition-none motion-reduce:group-hover/button:scale-100",
} as const;

export type ButtonIconMotionKind = keyof typeof buttonIconMotion;

export type ButtonProps = ButtonHTMLAttributes<HTMLButtonElement> &
  VariantProps<typeof buttonVariants> & {
    asChild?: boolean;
  };

export function Button({
  className,
  variant,
  size,
  shape,
  treatment,
  asChild = false,
  ...props
}: ButtonProps) {
  const Comp = asChild ? Slot : "button";
  return (
    <Comp
      className={cn(buttonVariants({ variant, size, shape, treatment }), className)}
      {...props}
    />
  );
}
