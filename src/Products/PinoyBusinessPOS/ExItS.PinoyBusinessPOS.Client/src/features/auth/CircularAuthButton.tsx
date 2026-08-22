import type { ReactNode } from "react";
import { cn } from "@/lib/cn";

type CircularAuthButtonProps = {
  label: string;
  testId: string;
  disabled?: boolean;
  onClick: () => void;
  icon: ReactNode;
  variant?: "default" | "pin";
};

/** Circular 44px auth action — social providers and offline PIN entry. */
export function CircularAuthButton({
  label,
  testId,
  disabled,
  onClick,
  icon,
  variant = "default",
}: CircularAuthButtonProps) {
  return (
    <button
      type="button"
      data-testid={testId}
      disabled={disabled}
      aria-label={label}
      title={label}
      className={cn(
        "inline-flex size-11 min-h-11 min-w-11 shrink-0 items-center justify-center rounded-full border transition-colors disabled:cursor-not-allowed disabled:opacity-60",
        variant === "pin"
          ? "border-primary bg-primary text-primary-foreground hover:opacity-90"
          : "border-border bg-surface text-foreground shadow-sm hover:bg-[var(--exits-surface-muted)]",
      )}
      onClick={onClick}
    >
      <span className="inline-flex size-5 items-center justify-center" aria-hidden>
        {icon}
      </span>
    </button>
  );
}
