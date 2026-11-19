import { useMemo, useState, type ReactNode } from "react";
import { Link } from "react-router-dom";
import type { LucideIcon } from "lucide-react";
import { Activity, Share2, Users, Wallet } from "lucide-react";
import type { PersonalDashboardDto } from "@/api/platform/personal-dashboard-client";
import { ExitsOrgEhgSelect } from "@/components/exits/ExitsOrgEhgSelect";
import { SoftGlassDateField } from "@/components/exits/SoftGlassDateField";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { PersonalHomeKpiSpark } from "@/features/personal/PersonalHomeKpiSpark";
import {
  PERSONAL_HOME_KPI_DATE_PRESETS,
  PERSONAL_HOME_KPI_DEFAULT_PRESET,
  resolvePersonalHomeKpiDatePreset,
  type PersonalHomeKpiDatePreset,
} from "@/features/personal/personal-home-kpi-date-range";
import type { PersonalHomeKpiSeriesKey } from "@/features/personal/personal-home-kpi-balance-history";
import { usePersonalHomeKpiSparklines } from "@/features/personal/use-personal-home-kpi-sparklines";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey, TranslateFn } from "@/i18n/messages";
import type { ReportDateRangeValue } from "@/features/reports/report-date-range";
import { personalHomeKpiPeriodDeltaView } from "@/features/personal/personal-home-kpi-period-delta";
import { cn } from "@/lib/cn";

type PersonalHomeKpiIdentity = "lent" | "owe";

type PersonalHomeKpiMetaScope = "my-record" | "shared";

type PersonalHomeKpiCardConfig = {
  identity: PersonalHomeKpiIdentity;
  primaryLabelKey: MessageKey;
  secondaryLabelKey: MessageKey;
  metaScope: PersonalHomeKpiMetaScope;
  to: string;
  testId: string;
  icon: LucideIcon;
  metric: ReactNode;
  scope: "my-record" | "shared";
  seriesKey: PersonalHomeKpiSeriesKey;
};

export function formatPersonalHomeKpiActiveMeta(
  t: TranslateFn,
  metaScope: PersonalHomeKpiMetaScope,
  count: number,
): string {
  if (count === 0) {
    return metaScope === "my-record"
      ? t("personal.home.kpiMyRecordsActiveNone")
      : t("personal.home.kpiSharedActiveNone");
  }
  if (count === 1) {
    return metaScope === "my-record"
      ? t("personal.home.kpiMyRecordsActiveOne")
      : t("personal.home.kpiSharedActiveOne");
  }
  const manyKey =
    metaScope === "my-record"
      ? "personal.home.kpiMyRecordsActiveMany"
      : "personal.home.kpiSharedActiveMany";
  return t(manyKey).replace("{count}", String(count));
}

function PersonalHomeKpiCard({
  card,
  meta,
  sparkValues,
  periodOpening,
  showSpark,
}: {
  card: PersonalHomeKpiCardConfig;
  meta: ReactNode;
  sparkValues: number[] | null;
  periodOpening: number | null;
  showSpark: boolean;
}) {
  const { t } = useI18n();
  const Icon = card.icon;
  const primaryLabel = t(card.primaryLabelKey);
  const secondaryLabel = t(card.secondaryLabelKey);
  const periodTrend = personalHomeKpiPeriodDeltaView(sparkValues, card.identity, periodOpening);

  return (
    <Link
      to={card.to}
      className={cn("personal-home-kpi-card", `personal-home-kpi-card--${card.identity}`)}
      data-testid={card.testId}
      data-identity={card.identity}
      data-scope={card.scope}
      aria-label={`${primaryLabel}, ${secondaryLabel}`}
    >
      {periodTrend.visible ? (
        <span
          className={cn(
            "personal-home-kpi-card__trend",
            `personal-home-kpi-card__trend--${periodTrend.tone}`,
          )}
          data-testid={`${card.testId}-trend`}
        >
          {periodTrend.label}
        </span>
      ) : null}
      <div className="personal-home-kpi-card__main">
        <div className="personal-home-kpi-card__head">
          <span className="personal-home-kpi-card__chip" aria-hidden>
            <Icon className="personal-home-kpi-card__chip-icon" strokeWidth={2.25} />
          </span>
          <span className="personal-home-kpi-card__labels">
            <span className="personal-home-kpi-card__label-scope">{primaryLabel}</span>
            <span className="personal-home-kpi-card__label-role">{secondaryLabel}</span>
          </span>
        </div>
        <p className="personal-home-kpi-card__metric m-0 tabular-nums">{card.metric}</p>
        <p className="personal-home-kpi-card__meta m-0">{meta}</p>
      </div>
      {showSpark && sparkValues && sparkValues.length > 0 ? (
        <PersonalHomeKpiSpark values={sparkValues} testId={`${card.testId}-spark`} />
      ) : null}
    </Link>
  );
}

export const PERSONAL_HOME_KPI_TEST_IDS = {
  strip: "personal-home-kpi-strip",
  trendRange: "personal-home-kpi-trend-range",
  myRecordLent: "personal-home-kpi-my-record-lent",
  myRecordOwe: "personal-home-kpi-my-record-owe",
  sharedLent: "personal-home-kpi-shared-lent",
  sharedOwe: "personal-home-kpi-shared-owe",
} as const;

