import { useEffect, useId, useRef, useState, type ReactNode } from "react";
import type { LucideIcon } from "lucide-react";
import { ArrowLeft, Info } from "lucide-react";
import { Link } from "react-router-dom";
import { InfoPopover } from "@/components/exits/InfoPopover";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/cn";
import { useI18n } from "@/i18n/I18nProvider";

export type PageHeaderVariant = "default" | "compact";

export type PageHeaderProps = {
  title: string;
  /** Optional icon shown before the page title. */
  titleIcon?: LucideIcon;
  /** Muted line under the title (e.g. branch name or record name). */
  subtitle?: ReactNode;
  /** Extra classes for the subtitle line. Replaces the default single-line trim. */
  subtitleClassName?: string;
  /**
   * Help text. Shown in the info popover beside the title, not as a line under it.
   * Ignored for `variant="compact"`.
   */
  description?: string;
  /**
   * Kept for existing call sites. Descriptions always open from the info icon.
   */
  descriptionCollapsible?: boolean;
  /** Accessible name for the info icon control. */
  infoToggleLabel?: string;
  /** Optional id for the title element. */
  titleTestId?: string;
  /**
   * Optional trailing / right-slot content (badge, primary action, etc.).
   * Prefer `actions` for new call sites; `trailing` remains as an alias.
   */
  actions?: ReactNode;
  /** @deprecated Prefer `actions`. */
  trailing?: ReactNode;
  /** Canonical parent route for child pages. Omit on root bottom-nav destinations. */
  backTo?: string;
  /** Accessible name for the back control (also used as aria-label). */
  backLabel?: string;
  backTestId?: string;
  /**
   * When set (e.g. from useSmartBack.goBack), left-click runs this instead of
   * a plain Link navigation so returnTo storage can be cleared. Href still
   * supports middle-click / open-in-new-tab via `backTo`.
   */
  onBack?: () => void;
  /**
   * `default` — application pages (Reports, Shifts, …).
   * `compact` — operational POS workspaces (Sell) — single dense row, no lede.
   */
  variant?: PageHeaderVariant;
};

/**
 * Canonical ExItS page header. Sits outside content cards.
 * When a description is set, an info icon beside the title opens it in a popover.
 */
