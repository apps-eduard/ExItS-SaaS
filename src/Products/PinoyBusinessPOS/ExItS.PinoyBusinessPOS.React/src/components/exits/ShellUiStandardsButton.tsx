import { Building2, UserRound } from "lucide-react";
import { Link } from "react-router-dom";
import { isFrontendLocalValidationMode } from "@/api/platform/local-validation-gate";
import {
  uiStandardsHref,
  type UiStandardsProduct,
} from "@/features/ui-standards/ui-standard-catalog";
import { cn } from "@/lib/cn";

export type ShellUiStandardsButtonProps = {
  /** Personal = Soft Glass playground. Organization = Hybrid Glass live pages. */
  product?: UiStandardsProduct;
  to?: string;
  label: string;
  testId?: string;
  className?: string;
};

/**
 * Dev/local-validation only — icon-only shortcut to the matching UI Standards surface.
 * Personal context: UserRound → classic playground.
 * Organization/business context: Building2 → Org Hybrid Glass pages.
 */
export function ShellUiStandardsButton({
  product = "personal",
  to,
  label,
  testId = "shell-ui-standards-button",
  className,
}: ShellUiStandardsButtonProps) {
  if (!import.meta.env.DEV || !isFrontendLocalValidationMode()) {
    return null;
  }

  const href = to ?? uiStandardsHref(product);
  const Icon = product === "organization" ? Building2 : UserRound;

  return (
    <Link
      to={href}
      data-testid={testId}
      data-ui-standards-product={product}
      aria-label={label}
      title={label}
      className={cn(
        "relative inline-flex size-11 min-w-11 shrink-0 items-center justify-center rounded-full text-foreground no-underline transition-colors hover:bg-[var(--exits-surface-muted)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring",
        className,
      )}
    >
      <Icon className="size-5" aria-hidden strokeWidth={2.25} />
      <span className="sr-only">{label}</span>
    </Link>
  );
}
