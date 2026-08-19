import { cva, type VariantProps } from "class-variance-authority";
import type { HTMLAttributes } from "react";
import { cn } from "@/lib/utils";

const badgeVariants = cva(
  "inline-flex items-center rounded-full px-2.5 py-0.5 text-[length:var(--exits-text-xs)] font-semibold",
  {
    variants: {
      tone: {
        success: "bg-[var(--exits-success-bg)] text-success",
        warning: "bg-[var(--exits-warning-bg)] text-warning",
        danger: "bg-[var(--exits-danger-bg)] text-destructive",
        info: "bg-[var(--exits-info-bg)] text-info",
        neutral: "bg-surface-muted text-muted",
      },
    },
    defaultVariants: {
      tone: "neutral",
    },
  },
);

export type BadgeProps = HTMLAttributes<HTMLSpanElement> & VariantProps<typeof badgeVariants>;

export function Badge({ className, tone, ...props }: BadgeProps) {
  return <span className={cn(badgeVariants({ tone }), className)} {...props} />;
}
