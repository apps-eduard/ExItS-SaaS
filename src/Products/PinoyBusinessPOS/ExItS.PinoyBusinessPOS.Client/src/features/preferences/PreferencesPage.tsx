import { Card } from "@/components/ui/card";
import { LanguageControl } from "@/components/exits/LanguageControl";
import { PageHeader } from "@/components/exits/PageHeader";
import { ThemeControl } from "@/components/exits/ThemeControl";
import { useI18n } from "@/i18n/I18nProvider";

export function PreferencesPage() {
  const { t } = useI18n();

  return (
    <div className="flex min-w-0 flex-col gap-4">
      <PageHeader title={t("preferences.title")} description={t("preferences.lede")} />
      <Card className="flex flex-col gap-4">
        <LanguageControl />
        <ThemeControl />
      </Card>
    </div>
  );
}
