import * as TooltipPrimitive from "@radix-ui/react-tooltip";
import type { ReactNode } from "react";
import { cn } from "@/lib/utils";

const tooltipContentBase = cn(
  "z-[var(--exits-z-dropdown)] origin-[var(--radix-tooltip-content-transform-origin)]",
  "rounded-lg border shadow-lg outline-none",
  "animate-[exits-tooltip-in_var(--exits-motion-base)_var(--exits-ease-out)]",
  "data-[state=closed]:animate-[exits-tooltip-out_var(--exits-motion-fast)_var(--exits-ease-in)_forwards]",
);

const tooltipVariants = {
  default: cn(
    tooltipContentBase,
    "max-w-xs border-border bg-surface-elevated px-2.5 py-1.5 text-[length:var(--exits-text-xs)] text-foreground shadow-md",
  ),
  nav: cn(
    tooltipContentBase,
    "min-w-[9rem] max-w-[14rem] border-border/80 bg-surface-elevated px-3 py-2 shadow-[var(--exits-shadow-md)]",
  ),
} as const;

const tooltipArrowClass = "fill-surface-elevated";

export function TooltipProvider({ children }: { children: ReactNode }) {
  return (
    <TooltipPrimitive.Provider delayDuration={300} skipDelayDuration={100}>
      {children}
    </TooltipPrimitive.Provider>
  );
}

export type TooltipProps = {
  content: ReactNode;
  description?: string;
  children: ReactNode;
  side?: "top" | "right" | "bottom" | "left";
  align?: "start" | "center" | "end";
  variant?: keyof typeof tooltipVariants;
  delayDuration?: number;
  disabled?: boolean;
};

export function Tooltip({
  content,
  description,
  children,
  side = "top",
  align = "center",
  variant = "default",
  delayDuration,
  disabled = false,
}: TooltipProps) {
  if (disabled) {
    return children;
  }

  const body =
    typeof content === "string" && !description ? (
      content
    ) : (
      <span className="flex flex-col gap-0.5">
        <span
          className={cn(
            variant === "nav"
              ? "text-[length:var(--exits-text-sm)] font-semibold leading-snug text-foreground"
              : "font-medium",
          )}
        >
          {content}
        </span>
        {description ? (
          <span className="text-[length:var(--exits-text-xs)] leading-snug text-muted">{description}</span>
        ) : null}
      </span>
    );

  return (
    <TooltipPrimitive.Root delayDuration={delayDuration}>
      <TooltipPrimitive.Trigger asChild>{children}</TooltipPrimitive.Trigger>
      <TooltipPrimitive.Portal>
        <TooltipPrimitive.Content
          side={side}
          align={align}
          sideOffset={variant === "nav" ? 10 : 6}
          collisionPadding={12}
          avoidCollisions
          className={cn(tooltipVariants[variant], "pointer-events-none")}
        >
          {body}
          <TooltipPrimitive.Arrow className={tooltipArrowClass} width={10} height={5} />
        </TooltipPrimitive.Content>
      </TooltipPrimitive.Portal>
    </TooltipPrimitive.Root>
  );
}
