import { ArrowLeft } from "lucide-react";
import { Link } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/cn";
import { useSmartBack, type UseSmartBackOptions } from "@/navigation/useSmartBack";

export type AppBackButtonProps = UseSmartBackOptions & {
  className?: string;
  /** Prefer Link (default) so middle-click / open-in-new-tab work with resolved backTo. */
  mode?: "link" | "button";
};

/**
 * Canonical Back control for detail/edit pages — resolves via useSmartBack.
 * Prefer PageHeader `backTo`/`backLabel` from useSmartBack for standard headers;
 * use this when a standalone control is required.
 */
export function AppBackButton({
  className,
  mode = "link",
  ...smartOptions
}: AppBackButtonProps) {
  const { backTo, backLabel, backTestId, goBack } = useSmartBack(smartOptions);

  if (mode === "button") {
    return (
      <Button
        type="button"
        variant="ghost"
        className={cn("inline-flex items-center gap-1.5", className)}
        data-testid={backTestId}
        aria-label={backLabel}
        onClick={goBack}
      >
        <ArrowLeft className="size-4 shrink-0 rtl:rotate-180" aria-hidden />
        <span className="sr-only">{backLabel}</span>
      </Button>
    );
  }

  return (
    <Link
      to={backTo}
      data-testid={backTestId}
      aria-label={backLabel}
      className={cn(
        "inline-flex shrink-0 items-center justify-center rounded-[var(--exits-radius-md)] text-foreground no-underline transition-colors hover:bg-[var(--exits-surface-muted)] hover:text-[var(--exits-primary)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring size-[var(--exits-control-height)]",
        className,
      )}
      onClick={(event) => {
        // Keep Link href for progressive enhancement; still clear storage via goBack path
        // when left-click navigates in-app.
        if (
          event.button === 0 &&
          !event.metaKey &&
          !event.ctrlKey &&
          !event.shiftKey &&
          !event.altKey
        ) {
          event.preventDefault();
          goBack();
        }
      }}
    >
      <ArrowLeft className="size-5 shrink-0 rtl:rotate-180" aria-hidden />
    </Link>
  );
}
