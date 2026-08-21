import { Link } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { useI18n } from "@/i18n/I18nProvider";
import type { ReportDatePreset, ReportDateRangeValue } from "@/features/reports/report-date-range";
import { isReportRangeValid } from "@/features/reports/report-date-range";

const PRESETS: ReportDatePreset[] = ["today", "yesterday", "thisWeek", "thisMonth", "custom"];

type ReportFiltersProps = {
  preset: ReportDatePreset;
  range: ReportDateRangeValue;
  custom: ReportDateRangeValue;
  branchLabel: string;
  onPresetChange: (preset: ReportDatePreset) => void;
  onCustomChange: (custom: ReportDateRangeValue) => void;
  onApply: () => void;
  loading?: boolean;
  showDates?: boolean;
};

export function ReportFilters({
  preset,
  range,
  custom,
  branchLabel,
  onPresetChange,
  onCustomChange,
  onApply,
  loading = false,
  showDates = true,
}: ReportFiltersProps) {
  const { t } = useI18n();
  const customValid = isReportRangeValid(custom);

  return (
    <section
      className="flex min-w-0 flex-col gap-3 rounded-[var(--exits-radius-md)] border border-border p-3"
      data-testid="report-filters"
      aria-labelledby="report-filters-heading"
    >
      <h2
        id="report-filters-heading"
        className="m-0 text-[length:var(--exits-text-md)] font-semibold"
      >
        {t("reports.filtersTitle")}
      </h2>

      <p
        className="m-0 text-[length:var(--exits-text-sm)] text-muted"
        data-testid="report-timezone-note"
      >
        {t("reports.timezoneNote")}
      </p>

      <div className="flex min-w-0 flex-col gap-1" data-testid="report-branch-filter">
        <span className="text-[length:var(--exits-text-sm)] font-medium">
          {t("reports.branchLabel")}
        </span>
        <div className="flex min-w-0 flex-wrap items-center gap-2">
          <span className="text-[length:var(--exits-text-sm)]" data-testid="report-branch-value">
            {branchLabel}
          </span>
          <Button
            asChild
            variant="ghost"
            className="min-h-11 w-fit"
            data-testid="report-branch-switch"
          >
            <Link to="/workspace">{t("reports.switchBranch")}</Link>
          </Button>
        </div>
        <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
          {t("reports.branchOrgWideNote")}
        </p>
      </div>

      {showDates ? (
        <>
          <div
            className="flex min-w-0 flex-wrap gap-2"
            role="group"
            aria-label={t("reports.datePresets")}
            data-testid="report-date-presets"
          >
            {PRESETS.map((key) => (
              <Button
                key={key}
                type="button"
                variant={preset === key ? "default" : "ghost"}
                className="min-h-11"
                data-testid={`report-preset-${key}`}
                disabled={loading}
                onClick={() => onPresetChange(key)}
              >
                {t(`reports.preset.${key}` as "reports.preset.today")}
              </Button>
            ))}
          </div>

          {preset === "custom" ? (
            <div
              className="grid min-w-0 grid-cols-1 gap-2 sm:grid-cols-2"
              data-testid="report-custom-dates"
            >
              <label className="flex min-w-0 flex-col gap-1 text-[length:var(--exits-text-sm)]">
                {t("reports.fromDate")}
                <input
                  type="date"
                  className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-background px-3"
                  value={custom.fromDate}
                  data-testid="report-from-date"
                  disabled={loading}
                  onChange={(event) => onCustomChange({ ...custom, fromDate: event.target.value })}
                />
              </label>
              <label className="flex min-w-0 flex-col gap-1 text-[length:var(--exits-text-sm)]">
                {t("reports.toDate")}
                <input
                  type="date"
                  className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-background px-3"
                  value={custom.toDate}
                  data-testid="report-to-date"
                  disabled={loading}
                  onChange={(event) => onCustomChange({ ...custom, toDate: event.target.value })}
                />
              </label>
              {!customValid ? (
                <p className="m-0 text-[length:var(--exits-text-sm)] text-destructive sm:col-span-2">
                  {t("reports.invalidRange")}
                </p>
              ) : null}
            </div>
          ) : (
            <p
              className="m-0 text-[length:var(--exits-text-sm)] text-muted"
              data-testid="report-active-range"
            >
              {t("reports.activeRange")}: {range.fromDate} → {range.toDate}
            </p>
          )}

          <Button
            type="button"
            className="min-h-11 w-fit"
            data-testid="report-apply-filters"
            disabled={loading || (preset === "custom" && !customValid)}
            onClick={onApply}
          >
            {t("reports.apply")}
          </Button>
        </>
      ) : null}
    </section>
  );
}
