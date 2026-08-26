import { Link } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/exits/EmptyState";
import { PageHeader } from "@/components/exits/PageHeader";
import { useI18n } from "@/i18n/I18nProvider";

export function NoAccessibleBranchPage() {
  const { t } = useI18n();

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="no-accessible-branch">
      <PageHeader title={t("noLocation.title")} description={t("noLocation.lede")} />
      <EmptyState title={t("noLocation.title")} detail={t("noLocation.detail")} />
      <Button asChild variant="ghost" className="min-h-11">
        <Link to="/settings/preferences">{t("preferences.title")}</Link>
      </Button>
    </div>
  );
}
