import { useEffect, useMemo, useRef, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Link } from "react-router-dom";
import { symbol, type IconComponent } from "@/components/icons/MaterialSymbol";

const ArrowDownwardIcon = symbol("arrow_downward");
const ArrowUpwardIcon = symbol("arrow_upward");
const BusinessCenterIcon = symbol("business_center");
const ChevronRightIcon = symbol("chevron_right");
const HandshakeIcon = symbol("handshake");
const InfoIcon = symbol("info");
const PaymentsIcon = symbol("payments");
const PlaylistAddIcon = symbol("playlist_add");
const ChecklistIcon = symbol("checklist");
const TodayIcon = symbol("today");
const OverdueIcon = symbol("event_busy");
const UpcomingIcon = symbol("event_upcoming");
const RefreshIcon = symbol("refresh");
const ShareIcon = symbol("share");
const StoreIcon = symbol("store");
const PersonAddIcon = symbol("person_add");
const GroupIcon = symbol("group");
const WalletIcon = symbol("account_balance_wallet");
const BoltIcon = symbol("bolt");
const MonitoringIcon = symbol("monitoring");
const NotificationIcon = symbol("notifications");
import { getPersonalDashboard } from "@/api/platform/personal-dashboard-client";
import {
  listBorrowedRelationships,
  listLentRelationships,
  listPersonalContacts,
} from "@/api/platform/personal-utang-client";
import {
  listPersonalTodos,
  summarizeTodoCounts,
  todoAgendaTabHref,
} from "@/api/platform/personal-todo-client";
import { Button } from "@/components/ui/button";
import { CountBadge } from "@/components/exits/CountChip";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { formatPeso } from "@/lib/format-money";
import { InfoPopover } from "@/components/exits/InfoPopover";
import { cn } from "@/lib/cn";
import { PersonAvatar } from "@/components/exits/PersonAvatar";
import { PersonalBusinessSummary } from "@/features/personal/businesses/PersonalBusinessSummary";
import { PersonalGuideHomeCard } from "@/features/personal/guide/PersonalGuideHomeCard";
import { usePersonalAvatarPhoto } from "@/features/personal/personal-avatar-context";
import { profilePhotoSrc } from "@/features/personal/profile-photo";
import { loadStoresToPayPreview } from "@/features/personal/stores-to-pay";
import {
  buildHomeAttentionItems,
  isActiveUtangAccount,
  mergeUtangAccounts,
} from "@/features/personal/utang/utang-workspace";
import { useI18n } from "@/i18n/I18nProvider";
import { resolveUserDisplayName } from "@/lib/user-display";
import { useSession } from "@/session/SessionProvider";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { usePersonalOfflineContext } from "@/offline/personal-offline-context";
import { listCachedPersonalTodos } from "@/offline/personal-todo-cache";

const personalSoftPrimaryButtonClass =
  "border border-border bg-[color-mix(in_srgb,var(--exits-primary)_10%,var(--exits-surface))] text-[var(--exits-primary)] hover:border-[var(--exits-primary)] hover:bg-[color-mix(in_srgb,var(--exits-primary)_14%,var(--exits-surface))]";

const personalSoftInfoButtonClass =
  "border border-border bg-[color-mix(in_srgb,var(--exits-severity-info)_12%,var(--exits-surface))] text-[var(--exits-severity-info)] hover:border-[var(--exits-severity-info)] hover:bg-[color-mix(in_srgb,var(--exits-severity-info)_16%,var(--exits-surface))]";

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
  showTotal = true,
  layout = "default",
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
  showTotal?: boolean;
  layout?: "default" | "pendant";
}) {
  const foot = (
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
  );

  if (layout === "pendant") {
    return (
      <div
        className={`tracker-summary tracker-summary--${tone} tracker-summary--pendant`}
        data-testid={rowTestId}
      >
        <span
          className="tracker-summary__pendant"
          aria-hidden
          data-testid={`${rowTestId}-pendant`}
        >
          <Icon className="tracker-summary__pendant-icon" />
        </span>
        <div className="tracker-summary__stage">
          <span className="tracker-summary__copy">
            <span className="tracker-summary__title">{title}</span>
            {showTotal ? (
              <span className="tracker-summary__amount tabular-nums">
                {pesoLabel(mineAmount + sharedAmount)}
              </span>
            ) : null}
          </span>
          <span className="tracker-balance__rule" aria-hidden />
          {foot}
        </div>
      </div>
    );
  }

  return (
    <div className={`tracker-summary tracker-summary--${tone}`} data-testid={rowTestId}>
      <span className="tracker-summary__head">
        <span className={`tracker-summary__icon tracker-summary__icon--${tone}`} aria-hidden>
          <Icon />
        </span>
        <span className="tracker-summary__copy">
          <span className="tracker-summary__title">{title}</span>
          {showTotal ? (
            <span className="tracker-summary__amount tabular-nums">{pesoLabel(mineAmount + sharedAmount)}</span>
          ) : null}
        </span>
      </span>
      <span className="tracker-balance__rule" aria-hidden />
      {foot}
    </div>
  );
}

