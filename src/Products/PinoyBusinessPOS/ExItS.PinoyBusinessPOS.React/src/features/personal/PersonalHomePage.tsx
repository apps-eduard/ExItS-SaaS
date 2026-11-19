import { useEffect, useMemo, useRef, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Link } from "react-router-dom";
import {
  ArrowDown,
  ArrowUp,
  BellRing,
  BriefcaseBusiness,
  Calendar,
  CalendarDays,
  ChevronRight,
  Clock3,
  HandCoins,
  History,
  Info,
  ListPlus,
  ListTodo,
  PenLine,
  RefreshCw,
  Share2,
  Store,
  TriangleAlert,
  UserPlus,
  Wallet,
  Zap,
  type LucideIcon,
} from "lucide-react";
import { getPersonalDashboard } from "@/api/platform/personal-dashboard-client";
import { listPersonalNotifications } from "@/api/platform/personal-social-client";
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
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { InfoPopover } from "@/components/exits/InfoPopover";
import { Notice } from "@/components/exits/Notice";
import { cn } from "@/lib/cn";
import { PersonAvatar } from "@/components/exits/PersonAvatar";
import { PersonalGuideHomeCard } from "@/features/personal/guide/PersonalGuideHomeCard";
import { PersonalHomeKpiStrip } from "@/features/personal/PersonalHomeKpiStrip";
import { usePersonalHomeSurfaceStyle } from "@/features/personal/personal-home-surface-style";
import { PersonalYourRecordsCard } from "@/features/personal/PersonalYourRecordsCard";
import {
  countActivePersonalHomeKpiRelationships,
  mergePersonalUtangRelationships,
  summarizePersonalYourRecordCounts,
} from "@/features/personal/personal-home-kpi-balance-history";
import { usePersonalAvatarPhoto } from "@/features/personal/personal-avatar-context";
import { profilePhotoSrc } from "@/features/personal/profile-photo";
import {
  buildHomeRecentActivityItems,
  formatHomeActivityWhen,
  type HomeRecentActivityIcon,
} from "@/features/personal/home-recent-activity";
import { useHomeRecentActivitySources } from "@/features/personal/use-home-recent-activity-sources";
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

function homeRecentActivityIcon(kind: HomeRecentActivityIcon): LucideIcon {
  switch (kind) {
    case "notification":
      return BellRing;
    case "utang-loan":
      return HandCoins;
    case "utang-payment":
      return Wallet;
    case "utang-adjustment":
      return PenLine;
    case "utang-shared":
      return Share2;
    case "order":
      return Store;
    case "todo":
      return ListTodo;
    default:
      return History;
  }
}

/** Greeting avatar — photo with initials fallback when the image fails to load. */
function PersonalHomeHeroAvatar({
  name,
  photoSrc,
}: {
  name: string;
  photoSrc: string | null;
}) {
  const [photoFailed, setPhotoFailed] = useState(false);

  useEffect(() => {
    setPhotoFailed(false);
  }, [photoSrc]);

  const showPhoto = Boolean(photoSrc) && !photoFailed;

  return (
    <span className="personal-home-hero__avatar" data-testid="personal-home-avatar">
      {showPhoto ? (
        <img
          className="personal-home-hero__photo"
          src={photoSrc!}
          alt=""
          data-testid="personal-home-avatar-photo"
          onError={() => setPhotoFailed(true)}
        />
      ) : (
        <PersonAvatar name={name} size="lg" className="personal-home-hero__avatar-fallback" />
      )}
    </span>
  );
}

