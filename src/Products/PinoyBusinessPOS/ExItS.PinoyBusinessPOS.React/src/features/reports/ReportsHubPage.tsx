import type { ReactNode } from "react";
import { LayoutDashboard } from "lucide-react";
import { canViewDashboard, canViewReports } from "@/access/pos-capabilities";
import { PageHeader } from "@/components/exits/PageHeader";
import { pageBackNav } from "@/navigation/page-back-nav";
import {
  buildOperationalReportGroups,
  type ClassicReportKind,
} from "@/features/reports/report-access";
import { ReportHubCard, ReportHubCardGrid } from "@/features/reports/ReportHubCard";
import {
  iconForClassicReport,
  iconForOperationalReport,
} from "@/features/reports/report-hub-icons";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

const CLASSIC_REPORT_LINKS: ReadonlyArray<{
  kind: ClassicReportKind;
  path: string;
  titleKey: MessageKey;
  detailKey: MessageKey;
  testId: string;
}> = [
  {
    kind: "sales",
    path: "/reports/sales",
    titleKey: "reports.hub.salesTitle",
    detailKey: "reports.hub.salesDetail",
    testId: "report-link-sales",
  },
  {
    kind: "utang",
    path: "/reports/utang",
    titleKey: "reports.hub.utangTitle",
    detailKey: "reports.hub.utangDetail",
    testId: "report-link-utang",
  },
  {
    kind: "inventory",
    path: "/reports/inventory",
    titleKey: "reports.hub.inventoryTitle",
    detailKey: "reports.hub.inventoryDetail",
    testId: "report-link-inventory",
  },
  {
    kind: "expenses",
    path: "/reports/expenses",
    titleKey: "reports.hub.expensesTitle",
    detailKey: "reports.hub.expensesDetail",
    testId: "report-link-expenses",
  },
];

function HubSection({
  title,
  children,
  testId,
}: {
  title: string;
  children: ReactNode;
  testId?: string;
}) {
  return (
    <section className="reports-hub-section" data-testid={testId}>
      <h2 className="reports-hub-section__title exits-type-section-title m-0">{title}</h2>
      {children}
    </section>
  );
}

export function ReportsHubPage() {
  const { t } = useI18n();
  const { sessionGrant } = useWorkspace();
  const groups = buildOperationalReportGroups(sessionGrant);
  const showDashboard = canViewDashboard(sessionGrant);
  const showClassic = canViewReports(sessionGrant);

  return (
    <div className="reports-hub-page exits-page" data-testid="reports-hub-page">
      <PageHeader
        title={t("reports.title")}
        description={t("reports.lede")}
        backTo={pageBackNav.managerHome.to}
        backLabel={t(pageBackNav.managerHome.labelKey)}
        backTestId="page-header-back-reports"
      />

      {showDashboard ? (
        <HubSection title={t("reports.overview")} testId="reports-group-overview">
          <ReportHubCardGrid testId="reports-overview-grid" className="reports-hub-grid--featured">
            <ReportHubCard
              to="/dashboard"
              title={t("dashboard.open")}
              description={t("reports.hub.dashboardDetail")}
              icon={LayoutDashboard}
              testId="reports-open-dashboard"
              featured
            />
          </ReportHubCardGrid>
        </HubSection>
      ) : null}

      {showClassic ? (
        <HubSection title={t("reports.classicSection")} testId="reports-group-classic">
          <ReportHubCardGrid testId="reports-classic-grid">
            {CLASSIC_REPORT_LINKS.map((item) => (
              <ReportHubCard
                key={item.kind}
                to={item.path}
                title={t(item.titleKey)}
                description={t(item.detailKey)}
                icon={iconForClassicReport(item.kind)}
                testId={item.testId}
              />
            ))}
          </ReportHubCardGrid>
        </HubSection>
      ) : null}

      {groups.map((group) => (
        <HubSection
          key={group.id}
          title={t(group.titleKey as MessageKey)}
          testId={`reports-group-${group.id}`}
        >
          <ReportHubCardGrid testId={`reports-grid-${group.id}`}>
            {group.items.map((item) => (
              <ReportHubCard
                key={item.kind}
                to={item.path}
                title={t(item.titleKey as MessageKey)}
                icon={iconForOperationalReport(item.kind)}
                testId={`report-link-${item.kind}`}
              />
            ))}
          </ReportHubCardGrid>
        </HubSection>
      ))}
    </div>
  );
}
