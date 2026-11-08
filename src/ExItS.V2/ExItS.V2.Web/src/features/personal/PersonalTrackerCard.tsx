import { useState } from "react";
import { useTranslation } from "react-i18next";
import { DeferredNav } from "@/features/personal/DeferredNav";
import { MaterialIcon } from "@/features/personal/MaterialIcon";
import { MoneyDisplay } from "@/features/personal/MoneyDisplay";
import { cn } from "@/lib/utils";

export type PersonalTrackerDashboard = {
  myBorrowed: number;
  myLent: number;
  sharedBorrowed: number;
  sharedLent: number;
  contactCount: number;
  activeMine: number;
  activeShared: number;
};

type TrackerMetricKey = "people" | "mine" | "shared";

function TrackerAmountCard({
  label,
  amount,
  direction,
  testId,
  labelTestId,
}: {
  label: string;
  amount: number;
  direction: "lent" | "owe";
  testId: string;
  labelTestId: string;
}) {
  const identity = direction === "lent" ? "lent" : "owe";
  return (
    <DeferredNav
      className={cn(
        "ui-standards-qcard ui-standards-qcard--settings-card utang-account-card utang-account-card--standard utang-settings-card tracker-amount-card text-fix no-underline",
        `utang-settings-card--${identity}`,
      )}
      testId={testId}
      data-shell="org-ehg"
      data-recipe={`ext org-ehg amount-card ${identity} md card-soft no-shadow`}
    >
      <div className="ui-standards-qcard__settings-ribbon">
        <div className="ui-standards-qcard__settings-tab utang-settings-card__tab">
          <span className="truncate" data-testid={labelTestId}>
            {label}
          </span>
        </div>
        <div className="utang-settings-card__content">
          <div className="utang-settings-card__meta min-w-0 flex-1">
            <div className="utang-settings-card__headline tracker-amount-card__headline">
              <span className="utang-settings-card__amount">
                <MoneyDisplay amount={amount} />
              </span>
            </div>
          </div>
        </div>
      </div>
    </DeferredNav>
  );
}

function TrackerSummaryGroup({
  title,
  mineLabel,
  sharedLabel,
  mineAmount,
  sharedAmount,
  icon,
  tone,
  rowTestId,
  mineTestId,
  sharedTestId,
  mineLabelTestId,
  sharedLabelTestId,
}: {
  title: string;
  mineLabel: string;
  sharedLabel: string;
  mineAmount: number;
  sharedAmount: number;
  icon: string;
  tone: "owed" | "owe";
  rowTestId: string;
  mineTestId: string;
  sharedTestId: string;
  mineLabelTestId: string;
  sharedLabelTestId: string;
}) {
  return (
    <div
      className={`tracker-summary tracker-summary--${tone} tracker-summary--pendant tracker-amount-group`}
      data-testid={rowTestId}
      data-recipe={`ext soft-glass pendant ${tone === "owed" ? "lent" : "owe"}`}
    >
      <div className="tracker-summary__stage">
        <div className="tracker-summary__title-row">
          <span className="tracker-summary__copy">
            <h3 className="tracker-summary__title tracker-amount-group__title m-0">
              {title}
            </h3>
          </span>
          <span
            className="tracker-summary__pendant"
            aria-hidden
            data-testid={`${rowTestId}-pendant`}
          >
            <MaterialIcon name={icon} className="tracker-summary__pendant-icon" />
          </span>
        </div>
        <span className="tracker-balance__rule" aria-hidden />
        <ul className="tracker-amount-list utang-relationship-list m-0 flex list-none flex-row p-0">
          <li>
            <TrackerAmountCard
              label={mineLabel}
              amount={mineAmount}
              direction="owe"
              testId={mineTestId}
              labelTestId={mineLabelTestId}
            />
          </li>
          <li>
            <TrackerAmountCard
              label={sharedLabel}
              amount={sharedAmount}
              direction="lent"
              testId={sharedTestId}
              labelTestId={sharedLabelTestId}
            />
          </li>
        </ul>
      </div>
    </div>
  );
}