export function PersonalHomePage() {
  const { t } = useI18n();
  const { session } = useSession();
  const [surfaceStyle, setSurfaceStyle] = usePersonalHomeSurfaceStyle();
  const displayName = resolveUserDisplayName(session) || t("account.signedIn");
  const avatarPhotoSrc = profilePhotoSrc(usePersonalAvatarPhoto());
  const greetingKey = useMemo(() => {
    const hour = new Date().getHours();
    if (hour < 12) {
      return "personal.home.greetingMorning" as const;
    }
    if (hour < 17) {
      return "personal.home.greetingAfternoon" as const;
    }
    return "personal.home.greetingEvening" as const;
  }, []);
  const dateChipLabel = useMemo(
    () =>
      new Intl.DateTimeFormat(undefined, {
        weekday: "short",
        month: "short",
        day: "numeric",
        year: "numeric",
      }).format(new Date()),
    [],
  );
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
  const notificationsQuery = useQuery({
    queryKey: ["personal", "notifications", "recent"],
    queryFn: ({ signal }) => listPersonalNotifications(signal),
    enabled: online,
    meta: { suppressGlobalError: true, operation: "list personal notifications" },
  });

  const accounts = useMemo(() => {
    if (!contactsQuery.data || !lentQuery.data || !oweQuery.data) {
      return [];
    }
    return mergeUtangAccounts(lentQuery.data, oweQuery.data, contactsQuery.data).filter(
      isActiveUtangAccount,
    );
  }, [contactsQuery.data, lentQuery.data, oweQuery.data]);

  const yourRecordCounts = useMemo(() => {
    if (lentQuery.data && oweQuery.data) {
      const relationships = mergePersonalUtangRelationships(lentQuery.data, oweQuery.data);
      const active = countActivePersonalHomeKpiRelationships(relationships);
      return summarizePersonalYourRecordCounts(active);
    }
    if (!dashboardQuery.data) {
      return { myRecords: 0, shared: 0 };
    }
    return {
      myRecords: dashboardQuery.data.activeRelationshipCount,
      shared: dashboardQuery.data.sharedWithMeActiveCount ?? 0,
    };
  }, [dashboardQuery.data, lentQuery.data, oweQuery.data]);

  const storeCountForRecords = storesToPayQuery.data?.storeCount ?? 0;

  const attentionItems = useMemo(() => {
    const pending = dashboardQuery.data?.pendingConfirmationCount ?? 0;
    return buildHomeAttentionItems({
      pendingConfirmationCount: pending,
      accounts,
    });
  }, [accounts, dashboardQuery.data?.pendingConfirmationCount]);

  const activitySources = useHomeRecentActivitySources(accounts);
  const todoActivityRows = todosQuery.isSuccess
    ? todosQuery.data
    : cachedTodos.length > 0
      ? cachedTodos
      : undefined;

  const recentActivityItems = useMemo(
    () =>
      buildHomeRecentActivityItems({
        notifications: notificationsQuery.data,
        accounts,
        history: activitySources.history,
        orders: activitySources.orders,
        todos: todoActivityRows,
        t,
      }),
    [
      accounts,
      activitySources.history,
      activitySources.orders,
      notificationsQuery.data,
      t,
      todoActivityRows,
    ],
  );
  const recentActivityLoading =
    recentActivityItems.length === 0 &&
    (notificationsQuery.isPending ||
      activitySources.historyLoading ||
      activitySources.ordersLoading);
  const recentActivityUnavailable =
    recentActivityItems.length === 0 &&
    !recentActivityLoading &&
    notificationsQuery.isError &&
    !activitySources.hasHistoryData &&
    !activitySources.hasOrdersData &&
    !(todoActivityRows && todoActivityRows.length > 0);

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
            <RefreshCw className="size-4 shrink-0" aria-hidden />
            {t("personal.home.retry")}
          </Button>
        </div>
      </div>
    );
  }

  const dashboard = dashboardQuery.data;

  function attentionTitle(item: (typeof attentionItems)[number]): string {
    if (item.kind === "pendingConfirmation") {
      return t("personal.home.attentionPending");
    }
    if (item.kind === "overdue") {
      if (item.displayName) {
        return t("personal.home.attentionOverdueOne").replace("{name}", item.displayName);
      }
      return t("personal.home.attentionOverdue");
    }
    if (item.displayName) {
      return t("personal.home.attentionDueSoonOne").replace("{name}", item.displayName);
    }
    return t("personal.home.attentionDueSoon");
  }

  return (
    <div
      className="personal-page personal-home-page exits-page flex min-w-0 flex-col gap-3"
      data-testid="personal-home-page"
      data-surface-style={surfaceStyle}
    >
      <header
        ref={infoRootRef}
        className={cn("personal-home-hero", infoOpen && "z-30")}
        data-testid="personal-home-welcome"
      >
        <div className="personal-home-hero__main min-w-0">
          <span className="personal-home-hero__avatar-wrap" aria-hidden="true">
            <PersonalHomeHeroAvatar name={displayName} photoSrc={avatarPhotoSrc} />
          </span>
          <div className="personal-home-hero__copy min-w-0">
            <p className="personal-home-hero__greeting m-0">{t(greetingKey)}</p>
            <h1 className="personal-home-hero__name m-0 min-w-0 truncate">{displayName}</h1>
          </div>
        </div>
        <div className="personal-home-hero__end">
          <span ref={infoButtonRef} className="personal-home-hero__info relative inline-flex shrink-0">
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
              <Info className="size-5" aria-hidden="true" />
            </Button>
            {infoOpen ? (
              <InfoPopover
                id="personal-home-info-popover"
                titleId="personal-home-info-title"
                title={t("personal.info.title")}
                anchorRef={infoButtonRef}
                className="inset-x-auto start-auto end-0 w-[min(20rem,calc(100vw-1.5rem))] sm:inset-x-auto sm:start-auto sm:end-0 sm:w-[min(20rem,calc(100vw-1.5rem))]"
              >
                <Notice tone="info" className="mt-2" testId="personal-home-info-notice">
                  {t("personal.home.heroSubtitle")}
                </Notice>
              </InfoPopover>
            ) : null}
          </span>
          <aside className="personal-home-hero__date" data-testid="personal-home-date-chip">
            <span className="personal-home-hero__date-icon" aria-hidden="true">
              <CalendarDays className="size-5" />
            </span>
            <span className="personal-home-hero__date-copy">
              <strong className="personal-home-hero__date-label">{dateChipLabel}</strong>
              <span className="personal-home-hero__date-tail">{t("personal.home.dateChipTail")}</span>
            </span>
          </aside>
        </div>
      </header>

      <PersonalGuideHomeCard />

      <div className="personal-home-layout" data-testid="personal-home-layout">
        <div
          className="personal-home-layout__kpi min-w-0"
          data-testid="personal-home-layout-kpi"
        >
          <PersonalHomeKpiStrip
            dashboard={dashboard}
            surfaceStyle={surfaceStyle}
            onSurfaceStyleChange={setSurfaceStyle}
          />
        </div>
        <div className="personal-home-layout__main flex min-w-0 flex-col gap-3">
      <PersonalYourRecordsCard
        peopleCount={dashboard.contactCount}
        myRecordsCount={yourRecordCounts.myRecords}
        sharedCount={yourRecordCounts.shared}
        storeCount={storeCountForRecords}
      />

      <div
        className="personal-home-primary-row"
        data-testid="personal-home-primary-row"
      >
      <section
        className="catalog-form-section exits-animate-panel personal-section personal-stores-to-pay gap-3 max-md:gap-1.5"
        aria-label={t("personal.home.storesToPay")}
        data-testid="personal-stores-to-pay"
      >
        <div className="personal-stores-to-pay__header">
          <h2 className="personal-stores-to-pay__title" data-testid="personal-stores-to-pay-ribbon">
            <span
              className="personal-stores-to-pay__pennant"
              aria-hidden
              data-testid="personal-stores-to-pay-pennant"
            >
              <Store className="personal-stores-to-pay__pennant-icon size-[1.05rem] shrink-0" />
            </span>
            {t("personal.home.storesToPay")}
          </h2>
        </div>
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
            <div
              className="personal-stores-to-pay__metrics"
              data-testid="personal-stores-to-pay-meta"
            >
              <Link
                to="/personal/linked-merchants"
                className="personal-qa-glass personal-qa-glass--primary personal-qa-glass--md personal-qa-glass--card-soft personal-qa-glass--no-shadow personal-qa-glass--icon-soft personal-qa-glass--no-icon-shadow"
                data-testid="personal-stat-stores"
                data-recipe="ext org-ehg qa-glass primary md icon-soft card-soft trail-count no-shadow no-icon-shadow"
              >
                <span className="personal-qa-glass__icon personal-qa-glass__icon--soft" aria-hidden="true">
                  <Store className="personal-stores-to-pay__pennant-icon size-[0.95rem] shrink-0" />
                </span>
                <span className="personal-qa-glass__label">{t("personal.home.stores")}</span>
                <strong className="personal-qa-glass__badge tabular-nums">
                  {storesToPayQuery.data.storeCount}
                </strong>
              </Link>
              <Link
                to="/personal/linked-merchants"
                className="personal-qa-glass personal-qa-glass--primary personal-qa-glass--md personal-qa-glass--card-soft personal-qa-glass--no-shadow personal-qa-glass--icon-soft personal-qa-glass--no-icon-shadow"
                data-testid="personal-stat-stores-active"
                data-recipe="ext org-ehg qa-glass primary md icon-soft card-soft trail-count no-shadow no-icon-shadow"
              >
                <span className="personal-qa-glass__icon personal-qa-glass__icon--soft" aria-hidden="true">
                  <Wallet className="personal-stores-to-pay__pennant-icon size-[0.95rem] shrink-0" />
                </span>
                <span className="personal-qa-glass__label">{t("personal.home.active")}</span>
                <strong className="personal-qa-glass__badge tabular-nums">
                  {storesToPayQuery.data.activeCount}
                </strong>
              </Link>
            </div>
            {storesToPayQuery.data.preview.length > 0 ? (
              <ul
                className="personal-stores-to-pay-list m-0 grid list-none gap-2 p-0"
                data-testid="personal-stores-to-pay-list"
              >
                {storesToPayQuery.data.preview.map((store) => (
                  <li key={`${store.organizationId}:${store.businessCustomerId}`}>
                    <Link
                      to={store.href}
                      className="personal-stores-to-pay-row personal-home-infodata-card personal-home-infodata-card--row personal-qa-glass--primary personal-qa-glass--md personal-qa-glass--icon-soft personal-qa-glass--no-icon-shadow"
                      data-testid={`personal-store-row-${store.organizationId}`}
                      data-shell="org-ehg"
                      data-recipe="ext org-ehg qa-glass primary md icon-soft no-icon-shadow"
                    >
                      <span className="personal-qa-glass__icon personal-qa-glass__icon--soft" aria-hidden="true">
                        <Store className="personal-stores-to-pay__pennant-icon size-[0.95rem] shrink-0" />
                      </span>
                      <span className="personal-home-infodata-card__body">
                        <span className="personal-home-infodata-card__title truncate">{store.displayName}</span>
                      </span>
                      <span className="personal-stores-to-pay-row__amount shrink-0 tabular-nums">
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
          </>
        ) : null}
      </section>

      <section
        className="catalog-form-section exits-animate-panel personal-section personal-recent-activity gap-3 max-md:gap-1.5"
        aria-label={t("personal.home.recentActivity")}
        data-testid="personal-recent-activity"
      >
        <div className="personal-recent-activity__header">
          <h2 className="personal-recent-activity__title" data-testid="personal-recent-activity-ribbon">
            <span
              className="personal-recent-activity__pennant"
              aria-hidden
              data-testid="personal-recent-activity-pennant"
            >
              <History className="personal-recent-activity__pennant-icon size-[1.05rem] shrink-0" />
            </span>
            {t("personal.home.recentActivity")}
          </h2>
          <Link
            to="/personal/notifications"
            className="personal-recent-activity__view-all"
            data-testid="personal-activity-view-all"
          >
            {t("personal.home.viewAllActivity")}
          </Link>
        </div>
        {recentActivityLoading ? (
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted" data-testid="personal-activity-loading">
            {t("personal.home.activityLoading")}
          </p>
        ) : recentActivityUnavailable ? (
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted" data-testid="personal-activity-unavailable">
            {t("personal.home.activityUnavailable")}
          </p>
        ) : recentActivityItems.length === 0 ? (
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted" data-testid="personal-activity-empty">
            {t("personal.home.activityEmpty")}
          </p>
        ) : (
          <ul className="personal-recent-activity__list m-0 grid list-none gap-2 p-0" data-testid="personal-activity-list">
            {recentActivityItems.map((item) => {
              const Icon = homeRecentActivityIcon(item.icon);
              return (
              <li key={item.id}>
                <Link
                  to={item.href}
                  className="personal-recent-activity__row"
                  data-testid={`personal-activity-item-${item.id}`}
                >
                  <span className="personal-recent-activity__row-icon" aria-hidden="true">
                    <Icon className="size-[0.95rem] shrink-0" />
                  </span>
                  <span className="personal-recent-activity__row-body min-w-0">
                    <span className="personal-recent-activity__row-title truncate">{item.title}</span>
                    {item.detail ? (
                      <span className="personal-recent-activity__row-detail truncate">{item.detail}</span>
                    ) : null}
                  </span>
                  <time className="personal-recent-activity__row-when shrink-0" dateTime={item.at}>
                    {formatHomeActivityWhen(item.at)}
                  </time>
                </Link>
              </li>
              );
            })}
          </ul>
        )}
      </section>
      </div>

        </div>

        <aside
          className="personal-home-layout__aside flex min-w-0 flex-col gap-3"
          aria-label={t("personal.home.quickActions")}
        >
      <section
        className="catalog-form-section exits-animate-panel personal-section personal-quick-actions gap-3 max-md:gap-1.5"
        aria-label={t("personal.home.quickActions")}
        data-testid="personal-quick-actions"
      >
        <div className="personal-quick-actions__header">
          <h2 className="personal-quick-actions__title" data-testid="personal-quick-actions-ribbon">
            <span
              className="personal-quick-actions__pennant"
              aria-hidden
              data-testid="personal-quick-actions-pennant"
            >
              <Zap className="personal-quick-actions__pennant-icon size-[1.05rem] shrink-0" />
            </span>
            {t("personal.home.quickActions")}
          </h2>
        </div>
        <div className="personal-quick-actions__cards" data-testid="personal-quick-actions-cards">
          <Link
            to="/personal/businesses"
            className="personal-qa-glass personal-qa-glass--start personal-qa-glass--md personal-qa-glass--card-soft personal-qa-glass--no-shadow personal-qa-glass--icon-soft personal-qa-glass--no-icon-shadow"
            data-testid="personal-qa-start-business"
            data-recipe="ext org-ehg qa-glass start md icon-soft card-soft trail-chevron no-shadow no-icon-shadow"
          >
            <span className="personal-qa-glass__icon personal-qa-glass__icon--soft" aria-hidden="true">
              <BriefcaseBusiness className="personal-quick-actions__pennant-icon size-[0.95rem] shrink-0" />
            </span>
            <span className="personal-qa-glass__label">{t("personal.home.actionStartBusiness")}</span>
            <ChevronRight className="personal-qa-glass__chevron" aria-hidden />
          </Link>
          <Link
            to="/personal/utang/lent"
            className="personal-qa-glass personal-qa-glass--lent personal-qa-glass--md personal-qa-glass--card-soft personal-qa-glass--no-shadow personal-qa-glass--icon-soft personal-qa-glass--no-icon-shadow"
            data-testid="personal-qa-lent"
            data-recipe="ext org-ehg qa-glass lent md icon-soft card-soft trail-chevron no-shadow no-icon-shadow"
          >
            <span className="personal-qa-glass__icon personal-qa-glass__icon--soft" aria-hidden="true">
              <ArrowUp className="personal-quick-actions__pennant-icon size-[0.95rem] shrink-0" />
            </span>
            <span className="personal-qa-glass__label">{t("personal.home.actionLent")}</span>
            <ChevronRight className="personal-qa-glass__chevron" aria-hidden />
          </Link>
          <Link
            to="/personal/utang/owe"
            className="personal-qa-glass personal-qa-glass--owe personal-qa-glass--md personal-qa-glass--card-soft personal-qa-glass--no-shadow personal-qa-glass--icon-soft personal-qa-glass--no-icon-shadow"
            data-testid="personal-qa-owe"
            data-recipe="ext org-ehg qa-glass owe md icon-soft card-soft trail-chevron no-shadow no-icon-shadow"
          >
            <span className="personal-qa-glass__icon personal-qa-glass__icon--soft" aria-hidden="true">
              <ArrowDown className="personal-quick-actions__pennant-icon size-[0.95rem] shrink-0" />
            </span>
            <span className="personal-qa-glass__label">{t("personal.home.actionOwe")}</span>
            <ChevronRight className="personal-qa-glass__chevron" aria-hidden />
          </Link>
          <Link
            to="/personal/linked-merchants"
            className="personal-qa-glass personal-qa-glass--stores personal-qa-glass--md personal-qa-glass--card-soft personal-qa-glass--no-shadow personal-qa-glass--icon-soft personal-qa-glass--no-icon-shadow"
            data-testid="personal-qa-stores"
            data-recipe="ext org-ehg qa-glass stores md icon-soft card-soft trail-chevron no-shadow no-icon-shadow"
          >
            <span className="personal-qa-glass__icon personal-qa-glass__icon--soft" aria-hidden="true">
              <HandCoins className="personal-quick-actions__pennant-icon size-[0.95rem] shrink-0" />
            </span>
            <span className="personal-qa-glass__label">{t("personal.home.actionStores")}</span>
            <ChevronRight className="personal-qa-glass__chevron" aria-hidden />
          </Link>
          <Link
            to="/personal/people?add=1"
            className="personal-qa-glass personal-qa-glass--people personal-qa-glass--md personal-qa-glass--card-soft personal-qa-glass--no-shadow personal-qa-glass--icon-soft personal-qa-glass--no-icon-shadow"
            data-testid="personal-qa-people"
            data-recipe="ext org-ehg qa-glass people md icon-soft card-soft trail-chevron no-shadow no-icon-shadow"
          >
            <span className="personal-qa-glass__icon personal-qa-glass__icon--soft" aria-hidden="true">
              <UserPlus className="personal-quick-actions__pennant-icon size-[0.95rem] shrink-0" />
            </span>
            <span className="personal-qa-glass__label">{t("personal.home.actionPeople")}</span>
            <ChevronRight className="personal-qa-glass__chevron" aria-hidden />
          </Link>
        </div>
      </section>

      <section
        className="catalog-form-section exits-animate-panel personal-section personal-todo-summary gap-3 max-md:gap-1.5"
        aria-label={t("personal.home.todoSummary")}
        data-testid="personal-todo-summary"
      >
        <div className="personal-todo-summary__header">
          <h2 className="personal-todo-summary__title" data-testid="personal-todo-summary-ribbon">
            <span
              className="personal-todo-summary__pennant"
              aria-hidden
              data-testid="personal-todo-summary-pennant"
            >
              <ListTodo className="personal-todo-summary__pennant-icon size-[1.05rem] shrink-0" />
            </span>
            {t("personal.home.todoSummary")}
          </h2>
          <Link
            to="/personal/todo"
            className="personal-todo-summary__view-all"
            data-testid="personal-todo-view-all"
          >
            {t("personal.home.viewAllTodo")}
          </Link>
        </div>
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
                className="personal-qa-glass personal-qa-glass--primary personal-qa-glass--md personal-qa-glass--card-soft personal-qa-glass--no-shadow personal-qa-glass--icon-soft personal-qa-glass--no-icon-shadow"
                data-testid="personal-todo-add"
                data-recipe="ext org-ehg qa-glass primary md icon-soft card-soft trail-chevron no-shadow no-icon-shadow"
              >
                <span className="personal-qa-glass__icon personal-qa-glass__icon--soft" aria-hidden="true">
                  <ListPlus className="personal-todo-summary__pennant-icon size-[0.95rem] shrink-0" />
                </span>
                <span className="personal-qa-glass__label">{t("personal.home.actionTodo")}</span>
                <ChevronRight className="personal-qa-glass__chevron" aria-hidden />
              </Link>
              <Link
                to={todoAgendaTabHref("today")}
                className="personal-qa-glass personal-qa-glass--primary personal-qa-glass--md personal-qa-glass--card-soft personal-qa-glass--no-shadow personal-qa-glass--icon-soft personal-qa-glass--no-icon-shadow"
                data-testid="personal-todo-stat-today"
                data-recipe="ext org-ehg qa-glass primary md icon-soft card-soft trail-count no-shadow no-icon-shadow"
              >
                <span className="personal-qa-glass__icon personal-qa-glass__icon--soft" aria-hidden="true">
                  <Clock3 className="personal-todo-summary__pennant-icon size-[0.95rem] shrink-0" />
                </span>
                <span className="personal-qa-glass__label">{t("personal.todo.countToday")}</span>
                <strong className="personal-qa-glass__badge tabular-nums">{counts.today}</strong>
              </Link>
              <Link
                to={todoAgendaTabHref("upcoming")}
                className="personal-qa-glass personal-qa-glass--primary personal-qa-glass--md personal-qa-glass--card-soft personal-qa-glass--no-shadow personal-qa-glass--icon-soft personal-qa-glass--no-icon-shadow"
                data-testid="personal-todo-stat-upcoming"
                data-recipe="ext org-ehg qa-glass primary md icon-soft card-soft trail-count no-shadow no-icon-shadow"
              >
                <span className="personal-qa-glass__icon personal-qa-glass__icon--soft" aria-hidden="true">
                  <Calendar className="personal-todo-summary__pennant-icon size-[0.95rem] shrink-0" />
                </span>
                <span className="personal-qa-glass__label">{t("personal.todo.countUpcoming")}</span>
                <strong className="personal-qa-glass__badge tabular-nums">{counts.upcoming}</strong>
              </Link>
              <Link
                to={todoAgendaTabHref("overdue")}
                className="personal-qa-glass personal-qa-glass--todo-overdue personal-qa-glass--md personal-qa-glass--card-soft personal-qa-glass--no-shadow personal-qa-glass--icon-soft personal-qa-glass--no-icon-shadow"
                data-testid="personal-todo-stat-overdue"
                data-recipe="ext org-ehg qa-glass todo-overdue md icon-soft card-soft trail-count no-shadow no-icon-shadow"
              >
                <span className="personal-qa-glass__icon personal-qa-glass__icon--soft" aria-hidden="true">
                  <TriangleAlert className="personal-todo-summary__pennant-icon size-[0.95rem] shrink-0" />
                </span>
                <span className="personal-qa-glass__label">{t("personal.todo.countOverdue")}</span>
                <strong className="personal-qa-glass__badge tabular-nums">{counts.overdue}</strong>
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

      {attentionItems.length > 0 ? (
        <section
          className="catalog-form-section exits-animate-panel personal-section personal-needs-attention gap-2 max-md:gap-1.5"
          aria-label={t("personal.home.needsAttention")}
          data-testid="personal-needs-attention"
        >
          <div className="personal-needs-attention__header">
            <h2 className="personal-needs-attention__title" data-testid="personal-needs-attention-ribbon">
              <span
                className="personal-needs-attention__pennant"
                aria-hidden
                data-testid="personal-needs-attention-pennant"
              >
                <BellRing className="personal-needs-attention__pennant-icon size-[1.05rem] shrink-0" />
              </span>
              {t("personal.home.needsAttention")}
            </h2>
            <Link
              to="/personal/utang"
              className="personal-needs-attention__view-all"
              data-testid="personal-attention-view-all"
            >
              {t("personal.home.viewAllUtang")}
            </Link>
          </div>
          <ul className="personal-needs-attention__list m-0 grid list-none gap-2 p-0">
            {attentionItems.map((item) => (
              <li key={item.key}>
                <Link
                  to={item.href}
                  className="personal-qa-glass personal-qa-glass--warn personal-qa-glass--md personal-qa-glass--card-soft personal-qa-glass--no-shadow personal-qa-glass--icon-soft personal-qa-glass--no-icon-shadow personal-attention-row"
                  data-shell="org-ehg"
                  data-testid={`personal-attention-${item.kind}`}
                  data-recipe="ext org-ehg qa-glass warn md icon-soft card-soft trail-count no-shadow no-icon-shadow"
                >
                  <span className="personal-qa-glass__icon personal-qa-glass__icon--soft" aria-hidden="true">
                    <BellRing className="size-[0.95rem] shrink-0" />
                  </span>
                  <span className="personal-qa-glass__label personal-attention-row__title">
                    {item.displayName && item.kind !== "pendingConfirmation" ? (
                      <>
                        <span className="personal-attention-row__name">{item.displayName}</span>
                        {t(
                          item.kind === "overdue"
                            ? "personal.home.attentionOverdueOne"
                            : "personal.home.attentionDueSoonOne",
                        ).replace("{name}", "")}
                      </>
                    ) : (
                      attentionTitle(item)
                    )}
                  </span>
                  <strong className="personal-qa-glass__badge tabular-nums" aria-hidden="true">
                    {item.count}
                  </strong>
                </Link>
              </li>
            ))}
          </ul>
        </section>
      ) : null}
        </aside>
      </div>
    </div>
  );
}
