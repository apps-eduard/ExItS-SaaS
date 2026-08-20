import { LanguageControl } from "@/components/exits/LanguageControl";
import { PageHeader } from "@/components/exits/PageHeader";
import { ThemeControl } from "@/components/exits/ThemeControl";
import { useI18n } from "@/i18n/I18nProvider";

export function PreferencesPage() {
  const { t } = useI18n();

  return (
    <div className="mx-auto flex w-full max-w-lg min-w-0 flex-col gap-4">
      <PageHeader title={t("preferences.title")} description={t("preferences.lede")} />
      <section
        className="overflow-hidden rounded-[var(--exits-radius-md)] border border-border bg-surface"
        aria-labelledby="preferences-appearance"
      >
        <div className="border-b border-border px-4 py-3">
          <h2
            id="preferences-appearance"
            className="m-0 text-[length:var(--exits-text-md)] font-semibold text-foreground"
          >
            {t("preferences.appearance")}
          </h2>
        </div>
        <div className="divide-y divide-border px-4">
          <LanguageControl />
          <ThemeControl />
        </div>
      </section>
    </div>
  );
}
