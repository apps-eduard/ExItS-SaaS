import { Slot } from "@radix-ui/react-slot";
import { cva, type VariantProps } from "class-variance-authority";
import type { ButtonHTMLAttributes } from "react";
import { cn } from "@/lib/cn";

const buttonVariants = cva(
  "inline-flex items-center justify-center gap-2 rounded-[var(--exits-radius-md)] text-[length:var(--exits-text-sm)] font-medium transition-[background-color,color,box-shadow,border-color] duration-[var(--exits-motion-fast)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--exits-ring)] focus-visible:ring-offset-2 focus-visible:ring-offset-[var(--exits-bg)] disabled:pointer-events-none disabled:opacity-50",
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
    },
    defaultVariants: {
      variant: "default",
      size: "default",
    },
  },
);

export type ButtonProps = ButtonHTMLAttributes<HTMLButtonElement> &
  VariantProps<typeof buttonVariants> & {
    asChild?: boolean;
  };

export function Button({ className, variant, size, asChild = false, ...props }: ButtonProps) {
  const Comp = asChild ? Slot : "button";
  return <Comp className={cn(buttonVariants({ variant, size }), className)} {...props} />;
}
