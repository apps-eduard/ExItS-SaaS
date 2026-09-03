import * as React from "react";
import { cva, type VariantProps } from "class-variance-authority";

import { cn } from "@/lib/utils";

const badgeVariants = cva(
  "inline-flex items-center rounded-pill border px-2.5 py-0.5 text-xs font-semibold transition-colors",
  {
    variants: {
      variant: {
        default: "border-borderDefault text-muted bg-transparent",
        available: "border-emerald/50 text-emerald bg-emerald/15 shadow-[0_0_12px_rgba(16,185,129,0.25)]",
        comingSoon: "border-magenta/45 text-magenta bg-magenta/15 shadow-[0_0_12px_rgba(232,121,249,0.2)]",
        inDevelopment: "border-brand/45 text-brandBright bg-brand/15 shadow-[0_0_12px_rgba(139,92,246,0.2)]",
      },
    },
    defaultVariants: {
      variant: "default",
    },
  },
);

export type BadgeProps = React.HTMLAttributes<HTMLSpanElement> &
  VariantProps<typeof badgeVariants>;

export function Badge({ className, variant, ...props }: BadgeProps) {
  return (
    <span className={cn(badgeVariants({ variant }), className)} {...props} />
  );
}
