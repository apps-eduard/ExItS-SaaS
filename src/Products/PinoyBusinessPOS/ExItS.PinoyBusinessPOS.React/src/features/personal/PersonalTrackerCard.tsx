import type { ReactNode } from "react";
import { Link } from "react-router-dom";
import type { PersonalDashboardDto } from "@/api/platform/personal-dashboard-client";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { symbol, type IconComponent } from "@/components/icons/MaterialSymbol";
import { useI18n } from "@/i18n/I18nProvider";
import { ORG_EHG_QA_GLASS_CLASS_NAME } from "@/features/ui-standards/org-enterprise-hybrid-glass/org-ehg-qa-glass-global-default";
import { cn } from "@/lib/cn";

const GroupIcon = symbol("group");
const HandshakeIcon = symbol("handshake");
const MonitoringIcon = symbol("monitoring");
const ShareIcon = symbol("share");
const WalletIcon = symbol("account_balance_wallet");

/** Compact utang settings card: ribbon label and amount. */
export function TrackerAmountCard({
  label,
  amount,
  to,
  direction,
  testId,
  labelTestId,
}: {
  label: string;
  amount: number;
  to: string;
  direction: "lent" | "owe";
  testId: string;
  labelTestId: string;
}) {
  const lent = direction === "lent";
  const identity = lent ? "lent" : "owe";
  return (
    <Link
      to={to}
      className={cn(
        "ui-standards-qcard ui-standards-qcard--settings-card utang-account-card utang-account-card--standard utang-settings-card tracker-amount-card text-foreground no-underline",
        `utang-settings-card--${identity}`,
        "personal-qa-glass--md personal-qa-glass--card-soft personal-qa-glass--no-shadow",
      )}
      data-testid={testId}
      data-recipe={`ext org-ehg qa-glass ${identity} md card-soft no-shadow`}
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
    </Link>
  );
}

