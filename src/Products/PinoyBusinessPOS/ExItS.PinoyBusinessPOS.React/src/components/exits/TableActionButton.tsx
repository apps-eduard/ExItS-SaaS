import type { ButtonHTMLAttributes, ReactNode } from "react";
import { ExitsTooltip } from "@/components/exits/ExitsTooltip";
import {
  getActionIcon,
  getActionIntent,
  type ExitsActionId,
} from "@/components/exits/action-semantics";
import { Button, type ButtonProps } from "@/components/ui/button";
import { cn } from "@/lib/cn";

export type TableActionButtonProps = Omit<
  ButtonHTMLAttributes<HTMLButtonElement>,
  "children"
> & {
  action: ExitsActionId;
  /** Accessible name + tooltip content. */
  label: string;
  /** Override Button variant (defaults from action semantics). */
  variant?: ButtonProps["variant"];
  /** Optional custom icon; defaults to canonical action icon. */
  icon?: ReactNode;
  testId?: string;
};

/**
 * Compact table-row action control — icon-only, shared semantics, aria-label + tooltip.
 */
export function TableActionButton({
  action,
  label,
  variant,
  icon,
  className,
  testId,
  type = "button",
  ...props
}: TableActionButtonProps) {
  const Icon = getActionIcon(action);
  const resolvedVariant = variant ?? (getActionIntent(action) as ButtonProps["variant"]);

  return (
    <ExitsTooltip content={label}>
      <Button
        type={type}
        size="icon"
        variant={resolvedVariant}
        aria-label={label}
        title={label}
        className={cn("shrink-0", className)}
        data-testid={testId}
        data-action={action}
        {...props}
      >
        {icon ?? (Icon ? <Icon className="size-4" aria-hidden /> : null)}
      </Button>
    </ExitsTooltip>
  );
}
