import { useEffect, useMemo, useState } from "react";
import { useLocation } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import {
  hasOrganizationManagementAuthority,
  isPosOperationsManager,
  isPosOwnerRole,
  resolveEffectivePosRoleCode,
} from "@/access/pos-capabilities";
import { describePosApiError } from "@/access/pos-commercial-errors";
import {
  formatReportPaymentMethod,
  getExpensesReport,
  getInventoryReport,
  getSalesReport,
  getUtangReport,
} from "@/api/pos/pos-reporting-client";
import { Card } from "@/components/ui/card";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { PageHeader } from "@/components/exits/PageHeader";
import { pageBackNav } from "@/navigation/page-back-nav";
import { ReportFilters } from "@/features/reports/ReportFilters";
import { ReportScopeControls } from "@/features/reports/ReportScopeControls";
import { type ClassicReportKind } from "@/features/reports/report-access";
import {
  canSelectAllBranches,
  reportScopeModeForClassic,
  resolveReportBranchIdQuery,
  type ReportBranchScopeSelection,
} from "@/features/reports/report-branch-scope";
import {
  resolveReportDatePreset,
  type ReportDatePreset,
  type ReportDateRangeValue,
} from "@/features/reports/report-date-range";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

function kindFromPath(pathname: string): ClassicReportKind {
  if (pathname.endsWith("/utang")) {
    return "utang";
  }
  if (pathname.endsWith("/inventory")) {
    return "inventory";
  }
  if (pathname.endsWith("/expenses")) {
    return "expenses";
  }
  return "sales";
}

