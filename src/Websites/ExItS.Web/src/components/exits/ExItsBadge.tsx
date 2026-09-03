import * as React from "react";

import { Badge as UiBadge, type BadgeProps } from "@/components/ui/badge";

export type ExItsBadgeVariant = "available" | "coming-soon" | "in-development";

export type ExItsBadgeProps = Omit<BadgeProps, "variant"> & {
  variant?: ExItsBadgeVariant;
};

const variantMap: Record<ExItsBadgeVariant, BadgeProps["variant"]> = {
  available: "available",
  "coming-soon": "comingSoon",
  "in-development": "inDevelopment",
};

export function ExItsBadge({
  variant = "available",
  className,
  ...props
}: ExItsBadgeProps) {
  return (
    <UiBadge
      variant={variantMap[variant]}
      className={className}
      {...props}
    />
  );
}

