import { useEffect, useMemo, useRef, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Link } from "react-router-dom";
import {
  Building2,
  ChevronRight,
  HandCoins,
  Home,
  Info,
  ListPlus,
  ListTodo,
  RefreshCw,
  Share2,
  Store,
  UserPlus,
  Users,
  Wallet,
  Zap,
} from "lucide-react";
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
import { ActionTileGrid } from "@/components/exits/ActionTileGrid";
import { CountBadge, CountChip } from "@/components/exits/CountChip";
import { ExitsChipBar } from "@/components/exits/ExitsChipBar";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { InfoPopover } from "@/components/exits/InfoPopover";
import { cn } from "@/lib/cn";
import { StatusChip } from "@/components/exits/StatusChip";
import { DashboardMetricCard } from "@/features/reports/DashboardMetricCards";
import { PersonalBusinessSummary } from "@/features/personal/businesses/PersonalBusinessSummary";
import { PersonalGuideHomeCard } from "@/features/personal/guide/PersonalGuideHomeCard";
import { loadStoresToPayPreview } from "@/features/personal/stores-to-pay";
import {
  UTANG_OWNERSHIP_MINE,
  UTANG_OWNERSHIP_SHARED,
  UTANG_READ_ONLY_CHIP,
} from "@/features/personal/utang/utang-ownership-ui";
import {
  buildHomeAttentionItems,
  isActiveUtangAccount,
  mergeUtangAccounts,
} from "@/features/personal/utang/utang-workspace";
import { useI18n } from "@/i18n/I18nProvider";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { usePersonalOfflineContext } from "@/offline/personal-offline-context";
import { listCachedPersonalTodos } from "@/offline/personal-todo-cache";

const personalSoftPrimaryButtonClass =
  "border border-border bg-[color-mix(in_srgb,var(--exits-primary)_10%,var(--exits-surface))] text-[var(--exits-primary)] hover:border-[var(--exits-primary)] hover:bg-[color-mix(in_srgb,var(--exits-primary)_14%,var(--exits-surface))]";

const personalSoftInfoButtonClass =
  "border border-border bg-[color-mix(in_srgb,var(--exits-severity-info)_12%,var(--exits-surface))] text-[var(--exits-severity-info)] hover:border-[var(--exits-severity-info)] hover:bg-[color-mix(in_srgb,var(--exits-severity-info)_16%,var(--exits-surface))]";

