import { Settings } from "lucide-react";
import { useNavigate } from "react-router-dom";
import { IconButton } from "@/components/ui/icon-button";
import { useI18n } from "@/i18n/I18nProvider";

export function AppTopBar() {
  const { t } = useI18n();
  const navigate = useNavigate();

  return (
    <header
      data-density="compact"
      className="sticky top-0 z-[var(--exits-z-topbar)] border-b border-border bg-surface/95 backdrop-blur-sm"
    >
      <div className="mx-auto flex h-14 max-w-5xl items-center gap-3 px-4">
        <div
          className="flex size-8 shrink-0 items-center justify-center rounded-[var(--exits-radius-md)] bg-primary text-xs font-bold text-primary-foreground"
          aria-hidden="true"
        >
          E
        </div>
        <p className="m-0 min-w-0 flex-1 truncate text-[length:var(--exits-text-md)] font-bold tracking-tight">
          {t("app.name")}
        </p>
        <IconButton label={t("shell.settings")} onClick={() => navigate("/appearance")}>
          <Settings className="size-5" aria-hidden="true" />
        </IconButton>
      </div>
    </header>
  );
}
