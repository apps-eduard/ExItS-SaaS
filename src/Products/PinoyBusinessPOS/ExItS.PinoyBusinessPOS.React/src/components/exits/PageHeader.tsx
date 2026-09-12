import { useId, useState, type ReactNode } from "react";
import type { LucideIcon } from "lucide-react";
import { ArrowLeft, Info } from "lucide-react";
import { Link } from "react-router-dom";
import { cn } from "@/lib/cn";
import { useI18n } from "@/i18n/I18nProvider";

export type PageHeaderProps = {
  title: string;
  /** Optional icon shown before the page title. */
  titleIcon?: LucideIcon;
  /** Muted line under the title (e.g. branch name or record name). */
  subtitle?: string;
  description?: string;
  /**
   * When true, description stays behind the info control until revealed.
   * Default false — major pages show a one-line lede under the title.
   */
  descriptionCollapsible?: boolean;
  /** Accessible name for the info icon control. */
  infoToggleLabel?: string;
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
};

/**
 * Canonical ExItS page header: optional back, title, description, badge/actions.
 * Structural surface — not a heavy Card; not Control Shape / Primary-tinted.
 */
export function PageHeader({
  title,
  titleIcon: TitleIcon,
  subtitle,
  description,
  descriptionCollapsible = false,
  infoToggleLabel,
  actions,
  trailing,
  backTo,
  backLabel,
  backTestId = "page-header-back",
}: PageHeaderProps) {
  const { t } = useI18n();
  const [infoPinned, setInfoPinned] = useState(false);
  const [infoHovered, setInfoHovered] = useState(false);
  const descriptionId = useId();
  const showBack = Boolean(backTo && backLabel);
  const hasDescription = Boolean(description?.trim());
  const collapsible = hasDescription && descriptionCollapsible;
  const alwaysVisible = hasDescription && !descriptionCollapsible;
  const infoVisible = infoPinned || infoHovered;
  const toggleLabel = infoToggleLabel ?? t("pageHeader.infoToggle");
  const rightSlot = actions ?? trailing;

  return (
    <header className="page-header flex min-w-0 flex-col gap-1" data-testid="page-header">
      <div className="flex min-w-0 gap-1.5">
        {showBack ? (
          <div className="flex h-[var(--exits-control-height)] shrink-0 items-center">
            <Link
              to={backTo!}
              data-testid={backTestId}
              aria-label={backLabel}
              className={cn(
                "-ms-1 inline-flex size-[var(--exits-control-height)] min-h-[var(--exits-control-height)] min-w-[var(--exits-control-height)] shrink-0 items-center justify-center rounded-[var(--exits-radius-md)] text-foreground no-underline transition-colors hover:bg-[var(--exits-surface-muted)] hover:text-[var(--exits-primary)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring",
              )}
            >
              <ArrowLeft className="size-5 shrink-0 rtl:rotate-180" aria-hidden />
            </Link>
          </div>
        ) : null}

        <div
          className="page-header__main flex min-w-0 flex-1 flex-col gap-1"
          onMouseLeave={() => setInfoHovered(false)}
        >
          <div className="page-header__head">
            <div className="page-header__title-row flex min-h-[var(--exits-control-height)] min-w-0 items-center gap-1.5">
              {TitleIcon ? (
                <span className="page-header__title-icon shrink-0" aria-hidden>
                  <TitleIcon className="size-5" />
                </span>
              ) : null}
              <h1 className="page-header__title exits-type-page-title m-0 min-w-0 flex-1 truncate">
                {title}
              </h1>
              {collapsible ? (
                <button
                  type="button"
                  className={cn(
                    "page-header__info",
                    infoVisible && "page-header__info--visible",
                    infoPinned && "page-header__info--pinned",
                  )}
                  data-testid="page-header-info-toggle"
                  aria-label={toggleLabel}
                  aria-expanded={infoVisible}
                  aria-controls={descriptionId}
                  onMouseEnter={() => setInfoHovered(true)}
                  onFocus={() => setInfoHovered(true)}
                  onBlur={(event) => {
                    if (!event.currentTarget.contains(event.relatedTarget as Node | null)) {
                      setInfoHovered(false);
                    }
                  }}
                  onClick={() => setInfoPinned((pinned) => !pinned)}
                >
                  <Info className="size-4 shrink-0" aria-hidden />
                </button>
              ) : null}
            </div>
            {rightSlot ? (
              <div className="page-header__trailing" data-testid="page-header-actions">
                {rightSlot}
              </div>
            ) : null}
          </div>

          {subtitle ? (
            <p
              data-testid="page-header-subtitle"
              className="page-header__subtitle m-0 truncate text-[length:var(--exits-text-sm)] font-medium text-muted"
            >
              {subtitle}
            </p>
          ) : null}

          {collapsible ? (
            <div
              id={descriptionId}
              className={cn(
                "page-header__description-shell",
                infoVisible && "page-header__description-shell--open",
              )}
              data-testid="page-header-description-shell"
              aria-hidden={!infoVisible}
              onMouseEnter={() => setInfoHovered(true)}
            >
              <div className="page-header__description-clip">
                <p
                  data-testid="page-header-description"
                  className="page-header__description m-0 text-[length:var(--exits-text-sm)] leading-relaxed text-muted"
                >
                  {description}
                </p>
              </div>
            </div>
          ) : null}
        </div>
      </div>

      {alwaysVisible ? (
        <p
          data-testid="page-header-description"
          className={cn(
            "page-header__description m-0 text-[length:var(--exits-text-sm)] leading-snug text-muted",
            showBack && "ps-10",
          )}
        >
          {description}
        </p>
      ) : null}
    </header>
  );
}
