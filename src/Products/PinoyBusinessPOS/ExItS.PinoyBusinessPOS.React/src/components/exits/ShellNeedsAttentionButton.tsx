import { AlertTriangle, CheckCircle2, ChevronRight } from "lucide-react";
import { Link } from "react-router-dom";
import { BottomSheet } from "@/components/exits/SheetDialog";
import { DropdownMenu, useDismissibleOpen } from "@/components/ui/dropdown-menu";
import { type NeedsAttentionGroup } from "@/features/shell/needs-attention";
import { useNeedsAttentionAlerts } from "@/features/shell/useNeedsAttentionAlerts";
import { useMediaMin } from "@/hooks/useMediaQuery";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";

export type ShellNeedsAttentionButtonProps = {
  testId?: string;
  className?: string;
  /** Test / story overrides — when omitted, live workspace queries drive the panel. */
  groupsOverride?: NeedsAttentionGroup[];
  badgeOverride?: string | null;
  countOverride?: number;
};

function AlertRows({
  groups,
  testId,
  onNavigate,
}: {
  groups: NeedsAttentionGroup[];
  testId: string;
  onNavigate: () => void;
}) {
  const { t } = useI18n();

  return (
    <div className="flex flex-col gap-3" data-testid={`${testId}-groups`}>
      {groups.map((group) => (
        <section
          key={group.id}
          className="flex flex-col gap-1.5"
          data-testid={`${testId}-group-${group.id}`}
          aria-label={t(group.labelKey)}
        >
          <p className="m-0 px-0.5 text-[length:var(--exits-text-xs)] font-semibold uppercase tracking-wide text-muted">
            {t(group.labelKey)}
          </p>
          <ul className="m-0 flex list-none flex-col gap-1.5 p-0">
            {group.alerts.map((alert) => {
              const title = t(alert.titleKey);
              const reason = t(alert.reasonKey);
              return (
                <li key={alert.id}>
                  <Link
                    to={alert.href}
                    data-testid={alert.testId}
                    data-tone="warning"
                    className={cn(
                      "exits-toast exits-toast--warning shell-needs-attention-toast exits-toast--interactive",
                      "items-center no-underline transition-[box-shadow,transform] hover:shadow-[0_10px_28px_color-mix(in_srgb,#000_16%,transparent)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring",
                    )}
                    onClick={onNavigate}
                  >
                    <span className="exits-toast__icon text-[var(--exits-warning)]" aria-hidden>
                      <AlertTriangle className="size-4" strokeWidth={2} />
                    </span>
                    <span className="exits-toast__body">
                      <span className="exits-toast__title block">
                        {title}
                        <span className="font-medium text-muted" aria-hidden>
                          {" "}
                          · {alert.count}
                        </span>
                      </span>
                      <span className="exits-toast__description block">{reason}</span>
                    </span>
                    <span
                      className="inline-flex size-7 shrink-0 items-center justify-center self-center text-muted"
                      aria-hidden
                    >
                      <ChevronRight className="size-4" />
                    </span>
                  </Link>
                </li>
              );
            })}
          </ul>
        </section>
      ))}
    </div>
  );
}

function EmptyState({ testId }: { testId: string }) {
  const { t } = useI18n();
  return (
    <div
      className="exits-toast exits-toast--success shell-needs-attention-toast"
      data-testid={`${testId}-empty`}
      data-tone="success"
      role="status"
    >
      <span className="exits-toast__icon text-[var(--exits-success)]" aria-hidden>
        <CheckCircle2 className="size-4" strokeWidth={2} />
      </span>
      <div className="exits-toast__body">
        <div className="exits-toast__title">{t("shell.needsAttention.allClear")}</div>
        <div className="exits-toast__description">
          {t("shell.needsAttention.allClearDetail")}
        </div>
      </div>
    </div>
  );
}

/**
 * Navbar Needs Attention control — always visible.
 * Badge only when count > 0. Empty state when nothing needs attention.
 * Not manually dismissible; alerts clear when underlying conditions resolve.
 */
