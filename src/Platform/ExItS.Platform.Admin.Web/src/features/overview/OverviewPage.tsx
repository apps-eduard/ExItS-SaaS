import { PageHeader } from "@/components/exits/PageHeader";
import { usePreferences } from "@/hooks/use-preferences";

export function OverviewPage() {
  const { t } = usePreferences();

  return (
    <section>
      <PageHeader title={t("nav.overview")} description={t("overview.description")} />
    </section>
  );
}
