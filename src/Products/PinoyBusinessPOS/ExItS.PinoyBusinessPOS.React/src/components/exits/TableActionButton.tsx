import type { ButtonHTMLAttributes, ReactNode } from "react";
import { ExitsTooltip } from "@/components/exits/ExitsTooltip";
import {
  getActionButtonStyle,
  getActionIcon,
  type ExitsActionId,
} from "@/components/exits/action-semantics";
import { Button, type ButtonAppearance, type ButtonIntentTone, type ButtonProps } from "@/components/ui/button";
import { cn } from "@/lib/cn";

export type TableActionButtonProps = Omit<
  ButtonHTMLAttributes<HTMLButtonElement>,
  "children"
> & {
  action: ExitsActionId;
  /** Accessible name + tooltip content. */
  label: string;
  /** Override semantic intent (defaults from action semantics). */
  intent?: ButtonIntentTone;
  /** Override appearance (defaults ghost for compact table chrome). */
  appearance?: ButtonAppearance;
  /**
   * @deprecated Prefer intent / appearance. Legacy Button variant override.
   */
  variant?: ButtonProps["variant"];
  /** Optional custom icon; defaults to canonical action icon. */
  icon?: ReactNode;
  testId?: string;
};

/**
 * Compact table-row action control — icon-only, shared semantics, aria-label + tooltip.
 * Default appearance is Ghost (quiet row chrome); intent still comes from action semantics.
 */
export function TableActionButton({
  action,
  label,
  intent,
  appearance,
  variant,
  icon,
  className,
  testId,
  type = "button",
  ...props
}: TableActionButtonProps) {
  const Icon = getActionIcon(action);
  const style = getActionButtonStyle(action);
  const resolvedIntent = intent ?? style.intent;
  // Table chrome: ghost by default unless caller overrides appearance or legacy variant.
  const resolvedAppearance = appearance ?? (variant ? undefined : "ghost");

  return (
    <ExitsTooltip content={label}>
      <Button
        type={type}
        size="icon"
        intent={variant ? undefined : resolvedIntent}
        appearance={variant ? undefined : resolvedAppearance}
        variant={variant}
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
