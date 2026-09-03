import * as React from "react";
import { cva, type VariantProps } from "class-variance-authority";

import { cn } from "@/lib/utils";

const buttonVariants = cva(
  "inline-flex items-center justify-center gap-2 whitespace-nowrap rounded-pill text-sm font-semibold transition-all duration-300 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brandBright disabled:pointer-events-none disabled:opacity-50 ring-offset-base active:scale-[0.98]",
  {
    variants: {
      variant: {
        primary:
          "exits-cta-gradient text-white border border-white/10 shadow-cta hover:-translate-y-0.5 hover:scale-[1.02] hover:brightness-110",
        secondary:
          "bg-elevated/70 text-primary border border-borderDefault hover:-translate-y-0.5 hover:border-borderActive hover:bg-raised/80",
        outline:
          "bg-transparent text-primary border border-borderDefault hover:bg-surface",
        ghost: "bg-transparent text-muted border border-transparent hover:bg-surface",
      },
      size: {
        default: "h-12 px-6",
        sm: "h-9 px-4",
        lg: "h-14 px-8",
        icon: "h-12 w-12 p-0",
      },
    },
    defaultVariants: {
      variant: "primary",
      size: "default",
    },
  },
);

export type ButtonProps = React.ButtonHTMLAttributes<HTMLButtonElement> &
  VariantProps<typeof buttonVariants>;

export function Button({ className, variant, size, ...props }: ButtonProps) {
  return (
    <button
      className={cn(buttonVariants({ variant, size }), className)}
      {...props}
    />
  );
}
