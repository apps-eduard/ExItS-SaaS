import { useId, useMemo, type ReactNode } from "react";
import { Link } from "react-router-dom";
import type { LucideIcon } from "lucide-react";
import { Activity, Share2, Users, Wallet } from "lucide-react";
import type { PersonalDashboardDto } from "@/api/platform/personal-dashboard-client";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { useI18n } from "@/i18n/I18nProvider";
import {
  personalHomeKpiSparkGeometry,
  personalHomeKpiSparkSeries,
} from "@/features/personal/personal-home-kpi-sparkline";
import { cn } from "@/lib/cn";

type PersonalHomeKpiIdentity = "lent" | "owe";

type PersonalHomeKpiCardConfig = {
  identity: PersonalHomeKpiIdentity;
  scopeLabelKey: string;
  roleLabelKey: string;
  meta: ReactNode;
  to: string;
  testId: string;
  icon: LucideIcon;
  anchor: number;
  metric: ReactNode;
  scope: "my-record" | "shared";
};

function PersonalHomeKpiSparkline({ anchor }: { anchor: number }) {
  const gradientId = useId().replace(/:/g, "");
  const geometry = useMemo(() => {
    const series = personalHomeKpiSparkSeries(anchor);
    return personalHomeKpiSparkGeometry(series);
  }, [anchor]);

  if (!geometry.linePath) {
    return null;
  }

  return (
    <svg
      className="personal-home-kpi-card__spark"
      viewBox="0 0 160 40"
      aria-hidden
      preserveAspectRatio="none"
    >
      <defs>
        <linearGradient id={`personal-home-kpi-spark-${gradientId}`} x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" stopColor="currentColor" stopOpacity="0.38" />
          <stop offset="100%" stopColor="currentColor" stopOpacity="0" />
        </linearGradient>
      </defs>
      <path d={geometry.fillPath} fill={`url(#personal-home-kpi-spark-${gradientId})`} />
      <path
        d={geometry.linePath}
        fill="none"
        stroke="currentColor"
        strokeWidth="2.25"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
      <circle
        className="personal-home-kpi-card__spark-dot"
        cx={geometry.endX}
        cy={geometry.endY}
        r="3.25"
        fill="currentColor"
      />
    </svg>
  );
}

function PersonalHomeKpiCard({ card }: { card: PersonalHomeKpiCardConfig }) {
  const { t } = useI18n();
  const Icon = card.icon;
  const scopeLabel = t(card.scopeLabelKey);
  const roleLabel = t(card.roleLabelKey);

  return (
    <Link
      to={card.to}
      className={cn("personal-home-kpi-card", `personal-home-kpi-card--${card.identity}`)}
      data-testid={card.testId}
      data-identity={card.identity}
      data-scope={card.scope}
      aria-label={`${scopeLabel}, ${roleLabel}`}
    >
      <div className="personal-home-kpi-card__main">
        <div className="personal-home-kpi-card__head">
          <span className="personal-home-kpi-card__chip" aria-hidden>
            <Icon className="personal-home-kpi-card__chip-icon" strokeWidth={2.25} />
          </span>
          <span className="personal-home-kpi-card__labels">
            <span className="personal-home-kpi-card__label-scope">{scopeLabel}</span>
            <span className="personal-home-kpi-card__label-role">{roleLabel}</span>
          </span>
        </div>
        <p className="personal-home-kpi-card__metric m-0 tabular-nums">{card.metric}</p>
        <p className="personal-home-kpi-card__meta m-0">{card.meta}</p>
      </div>
      <PersonalHomeKpiSparkline anchor={card.anchor} />
    </Link>
  );
}

export const PERSONAL_HOME_KPI_TEST_IDS = {
  strip: "personal-home-kpi-strip",
  myRecordLent: "personal-home-kpi-my-record-lent",
  myRecordOwe: "personal-home-kpi-my-record-owe",
  sharedLent: "personal-home-kpi-shared-lent",
  sharedOwe: "personal-home-kpi-shared-owe",
} as const;

export function PersonalHomeKpiStrip({ dashboard }: { dashboard: PersonalDashboardDto }) {
  const { t } = useI18n();

  const sharedActiveCount = dashboard.sharedWithMeActiveCount ?? 0;
  const sharedActiveMeta = t("personal.home.kpiSharedActiveMeta").replace(
    "{count}",
    String(sharedActiveCount),
  );

  const cards: PersonalHomeKpiCardConfig[] = [
    {
      identity: "lent",
      scope: "my-record",
      scopeLabelKey: "personal.home.kpiScopeMyRecord",
      roleLabelKey: "personal.utang.filterOwedToMe",
      meta: t("personal.home.kpiActiveMeta").replace(
        "{count}",
        String(dashboard.activeRelationshipCount),
      ),
      to: "/personal/utang/lent",
      testId: PERSONAL_HOME_KPI_TEST_IDS.myRecordLent,
      icon: Wallet,
      anchor: dashboard.totalLentBalance,
      metric: <MoneyDisplay amount={dashboard.totalLentBalance} />,
    },
    {
      identity: "owe",
      scope: "my-record",
      scopeLabelKey: "personal.home.kpiScopeMyRecord",
      roleLabelKey: "personal.utang.filterIOwe",
      meta: t("personal.home.kpiPendingMeta").replace(
        "{count}",
        String(dashboard.pendingConfirmationCount ?? 0),
      ),
      to: "/personal/utang/owe",
      testId: PERSONAL_HOME_KPI_TEST_IDS.myRecordOwe,
      icon: Activity,
      anchor: dashboard.totalBorrowedBalance,
      metric: <MoneyDisplay amount={dashboard.totalBorrowedBalance} />,
    },
    {
      identity: "lent",
      scope: "shared",
      scopeLabelKey: "personal.home.kpiScopeShared",
      roleLabelKey: "personal.utang.filterOwedToMe",
      meta: sharedActiveMeta,
      to: "/personal/utang",
      testId: PERSONAL_HOME_KPI_TEST_IDS.sharedLent,
      icon: Share2,
      anchor: dashboard.sharedWithMeLentBalance ?? 0,
      metric: <MoneyDisplay amount={dashboard.sharedWithMeLentBalance ?? 0} />,
    },
    {
      identity: "owe",
      scope: "shared",
      scopeLabelKey: "personal.home.kpiScopeShared",
      roleLabelKey: "personal.utang.filterIOwe",
      meta: sharedActiveMeta,
      to: "/personal/utang",
      testId: PERSONAL_HOME_KPI_TEST_IDS.sharedOwe,
      icon: Users,
      anchor: dashboard.sharedWithMeBorrowedBalance ?? 0,
      metric: <MoneyDisplay amount={dashboard.sharedWithMeBorrowedBalance ?? 0} />,
    },
  ];

  return (
    <section
      className="personal-home-kpi-strip"
      aria-label={t("personal.home.kpiStripAria")}
      data-testid={PERSONAL_HOME_KPI_TEST_IDS.strip}
    >
      {cards.map((card) => (
        <PersonalHomeKpiCard key={card.testId} card={card} />
      ))}
    </section>
  );
}