export function PersonalHomeKpiStrip({ dashboard }: { dashboard: PersonalDashboardDto }) {
  const { t } = useI18n();
  const [preset, setPreset] = useState<PersonalHomeKpiDatePreset>(PERSONAL_HOME_KPI_DEFAULT_PRESET);
  const [customRange, setCustomRange] = useState<ReportDateRangeValue>(() =>
    resolvePersonalHomeKpiDatePreset(PERSONAL_HOME_KPI_DEFAULT_PRESET),
  );

  const range = useMemo(
    () => resolvePersonalHomeKpiDatePreset(preset, new Date(), customRange),
    [customRange, preset],
  );

  const { series, periodOpening, activeCounts, loading } = usePersonalHomeKpiSparklines(
    dashboard,
    range,
  );

  const presetOptions = useMemo(
    () =>
      PERSONAL_HOME_KPI_DATE_PRESETS.map((key) => ({
        value: key,
        label: t(`personal.home.kpiRange.${key}` as "personal.home.kpiRange.thisWeek"),
      })),
    [t],
  );

  const cards: PersonalHomeKpiCardConfig[] = [
    {
      identity: "lent",
      scope: "my-record",
      seriesKey: "my-record-lent",
      primaryLabelKey: "personal.home.kpiPrimaryLent",
      secondaryLabelKey: "personal.home.kpiScopeMyRecord",
      metaScope: "my-record",
      to: "/personal/utang/lent",
      testId: PERSONAL_HOME_KPI_TEST_IDS.myRecordLent,
      icon: Wallet,
      metric: (
        <MoneyDisplay
          amount={dashboard.totalLentBalance}
          className="personal-home-kpi-card__money"
        />
      ),
    },
    {
      identity: "owe",
      scope: "my-record",
      seriesKey: "my-record-owe",
      primaryLabelKey: "personal.home.kpiPrimaryOwe",
      secondaryLabelKey: "personal.home.kpiScopeMyRecord",
      metaScope: "my-record",
      to: "/personal/utang/owe",
      testId: PERSONAL_HOME_KPI_TEST_IDS.myRecordOwe,
      icon: Activity,
      metric: (
        <MoneyDisplay
          amount={dashboard.totalBorrowedBalance}
          className="personal-home-kpi-card__money"
        />
      ),
    },
    {
      identity: "lent",
      scope: "shared",
      seriesKey: "shared-lent",
      primaryLabelKey: "personal.home.kpiPrimaryLent",
      secondaryLabelKey: "personal.home.kpiScopeShared",
      metaScope: "shared",
      to: "/personal/utang",
      testId: PERSONAL_HOME_KPI_TEST_IDS.sharedLent,
      icon: Share2,
      metric: (
        <MoneyDisplay
          amount={dashboard.sharedWithMeLentBalance ?? 0}
          className="personal-home-kpi-card__money"
        />
      ),
    },
    {
      identity: "owe",
      scope: "shared",
      seriesKey: "shared-owe",
      primaryLabelKey: "personal.home.kpiPrimaryOwe",
      secondaryLabelKey: "personal.home.kpiScopeShared",
      metaScope: "shared",
      to: "/personal/utang",
      testId: PERSONAL_HOME_KPI_TEST_IDS.sharedOwe,
      icon: Users,
      metric: (
        <MoneyDisplay
          amount={dashboard.sharedWithMeBorrowedBalance ?? 0}
          className="personal-home-kpi-card__money"
        />
      ),
    },
  ];

  return (
    <div className="personal-home-kpi-strip-shell" data-testid="personal-home-kpi-strip-shell">
      <div className="personal-home-kpi-strip__toolbar">
        <p className="personal-home-kpi-strip__hint m-0">{t("personal.home.kpiTrendRangeHint")}</p>
        <div className="personal-home-kpi-strip__range">
          <ExitsOrgEhgSelect
            label={t("personal.home.kpiTrendRangeLabel")}
            value={preset}
            options={presetOptions}
            onChange={(value) => {
              if (value) {
                setPreset(value as PersonalHomeKpiDatePreset);
              }
            }}
            testId={PERSONAL_HOME_KPI_TEST_IDS.trendRange}
            className="personal-home-kpi-strip__range-select"
          />
        </div>
        {preset === "custom" ? (
          <div
            className="personal-home-kpi-strip__custom"
            data-testid="personal-home-kpi-custom-range"
          >
            <SoftGlassDateField
              label={t("reports.fromDate")}
              value={customRange.fromDate}
              maxDate={customRange.toDate}
              onChange={(fromDate) => setCustomRange((current) => ({ ...current, fromDate }))}
              testId="personal-home-kpi-custom-from"
            />
            <SoftGlassDateField
              label={t("reports.toDate")}
              value={customRange.toDate}
              minDate={customRange.fromDate}
              onChange={(toDate) => setCustomRange((current) => ({ ...current, toDate }))}
              testId="personal-home-kpi-custom-to"
            />
          </div>
        ) : null}
      </div>

      <section
        className="personal-home-kpi-strip"
        aria-label={t("personal.home.kpiStripAria")}
        data-testid={PERSONAL_HOME_KPI_TEST_IDS.strip}
      >
        {cards.map((card) => {
          const sparkValues = loading ? null : series[card.seriesKey];
          const opening = loading ? null : periodOpening[card.seriesKey];
          const showSpark = sparkValues !== null && sparkValues.length > 0;
          const meta = formatPersonalHomeKpiActiveMeta(
            t,
            card.metaScope,
            activeCounts[card.seriesKey],
          );
          return (
            <PersonalHomeKpiCard
              key={card.testId}
              card={card}
              meta={meta}
              sparkValues={sparkValues}
              periodOpening={opening}
              showSpark={showSpark}
            />
          );
        })}
      </section>
    </div>
  );
}