export function PersonalHomePage() {
  const { t } = useI18n();
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
        <div className="flex min-w-0 items-center gap-1.5">
          <span className="page-header__title-icon shrink-0" aria-hidden>
            <Home className="size-5" />
          </span>
          <h1 className="page-header__title exits-type-page-title m-0 min-w-0 truncate">
            {t("personal.title")}
          </h1>
          <span ref={infoButtonRef} className="inline-flex shrink-0">
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
          </span>
        </div>
        {infoOpen ? (
          <InfoPopover
            id="personal-home-info-popover"
            titleId="personal-home-info-title"
            title={t("personal.info.title")}
            anchorRef={infoButtonRef}
          >
            <p className="m-0 mt-2 text-[length:var(--exits-text-sm)] text-muted">{t("personal.lede")}</p>
          </InfoPopover>
        ) : null}
      </header>

      <PersonalGuideHomeCard />
      <PersonalBusinessSummary />

      <div className="personal-home-layout" data-testid="personal-home-layout">
        <div className="personal-home-layout__main flex min-w-0 flex-col gap-3">
      <section
        className="catalog-form-section exits-animate-panel personal-section gap-3"
        aria-label={t("personal.home.personalTracker")}
        data-testid="personal-utang-summary"
      >
        <h2 className="catalog-form-section__title personal-todo-create-form__title text-muted">
          {t("personal.home.personalTracker")}
        </h2>
        <div className="flex min-w-0 flex-col gap-3">
          <div className="flex min-w-0 flex-col gap-2" data-testid="personal-utang-owed-to-me">
            <h3 className="m-0 inline-flex items-center gap-1.5 text-[length:var(--exits-text-sm)] font-semibold text-muted">
              <HandCoins className="size-[1.1rem] shrink-0 text-primary" aria-hidden />
              {t("personal.home.owedToMe")}
            </h3>
            <div className="personal-summary-grid personal-summary-grid--balances" role="list">
              <DashboardMetricCard
                label={
                  <StatusChip
                    tone={UTANG_OWNERSHIP_MINE.chipTone}
                    appearance={UTANG_OWNERSHIP_MINE.appearance}
                    shape={UTANG_OWNERSHIP_MINE.shape}
                    data-testid="personal-stat-lent-mine-label"
                  >
                    {t("personal.utang.ownershipMine")}
                  </StatusChip>
                }
                icon={HandCoins}
                tone={UTANG_OWNERSHIP_MINE.metricTone}
                testId="personal-stat-lent-mine"
                to="/personal/utang/lent"
              >
                <MoneyDisplay amount={dashboard.totalLentBalance} />
              </DashboardMetricCard>
              <DashboardMetricCard
                label={
                  <StatusChip
                    tone={UTANG_OWNERSHIP_SHARED.chipTone}
                    appearance={UTANG_OWNERSHIP_SHARED.appearance}
                    shape={UTANG_OWNERSHIP_SHARED.shape}
                    data-testid="personal-stat-lent-shared-label"
                  >
                    {t("personal.home.activeShared")}
                  </StatusChip>
                }
                icon={HandCoins}
                tone={UTANG_OWNERSHIP_SHARED.metricTone}
                testId="personal-stat-lent-shared"
                to="/personal/utang/lent"
                tag={
                  <StatusChip
                    tone={UTANG_READ_ONLY_CHIP.tone}
                    appearance={UTANG_READ_ONLY_CHIP.appearance}
                    shape={UTANG_READ_ONLY_CHIP.shape}
                    data-testid="personal-stat-lent-shared-readonly"
                  >
                    {t("personal.utang.readOnly")}
                  </StatusChip>
                }
              >
                <MoneyDisplay amount={dashboard.sharedWithMeLentBalance} />
              </DashboardMetricCard>
            </div>
          </div>
          <div
            className="flex min-w-0 flex-col gap-2 border-t border-border pt-3"
            data-testid="personal-utang-i-owe"
          >
            <h3 className="m-0 inline-flex items-center gap-1.5 text-[length:var(--exits-text-sm)] font-semibold text-muted">
              <Wallet className="size-[1.1rem] shrink-0 text-primary" aria-hidden />
              {t("personal.home.iOwe")}
            </h3>
            <div className="personal-summary-grid personal-summary-grid--balances" role="list">
              <DashboardMetricCard
                label={
                  <StatusChip
                    tone={UTANG_OWNERSHIP_MINE.chipTone}
                    appearance={UTANG_OWNERSHIP_MINE.appearance}
                    shape={UTANG_OWNERSHIP_MINE.shape}
                    data-testid="personal-stat-borrowed-mine-label"
                  >
                    {t("personal.utang.ownershipMine")}
                  </StatusChip>
                }
                icon={Wallet}
                tone={UTANG_OWNERSHIP_MINE.metricTone}
                testId="personal-stat-borrowed-mine"
                to="/personal/utang/owe"
              >
                <MoneyDisplay amount={dashboard.totalBorrowedBalance} />
              </DashboardMetricCard>
              <DashboardMetricCard
                label={
                  <StatusChip
                    tone={UTANG_OWNERSHIP_SHARED.chipTone}
                    appearance={UTANG_OWNERSHIP_SHARED.appearance}
                    shape={UTANG_OWNERSHIP_SHARED.shape}
                    data-testid="personal-stat-borrowed-shared-label"
                  >
                    {t("personal.home.activeShared")}
                  </StatusChip>
                }
                icon={Wallet}
                tone={UTANG_OWNERSHIP_SHARED.metricTone}
                testId="personal-stat-borrowed-shared"
                to="/personal/utang/owe"
                tag={
                  <StatusChip
                    tone={UTANG_READ_ONLY_CHIP.tone}
                    appearance={UTANG_READ_ONLY_CHIP.appearance}
                    shape={UTANG_READ_ONLY_CHIP.shape}
                    data-testid="personal-stat-borrowed-shared-readonly"
                  >
                    {t("personal.utang.readOnly")}
                  </StatusChip>
                }
              >
                <MoneyDisplay amount={dashboard.sharedWithMeBorrowedBalance} />
              </DashboardMetricCard>
            </div>
          </div>
        </div>
        <div
          className="personal-home-meta personal-home-meta--scroll flex flex-wrap items-center gap-2 border-t border-border pt-3"
          data-testid="personal-home-meta"
        >
          <Button asChild intent="success" appearance="solid" emphasis="soft" shape="auto">
            <Link to="/personal/people" data-testid="personal-stat-people">
              <Users className="size-4 shrink-0" aria-hidden="true" />
              {t("personal.home.people")}
              <CountBadge count={dashboard.contactCount} tone="primary" />
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
            <Link to="/personal/utang" data-testid="personal-stat-active">
              <HandCoins className="size-4 shrink-0" aria-hidden="true" />
              {t("personal.home.activeMine")}
              <CountBadge count={dashboard.activeRelationshipCount} tone="primary" />
            </Link>
          </Button>
          <Button
            asChild
            intent="info"
            appearance="solid"
            emphasis="soft"
            shape="auto"
            className={personalSoftInfoButtonClass}
          >
            <Link to="/personal/utang" data-testid="personal-stat-active-shared">
              <Share2 className="size-4 shrink-0" aria-hidden="true" />
              {t("personal.home.activeShared")}
              <CountBadge count={dashboard.sharedWithMeActiveCount ?? 0} tone="primary" />
            </Link>
          </Button>
        </div>
      </section>

      <section
        className="catalog-form-section exits-animate-panel personal-section gap-3"
        aria-label={t("personal.home.storesToPay")}
        data-testid="personal-stores-to-pay"
      >
        <h2 className="catalog-form-section__title personal-todo-create-form__title text-muted">
          <Store
            className="personal-todo-create-form__title-icon size-[1.1rem] shrink-0"
            aria-hidden
          />
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
                className="m-0 text-[length:var(--exits-text-sm)] text-muted"
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
                  <Store className="size-4 shrink-0" aria-hidden="true" />
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
                  <Wallet className="size-4 shrink-0" aria-hidden="true" />
                  {t("personal.home.active")}
                  <CountBadge count={storesToPayQuery.data.activeCount} tone="primary" />
                </Link>
              </Button>
            </div>
          </>
        ) : null}
      </section>

      {attentionItems.length > 0 ? (
        <section
          className="catalog-form-section exits-animate-panel personal-section gap-2"
          aria-label={t("personal.home.needsAttention")}
          data-testid="personal-needs-attention"
        >
          <h2 className="catalog-form-section__title text-muted">
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
                  <ChevronRight className="size-4 shrink-0 text-muted" aria-hidden />
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
        </div>

        <aside
          className="personal-home-layout__aside flex min-w-0 flex-col gap-3"
          aria-label={t("personal.home.quickActions")}
        >
      <section
        className="catalog-form-section exits-animate-panel personal-section gap-3"
        aria-label={t("personal.home.quickActions")}
        data-testid="personal-quick-actions"
      >
        <h2 className="catalog-form-section__title personal-todo-create-form__title text-muted">
          <Zap
            className="personal-todo-create-form__title-icon size-[1.1rem] shrink-0"
            aria-hidden
          />
          {t("personal.home.quickActions")}
        </h2>
        <ActionTileGrid
          emphasizePrimary
          tiles={[
            {
              key: "start-business",
              label: t("personal.home.actionStartBusiness"),
              icon: Building2,
              testId: "personal-qa-start-business",
              to: "/personal/businesses",
              primary: true,
            },
            {
              key: "lent",
              label: t("personal.home.actionLent"),
              icon: HandCoins,
              testId: "personal-qa-lent",
              to: "/personal/utang/lent",
            },
            {
              key: "owe",
              label: t("personal.home.actionOwe"),
              icon: Wallet,
              testId: "personal-qa-owe",
              to: "/personal/utang/owe",
            },
            {
              key: "stores",
              label: t("personal.home.actionStores"),
              icon: Store,
              testId: "personal-qa-stores",
              to: "/personal/linked-merchants",
            },
            {
              key: "people",
              label: t("personal.home.actionPeople"),
              icon: UserPlus,
              testId: "personal-qa-people",
              to: "/personal/people",
            },
          ]}
        />
      </section>

      <section
        className="catalog-form-section exits-animate-panel personal-section gap-3"
        aria-label={t("personal.home.todoSummary")}
        data-testid="personal-todo-summary"
      >
        <h2 className="catalog-form-section__title personal-todo-create-form__title text-muted">
          <ListTodo className="personal-todo-create-form__title-icon size-[1.1rem] shrink-0" aria-hidden />
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
            <div
              className="personal-todo-compact flex flex-wrap items-center gap-2"
              data-testid="personal-todo-counts"
              role="list"
            >
              <Link
                to={todoAgendaTabHref("today")}
                className="no-underline"
                data-testid="personal-todo-stat-today"
              >
                <CountChip
                  label={t("personal.todo.countToday")}
                  count={counts.today}
                  tone="primary"
                  shape="soft"
                />
              </Link>
              <Link
                to={todoAgendaTabHref("overdue")}
                className="no-underline"
                data-testid="personal-todo-stat-overdue"
              >
                <CountChip
                  label={t("personal.todo.countOverdue")}
                  count={counts.overdue}
                  tone="danger"
                  shape="soft"
                />
              </Link>
              <Link
                to={todoAgendaTabHref("upcoming")}
                className="no-underline"
                data-testid="personal-todo-stat-upcoming"
              >
                <CountChip
                  label={t("personal.todo.countUpcoming")}
                  count={counts.upcoming}
                  tone="info"
                  shape="soft"
                  className="border-[color-mix(in_srgb,var(--exits-severity-info)_28%,transparent)] bg-[color-mix(in_srgb,var(--exits-severity-info)_12%,transparent)] text-[var(--exits-severity-info)]"
                />
              </Link>
            </div>
            {counts.open === 0 && counts.completed === 0 ? (
              <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                {t("personal.home.todoEmpty")}
              </p>
            ) : null}
          </>
        ) : null}
        <ExitsChipBar
          variant="actions"
          ariaLabel={t("personal.home.actionTodo")}
          testId="personal-todo-add-link"
          items={[
            {
              key: "add",
              label: t("personal.home.actionTodo"),
              icon: <ListPlus />,
              href: "/personal/todo?add=1",
              testId: "personal-todo-add",
              emphasis: "primary",
            },
          ]}
        />
      </section>
        </aside>
      </div>
    </div>
  );
}
