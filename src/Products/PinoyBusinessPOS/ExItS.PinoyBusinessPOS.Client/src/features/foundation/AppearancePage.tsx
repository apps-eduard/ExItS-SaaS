import { LanguageControl } from "@/components/exits/LanguageControl";
import { PageHeader } from "@/components/exits/PageHeader";
import { ThemeControl } from "@/components/exits/ThemeControl";
import { Card } from "@/components/ui/card";
import { useI18n } from "@/i18n/I18nProvider";

export function AppearancePage() {
  const { t } = useI18n();

  return (
    <div className="flex flex-col gap-5">
      <PageHeader title={t("appearance.title")} subtitle={t("appearance.subtitle")} />
      <Card className="flex flex-col gap-6">
        <LanguageControl />
        <ThemeControl />
      </Card>
    </div>
  );
}
