import * as React from "react";
import { cva, type VariantProps } from "class-variance-authority";

import { cn } from "@/lib/utils";

const buttonVariants = cva(
  "inline-flex items-center justify-center gap-2 whitespace-nowrap rounded-md text-sm font-semibold transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brandBright disabled:pointer-events-none disabled:opacity-50 ring-offset-base",
  {
    variants: {
      variant: {
        primary:
          "bg-gradient-to-r from-brand to-brandBright text-primary border border-borderDefault hover:brightness-110",
        secondary:
          "bg-surface text-primary border border-borderDefault hover:bg-elevated",
        outline:
          "bg-transparent text-primary border border-borderDefault hover:bg-surface",
        ghost: "bg-transparent text-muted border border-transparent hover:bg-surface",
      },
      size: {
        default: "h-11 px-5",
        sm: "h-9 px-4",
        lg: "h-12 px-7",
        icon: "h-11 w-11 p-0",
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

