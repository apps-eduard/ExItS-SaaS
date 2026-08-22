import { ArrowLeft } from "lucide-react";
import { Link } from "react-router-dom";
import { cn } from "@/lib/cn";

export type PageHeaderProps = {
  title: string;
  description?: string;
  /** Canonical parent route for child pages. Omit on root bottom-nav destinations. */
  backTo?: string;
  /** Accessible name for the back control (also used as aria-label). */
  backLabel?: string;
  backTestId?: string;
};

export function PageHeader({
  title,
  description,
  backTo,
  backLabel,
  backTestId = "page-header-back",
}: PageHeaderProps) {
  const showBack = Boolean(backTo && backLabel);

  return (
    <header className="flex min-w-0 flex-col gap-1">
      <div className="flex min-w-0 items-center gap-1.5">
        {showBack ? (
          <Link
            to={backTo!}
            data-testid={backTestId}
            aria-label={backLabel}
            className={cn(
              "-ml-2 inline-flex size-11 min-h-11 min-w-11 shrink-0 items-center justify-center rounded-[var(--exits-radius-md)] text-foreground no-underline transition-colors hover:bg-[var(--exits-surface-muted)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring",
            )}
          >
            <ArrowLeft className="size-5 shrink-0" aria-hidden />
          </Link>
        ) : null}
        <div className="min-w-0 flex-1">
          <h1 className="m-0 text-[length:var(--exits-text-xl)] font-bold leading-tight tracking-tight">
            {title}
          </h1>
        </div>
      </div>
      {description ? (
        <p
          className={cn(
            "m-0 text-[length:var(--exits-text-sm)] leading-relaxed text-muted",
            showBack && "pl-11",
          )}
        >
          {description}
        </p>
      ) : null}
    </header>
  );
}
