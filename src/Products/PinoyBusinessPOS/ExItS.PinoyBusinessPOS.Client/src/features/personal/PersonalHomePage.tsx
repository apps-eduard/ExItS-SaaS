import { Link } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/exits/EmptyState";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/exits/StatusChip";
import { useI18n } from "@/i18n/I18nProvider";

export function PersonalHomePage() {
  const { t } = useI18n();

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="personal-home-page">
      <PageHeader title={t("personal.title")} description={t("personal.lede")} />
      <StatusChip tone="info">{t("personal.badge")}</StatusChip>
      <Card>
        <p className="m-0 text-[length:var(--exits-text-md)]">{t("personal.body")}</p>
      </Card>
      <EmptyState title={t("personal.emptyTitle")} detail={t("personal.emptyDetail")} />
      <div className="flex flex-wrap gap-2">
        <Button asChild className="min-h-11" data-testid="open-linked-merchants">
          <Link to="/personal/linked-merchants">{t("personal.merchantsLink")}</Link>
        </Button>
        <Button asChild variant="ghost" className="min-h-11" data-testid="open-my-orders">
          <Link to="/personal/orders">{t("personal.myOrdersLink")}</Link>
        </Button>
        <Button asChild variant="ghost" className="min-h-11">
          <Link to="/settings/preferences">{t("preferences.title")}</Link>
        </Button>
      </div>
    </div>
  );
}
