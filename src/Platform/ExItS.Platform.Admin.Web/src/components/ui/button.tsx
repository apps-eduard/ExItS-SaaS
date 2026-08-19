import { Slot } from "@radix-ui/react-slot";
import { cva, type VariantProps } from "class-variance-authority";
import type { ButtonHTMLAttributes } from "react";
import { cn } from "@/lib/utils";

const buttonVariants = cva(
  "inline-flex items-center justify-center gap-2 rounded-[var(--exits-density-radius)] text-[length:var(--exits-text-sm)] font-semibold transition-[background-color,border-color,color] duration-[var(--exits-motion-fast)] ease-[var(--exits-ease)] focus-visible:outline-none disabled:pointer-events-none disabled:bg-[var(--exits-disabled-bg)] disabled:text-[var(--exits-disabled-text)] disabled:border-[var(--exits-disabled-border)]",
  {
    variants: {
      variant: {
        default: "bg-primary text-primary-foreground hover:bg-[var(--exits-primary-hover)]",
        secondary: "bg-secondary text-secondary-foreground hover:bg-[var(--exits-secondary-hover)]",
        outline: "border border-border bg-surface text-foreground hover:bg-surface-muted",
        destructive: "bg-destructive text-white hover:opacity-90",
        ghost: "bg-transparent text-foreground hover:bg-surface-muted",
      },
      size: {
        default:
          "h-[var(--exits-control-height)] min-h-[var(--exits-touch-target-min)] px-3.5 text-[length:var(--exits-text-sm)]",
        sm: "h-8 min-h-11 px-2.5 text-[length:var(--exits-text-sm)] lg:min-h-8",
        icon: "size-8 min-h-11 px-0 lg:min-h-8",
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