function TrackerSummaryGroup({
  title,
  mineLabel,
  sharedLabel,
  mineAmount,
  sharedAmount,
  mineTo,
  sharedTo,
  icon: Icon,
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
  mineTo: string;
  sharedTo: string;
  icon: IconComponent;
  tone: "owed" | "owe";
  rowTestId: string;
  mineTestId: string;
  sharedTestId: string;
  mineLabelTestId: string;
  sharedLabelTestId: string;
}) {
  return (
    <div
      className={`tracker-summary tracker-summary--${tone} tracker-summary--pendant tracker-amount-group personal-qa-glass--md personal-qa-glass--card-soft personal-qa-glass--no-shadow`}
      data-testid={rowTestId}
      data-recipe={`ext org-ehg qa-glass ${tone === "owed" ? "lent" : "owe"} md card-soft no-shadow`}
    >
      <div className="tracker-summary__stage">
        <div className="tracker-summary__title-row">
          <span className="tracker-summary__copy">
            <h3 className="tracker-summary__title tracker-amount-group__title m-0">{title}</h3>
          </span>
          <span
            className="tracker-summary__pendant"
            aria-hidden
            data-testid={`${rowTestId}-pendant`}
          >
            <Icon className="tracker-summary__pendant-icon" />
          </span>
        </div>
        <span className="tracker-balance__rule" aria-hidden />
        <ul className="tracker-amount-list utang-relationship-list m-0 flex list-none flex-row p-0">
          <li>
            <TrackerAmountCard
              label={mineLabel}
              amount={mineAmount}
              to={mineTo}
              direction="owe"
              testId={mineTestId}
              labelTestId={mineLabelTestId}
            />
          </li>
          <li>
            <TrackerAmountCard
              label={sharedLabel}
              amount={sharedAmount}
              to={sharedTo}
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

export type PersonalTrackerTestIds = {
  section: string;
  ribbon: string;
  pennant: string;
  myLedger: string;
  myLedgerOwe: string;
  myLedgerOweLabel: string;
  myLedgerLent: string;
  myLedgerLentLabel: string;
  sharedLedger: string;
  sharedLedgerOwe: string;
  sharedLedgerOweLabel: string;
  sharedLedgerLent: string;
  sharedLedgerLentLabel: string;
  meta: string;
  people: string;
  activeMine: string;
  activeShared: string;
};

export const PERSONAL_HOME_TRACKER_TEST_IDS: PersonalTrackerTestIds = {
  section: "personal-utang-summary",
  ribbon: "personal-tracker-ribbon",
  pennant: "personal-tracker-pennant",
  myLedger: "personal-utang-owed-to-me",
  myLedgerOwe: "personal-stat-borrowed-mine",
  myLedgerOweLabel: "personal-stat-borrowed-mine-label",
  myLedgerLent: "personal-stat-lent-mine",
  myLedgerLentLabel: "personal-stat-lent-mine-label",
  sharedLedger: "personal-utang-i-owe",
  sharedLedgerOwe: "personal-stat-borrowed-shared",
  sharedLedgerOweLabel: "personal-stat-borrowed-shared-label",
  sharedLedgerLent: "personal-stat-lent-shared",
  sharedLedgerLentLabel: "personal-stat-lent-shared-label",
  meta: "personal-home-meta",
  people: "personal-stat-people",
  activeMine: "personal-stat-active",
  activeShared: "personal-stat-active-shared",
};

/** Personal Tracker — My Record / Shared with Me groups of I owed / I lent amount cards. */
export function PersonalTrackerCard({
  dashboard,
  testIds = PERSONAL_HOME_TRACKER_TEST_IDS,
  footer,
  metricsOutline = false,
}: {
  dashboard: PersonalDashboardDto;
  testIds?: PersonalTrackerTestIds;
  footer?: ReactNode;
  /** Home page uses outline index cards. */
  metricsOutline?: boolean;
}) {
  const { t } = useI18n();
  /** Global Quick Action lock — Org EHG (Stores to pay). */
  const metricClass = (identity: "people" | "lent" | "owe") =>
    cn("personal-qa-glass", `personal-qa-glass--${identity}`, ORG_EHG_QA_GLASS_CLASS_NAME);
  const metricRecipe = (identity: "people" | "lent" | "owe") =>
    `ext org-ehg qa-glass ${identity} md icon-soft card-soft trail-count no-shadow no-icon-shadow`;

  return (
    <section
      className="catalog-form-section exits-animate-panel personal-section personal-tracker-card gap-3 max-md:gap-1.5"
      aria-label={t("personal.home.personalTracker")}
      data-testid={testIds.section}
    >
      <div className="personal-tracker-card__header">
        <h2 className="personal-tracker-card__title" data-testid={testIds.ribbon}>
          <span className="personal-tracker-card__pennant" aria-hidden data-testid={testIds.pennant}>
            <MonitoringIcon className="personal-tracker-card__pennant-icon size-[1.05rem] shrink-0" />
          </span>
          {t("personal.home.personalTracker")}
        </h2>
      </div>
      <div className="tracker-summary-row">
        <TrackerSummaryGroup
          title={t("personal.home.trackerOwedTitle")}
          mineLabel={t("personal.home.myIOweRecord")}
          sharedLabel={t("personal.home.sharedOwedRecord")}
          mineAmount={dashboard.totalBorrowedBalance}
          sharedAmount={dashboard.totalLentBalance}
          mineTo="/personal/utang/owe"
          sharedTo="/personal/utang/lent"
          icon={HandshakeIcon}
          tone="owed"
          rowTestId={testIds.myLedger}
          mineTestId={testIds.myLedgerOwe}
          sharedTestId={testIds.myLedgerLent}
          mineLabelTestId={testIds.myLedgerOweLabel}
          sharedLabelTestId={testIds.myLedgerLentLabel}
        />
        <TrackerSummaryGroup
          title={t("personal.home.trackerOweTitle")}
          mineLabel={t("personal.home.myIOweRecord")}
          sharedLabel={t("personal.home.sharedOwedRecord")}
          mineAmount={dashboard.sharedWithMeBorrowedBalance}
          sharedAmount={dashboard.sharedWithMeLentBalance}
          mineTo="/personal/utang/owe"
          sharedTo="/personal/utang/lent"
          icon={WalletIcon}
          tone="owe"
          rowTestId={testIds.sharedLedger}
          mineTestId={testIds.sharedLedgerOwe}
          sharedTestId={testIds.sharedLedgerLent}
          mineLabelTestId={testIds.sharedLedgerOweLabel}
          sharedLabelTestId={testIds.sharedLedgerLentLabel}
        />
      </div>
      <div className="personal-home-metrics" data-testid={testIds.meta}>
        <Link
          to="/personal/people"
          className={metricClass("people")}
          data-testid={testIds.people}
          data-shell="org-ehg"
          data-recipe={metricRecipe("people")}
        >
          <span className="personal-qa-glass__icon personal-qa-glass__icon--soft" aria-hidden="true">
            <GroupIcon className="personal-tracker-card__pennant-icon size-[0.95rem] shrink-0" />
          </span>
          <span className="personal-qa-glass__label">
            {metricsOutline ? t("personal.home.friends") : t("personal.home.people")}
          </span>
          <strong className="personal-qa-glass__badge tabular-nums">{dashboard.contactCount}</strong>
        </Link>
        <Link
          to="/personal/utang"
          className={metricClass("lent")}
          data-testid={testIds.activeMine}
          data-shell="org-ehg"
          data-recipe={metricRecipe("lent")}
        >
          <span className="personal-qa-glass__icon personal-qa-glass__icon--soft" aria-hidden="true">
            <HandshakeIcon className="personal-tracker-card__pennant-icon size-[0.95rem] shrink-0" />
          </span>
          <span className="personal-qa-glass__label">{t("personal.home.activeMine")}</span>
          <strong className="personal-qa-glass__badge tabular-nums">
            {dashboard.activeRelationshipCount}
          </strong>
        </Link>
        <Link
          to="/personal/utang"
          className={metricClass("owe")}
          data-testid={testIds.activeShared}
          data-shell="org-ehg"
          data-recipe={metricRecipe("owe")}
        >
          <span className="personal-qa-glass__icon personal-qa-glass__icon--soft" aria-hidden="true">
            <ShareIcon className="personal-tracker-card__pennant-icon size-[0.95rem] shrink-0" />
          </span>
          <span className="personal-qa-glass__label">{t("personal.home.sharedLedgerStat")}</span>
          <strong className="personal-qa-glass__badge tabular-nums">
            {dashboard.sharedWithMeActiveCount ?? 0}
          </strong>
        </Link>
      </div>
      {footer}
    </section>
  );
}
