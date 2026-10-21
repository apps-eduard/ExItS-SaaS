import { useEffect, useMemo, useRef, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Link } from "react-router-dom";
import { symbol } from "@/components/icons/MaterialSymbol";

const ArrowDownwardIcon = symbol("arrow_downward");
const ArrowForwardIcon = symbol("arrow_forward");
const ArrowUpwardIcon = symbol("arrow_upward");
const BusinessCenterIcon = symbol("business_center");
const ChevronRightIcon = symbol("chevron_right");
const InfoIcon = symbol("info");
const PaymentsIcon = symbol("payments");
const PlaylistAddIcon = symbol("playlist_add");
const ChecklistIcon = symbol("checklist");
const TodayIcon = symbol("today");
const OverdueIcon = symbol("event_busy");
const UpcomingIcon = symbol("event_upcoming");
const RefreshIcon = symbol("refresh");
const StoreIcon = symbol("store");
const PersonAddIcon = symbol("person_add");
const WalletIcon = symbol("account_balance_wallet");
const BoltIcon = symbol("bolt");
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
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { InfoPopover } from "@/components/exits/InfoPopover";
import { cn } from "@/lib/cn";
import { PersonAvatar } from "@/components/exits/PersonAvatar";
import { PersonalBusinessSummary } from "@/features/personal/businesses/PersonalBusinessSummary";
import { PersonalGuideHomeCard } from "@/features/personal/guide/PersonalGuideHomeCard";
import { PersonalTrackerCard } from "@/features/personal/PersonalTrackerCard";
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
      <PersonalTrackerCard dashboard={dashboard} metricsOutline />

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
            <div
              className="personal-home-metrics personal-home-metrics--pair"
              data-testid="personal-stores-to-pay-meta"
            >
              <Link
                to="/personal/linked-merchants"
                className="personal-home-index-card personal-home-index-card--info personal-home-index-card--solid"
                data-testid="personal-stat-stores"
              >
                <span className="personal-home-index-card__ribbon" aria-hidden="true">
                  <span className="personal-home-index-card__fold" />
                  <StoreIcon className="personal-home-index-card__icon" />
                </span>
                <span className="personal-home-index-card__body">
                  <span className="personal-home-index-card__title">{t("personal.home.stores")}</span>
                </span>
                <strong className="personal-home-index-card__badge personal-home-index-card__badge--ghost tabular-nums">
                  {storesToPayQuery.data.storeCount}
                </strong>
              </Link>
              <Link
                to="/personal/linked-merchants"
                className="personal-home-index-card personal-home-index-card--primary personal-home-index-card--solid"
                data-testid="personal-stat-stores-active"
              >
                <span className="personal-home-index-card__ribbon" aria-hidden="true">
                  <span className="personal-home-index-card__fold" />
                  <WalletIcon className="personal-home-index-card__icon" />
                </span>
                <span className="personal-home-index-card__body">
                  <span className="personal-home-index-card__title">{t("personal.home.active")}</span>
                </span>
                <strong className="personal-home-index-card__badge personal-home-index-card__badge--ghost tabular-nums">
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
                      className="personal-stores-to-pay-row personal-home-infodata-card personal-home-infodata-card--row"
                      data-testid={`personal-store-row-${store.organizationId}`}
                    >
                      <span className="personal-home-infodata-card__circle" aria-hidden="true">
                        <PersonAvatar
                          name={store.displayName}
                          size="sm"
                          className="personal-home-infodata-card__avatar"
                        />
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
          <Link
            to="/personal/utang"
            className="personal-needs-attention__view-all"
            data-testid="personal-attention-view-all"
          >
            {t("personal.home.viewAllUtang")}
            <ArrowForwardIcon className="personal-needs-attention__view-all-icon" aria-hidden />
          </Link>
          <ul className="m-0 grid list-none gap-2 p-0">
            {attentionItems.map((item) => (
              <li key={item.key}>
                <Link
                  to={item.href}
                  className="personal-attention-row personal-home-infodata-card personal-home-infodata-card--row"
                  data-testid={`personal-attention-${item.kind}`}
                >
                  <span className="personal-home-infodata-card__circle" aria-hidden="true">
                    {item.displayName ? (
                      <PersonAvatar
                        name={item.displayName}
                        size="sm"
                        className="personal-home-infodata-card__avatar"
                      />
                    ) : (
                      <span className="personal-home-infodata-card__avatar flex items-center justify-center rounded-full bg-primary font-bold text-primary-foreground tabular-nums">
                        {item.count}
                      </span>
                    )}
                  </span>
                  <span className="personal-home-infodata-card__body">
                    <span className="personal-home-infodata-card__title personal-attention-row__title">
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
                  </span>
                  <ChevronRightIcon className="personal-attention-row__chevron shrink-0" aria-hidden />
                </Link>
              </li>
            ))}
          </ul>
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
            className="personal-qa-glass personal-qa-glass--start"
            data-testid="personal-qa-start-business"
          >
            <span className="personal-qa-glass__icon" aria-hidden="true">
              <BusinessCenterIcon className="personal-quick-actions__pennant-icon size-[0.95rem] shrink-0" />
            </span>
            <span className="personal-qa-glass__label">{t("personal.home.actionStartBusiness")}</span>
            <ChevronRightIcon className="personal-qa-glass__chevron" aria-hidden />
          </Link>
          <Link
            to="/personal/utang/lent"
            className="personal-qa-glass personal-qa-glass--lent"
            data-testid="personal-qa-lent"
          >
            <span className="personal-qa-glass__icon" aria-hidden="true">
              <ArrowUpwardIcon className="personal-quick-actions__pennant-icon size-[0.95rem] shrink-0" />
            </span>
            <span className="personal-qa-glass__label">{t("personal.home.actionLent")}</span>
            <ChevronRightIcon className="personal-qa-glass__chevron" aria-hidden />
          </Link>
          <Link
            to="/personal/utang/owe"
            className="personal-qa-glass personal-qa-glass--owe"
            data-testid="personal-qa-owe"
          >
            <span className="personal-qa-glass__icon" aria-hidden="true">
              <ArrowDownwardIcon className="personal-quick-actions__pennant-icon size-[0.95rem] shrink-0" />
            </span>
            <span className="personal-qa-glass__label">{t("personal.home.actionOwe")}</span>
            <ChevronRightIcon className="personal-qa-glass__chevron" aria-hidden />
          </Link>
          <Link
            to="/personal/linked-merchants"
            className="personal-qa-glass personal-qa-glass--stores"
            data-testid="personal-qa-stores"
          >
            <span className="personal-qa-glass__icon" aria-hidden="true">
              <PaymentsIcon className="personal-quick-actions__pennant-icon size-[0.95rem] shrink-0" />
            </span>
            <span className="personal-qa-glass__label">{t("personal.home.actionStores")}</span>
            <ChevronRightIcon className="personal-qa-glass__chevron" aria-hidden />
          </Link>
          <Link
            to="/personal/people"
            className="personal-qa-glass personal-qa-glass--people"
            data-testid="personal-qa-people"
          >
            <span className="personal-qa-glass__icon" aria-hidden="true">
              <PersonAddIcon className="personal-quick-actions__pennant-icon size-[0.95rem] shrink-0" />
            </span>
            <span className="personal-qa-glass__label">{t("personal.home.actionPeople")}</span>
            <ChevronRightIcon className="personal-qa-glass__chevron" aria-hidden />
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
                className="personal-home-index-card personal-home-index-card--info personal-home-index-card--solid"
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
                  className="personal-home-index-card__badge personal-home-index-card__badge--plus personal-home-index-card__badge--ghost"
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
                <strong className="personal-home-index-card__badge personal-home-index-card__badge--ghost tabular-nums">
                  {counts.today}
                </strong>
              </Link>
              <Link
                to={todoAgendaTabHref("upcoming")}
                className="personal-home-index-card personal-home-index-card--info personal-home-index-card--solid"
                data-testid="personal-todo-stat-upcoming"
              >
                <span className="personal-home-index-card__ribbon" aria-hidden="true">
                  <span className="personal-home-index-card__fold" />
                  <UpcomingIcon className="personal-home-index-card__icon" />
                </span>
                <span className="personal-home-index-card__body">
                  <span className="personal-home-index-card__title">{t("personal.todo.countUpcoming")}</span>
                </span>
                <strong className="personal-home-index-card__badge personal-home-index-card__badge--ghost tabular-nums">
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
                <strong className="personal-home-index-card__badge personal-home-index-card__badge--ghost tabular-nums">
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
