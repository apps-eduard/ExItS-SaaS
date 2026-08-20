import { Link } from "react-router-dom";
import { canInviteOrganizationStaff, canCreateSale } from "@/access/pos-capabilities";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/exits/EmptyState";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/exits/StatusChip";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

export function OrgEssentialsPage() {
  const { t } = useI18n();
  const { sessionGrant } = useWorkspace();
  const canInvite = canInviteOrganizationStaff(sessionGrant);
  const canSell = canCreateSale(sessionGrant);

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="org-essentials-page">
      <PageHeader title={t("org.title")} description={t("org.lede")} />
      <StatusChip tone="warning">{t("org.badge")}</StatusChip>
      <Card>
        <p className="m-0 text-[length:var(--exits-text-md)]">{t("org.body")}</p>
        <p className="mt-3 mb-0 text-[length:var(--exits-text-sm)] text-muted">
          {t("org.noPayChrome")}
        </p>
      </Card>
      <EmptyState title={t("org.emptyTitle")} detail={t("org.emptyDetail")} />
      <div className="flex flex-wrap gap-2">
        {canInvite ? (
          <Button asChild className="min-h-11">
            <Link to="/org/staff/invite">{t("staffInvite.title")}</Link>
          </Button>
        ) : null}
        {canSell ? (
          <Button asChild variant="ghost" className="min-h-11">
            <Link to="/sell">{t("experience.startSelling")}</Link>
          </Button>
        ) : null}
        <Button asChild variant="ghost" className="min-h-11">
          <Link to="/workspace">{t("workspace.switch")}</Link>
        </Button>
      </div>
    </div>
  );
}
