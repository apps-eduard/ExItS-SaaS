import type { ReactNode } from "react";
import { cn } from "@/lib/cn";

type CircularAuthButtonProps = {
  label: string;
  testId: string;
  disabled?: boolean;
  onClick: () => void;
  icon: ReactNode;
  variant?: "facebook" | "google" | "pin";
};

/** Circular 44px auth action — social providers and offline PIN entry. */
export function CircularAuthButton({
  label,
  testId,
  disabled,
  onClick,
  icon,
  variant = "google",
}: CircularAuthButtonProps) {
  return (
    <button
      type="button"
      data-testid={testId}
      disabled={disabled}
      aria-label={label}
      title={label}
      className={cn(
        "inline-flex size-11 min-h-11 min-w-11 shrink-0 items-center justify-center rounded-full border shadow-[0_6px_16px_rgba(20,32,26,0.18)] transition-[opacity,transform,box-shadow] hover:shadow-[0_8px_20px_rgba(20,32,26,0.22)] active:scale-[0.98] disabled:cursor-not-allowed disabled:opacity-55 disabled:shadow-[0_2px_8px_rgba(20,32,26,0.12)]",
        variant === "facebook" && "border-[#1877f2] bg-[#1877f2] text-white hover:opacity-95",
        variant === "google" &&
          "border-border bg-surface text-foreground hover:bg-[var(--exits-surface-muted)]",
        variant === "pin" && "border-primary bg-primary text-primary-foreground hover:opacity-95",
      )}
      onClick={onClick}
    >
      <span className="inline-flex size-5 items-center justify-center" aria-hidden>
        {icon}
      </span>
    </button>
  );
}
