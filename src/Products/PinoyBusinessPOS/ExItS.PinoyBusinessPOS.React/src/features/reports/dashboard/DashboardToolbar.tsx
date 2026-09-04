import type { ReactNode } from "react";
import { Link } from "react-router-dom";
import { RefreshCw } from "lucide-react";
import { ReportScopeControls } from "@/features/reports/ReportScopeControls";
import {
  isReportRangeValid,
  type ReportDatePreset,
  type ReportDateRangeValue,
} from "@/features/reports/report-date-range";
import type {
  ReportBranchScopeSelection,
  ReportScopeMode,
} from "@/features/reports/report-branch-scope";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";

const PRESETS: ReportDatePreset[] = ["today", "yesterday", "thisWeek", "thisMonth", "custom"];

type DashboardToolbarProps = {
  preset: ReportDatePreset;
  range: ReportDateRangeValue;
  custom: ReportDateRangeValue;
  onPresetChange: (preset: ReportDatePreset) => void;
  onCustomChange: (custom: ReportDateRangeValue) => void;
  onApply: () => void;
  loading?: boolean;
  refreshing?: boolean;
  onRefresh: () => void;
  scopeMode: ReportScopeMode;
  organizationId: string;
  currentBranchId: string | null | undefined;
  currentBranchName: string | null | undefined;
  selection: ReportBranchScopeSelection;
  onSelectionChange: (next: ReportBranchScopeSelection) => void;
  allowAllBranches: boolean;
  reportsHref?: string;
};

export function DashboardToolbar({
  preset,
  range,
  custom,
  onPresetChange,
  onCustomChange,
  onApply,
  loading = false,
  refreshing = false,
  onRefresh,
  scopeMode,
  organizationId,
  currentBranchId,
  currentBranchName,
  selection,
  onSelectionChange,
  allowAllBranches,
  reportsHref = "/reports",
}: DashboardToolbarProps) {
  const { t } = useI18n();
  const customValid = isReportRangeValid(custom);

  return (
    <div className="dashboard-toolbar" data-testid="dashboard-toolbar">
      <div className="dashboard-toolbar__row">
        <div
          className="dashboard-toolbar__presets"
          role="tablist"
          aria-label={t("reports.datePresets")}
          data-testid="report-date-presets"
        >
          {PRESETS.map((key) => (
            <button
              key={key}
              type="button"
              role="tab"
              aria-selected={preset === key}
              className={cn(
                "dashboard-toolbar__preset",
                preset === key && "dashboard-toolbar__preset--active",
              )}
              data-testid={`report-preset-${key}`}
              disabled={loading}
              onClick={() => onPresetChange(key)}
            >
              {t(`reports.preset.${key}` as "reports.preset.today")}
            </button>
          ))}
        </div>

        <div className="dashboard-toolbar__scope">
          <ReportScopeControls
            scopeMode={scopeMode}
            organizationId={organizationId}
            currentBranchId={currentBranchId}
            currentBranchName={currentBranchName}
            selection={selection}
            onSelectionChange={onSelectionChange}
            allowAllBranches={allowAllBranches}
            loading={loading}
            compact
          />
        </div>

        <div className="dashboard-toolbar__actions">
          <Link
            to={reportsHref}
            className="dashboard-toolbar__link"
            data-testid="open-reports-hub"
          >
            {t("reports.open")}
          </Link>
          <button
            type="button"
            className="dashboard-toolbar__icon-btn"
            data-testid="dashboard-refresh"
            aria-label={t("dashboard.refresh")}
            disabled={refreshing}
            onClick={onRefresh}
          >
            <RefreshCw className={cn("size-4", refreshing && "dashboard-refresh-spin")} />
          </button>
        </div>
      </div>

      <p
        className="dashboard-toolbar__range m-0"
        data-testid="dashboard-period-range"
      >
        {range.fromDate === range.toDate
          ? range.fromDate
          : `${range.fromDate} → ${range.toDate}`}
      </p>

      {preset === "custom" ? (
        <div className="dashboard-toolbar__custom" data-testid="report-custom-dates">
          <label className="dashboard-toolbar__field">
            <span>{t("reports.fromDate")}</span>
            <input
              type="date"
              className="catalog-form-input"
              value={custom.fromDate}
              disabled={loading}
              onChange={(e) => onCustomChange({ ...custom, fromDate: e.target.value })}
              data-testid="report-from-date"
            />
          </label>
          <label className="dashboard-toolbar__field">
            <span>{t("reports.toDate")}</span>
            <input
              type="date"
              className="catalog-form-input"
              value={custom.toDate}
              disabled={loading}
              onChange={(e) => onCustomChange({ ...custom, toDate: e.target.value })}
              data-testid="report-to-date"
            />
          </label>
          <button
            type="button"
            className="dashboard-toolbar__apply"
            data-testid="report-apply"
            disabled={loading || !customValid}
            onClick={onApply}
          >
            {t("reports.apply")}
          </button>
        </div>
      ) : null}
    </div>
  );
}

export function DashboardPanel({
  title,
  scopeLabel,
  scopeTestId,
  children,
  className,
  testId,
  compact,
}: {
  title: string;
  scopeLabel?: string;
  scopeTestId?: string;
  children: ReactNode;
  className?: string;
  testId?: string;
  compact?: boolean;
}) {
  return (
    <section
      className={cn(
        "dashboard-panel",
        compact && "dashboard-panel--compact",
        className,
      )}
      data-testid={testId}
    >
      <div className="dashboard-panel__header">
        <h3 className="dashboard-panel__title">{title}</h3>
        {scopeLabel ? (
          <span className="dashboard-panel__scope" data-testid={scopeTestId}>
            {scopeLabel}
          </span>
        ) : null}
      </div>
      <div className="dashboard-panel__body">{children}</div>
    </section>
  );
}

export function DashboardQuietEmpty({
  title,
  detail,
  testId,
  action,
}: {
  title: string;
  detail?: string;
  testId?: string;
  action?: ReactNode;
}) {
  return (
    <div className="dashboard-quiet-empty" data-testid={testId}>
      <p className="dashboard-quiet-empty__title m-0">{title}</p>
      {detail ? <p className="dashboard-quiet-empty__detail m-0">{detail}</p> : null}
      {action}
    </div>
  );
}
