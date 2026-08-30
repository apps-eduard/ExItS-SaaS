import { cn } from "@/lib/cn";

type BranchFulfillmentSwitchProps = {
  checked: boolean;
  disabled?: boolean;
  label: string;
  hint?: string | null;
  pending?: boolean;
  testId?: string;
  onCheckedChange: (next: boolean) => void;
};

/**
 * Accessible switch control (button role=switch) for pickup/delivery enablement.
 * Must never be nested inside a Link.
 */
export function BranchFulfillmentSwitch({
  checked,
  disabled = false,
  label,
  hint,
  pending = false,
  testId,
  onCheckedChange,
}: BranchFulfillmentSwitchProps) {
  return (
    <div className="branch-switch">
      <div className="branch-switch__row">
        <span className="branch-switch__label" id={testId ? `${testId}-label` : undefined}>
          {label}
        </span>
        <button
          type="button"
          role="switch"
          aria-checked={checked}
          aria-labelledby={testId ? `${testId}-label` : undefined}
          aria-busy={pending || undefined}
          disabled={disabled}
          className={cn(
            "branch-switch__control",
            checked && "branch-switch__control--on",
            pending && "branch-switch__control--pending",
          )}
          data-testid={testId}
          onClick={() => {
            if (disabled || pending) {
              return;
            }
            onCheckedChange(!checked);
          }}
        >
          <span className="branch-switch__thumb" aria-hidden />
        </button>
      </div>
      {hint ? (
        <p className="branch-switch__hint m-0" data-testid={testId ? `${testId}-hint` : undefined}>
          {hint}
        </p>
      ) : null}
    </div>
  );
}
