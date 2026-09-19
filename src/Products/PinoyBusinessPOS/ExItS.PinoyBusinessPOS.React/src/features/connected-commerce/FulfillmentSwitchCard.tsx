import type { LucideIcon } from "lucide-react";
import { Switch } from "@/components/ui/switch";
import { StatusChip } from "@/components/exits/StatusChip";
import { cn } from "@/lib/cn";

export type FulfillmentSwitchCardProps = {
  title: string;
  hint?: string | null;
  statusLabel: string;
  statusTone: "success" | "warning" | "neutral" | "info" | "danger";
  checked: boolean;
  disabled?: boolean;
  pending?: boolean;
  Icon?: LucideIcon;
  testId: string;
  statusTestId?: string;
  className?: string;
  onCheckedChange: (next: boolean) => void;
};

/**
 * ExItS standard switch card: label + exits-switch + status chip in one bordered card.
 * Matches BranchOverviewPanel / branch-overview-progress channel cards.
 */
export function FulfillmentSwitchCard({
  title,
  hint,
  statusLabel,
  statusTone,
  checked,
  disabled = false,
  pending = false,
  Icon,
  testId,
  statusTestId,
  className,
  onCheckedChange,
}: FulfillmentSwitchCardProps) {
  const labelId = `${testId}-label`;
  const switchId = `${testId}-control`;

  return (
    <div
      className={cn("branch-overview-progress__item", className)}
      data-testid={`${testId}-card`}
    >
      <div className="branch-overview-progress__top">
        <div className="branch-overview-progress__identity">
          {Icon ? (
            <span className="branch-overview-progress__icon" aria-hidden>
              <Icon className="size-4" strokeWidth={1.75} />
            </span>
          ) : null}
          <div className="min-w-0">
            <p className="branch-overview-progress__label m-0" id={labelId}>
              {title}
            </p>
            {hint ? (
              <p
                className="branch-overview-progress__value m-0"
                data-testid={`${testId}-hint`}
              >
                {hint}
              </p>
            ) : null}
          </div>
        </div>
        <div className="branch-overview-progress__controls">
          <Switch
            id={switchId}
            checked={checked}
            disabled={disabled || pending}
            aria-busy={pending || undefined}
            aria-labelledby={labelId}
            data-testid={testId}
            onCheckedChange={onCheckedChange}
          />
          <div data-testid={statusTestId ?? `${testId}-status`}>
            <StatusChip tone={statusTone} shape="soft" appearance="outline">
              {statusLabel}
            </StatusChip>
          </div>
        </div>
      </div>
    </div>
  );
}