export function PersonalTrackerCard({
  dashboard,
  metricsOutline = false,
}: {
  dashboard: PersonalTrackerDashboard;
  metricsOutline?: boolean;
}) {
  const { t } = useTranslation();
  const [metric, setMetric] = useState<TrackerMetricKey>("mine");
  const [metricsOpen, setMetricsOpen] = useState(true);

  const peopleLabel = metricsOutline
    ? t("personal.home.friends")
    : t("personal.home.people");

  const options: {
    key: TrackerMetricKey;
    label: string;
    count: number;
    testId: string;
    identity: "people" | "lent" | "owe";
  }[] = [
    {
      key: "people",
      label: peopleLabel,
      count: dashboard.contactCount,
      testId: "personal-stat-people",
      identity: "people",
    },
    {
      key: "mine",
      label: t("personal.home.activeMine"),
      count: dashboard.activeMine,
      testId: "personal-stat-active",
      identity: "lent",
    },
    {
      key: "shared",
      label: t("personal.home.sharedLedgerStat"),
      count: dashboard.activeShared,
      testId: "personal-stat-active-shared",
      identity: "owe",
    },
  ];

  return (
    <section
      className="catalog-form-section exits-animate-panel personal-section personal-tracker-card gap-3 max-md:gap-1.5"
      aria-label={t("personal.home.personalTracker")}
      data-testid="personal-utang-summary"
    >
      <div className="personal-tracker-card__header">
        <h2 className="personal-tracker-card__title" data-testid="personal-tracker-ribbon">
          <span
            className="personal-tracker-card__pennant"
            aria-hidden
            data-testid="personal-tracker-pennant"
          >
            <MaterialIcon
              name="monitoring"
              className="personal-tracker-card__pennant-icon size-[1.05rem] shrink-0"
            />
          </span>
          {t("personal.home.personalTracker")}
        </h2>
        <button
          type="button"
          className="personal-tracker-metrics-collapse"
          aria-expanded={metricsOpen}
          aria-controls="personal-tracker-metrics"
          data-testid="personal-tracker-metrics-toggle"
          onClick={() => setMetricsOpen((open) => !open)}
        >
          <span>{metricsOpen ? t("personal.home.hideMetrics") : t("personal.home.showMetrics")}</span>
          <MaterialIcon
            name={metricsOpen ? "expand_less" : "expand_more"}
            className="personal-tracker-metrics-collapse__icon"
          />
        </button>
      </div>

      <div className="tracker-summary-row">
        <TrackerSummaryGroup
          title={t("personal.home.trackerOwedTitle")}
          mineLabel={t("personal.home.myIOweRecord")}
          sharedLabel={t("personal.home.sharedOwedRecord")}
          mineAmount={dashboard.myBorrowed}
          sharedAmount={dashboard.myLent}
          icon="handshake"
          tone="owed"
          rowTestId="personal-utang-owed-to-me"
          mineTestId="personal-stat-borrowed-mine"
          sharedTestId="personal-stat-lent-mine"
          mineLabelTestId="personal-stat-borrowed-mine-label"
          sharedLabelTestId="personal-stat-lent-mine-label"
        />
        <TrackerSummaryGroup
          title={t("personal.home.trackerOweTitle")}
          mineLabel={t("personal.home.myIOweRecord")}
          sharedLabel={t("personal.home.sharedOwedRecord")}
          mineAmount={dashboard.sharedBorrowed}
          sharedAmount={dashboard.sharedLent}
          icon="account_balance_wallet"
          tone="owe"
          rowTestId="personal-utang-i-owe"
          mineTestId="personal-stat-borrowed-shared"
          sharedTestId="personal-stat-lent-shared"
          mineLabelTestId="personal-stat-borrowed-shared-label"
          sharedLabelTestId="personal-stat-lent-shared-label"
        />
      </div>

      {metricsOpen ? (
        <div
          id="personal-tracker-metrics"
          className="personal-tracker-metric-toggle personal-tracker-metric-toggle--cards"
          data-testid="personal-home-meta"
        >
          {options.map((option) => {
            const selected = metric === option.key;
            return (
              <button
                key={option.key}
                type="button"
                className={cn(
                  "personal-tracker-metric-toggle__item",
                  `personal-tracker-metric-toggle__item--${option.identity}`,
                  selected && "personal-tracker-metric-toggle__item--selected",
                )}
                data-testid={option.testId}
                aria-pressed={selected}
                onClick={() => setMetric(option.key)}
              >
                <span className="personal-tracker-metric-toggle__label">{option.label}</span>
                <strong className="personal-tracker-metric-toggle__count tabular-nums">
                  {option.count}
                </strong>
              </button>
            );
          })}
        </div>
      ) : null}
    </section>
  );
}
