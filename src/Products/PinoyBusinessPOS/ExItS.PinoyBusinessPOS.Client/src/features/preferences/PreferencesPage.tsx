import { Card } from "@/components/ui/card";
import { LanguageControl } from "@/components/exits/LanguageControl";
import { PageHeader } from "@/components/exits/PageHeader";
import { ThemeControl } from "@/components/exits/ThemeControl";
import { useI18n } from "@/i18n/I18nProvider";

export function PreferencesPage() {
  const { t } = useI18n();

  return (
    <div className="mx-auto flex w-full max-w-xl min-w-0 flex-col gap-4">
      <PageHeader title={t("preferences.title")} description={t("preferences.lede")} />
      <Card className="flex flex-col gap-5">
        <section className="flex flex-col gap-3" aria-labelledby="preferences-appearance">
          <h2
            id="preferences-appearance"
            className="m-0 text-[length:var(--exits-text-md)] font-semibold text-foreground"
          >
            {t("preferences.appearance")}
          </h2>
          <div className="flex max-w-md flex-col gap-4">
            <LanguageControl />
            <ThemeControl />
          </div>
        </section>
      </Card>
    </div>
  );
}
