import { useId, useState } from "react";
import { Link } from "react-router-dom";
import { Info } from "lucide-react";
import { UnderlineTabBar } from "@/components/exits/UnderlineTabBar";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/cn";
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
  const [infoPinned, setInfoPinned] = useState(false);
  const [infoHovered, setInfoHovered] = useState(false);
  const infoId = useId();
  const infoVisible = infoPinned || infoHovered;

  return (
    <section
      className="catalog-form-section exits-animate-panel report-filters gap-3"
      data-testid="report-filters"
      aria-labelledby="report-filters-heading"
      onMouseLeave={() => setInfoHovered(false)}
    >
      <div className="report-filters__title-row flex min-w-0 items-center gap-1.5">
        <h2 id="report-filters-heading" className="catalog-form-section__title min-w-0 flex-1">
          {t("reports.filtersTitle")}
        </h2>
        <button
          type="button"
          className={cn(
            "page-header__info",
            infoVisible && "page-header__info--visible",
            infoPinned && "page-header__info--pinned",
          )}
          data-testid="report-filters-info-toggle"
          aria-label={t("pageHeader.infoToggle")}
          aria-expanded={infoVisible}
          aria-controls={infoId}
          onMouseEnter={() => setInfoHovered(true)}
          onFocus={() => setInfoHovered(true)}
          onBlur={(event) => {
            if (!event.currentTarget.contains(event.relatedTarget as Node | null)) {
              setInfoHovered(false);
            }
          }}
          onClick={() => setInfoPinned((pinned) => !pinned)}
        >
          <Info className="size-4 shrink-0" aria-hidden />
        </button>
      </div>

      <div
        id={infoId}
        className={cn(
          "page-header__description-shell",
          infoVisible && "page-header__description-shell--open",
        )}
        data-testid="report-filters-info-shell"
        aria-hidden={!infoVisible}
        onMouseEnter={() => setInfoHovered(true)}
      >
        <div className="page-header__description-clip">
          <div className="report-filters__info flex flex-col gap-2 pb-0.5">
            <p
              className="m-0 text-[length:var(--exits-text-sm)] text-muted"
              data-testid="report-timezone-note"
            >
              {t("reports.timezoneNote")}
            </p>
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
              {t("reports.branchOrgWideNote")}
            </p>
          </div>
        </div>
      </div>

      <div className="flex min-w-0 flex-col gap-1.5" data-testid="report-branch-filter">
        <span className="text-[length:var(--exits-text-sm)] font-semibold">
          {t("reports.branchLabel")}
        </span>
        <div className="flex min-w-0 flex-wrap items-center gap-2">
          <span className="text-[length:var(--exits-text-sm)]" data-testid="report-branch-value">
            {branchLabel}
          </span>
          <Button
            asChild
            variant="outline"
            className="min-h-11 w-fit"
            data-testid="report-branch-switch"
          >
            <Link to="/workspace">{t("reports.switchBranch")}</Link>
          </Button>
        </div>
      </div>

      {showDates ? (
        <>
          <UnderlineTabBar
            items={PRESETS.map((key) => ({
              key,
              label: t(`reports.preset.${key}` as "reports.preset.today"),
              testId: `report-preset-${key}`,
              disabled: loading,
            }))}
            activeKey={preset}
            onChange={(key) => onPresetChange(key as ReportDatePreset)}
            ariaLabel={t("reports.datePresets")}
            testId="report-date-presets"
          />

          {preset === "custom" ? (
            <div className="catalog-form-section__grid" data-testid="report-custom-dates">
              <label className="flex min-w-0 flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold">
                {t("reports.fromDate")}
                <input
                  type="date"
                  className="catalog-form-select font-normal"
                  value={custom.fromDate}
                  data-testid="report-from-date"
                  disabled={loading}
                  onChange={(event) => onCustomChange({ ...custom, fromDate: event.target.value })}
                />
              </label>
              <label className="flex min-w-0 flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold">
                {t("reports.toDate")}
                <input
                  type="date"
                  className="catalog-form-select font-normal"
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
