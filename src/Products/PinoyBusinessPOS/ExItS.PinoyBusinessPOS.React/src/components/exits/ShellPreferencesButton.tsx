import { Settings } from "lucide-react";
import { Link, useLocation } from "react-router-dom";
import {
  capturePreferencesReturnFrom,
  preferencesNavigationState,
} from "@/features/preferences/preferences-return";
import { cn } from "@/lib/cn";

export type ShellPreferencesButtonProps = {
  to?: string;
  label: string;
  testId?: string;
  className?: string;
};

/**
 * Compact shell preferences control — icon only; opens the preferences drawer route.
 * Remembers the current route so close returns here instead of always landing on More.
 */
export function ShellPreferencesButton({
  to = "/settings/preferences",
  label,
  testId = "shell-preferences-button",
  className,
}: ShellPreferencesButtonProps) {
  const location = useLocation();
  const preferencesState = preferencesNavigationState(location.pathname, location.search);

  return (
    <Link
      to={to}
      state={preferencesState}
      onClick={
        preferencesState
          ? () => capturePreferencesReturnFrom(location.pathname, location.search)
          : undefined
      }
      data-testid={testId}
      aria-label={label}
      title={label}
      className={cn(
        "relative inline-flex size-11 min-w-11 shrink-0 items-center justify-center rounded-full text-foreground no-underline transition-colors hover:bg-[var(--exits-surface-muted)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring",
        className,
      )}
    >
      <Settings className="size-5" aria-hidden />
      <span className="sr-only">{label}</span>
    </Link>
  );
}