export function PageHeader({
  title,
  titleIcon: TitleIcon,
  subtitle,
  subtitleClassName,
  description,
  infoToggleLabel,
  titleTestId,
  actions,
  trailing,
  backTo,
  backLabel,
  backTestId = "page-header-back",
  onBack,
  variant = "default",
}: PageHeaderProps) {
  const { t } = useI18n();
  const [infoOpen, setInfoOpen] = useState(false);
  const headerRef = useRef<HTMLElement>(null);
  const infoButtonRef = useRef<HTMLElement | null>(null);
  const descriptionId = useId();
  const popoverTitleId = useId();
  const compact = variant === "compact";
  const showBack = Boolean(backTo && backLabel);
  const hasDescription = Boolean(description?.trim()) && !compact;
  const toggleLabel = infoToggleLabel ?? t("pageHeader.infoToggle");
  const rightSlot = actions ?? trailing;

  useEffect(() => {
    if (!infoOpen) {
      return;
    }
    function onKeyDown(event: KeyboardEvent) {
      if (event.key === "Escape") {
        setInfoOpen(false);
      }
    }
    function onPointerDown(event: PointerEvent) {
      if (!headerRef.current?.contains(event.target as Node)) {
        setInfoOpen(false);
      }
    }
    document.addEventListener("keydown", onKeyDown);
    document.addEventListener("pointerdown", onPointerDown);
    return () => {
      document.removeEventListener("keydown", onKeyDown);
      document.removeEventListener("pointerdown", onPointerDown);
    };
  }, [infoOpen]);

  return (
    <header
      ref={headerRef}
      className={cn(
        "page-header relative flex min-w-0 flex-col",
        compact ? "page-header--compact gap-0" : "gap-1",
        infoOpen && "z-30",
      )}
      data-testid="page-header"
      data-variant={variant}
    >
      <div className={cn("flex min-w-0", compact ? "items-center gap-1" : "gap-1.5")}>
        {showBack ? (
          <div
            className={cn(
              "flex shrink-0 items-center",
              compact ? "h-[var(--exits-control-height-sm,2rem)]" : "h-[var(--exits-control-height)]",
            )}
          >
            <Link
              to={backTo!}
              data-testid={backTestId}
              aria-label={backLabel}
              className={cn(
                "inline-flex shrink-0 items-center justify-center rounded-[var(--exits-radius-md)] text-foreground no-underline transition-colors hover:bg-[var(--exits-surface-muted)] hover:text-[var(--exits-primary)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring",
                compact
                  ? "-ms-0.5 size-8 min-h-8 min-w-8"
                  : "-ms-1 size-[var(--exits-control-height)] min-h-[var(--exits-control-height)] min-w-[var(--exits-control-height)]",
              )}
              onClick={
                onBack
                  ? (event) => {
                      if (
                        event.button === 0 &&
                        !event.metaKey &&
                        !event.ctrlKey &&
                        !event.shiftKey &&
                        !event.altKey
                      ) {
                        event.preventDefault();
                        onBack();
                      }
                    }
                  : undefined
              }
            >
              <ArrowLeft className={cn("shrink-0 rtl:rotate-180", compact ? "size-4" : "size-5")} aria-hidden />
            </Link>
          </div>
        ) : null}

        <div
          className={cn(
            "page-header__main flex min-w-0 flex-1",
            compact ? "flex-row items-center gap-1.5" : "flex-col gap-1",
          )}
        >
          <div className={cn("page-header__head", compact && "page-header__head--compact")}>
            <div
              className={cn(
                "page-header__title-row flex min-w-0 items-center gap-1",
                compact ? "min-h-8" : "min-h-[var(--exits-control-height)]",
              )}
            >
              {TitleIcon ? (
                <span className="page-header__title-icon shrink-0" aria-hidden>
                  <TitleIcon className={cn(compact ? "size-4" : "size-5")} />
                </span>
              ) : null}
              <h1
                className={cn(
                  "page-header__title exits-type-page-title m-0 min-w-0 truncate",
                  compact && "page-header__title--compact",
                )}
                data-testid={titleTestId}
              >
                {title}
              </h1>
              {hasDescription ? (
                <span ref={infoButtonRef} className="inline-flex shrink-0">
                <Button
                  type="button"
                  intent="info"
                  appearance="ghost"
                  size="icon"
                  className="shrink-0"
                  data-testid="page-header-info-toggle"
                  aria-label={toggleLabel}
                  aria-expanded={infoOpen}
                  aria-controls={descriptionId}
                  onClick={() => setInfoOpen((open) => !open)}
                >
                  <Info className="size-5 shrink-0" aria-hidden />
                </Button>
                </span>
              ) : null}
            </div>
            {rightSlot ? (
              <div
                className={cn("page-header__trailing", compact && "page-header__trailing--compact")}
                data-testid="page-header-actions"
              >
                {rightSlot}
              </div>
            ) : null}
          </div>

          {!compact && subtitle ? (
            <p
              data-testid="page-header-subtitle"
              className={cn(
                "page-header__subtitle m-0 text-[length:var(--exits-text-sm)] font-medium text-muted",
                subtitleClassName ?? "truncate",
              )}
            >
              {subtitle}
            </p>
          ) : null}
        </div>
      </div>

      {infoOpen && hasDescription ? (
        <InfoPopover
          id={descriptionId}
          titleId={popoverTitleId}
          title={title}
          anchorRef={infoButtonRef}
        >
          <p data-testid="page-header-description" className="m-0 mt-2 text-[length:var(--exits-text-sm)] text-muted">
            {description}
          </p>
        </InfoPopover>
      ) : null}
    </header>
  );
}