export function ClassicReportPage() {
  const location = useLocation();
  const kind = kindFromPath(location.pathname);
  const { t } = useI18n();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const [preset, setPreset] = useState<ReportDatePreset>("today");
  const [custom, setCustom] = useState<ReportDateRangeValue>(() =>
    resolveReportDatePreset("today"),
  );
  const [applied, setApplied] = useState<ReportDateRangeValue>(() =>
    resolveReportDatePreset("today"),
  );
  const [scopeSelection, setScopeSelection] = useState<ReportBranchScopeSelection>({
    mode: "current",
  });

  const workspace = useMemo(
    () =>
      boundWorkspace
        ? {
            organizationId: boundWorkspace.organizationId,
            branchId: boundWorkspace.branchId,
          }
        : null,
    [boundWorkspace],
  );

  const scopeMode = reportScopeModeForClassic(kind);
  const allowAll = canSelectAllBranches({
    hasOrgManagement: hasOrganizationManagementAuthority(sessionGrant),
    isOwner: isPosOwnerRole(sessionGrant),
    isManager: isPosOperationsManager(sessionGrant),
    isReportingUser: resolveEffectivePosRoleCode(sessionGrant)?.toLowerCase() === "reportinguser",
  });

  useEffect(() => {
    setScopeSelection({ mode: "current" });
  }, [workspace?.organizationId]);

  const reportBranchId = resolveReportBranchIdQuery(
    scopeMode,
    scopeSelection,
    workspace?.branchId,
  );

  const titleKeys: Record<ClassicReportKind, MessageKey> = {
    sales: "reports.classicSales",
    utang: "reports.classicUtang",
    inventory: "reports.classicInventory",
    expenses: "reports.classicExpenses",
  };

  const query = useQuery({
    queryKey: [
      "classic-report",
      kind,
      workspace?.organizationId,
      reportBranchId ?? "all",
      applied.fromDate,
      applied.toDate,
    ],
    enabled: Boolean(workspace),
    queryFn: async ({ signal }) => {
      if (kind === "sales") {
        const d = await getSalesReport(workspace!, applied, signal, reportBranchId);
        return [
          {
            label: t("reports.metric.gross"),
            value: <MoneyDisplay amount={d.completedSalesTotal} />,
          },
          {
            label: t("reports.metric.transactions"),
            value: String(d.completedSaleCount),
          },
          {
            label: t("reports.metric.voids"),
            value: (
              <>
                <MoneyDisplay amount={d.voidedSalesTotal} /> ({d.voidedSaleCount})
              </>
            ),
          },
          {
            label: t("reports.metric.utangSales"),
            value: (
              <>
                <MoneyDisplay amount={d.utangSalesTotal} /> ({d.utangSaleCount})
              </>
            ),
          },
          {
            label: t("reports.metric.commercialDiscountNote"),
            value: t("reports.commercialDiscountUnavailable"),
          },
          ...d.byPaymentMethod.map((row) => ({
            label: formatReportPaymentMethod(row.paymentMethod),
            value: (
              <>
                <MoneyDisplay amount={row.amount} /> ({row.count})
              </>
            ),
          })),
        ];
      }
      if (kind === "utang") {
        const d = await getUtangReport(workspace!, applied, signal);
        return [
          {
            label: t("reports.metric.outstanding"),
            value: <MoneyDisplay amount={d.activeCustomerOutstanding} />,
          },
          {
            label: t("reports.metric.overdue"),
            value: <MoneyDisplay amount={d.overdueAmount} />,
          },
          {
            label: t("reports.metric.utangSales"),
            value: <MoneyDisplay amount={d.productBasedUtangSalesInPeriod} />,
          },
        ];
      }
      if (kind === "inventory") {
        const d = await getInventoryReport(workspace!, applied, signal);
        return [
          { label: t("reports.metric.tracked"), value: String(d.trackedProductCount) },
          { label: t("reports.metric.lowStock"), value: String(d.lowStockProductCount) },
          { label: t("reports.metric.outOfStock"), value: String(d.outOfStockProductCount) },
        ];
      }
      const d = await getExpensesReport(workspace!, applied, signal);
      return [
        {
          label: t("reports.metric.expenses"),
          value: <MoneyDisplay amount={d.activeExpenseTotal} />,
        },
      ];
    },
  });

  function onPresetChange(next: ReportDatePreset) {
    setPreset(next);
    if (next !== "custom") {
      const range = resolveReportDatePreset(next);
      setCustom(range);
      setApplied(range);
    }
  }

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }

  const errorMessage = query.isError
    ? describePosApiError(query.error, t, "reports.loadError")
    : null;

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="classic-report-page" data-kind={kind}>
      <PageHeader
        title={t(titleKeys[kind])}
        description={t("reports.classicLede")}
        backTo={pageBackNav.reports.to}
        backLabel={t(pageBackNav.reports.labelKey)}
        backTestId="page-header-back-reports"
      />

      <ReportFilters
        preset={preset}
        range={applied}
        custom={custom}
        scopeSlot={
          <ReportScopeControls
            scopeMode={scopeMode}
            organizationId={workspace.organizationId}
            currentBranchId={workspace.branchId}
            currentBranchName={boundWorkspace?.branchName}
            selection={scopeSelection}
            onSelectionChange={setScopeSelection}
            allowAllBranches={allowAll}
            loading={query.isFetching}
          />
        }
        onPresetChange={onPresetChange}
        onCustomChange={setCustom}
        onApply={() => setApplied(resolveReportDatePreset(preset, new Date(), custom))}
        loading={query.isFetching}
      />

      {query.isLoading ? <LoadingState label={t("reports.loading")} /> : null}
      {errorMessage ? (
        <ErrorState title={t("reports.errorTitle")} detail={errorMessage} />
      ) : null}
      {query.data ? (
        <Card className="flex flex-col gap-2 p-4" data-testid="classic-report-results">
          {query.data.map((row) => (
            <div
              key={row.label}
              className="flex min-w-0 items-baseline justify-between gap-3 border-b border-border/60 py-2 last:border-0"
            >
              <span className="text-[length:var(--exits-text-sm)] text-muted">{row.label}</span>
              <span className="text-right font-semibold">{row.value}</span>
            </div>
          ))}
        </Card>
      ) : null}
    </div>
  );
}
