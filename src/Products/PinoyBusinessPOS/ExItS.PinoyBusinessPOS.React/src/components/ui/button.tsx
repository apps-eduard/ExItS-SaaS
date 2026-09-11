import { Slot } from "@radix-ui/react-slot";
import { cva, type VariantProps } from "class-variance-authority";
import type { ButtonHTMLAttributes } from "react";
import { cn } from "@/lib/cn";

/**
 * Shared Button — shape/treatment are opt-in pilots.
 * Defaults (standard + flat) preserve existing visual appearance.
 */
export const buttonVariants = cva(
  [
    "inline-flex items-center justify-center gap-2 text-[length:var(--exits-text-sm)] font-medium",
    "transition-[background-color,color,box-shadow,border-color,transform,filter] duration-[var(--exits-motion-fast)] ease-[var(--exits-ease-standard)]",
    "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--exits-ring)] focus-visible:ring-offset-2 focus-visible:ring-offset-[var(--exits-bg)]",
    "disabled:pointer-events-none disabled:opacity-50 disabled:translate-y-0 disabled:shadow-none",
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
        /** Pilot semantic — not locked as product standard yet. */
        success:
          "border border-border bg-[var(--exits-success-soft)] text-[var(--exits-success)] hover:border-[var(--exits-success)]",
        /** Pilot semantic — not locked as product standard yet. */
        info:
          "border border-border bg-[color-mix(in_srgb,var(--exits-info)_10%,var(--exits-surface))] text-[var(--exits-info)] hover:border-[var(--exits-info)]",
        /** Pilot semantic — not locked as product standard yet. */
        warning:
          "border border-border bg-[var(--exits-warning-soft)] text-[var(--exits-warning)] hover:border-[var(--exits-warning)]",
        /** Pilot strong danger — confirmation-only; not locked yet. */
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
      /** Pilot — default matches historical ExItS button radius. */
      shape: {
        standard: "rounded-[var(--exits-radius-md)]",
        soft: "rounded-[var(--exits-radius-soft)]",
        pill: "rounded-full",
      },
      /** Pilot — default flat preserves historical treatment. */
      treatment: {
        flat: "",
        elevated:
          "shadow-[var(--exits-shadow-sm)] hover:-translate-y-px hover:shadow-[var(--exits-shadow-md)] active:translate-y-0 active:scale-[0.99]",
        gradient:
          "shadow-[var(--exits-shadow-sm)] hover:-translate-y-px active:translate-y-0 active:scale-[0.99]",
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
