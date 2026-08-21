import { Link } from "react-router-dom";
import { canViewDashboard, canViewReports } from "@/access/pos-capabilities";
import { Button } from "@/components/ui/button";
import { PageHeader } from "@/components/exits/PageHeader";
import { buildOperationalReportGroups } from "@/features/reports/report-access";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

export function ReportsHubPage() {
  const { t } = useI18n();
  const { sessionGrant } = useWorkspace();
  const groups = buildOperationalReportGroups(sessionGrant);
  const showDashboard = canViewDashboard(sessionGrant);
  const showClassic = canViewReports(sessionGrant);

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="reports-hub-page">
      <PageHeader title={t("reports.title")} description={t("reports.lede")} />

      {showDashboard ? (
        <Button asChild className="min-h-11 w-fit" data-testid="reports-open-dashboard">
          <Link to="/dashboard">{t("dashboard.open")}</Link>
        </Button>
      ) : null}

      {groups.map((group) => (
        <section
          key={group.id}
          className="flex min-w-0 flex-col gap-2"
          data-testid={`reports-group-${group.id}`}
        >
          <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">
            {t(group.titleKey as MessageKey)}
          </h2>
          <div className="flex min-w-0 flex-col gap-2">
            {group.items.map((item) => (
              <Button
                key={item.path}
                asChild
                variant="ghost"
                className="min-h-11 w-full justify-start"
                data-testid={`report-link-${item.kind}`}
              >
                <Link to={item.path}>{t(item.titleKey as MessageKey)}</Link>
              </Button>
            ))}
          </div>
        </section>
      ))}

      {showClassic ? (
        <section className="flex min-w-0 flex-col gap-2" data-testid="reports-group-classic">
          <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">
            {t("reports.classicSection")}
          </h2>
          <Button
            asChild
            variant="ghost"
            className="min-h-11 w-full justify-start"
            data-testid="report-link-sales"
          >
            <Link to="/reports/sales">{t("reports.classicSales")}</Link>
          </Button>
          <Button
            asChild
            variant="ghost"
            className="min-h-11 w-full justify-start"
            data-testid="report-link-utang"
          >
            <Link to="/reports/utang">{t("reports.classicUtang")}</Link>
          </Button>
          <Button
            asChild
            variant="ghost"
            className="min-h-11 w-full justify-start"
            data-testid="report-link-inventory"
          >
            <Link to="/reports/inventory">{t("reports.classicInventory")}</Link>
          </Button>
          <Button
            asChild
            variant="ghost"
            className="min-h-11 w-full justify-start"
            data-testid="report-link-expenses"
          >
            <Link to="/reports/expenses">{t("reports.classicExpenses")}</Link>
          </Button>
        </section>
      ) : null}
    </div>
  );
}