export function PersonalHomePage() {
  const { t } = useI18n();
  const { session } = useSession();
  const displayName = resolveUserDisplayName(session) || t("account.signedIn");
  const avatarPhotoSrc = profilePhotoSrc(usePersonalAvatarPhoto());
  const [infoOpen, setInfoOpen] = useState(false);
  const infoRootRef = useRef<HTMLDivElement>(null);
  const infoButtonRef = useRef<HTMLElement | null>(null);
  const online = useBrowserOnline();
  const offline = usePersonalOfflineContext();
  const [cachedTodos, setCachedTodos] = useState<
    Awaited<ReturnType<typeof listCachedPersonalTodos>>
  >([]);

  const dashboardQuery = useQuery({
    queryKey: ["personal", "dashboard"],
    queryFn: ({ signal }) => getPersonalDashboard(signal),
  });
  const todosQuery = useQuery({
    queryKey: ["personal", "todos"],
    queryFn: ({ signal }) => listPersonalTodos(signal),
    enabled: online,
    meta: { suppressGlobalError: true, operation: "list personal todos" },
  });

  useEffect(() => {
    if (!infoOpen) {
      return;
    }
    function onKeyDown(event: KeyboardEvent) {
      if (event.key === "Escape") {
        setInfoOpen(false);
      }
    }
    function onPointerDown(event: PointerEvent) {
      if (!infoRootRef.current?.contains(event.target as Node)) {
        setInfoOpen(false);
      }
    }
    document.addEventListener("keydown", onKeyDown);
    document.addEventListener("pointerdown", onPointerDown);
    return () => {
      document.removeEventListener("keydown", onKeyDown);
      document.removeEventListener("pointerdown", onPointerDown);
    };
  }, [infoOpen]);

  useEffect(() => {
    if (!offline) {
      return;
    }
    let cancelled = false;
    void listCachedPersonalTodos(offline.db, offline.scopeBinding).then((rows) => {
      if (!cancelled) {
        setCachedTodos(rows);
      }
    });
    return () => {
      cancelled = true;
    };
  }, [offline, todosQuery.dataUpdatedAt]);
  const contactsQuery = useQuery({
    queryKey: ["personal", "utang", "contacts"],
    queryFn: ({ signal }) => listPersonalContacts(signal),
    enabled: online,
  });
  const lentQuery = useQuery({
    queryKey: ["personal", "utang", "lent"],
    queryFn: ({ signal }) => listLentRelationships(signal),
    enabled: online,
  });
  const oweQuery = useQuery({
    queryKey: ["personal", "utang", "owe"],
    queryFn: ({ signal }) => listBorrowedRelationships(signal),
    enabled: online,
  });
  const storesToPayQuery = useQuery({
    queryKey: ["personal", "home", "stores-to-pay"],
    queryFn: ({ signal }) => loadStoresToPayPreview(signal),
    enabled: online,
  });

  const accounts = useMemo(() => {
    if (!contactsQuery.data || !lentQuery.data || !oweQuery.data) {
      return [];
    }
    return mergeUtangAccounts(lentQuery.data, oweQuery.data, contactsQuery.data).filter(
      isActiveUtangAccount,
    );
  }, [contactsQuery.data, lentQuery.data, oweQuery.data]);

  const attentionItems = useMemo(() => {
    const pending = dashboardQuery.data?.pendingConfirmationCount ?? 0;
    return buildHomeAttentionItems({
      pendingConfirmationCount: pending,
      accounts,
    });
  }, [accounts, dashboardQuery.data?.pendingConfirmationCount]);

  const counts = useMemo(() => {
    if (todosQuery.isSuccess) {
      return summarizeTodoCounts(todosQuery.data);
    }
    if (cachedTodos.length > 0) {
      return summarizeTodoCounts(cachedTodos);
    }
    return null;
  }, [cachedTodos, todosQuery.data, todosQuery.isSuccess]);
  const usingCachedTodoCounts = !todosQuery.isSuccess && cachedTodos.length > 0;

  if (dashboardQuery.isPending) {
    return <LoadingSkeleton label={t("personal.home.loading")} />;
  }

  if (dashboardQuery.isError) {
    return (
      <div className="personal-page exits-page flex min-w-0 flex-col gap-3">
        <ErrorState
          title={t("personal.home.loadErrorTitle")}
          detail={t("personal.home.loadErrorDetail")}
        />
        <div className="exits-animate-toolbar flex w-full justify-center">
          <Button
            type="button"
            className="personal-error-retry w-full"
            onClick={() => void dashboardQuery.refetch()}
          >
            <RefreshIcon className="size-4 shrink-0" aria-hidden />
            {t("personal.home.retry")}
          </Button>
        </div>
      </div>
    );
  }

  const dashboard = dashboardQuery.data;

  function attentionTitle(item: (typeof attentionItems)[number]): string {
    if (item.kind === "pendingConfirmation") {
      return t("personal.home.attentionPending").replace("{count}", String(item.count));
    }
    if (item.kind === "overdue") {
      if (item.displayName) {
        return t("personal.home.attentionOverdueOne").replace("{name}", item.displayName);
      }
      return t("personal.home.attentionOverdue").replace("{count}", String(item.count));
    }
    if (item.displayName) {
      return t("personal.home.attentionDueSoonOne").replace("{name}", item.displayName);
    }
    return t("personal.home.attentionDueSoon").replace("{count}", String(item.count));
  }

  return (
    <div
      className="personal-page personal-home-page exits-page flex min-w-0 flex-col gap-3"
      data-testid="personal-home-page"
    >
      <header
        ref={infoRootRef}
        className={cn("relative flex items-center gap-2", infoOpen && "z-30")}
      >
        <div
          className="personal-home-welcome personal-home-capsule min-w-0"
          data-testid="personal-home-welcome"
        >
          <span className="personal-home-capsule__ring" aria-hidden="true">
            <span className="personal-home-capsule__disc" data-testid="personal-home-avatar">
              {avatarPhotoSrc ? (
                <img
                  className="personal-home-capsule__photo"
                  src={avatarPhotoSrc}
                  alt=""
                />
              ) : (
                <PersonAvatar
                  name={displayName}
                  size="md"
                  className="personal-home-capsule__avatar"
                />
              )}
            </span>
          </span>
          <div className="personal-home-capsule__body min-w-0">
            <div className="personal-home-capsule__copy min-w-0">
              <p className="personal-home-welcome__kicker personal-home-capsule__kicker">
                {t("personal.home.welcome")}
              </p>
              <h1 className="personal-home-welcome__name personal-home-capsule__name m-0 min-w-0 truncate">
                {displayName}
              </h1>
            </div>
            <span ref={infoButtonRef} className="personal-home-capsule__info relative inline-flex shrink-0">
              <Button
                type="button"
                intent="info"
                appearance="ghost"
                size="icon"
                className="shrink-0"
                aria-label={t("personal.info.open")}
                aria-expanded={infoOpen}
                aria-controls="personal-home-info-popover"
                data-testid="personal-home-info"
                onClick={() => setInfoOpen((open) => !open)}
              >
                <InfoIcon className="size-5" aria-hidden="true" />
              </Button>
              {infoOpen ? (
                <InfoPopover
                  id="personal-home-info-popover"
                  titleId="personal-home-info-title"
                  title={t("personal.info.title")}
                  anchorRef={infoButtonRef}
                  className="inset-x-auto start-auto end-0 w-[min(20rem,calc(100vw-1.5rem))] sm:inset-x-auto sm:start-auto sm:end-0 sm:w-[min(20rem,calc(100vw-1.5rem))]"
                >
                  <p className="m-0 mt-2 text-[length:var(--exits-text-sm)] text-muted">{t("personal.lede")}</p>
                </InfoPopover>
              ) : null}
            </span>
          </div>
        </div>
      </header>

      <PersonalGuideHomeCard />

      <div className="personal-home-layout" data-testid="personal-home-layout">
        <div className="personal-home-layout__main flex min-w-0 flex-col gap-3">
      <PersonalBusinessSummary />
      <section
        className="catalog-form-section exits-animate-panel personal-section personal-tracker-card gap-3"
        aria-label={t("personal.home.personalTracker")}
        data-testid="personal-utang-summary"
      >
        <h2 className="personal-tracker-card__ribbon" data-testid="personal-tracker-ribbon">
          <span
            className="personal-tracker-card__pennant"
            aria-hidden
            data-testid="personal-tracker-pennant"
          >
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
            layout="pendant"
            rowTestId="personal-utang-owed-to-me"
            mineTestId="personal-stat-borrowed-mine"
            sharedTestId="personal-stat-lent-mine"
            mineLabelTestId="personal-stat-borrowed-mine-label"
            sharedLabelTestId="personal-stat-lent-mine-label"
            showDivider
            showTotal={false}
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
            layout="pendant"
            rowTestId="personal-utang-i-owe"
            mineTestId="personal-stat-borrowed-shared"
            sharedTestId="personal-stat-lent-shared"
            mineLabelTestId="personal-stat-borrowed-shared-label"
            sharedLabelTestId="personal-stat-lent-shared-label"
            showTotal={false}
          />
        </div>
        <div className="personal-home-metrics" data-testid="personal-home-meta">
          <Link
            to="/personal/people"
            className="personal-home-index-card personal-home-index-card--secondary"
            data-testid="personal-stat-people"
          >
            <span className="personal-home-index-card__ribbon" aria-hidden="true">
              <span className="personal-home-index-card__fold" />
              <GroupIcon className="personal-home-index-card__icon" />
            </span>
            <span className="personal-home-index-card__body">
              <span className="personal-home-index-card__title">{t("personal.home.friends")}</span>
            </span>
            <strong className="personal-home-index-card__badge tabular-nums">
              {dashboard.contactCount}
            </strong>
          </Link>
          <Link
            to="/personal/utang"
            className="personal-home-index-card personal-home-index-card--success"
            data-testid="personal-stat-active"
          >
            <span className="personal-home-index-card__ribbon" aria-hidden="true">
              <span className="personal-home-index-card__fold" />
              <HandshakeIcon className="personal-home-index-card__icon" />
            </span>
            <span className="personal-home-index-card__body">
              <span className="personal-home-index-card__title">{t("personal.home.activeMine")}</span>
            </span>
            <strong className="personal-home-index-card__badge tabular-nums">
              {dashboard.activeRelationshipCount}
            </strong>
          </Link>
          <Link
            to="/personal/utang"
            className="personal-home-index-card personal-home-index-card--info"
            data-testid="personal-stat-active-shared"
          >
            <span className="personal-home-index-card__ribbon" aria-hidden="true">
              <span className="personal-home-index-card__fold" />
              <ShareIcon className="personal-home-index-card__icon" />
            </span>
            <span className="personal-home-index-card__body">
              <span className="personal-home-index-card__title">{t("personal.home.sharedLedgerStat")}</span>
            </span>
            <strong className="personal-home-index-card__badge tabular-nums">
              {dashboard.sharedWithMeActiveCount ?? 0}
            </strong>
          </Link>
        </div>
      </section>

      <section
        className="catalog-form-section exits-animate-panel personal-section personal-stores-to-pay gap-3"
        aria-label={t("personal.home.storesToPay")}
        data-testid="personal-stores-to-pay"
      >
        <h2 className="personal-stores-to-pay__ribbon" data-testid="personal-stores-to-pay-ribbon">
          <span
            className="personal-stores-to-pay__pennant"
            aria-hidden
            data-testid="personal-stores-to-pay-pennant"
          >
            <StoreIcon className="personal-stores-to-pay__pennant-icon size-[0.95rem] shrink-0" />
          </span>
          {t("personal.home.storesToPay")}
        </h2>
        {(!online && !storesToPayQuery.data) || storesToPayQuery.isError ? (
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("personal.home.storesUnavailable")}
          </p>
        ) : storesToPayQuery.isLoading ? (
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("personal.home.storesLoading")}
          </p>
        ) : storesToPayQuery.data ? (
          <>
            {storesToPayQuery.data.preview.length > 0 ? (
              <ul
                className="personal-stores-to-pay-list m-0 grid list-none gap-2 p-0"
                data-testid="personal-stores-to-pay-list"
              >
                {storesToPayQuery.data.preview.map((store) => (
                  <li key={`${store.organizationId}:${store.businessCustomerId}`}>
                    <Link
                      to={store.href}
                      className="personal-stores-to-pay-row exits-list__card flex items-center justify-between gap-3 text-foreground no-underline"
                      data-testid={`personal-store-row-${store.organizationId}`}
                    >
                      <span className="min-w-0 truncate text-[length:var(--exits-text-sm)] font-medium">
                        {store.displayName}
                      </span>
                      <span className="shrink-0 tabular-nums text-[length:var(--exits-text-sm)] font-semibold">
                        <MoneyDisplay amount={store.outstandingBalance} />
                      </span>
                    </Link>
                  </li>
                ))}
              </ul>
            ) : (
              <p
                className="personal-stores-empty m-0 text-[length:var(--exits-text-sm)] text-muted"
                data-testid="personal-stores-to-pay-empty"
              >
                {storesToPayQuery.data.storeCount === 0
                  ? t("personal.home.storesEmptyNone")
                  : t("personal.home.storesEmptyClear")}
              </p>
            )}
            <div
              className="personal-home-meta flex flex-wrap items-center gap-2"
              data-testid="personal-stores-to-pay-meta"
            >
              <Button
                asChild
                intent="info"
                appearance="solid"
                emphasis="soft"
                shape="auto"
                className={personalSoftInfoButtonClass}
              >
                <Link to="/personal/linked-merchants" data-testid="personal-stat-stores">
                  <StoreIcon className="size-4 shrink-0" aria-hidden="true" />
                  {t("personal.home.stores")}
                  <CountBadge count={storesToPayQuery.data.storeCount} tone="primary" />
                </Link>
              </Button>
              <Button
                asChild
                intent="primary"
                appearance="solid"
                emphasis="soft"
                shape="auto"
                className={personalSoftPrimaryButtonClass}
              >
                <Link to="/personal/linked-merchants" data-testid="personal-stat-stores-active">
                  <WalletIcon className="size-4 shrink-0" aria-hidden="true" />
                  {t("personal.home.active")}
                  <CountBadge count={storesToPayQuery.data.activeCount} tone="primary" />
                </Link>
              </Button>
            </div>
          </>
        ) : null}
      </section>

        </div>

        <aside
          className="personal-home-layout__aside flex min-w-0 flex-col gap-3"
          aria-label={t("personal.home.quickActions")}
        >
      {attentionItems.length > 0 ? (
        <section
          className="catalog-form-section exits-animate-panel personal-section personal-needs-attention gap-2"
          aria-label={t("personal.home.needsAttention")}
          data-testid="personal-needs-attention"
        >
          <h2 className="personal-needs-attention__ribbon" data-testid="personal-needs-attention-ribbon">
            <span
              className="personal-needs-attention__pennant"
              aria-hidden
              data-testid="personal-needs-attention-pennant"
            >
              <NotificationIcon className="personal-needs-attention__pennant-icon size-[0.95rem] shrink-0" />
            </span>
            {t("personal.home.needsAttention")}
          </h2>
          <ul className="m-0 grid list-none gap-2 p-0">
            {attentionItems.map((item) => (
              <li key={item.key}>
                <Link
                  to={item.href}
                  className="personal-attention-row exits-list__card flex items-center justify-between gap-3 text-foreground no-underline"
                  data-testid={`personal-attention-${item.kind}`}
                >
                  <span className="min-w-0 truncate text-[length:var(--exits-text-sm)] font-medium">
                    {attentionTitle(item)}
                  </span>
                  <ChevronRightIcon className="size-4 shrink-0 text-muted" aria-hidden />
                </Link>
              </li>
            ))}
          </ul>
          <Link
            to="/personal/utang"
            className="text-[length:var(--exits-text-sm)] font-medium text-[var(--exits-primary)] no-underline"
            data-testid="personal-attention-view-all"
          >
            {t("personal.home.viewAllUtang")}
          </Link>
        </section>
      ) : null}
      <section
        className="catalog-form-section exits-animate-panel personal-section personal-quick-actions gap-3"
        aria-label={t("personal.home.quickActions")}
        data-testid="personal-quick-actions"
      >
        <h2 className="personal-quick-actions__ribbon" data-testid="personal-quick-actions-ribbon">
          <span className="personal-quick-actions__pennant" aria-hidden data-testid="personal-quick-actions-pennant">
            <BoltIcon className="personal-quick-actions__pennant-icon size-[0.95rem] shrink-0" />
          </span>
          {t("personal.home.quickActions")}
        </h2>
        <div className="personal-quick-actions__cards" data-testid="personal-quick-actions-cards">
          <Link
            to="/personal/businesses"
            className="personal-home-infodata-card"
            data-testid="personal-qa-start-business"
          >
            <span className="personal-home-infodata-card__circle" aria-hidden="true">
              <BusinessCenterIcon className="personal-home-infodata-card__icon" />
            </span>
            <span className="personal-home-infodata-card__body">
              <span className="personal-home-infodata-card__title">{t("personal.home.actionStartBusiness")}</span>
              <span className="personal-home-infodata-card__note">{t("personal.home.startBusinessLede")}</span>
            </span>
          </Link>
          <Link
            to="/personal/utang/lent"
            className="personal-home-index-card personal-home-index-card--success personal-home-index-card--solid"
            data-testid="personal-qa-lent"
          >
            <span className="personal-home-index-card__ribbon" aria-hidden="true">
              <span className="personal-home-index-card__fold" />
              <ArrowUpwardIcon className="personal-home-index-card__icon" />
            </span>
            <span className="personal-home-index-card__body">
              <span className="personal-home-index-card__title">{t("personal.home.actionLent")}</span>
            </span>
            <strong className="personal-home-index-card__badge personal-home-index-card__badge--plus personal-home-index-card__badge--standard" aria-hidden="true">
              <ChevronRightIcon className="personal-home-index-card__icon" />
            </strong>
          </Link>
          <Link
            to="/personal/utang/owe"
            className="personal-home-index-card personal-home-index-card--warn personal-home-index-card--solid"
            data-testid="personal-qa-owe"
          >
            <span className="personal-home-index-card__ribbon" aria-hidden="true">
              <span className="personal-home-index-card__fold" />
              <ArrowDownwardIcon className="personal-home-index-card__icon" />
            </span>
            <span className="personal-home-index-card__body">
              <span className="personal-home-index-card__title">{t("personal.home.actionOwe")}</span>
            </span>
            <strong className="personal-home-index-card__badge personal-home-index-card__badge--plus personal-home-index-card__badge--standard" aria-hidden="true">
              <ChevronRightIcon className="personal-home-index-card__icon" />
            </strong>
          </Link>
          <Link
            to="/personal/linked-merchants"
            className="personal-home-index-card personal-home-index-card--info personal-home-index-card--solid"
            data-testid="personal-qa-stores"
          >
            <span className="personal-home-index-card__ribbon" aria-hidden="true">
              <span className="personal-home-index-card__fold" />
              <PaymentsIcon className="personal-home-index-card__icon" />
            </span>
            <span className="personal-home-index-card__body">
              <span className="personal-home-index-card__title">{t("personal.home.actionStores")}</span>
            </span>
            <strong className="personal-home-index-card__badge personal-home-index-card__badge--plus personal-home-index-card__badge--standard" aria-hidden="true">
              <ChevronRightIcon className="personal-home-index-card__icon" />
            </strong>
          </Link>
          <Link
            to="/personal/people"
            className="personal-home-index-card personal-home-index-card--secondary personal-home-index-card--solid"
            data-testid="personal-qa-people"
          >
            <span className="personal-home-index-card__ribbon" aria-hidden="true">
              <span className="personal-home-index-card__fold" />
              <PersonAddIcon className="personal-home-index-card__icon" />
            </span>
            <span className="personal-home-index-card__body">
              <span className="personal-home-index-card__title">{t("personal.home.actionPeople")}</span>
            </span>
            <strong className="personal-home-index-card__badge personal-home-index-card__badge--plus personal-home-index-card__badge--standard" aria-hidden="true">
              +
            </strong>
          </Link>
        </div>
      </section>

      <section
        className="catalog-form-section exits-animate-panel personal-section personal-todo-summary gap-3"
        aria-label={t("personal.home.todoSummary")}
        data-testid="personal-todo-summary"
      >
        <h2 className="personal-todo-summary__ribbon" data-testid="personal-todo-summary-ribbon">
          <span className="personal-todo-summary__pennant" aria-hidden data-testid="personal-todo-summary-pennant">
            <ChecklistIcon className="personal-todo-summary__pennant-icon size-[0.95rem] shrink-0" />
          </span>
          {t("personal.home.todoSummary")}
        </h2>
        {todosQuery.isPending && cachedTodos.length === 0 ? (
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("personal.todo.loading")}
          </p>
        ) : todosQuery.isError && cachedTodos.length === 0 ? (
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("personal.home.todoUnavailable")}
          </p>
        ) : counts ? (
          <>
            {usingCachedTodoCounts ? (
              <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
                {t("personal.home.todoCachedSummary")}
              </p>
            ) : null}
            <div className="personal-todo-cards" data-testid="personal-todo-counts" role="list">
              <Link
                to="/personal/todo?add=1"
                className="personal-home-index-card personal-home-index-card--primary personal-home-index-card--solid"
                data-testid="personal-todo-add"
              >
                <span className="personal-home-index-card__ribbon" aria-hidden="true">
                  <span className="personal-home-index-card__fold" />
                  <PlaylistAddIcon className="personal-home-index-card__icon" />
                </span>
                <span className="personal-home-index-card__body">
                  <span className="personal-home-index-card__title">{t("personal.home.actionTodo")}</span>
                </span>
                <strong
                  className="personal-home-index-card__badge personal-home-index-card__badge--plus personal-home-index-card__badge--standard"
                  aria-hidden="true"
                >
                  +
                </strong>
              </Link>
              <Link
                to={todoAgendaTabHref("today")}
                className="personal-home-index-card personal-home-index-card--info personal-home-index-card--solid"
                data-testid="personal-todo-stat-today"
              >
                <span className="personal-home-index-card__ribbon" aria-hidden="true">
                  <span className="personal-home-index-card__fold" />
                  <TodayIcon className="personal-home-index-card__icon" />
                </span>
                <span className="personal-home-index-card__body">
                  <span className="personal-home-index-card__title">{t("personal.todo.countToday")}</span>
                </span>
                <strong className="personal-home-index-card__badge personal-home-index-card__badge--standard tabular-nums">
                  {counts.today}
                </strong>
              </Link>
              <Link
                to={todoAgendaTabHref("upcoming")}
                className="personal-home-index-card personal-home-index-card--secondary personal-home-index-card--solid"
                data-testid="personal-todo-stat-upcoming"
              >
                <span className="personal-home-index-card__ribbon" aria-hidden="true">
                  <span className="personal-home-index-card__fold" />
                  <UpcomingIcon className="personal-home-index-card__icon" />
                </span>
                <span className="personal-home-index-card__body">
                  <span className="personal-home-index-card__title">{t("personal.todo.countUpcoming")}</span>
                </span>
                <strong className="personal-home-index-card__badge personal-home-index-card__badge--standard tabular-nums">
                  {counts.upcoming}
                </strong>
              </Link>
              <Link
                to={todoAgendaTabHref("overdue")}
                className="personal-home-index-card personal-home-index-card--warn personal-home-index-card--solid"
                data-testid="personal-todo-stat-overdue"
              >
                <span className="personal-home-index-card__ribbon" aria-hidden="true">
                  <span className="personal-home-index-card__fold" />
                  <OverdueIcon className="personal-home-index-card__icon" />
                </span>
                <span className="personal-home-index-card__body">
                  <span className="personal-home-index-card__title">{t("personal.todo.countOverdue")}</span>
                </span>
                <strong className="personal-home-index-card__badge personal-home-index-card__badge--standard tabular-nums">
                  {counts.overdue}
                </strong>
              </Link>
            </div>
            {counts.open === 0 && counts.completed === 0 ? (
              <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                {t("personal.home.todoEmpty")}
              </p>
            ) : null}
          </>
        ) : null}
      </section>
        </aside>
      </div>
    </div>
  );
}
