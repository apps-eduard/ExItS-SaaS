import { Link } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/exits/EmptyState";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/exits/StatusChip";
import { useI18n } from "@/i18n/I18nProvider";

export function OrgEssentialsPage() {
  const { t } = useI18n();

  return (
    <div className="flex min-w-0 flex-col gap-4">
      <PageHeader title={t("org.title")} description={t("org.lede")} />
      <StatusChip tone="warning">{t("org.badge")}</StatusChip>
      <Card>
        <p className="m-0 text-[length:var(--exits-text-md)]">{t("org.body")}</p>
        <p className="mt-3 mb-0 text-[length:var(--exits-text-sm)] text-muted">
          {t("org.noPayChrome")}
        </p>
      </Card>
      <EmptyState title={t("org.emptyTitle")} detail={t("org.emptyDetail")} />
      <Button asChild variant="ghost">
        <Link to="/workspace">{t("workspace.switch")}</Link>
      </Button>
    </div>
  );
}
