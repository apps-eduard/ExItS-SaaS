import { Palette } from "lucide-react";
import { Link } from "react-router-dom";
import { isFrontendLocalValidationMode } from "@/api/platform/local-validation-gate";
import { cn } from "@/lib/cn";

export type ShellUiStandardsButtonProps = {
  to?: string;
  label: string;
  testId?: string;
  className?: string;
};

/**
 * Dev/local-validation only — icon-only shortcut to the UI Standards reference page.
 */
export function ShellUiStandardsButton({
  to = "/ui-standards",
  label,
  testId = "shell-ui-standards-button",
  className,
}: ShellUiStandardsButtonProps) {
  if (!isFrontendLocalValidationMode()) {
    return null;
  }

  return (
    <Link
      to={to}
      data-testid={testId}
      aria-label={label}
      title={label}
      className={cn(
        "relative inline-flex size-11 min-w-11 shrink-0 items-center justify-center rounded-full text-foreground no-underline transition-colors hover:bg-[var(--exits-surface-muted)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring",
        className,
      )}
    >
      <Palette className="size-5" aria-hidden />
      <span className="sr-only">{label}</span>
    </Link>
  );
}
