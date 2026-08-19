import { ExItsMark } from "@/components/exits/ExItsMark";
import { useI18n } from "@/i18n/I18nProvider";

export function AppTopBar() {
  const { t } = useI18n();

  return (
    <header className="sticky top-0 z-50 border-b border-border/80 bg-surface/90 backdrop-blur-sm">
      <div className="mx-auto flex h-14 max-w-5xl items-center gap-2.5 px-4">
        <ExItsMark size="sm" />
        <p className="m-0 min-w-0 flex-1 truncate text-[length:var(--exits-text-md)] font-semibold tracking-tight">
          {t("app.name")}
        </p>
      </div>
    </header>
  );
}
