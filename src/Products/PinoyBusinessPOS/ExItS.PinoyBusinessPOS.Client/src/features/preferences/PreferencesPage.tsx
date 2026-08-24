import { X } from "lucide-react";
import { useNavigate } from "react-router-dom";
import { DensityControl } from "@/components/exits/DensityControl";
import { LanguageControl } from "@/components/exits/LanguageControl";
import { PageHeader } from "@/components/exits/PageHeader";
import { ThemeControl } from "@/components/exits/ThemeControl";
import { Button } from "@/components/ui/button";
import { useI18n } from "@/i18n/I18nProvider";
import { pageBackNav } from "@/navigation/page-back-nav";

export function PreferencesPage() {
  const { t } = useI18n();
  const navigate = useNavigate();

  function onClose() {
    navigate("/more", { replace: true });
  }

  return (
    <div className="mx-auto flex w-full max-w-lg min-w-0 flex-col gap-5">
      <div className="flex items-start justify-between gap-3">
        <PageHeader
          title={t("preferences.title")}
          description={t("preferences.lede")}
          backTo={pageBackNav.more.to}
          backLabel={t(pageBackNav.more.labelKey)}
          backTestId="page-header-back-preferences"
        />
        <Button
          type="button"
          variant="ghost"
          size="icon"
          className="shrink-0"
          data-testid="preferences-close"
          aria-label={t("preferences.close")}
          onClick={onClose}
        >
          <X className="size-5" aria-hidden="true" />
        </Button>
      </div>
      <section
        className="rounded-[var(--exits-radius-md)] border border-border bg-surface"
        aria-labelledby="preferences-appearance"
      >
        <div className="border-b border-border px-4 py-3.5">
          <h2
            id="preferences-appearance"
            className="m-0 text-[length:var(--exits-text-md)] font-semibold tracking-tight text-foreground"
          >
            {t("preferences.appearance")}
          </h2>
        </div>
        <div className="divide-y divide-border px-4">
          <LanguageControl />
          <ThemeControl />
          <DensityControl />
        </div>
      </section>
    </div>
  );
}
