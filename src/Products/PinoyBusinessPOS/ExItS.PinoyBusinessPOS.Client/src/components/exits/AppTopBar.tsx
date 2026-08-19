import { StatusChip } from "@/components/ui/badge";
import { useI18n } from "@/i18n/I18nProvider";

export function AppTopBar() {
  const { t } = useI18n();

  return (
    <header
      data-density="compact"
      className="sticky top-0 z-[var(--exits-z-topbar)] border-b border-border bg-surface/95 backdrop-blur-sm"
    >
      <div className="mx-auto flex min-h-[var(--exits-topbar-height)] max-w-6xl items-center gap-3 px-[var(--exits-page-padding)] py-2">
        <div className="flex size-9 shrink-0 items-center justify-center rounded-[var(--exits-radius-md)] bg-primary text-sm font-bold text-primary-foreground">
          E
        </div>
        <div className="min-w-0 flex-1">
          <p className="m-0 truncate text-[length:var(--exits-text-sm)] font-bold">
            {t("app.name")}
          </p>
          <p className="m-0 truncate text-[length:var(--exits-text-xs)] text-muted">
            {t("shell.productPlaceholder")}
          </p>
          <p className="m-0 truncate text-[length:var(--exits-text-xs)] text-muted">
            {t("shell.contextPlaceholder")}
          </p>
        </div>
        <StatusChip className="shrink-0">{t("shell.preview")}</StatusChip>
      </div>
    </header>
  );
}
