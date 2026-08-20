import { Link } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/exits/EmptyState";
import { PageHeader } from "@/components/exits/PageHeader";
import { useI18n } from "@/i18n/I18nProvider";

export function SellAccessDeniedPage() {
  const { t } = useI18n();

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="sell-access-denied">
      <PageHeader title={t("sell.accessDeniedTitle")} description={t("sell.accessDeniedDetail")} />
      <EmptyState title={t("sell.accessDeniedTitle")} detail={t("sell.accessDeniedDetail")} />
      <Button asChild variant="ghost">
        <Link to="/">{t("notFound.home")}</Link>
      </Button>
    </div>
  );
}
