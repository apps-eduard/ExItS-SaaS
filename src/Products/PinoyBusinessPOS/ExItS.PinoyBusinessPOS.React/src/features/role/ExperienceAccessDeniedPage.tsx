import { Link } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/exits/EmptyState";
import { PageHeader } from "@/components/exits/PageHeader";
import { useI18n } from "@/i18n/I18nProvider";

export function ExperienceAccessDeniedPage({ testId }: { testId: string }) {
  const { t } = useI18n();

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid={testId}>
      <PageHeader title={t("experience.deniedTitle")} description={t("experience.deniedDetail")} />
      <EmptyState title={t("experience.deniedTitle")} detail={t("experience.deniedDetail")} />
      <Button asChild variant="ghost">
        <Link to="/">{t("notFound.home")}</Link>
      </Button>
    </div>
  );
}