export function ShellNeedsAttentionButton({
  testId = "shell-needs-attention",
  className,
  groupsOverride,
  badgeOverride,
  countOverride,
}: ShellNeedsAttentionButtonProps) {
  const { t } = useI18n();
  const menu = useDismissibleOpen(false);
  const isDesktop = useMediaMin(768);
  const live = useNeedsAttentionAlerts();

  const groups = groupsOverride ?? live.groups;
  const count = countOverride ?? live.count;
  const badge = badgeOverride !== undefined ? badgeOverride : live.badge;
  const hasIssues = count > 0;

  const title = t("shell.needsAttention.title");
  const accessibleName = hasIssues
    ? t("shell.needsAttention.countLabel").replace("{count}", badge ?? String(count))
    : t("shell.needsAttention.allClearLabel");

  const panelBody = hasIssues ? (
    <AlertRows groups={groups} testId={testId} onNavigate={() => menu.close()} />
  ) : (
    <EmptyState testId={testId} />
  );

  const triggerClassName = cn(
    "relative inline-flex size-11 min-w-11 shrink-0 items-center justify-center rounded-full text-foreground transition-colors hover:bg-[var(--exits-surface-muted)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring",
    className,
  );

  const triggerContent = (
    <>
      <AlertTriangle
        className={cn(
          "size-5",
          hasIssues ? "text-[var(--exits-warning)]" : "text-muted",
        )}
        aria-hidden
        data-testid={`${testId}-icon`}
        data-emphasis={hasIssues ? "warning" : "neutral"}
      />
      {hasIssues && badge ? (
        <span
          data-testid={`${testId}-badge`}
          className="absolute top-1 right-1 inline-flex min-w-[1.1rem] items-center justify-center rounded-full bg-[var(--exits-warning)] px-1 text-[0.65rem] font-semibold leading-4 text-[var(--exits-warning-foreground,var(--exits-surface))]"
          aria-hidden
        >
          {badge}
        </span>
      ) : null}
      <span className="sr-only">{accessibleName}</span>
    </>
  );

  if (!isDesktop) {
    return (
      <>
        <button
          type="button"
          data-testid={testId}
          data-has-issues={hasIssues ? "true" : "false"}
          aria-label={accessibleName}
          aria-expanded={menu.open}
          aria-haspopup="dialog"
          onClick={() => menu.setOpen(true)}
          className={triggerClassName}
        >
          {triggerContent}
        </button>
        <BottomSheet
          open={menu.open}
          onClose={menu.close}
          title={title}
          panelId={`${testId}-sheet`}
          testId={`${testId}-sheet`}
          closeLabel={t("shell.needsAttention.close")}
          presentation="sheet"
          panelClassName="shell-needs-attention-sheet"
        >
          <div data-testid={`${testId}-panel`} className="min-h-0 overflow-y-auto">
            {panelBody}
          </div>
        </BottomSheet>
      </>
    );
  }

  return (
    <DropdownMenu
      align="end"
      open={menu.open}
      onOpenChange={menu.setOpen}
      menuLabel={title}
      menuClassName="shell-needs-attention-menu max-w-[22rem]"
      trigger={({ id, expanded, controls, onClick, onKeyDown }) => (
        <button
          type="button"
          id={id}
          data-testid={testId}
          data-has-issues={hasIssues ? "true" : "false"}
          aria-label={accessibleName}
          aria-expanded={expanded}
          aria-controls={controls}
          aria-haspopup="menu"
          onClick={onClick}
          onKeyDown={onKeyDown}
          className={triggerClassName}
        >
          {triggerContent}
        </button>
      )}
    >
      <div
        className="shell-needs-attention-panel flex min-w-[16rem] max-w-[22rem] flex-col gap-2 p-3"
        data-testid={`${testId}-panel`}
        role="group"
        aria-label={title}
      >
        <p className="m-0 text-[length:var(--exits-text-sm)] font-semibold">{title}</p>
        <div className="shell-connection-panel__divider" role="separator" />
        {panelBody}
      </div>
    </DropdownMenu>
  );
}
