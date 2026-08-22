import type { ReactNode } from "react";
import { LayoutDashboard } from "lucide-react";
import { canViewDashboard, canViewReports } from "@/access/pos-capabilities";
import { ActionTileGrid, type ActionTileDef } from "@/components/exits/ActionTileGrid";
import { PageHeader } from "@/components/exits/PageHeader";
import { pageBackNav } from "@/navigation/page-back-nav";
import {
  buildOperationalReportGroups,
  type ClassicReportKind,
} from "@/features/reports/report-access";
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
  testId: string;
}> = [
  {
    kind: "sales",
    path: "/reports/sales",
    titleKey: "reports.classicSales",
    testId: "report-link-sales",
  },
  {
    kind: "utang",
    path: "/reports/utang",
    titleKey: "reports.classicUtang",
    testId: "report-link-utang",
  },
  {
    kind: "inventory",
    path: "/reports/inventory",
    titleKey: "reports.classicInventory",
    testId: "report-link-inventory",
  },
  {
    kind: "expenses",
    path: "/reports/expenses",
    titleKey: "reports.classicExpenses",
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
    <section className="flex min-w-0 flex-col gap-2" data-testid={testId}>
      <h2 className="m-0 text-[length:var(--exits-text-sm)] font-semibold text-foreground">
        {title}
      </h2>
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

  const dashboardTiles: ActionTileDef[] = showDashboard
    ? [
        {
          key: "dashboard",
          label: t("dashboard.open"),
          icon: LayoutDashboard,
          testId: "reports-open-dashboard",
          to: "/dashboard",
          primary: true,
        },
      ]
    : [];

  const classicTiles: ActionTileDef[] = showClassic
    ? CLASSIC_REPORT_LINKS.map((item) => ({
        key: item.kind,
        label: t(item.titleKey),
        icon: iconForClassicReport(item.kind),
        testId: item.testId,
        to: item.path,
      }))
    : [];

  return (
    <div
      className="mx-auto flex w-full max-w-2xl min-w-0 flex-col gap-5"
      data-testid="reports-hub-page"
    >
      <PageHeader
        title={t("reports.title")}
        description={t("reports.lede")}
        backTo={pageBackNav.managerHome.to}
        backLabel={t(pageBackNav.managerHome.labelKey)}
        backTestId="page-header-back-reports"
      />

      {dashboardTiles.length > 0 ? <ActionTileGrid tiles={dashboardTiles} /> : null}

      {groups.map((group) => (
        <HubSection
          key={group.id}
          title={t(group.titleKey as MessageKey)}
          testId={`reports-group-${group.id}`}
        >
          <ActionTileGrid
            tiles={group.items.map((item) => ({
              key: item.kind,
              label: t(item.titleKey as MessageKey),
              icon: iconForOperationalReport(item.kind),
              testId: `report-link-${item.kind}`,
              to: item.path,
            }))}
          />
        </HubSection>
      ))}

      {classicTiles.length > 0 ? (
        <HubSection title={t("reports.classicSection")} testId="reports-group-classic">
          <ActionTileGrid tiles={classicTiles} />
        </HubSection>
      ) : null}
    </div>
  );
}
