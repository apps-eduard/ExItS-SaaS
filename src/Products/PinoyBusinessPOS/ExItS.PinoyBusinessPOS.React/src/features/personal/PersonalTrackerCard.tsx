import type { ReactNode } from "react";
import { Link } from "react-router-dom";
import type { PersonalDashboardDto } from "@/api/platform/personal-dashboard-client";
import { symbol, type IconComponent } from "@/components/icons/MaterialSymbol";
import { useI18n } from "@/i18n/I18nProvider";
import { formatPeso } from "@/lib/format-money";

const GroupIcon = symbol("group");
const HandshakeIcon = symbol("handshake");
const MonitoringIcon = symbol("monitoring");
const ShareIcon = symbol("share");
const WalletIcon = symbol("account_balance_wallet");

function pesoLabel(amount: number): string {
  return formatPeso(amount).replace("₱", "₱ ");
}

function TrackerSummaryCard({
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
  showDivider,
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
  showDivider?: boolean;
}) {
  return (
    <div className={`tracker-summary tracker-summary--${tone} tracker-summary--pendant`} data-testid={rowTestId}>
      <span className="tracker-summary__pendant" aria-hidden data-testid={`${rowTestId}-pendant`}>
        <Icon className="tracker-summary__pendant-icon" />
      </span>
      <div className="tracker-summary__stage">
        <span className="tracker-summary__copy">
          <span className="tracker-summary__title">{title}</span>
        </span>
        <span className="tracker-balance__rule" aria-hidden />
        <span className="tracker-summary__foot">
          <Link to={mineTo} className="tracker-summary__stat" data-testid={mineTestId}>
            <span className="tracker-summary__stat-label" data-testid={mineLabelTestId}>
              {mineLabel}
            </span>
            <span className="tracker-summary__stat-amount tabular-nums">{pesoLabel(mineAmount)}</span>
          </Link>
          <span
            className="tracker-summary__vrule"
            data-testid={showDivider ? "personal-owed-divider" : undefined}
            aria-hidden
          />
          <Link to={sharedTo} className="tracker-summary__stat" data-testid={sharedTestId}>
            <span className="tracker-summary__stat-label" data-testid={sharedLabelTestId}>
              {sharedLabel}
            </span>
            <span className="tracker-summary__stat-amount tabular-nums">{pesoLabel(sharedAmount)}</span>
          </Link>
        </span>
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

/** Personal Tracker card — ribbon, My Record / Shared with Me pendants, and people / ledger counts. */
export function PersonalTrackerCard({
  dashboard,
  testIds = PERSONAL_HOME_TRACKER_TEST_IDS,
  footer,
}: {
  dashboard: PersonalDashboardDto;
  testIds?: PersonalTrackerTestIds;
  footer?: ReactNode;
}) {
  const { t } = useI18n();

  return (
    <section
      className="catalog-form-section exits-animate-panel personal-section personal-tracker-card gap-3"
      aria-label={t("personal.home.personalTracker")}
      data-testid={testIds.section}
    >
      <h2 className="personal-tracker-card__ribbon" data-testid={testIds.ribbon}>
        <span className="personal-tracker-card__pennant" aria-hidden data-testid={testIds.pennant}>
          <MonitoringIcon className="personal-tracker-card__pennant-icon size-[0.95rem] shrink-0" />
        </span>
        {t("personal.home.personalTracker")}
      </h2>
      <div className="tracker-summary-row">
        <TrackerSummaryCard
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
          showDivider
        />
        <TrackerSummaryCard
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
          className="personal-home-index-card personal-home-index-card--secondary"
          data-testid={testIds.people}
        >
          <span className="personal-home-index-card__ribbon" aria-hidden="true">
            <span className="personal-home-index-card__fold" />
            <GroupIcon className="personal-home-index-card__icon" />
          </span>
          <span className="personal-home-index-card__body">
            <span className="personal-home-index-card__title">{t("personal.home.people")}</span>
          </span>
          <strong className="personal-home-index-card__badge personal-home-index-card__badge--ghost tabular-nums">
            {dashboard.contactCount}
          </strong>
        </Link>
        <Link
          to="/personal/utang"
          className="personal-home-index-card personal-home-index-card--success"
          data-testid={testIds.activeMine}
        >
          <span className="personal-home-index-card__ribbon" aria-hidden="true">
            <span className="personal-home-index-card__fold" />
            <HandshakeIcon className="personal-home-index-card__icon" />
          </span>
          <span className="personal-home-index-card__body">
            <span className="personal-home-index-card__title">{t("personal.home.activeMine")}</span>
          </span>
          <strong className="personal-home-index-card__badge personal-home-index-card__badge--ghost tabular-nums">
            {dashboard.activeRelationshipCount}
          </strong>
        </Link>
        <Link
          to="/personal/utang"
          className="personal-home-index-card personal-home-index-card--info"
          data-testid={testIds.activeShared}
        >
          <span className="personal-home-index-card__ribbon" aria-hidden="true">
            <span className="personal-home-index-card__fold" />
            <ShareIcon className="personal-home-index-card__icon" />
          </span>
          <span className="personal-home-index-card__body">
            <span className="personal-home-index-card__title">{t("personal.home.sharedLedgerStat")}</span>
          </span>
          <strong className="personal-home-index-card__badge personal-home-index-card__badge--ghost tabular-nums">
            {dashboard.sharedWithMeActiveCount ?? 0}
          </strong>
        </Link>
      </div>
      {footer}
    </section>
  );
}
