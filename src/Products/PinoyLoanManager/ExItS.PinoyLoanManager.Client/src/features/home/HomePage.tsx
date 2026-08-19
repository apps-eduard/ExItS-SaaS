import { ExItsMark } from "@/components/exits/ExItsMark";
import { LanguageControl } from "@/components/exits/LanguageControl";
import { PageHeader } from "@/components/exits/PageHeader";
import { ThemeControl } from "@/components/exits/ThemeControl";
import { Card } from "@/components/ui/card";
import { useI18n } from "@/i18n/I18nProvider";

export function HomePage() {
  const { t } = useI18n();

  return (
    <section className="flex max-w-md flex-col gap-6 pt-6">
      <ExItsMark size="lg" />
      <PageHeader title={t("home.title")} description={t("home.tagline")} />
      <Card className="flex flex-col gap-4">
        <LanguageControl />
        <ThemeControl />
      </Card>
    </section>
  );
}
