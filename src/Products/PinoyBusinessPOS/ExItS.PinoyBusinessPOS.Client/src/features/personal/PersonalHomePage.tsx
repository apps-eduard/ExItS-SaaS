import { useMemo } from "react";
import { useQuery } from "@tanstack/react-query";
import { Link } from "react-router-dom";
import {
  Building2,
  ChevronRight,
  HandCoins,
  Home,
  ListPlus,
  ListTodo,
  RefreshCw,
  Store,
  UserPlus,
  Wallet,
  WalletCards,
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
import { ExitsChipBar } from "@/components/exits/ExitsChipBar";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { PageHeader } from "@/components/exits/PageHeader";
import { DashboardMetricCard } from "@/features/reports/DashboardMetricCards";
import { loadStoresToPayPreview } from "@/features/personal/stores-to-pay";
import {
  buildHomeAttentionItems,
  isActiveUtangAccount,
  mergeUtangAccounts,
} from "@/features/personal/utang/utang-workspace";
import { useI18n } from "@/i18n/I18nProvider";
import { useBrowserOnline } from "@/connectivity/browser-online";

export function PersonalHomePage() {
  const { t } = useI18n();
  const online = useBrowserOnline();

  const dashboardQuery = useQuery({
    queryKey: ["personal", "dashboard"],
    queryFn: ({ signal }) => getPersonalDashboard(signal),
  });
  const todosQuery = useQuery({
    queryKey: ["personal", "todos"],
    queryFn: ({ signal }) => listPersonalTodos(signal),
  });
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
            className="personal-error-retry min-h-11 w-full"
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
  const counts = todosQuery.isSuccess ? summarizeTodoCounts(todosQuery.data) : null;

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
      className="personal-page exits-page flex min-w-0 flex-col gap-3"
      data-testid="personal-home-page"
    >
      <PageHeader
        title={t("personal.title")}
        titleIcon={Home}
        description={t("personal.lede")}
      />

      <section
        className="catalog-form-section exits-animate-panel personal-section gap-3"
        aria-label={t("personal.home.personalTracker")}
        data-testid="personal-utang-summary"
      >
        <h2 className="catalog-form-section__title personal-todo-create-form__title text-muted">
          <WalletCards
            className="personal-todo-create-form__title-icon size-[1.1rem] shrink-0"
            aria-hidden
          />
          {t("personal.home.personalTracker")}
        </h2>
        <div className="personal-summary-grid personal-summary-grid--balances" role="list">
          <DashboardMetricCard
            label={t("personal.home.owedToMe")}
            icon={HandCoins}
            tone="emphasis"
            testId="personal-stat-lent"
            to="/personal/utang/lent"
          >
            <MoneyDisplay amount={dashboard.totalLentBalance} />
          </DashboardMetricCard>
          <DashboardMetricCard
            label={t("personal.home.iOwe")}
            icon={Wallet}
            tone={dashboard.totalBorrowedBalance > 0 ? "attention" : "default"}
            testId="personal-stat-borrowed"
            to="/personal/utang/owe"
          >
            <MoneyDisplay amount={dashboard.totalBorrowedBalance} />
          </DashboardMetricCard>
        </div>
        <div
          className="personal-home-meta flex flex-wrap gap-x-4 gap-y-1 text-[length:var(--exits-text-sm)] text-muted"
          data-testid="personal-home-meta"
        >
          <Link to="/personal/utang/people" className="text-muted no-underline hover:underline">
            <span data-testid="personal-stat-people">
              {t("personal.home.people")}: {dashboard.contactCount}
            </span>
          </Link>
          <Link to="/personal/utang" className="text-muted no-underline hover:underline">
            <span data-testid="personal-stat-active">
              {t("personal.home.active")}: {dashboard.activeRelationshipCount}
            </span>
          </Link>
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
                      className="personal-stores-to-pay-row exits-list__card flex min-h-11 items-center justify-between gap-3 text-foreground no-underline"
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
              className="personal-home-meta flex flex-wrap gap-x-4 gap-y-1 text-[length:var(--exits-text-sm)] text-muted"
              data-testid="personal-stores-to-pay-meta"
            >
              <Link
                to="/personal/linked-merchants"
                className="text-muted no-underline hover:underline"
              >
                <span data-testid="personal-stat-stores">
                  {t("personal.home.stores")}: {storesToPayQuery.data.storeCount}
                </span>
              </Link>
              <Link
                to="/personal/linked-merchants"
                className="text-muted no-underline hover:underline"
              >
                <span data-testid="personal-stat-stores-active">
                  {t("personal.home.active")}: {storesToPayQuery.data.activeCount}
                </span>
              </Link>
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
                  className="personal-attention-row exits-list__card flex min-h-11 items-center justify-between gap-3 text-foreground no-underline"
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
              to: "/personal/explore-pos",
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
              to: "/personal/utang/people",
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
        {todosQuery.isPending ? (
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("personal.todo.loading")}
          </p>
        ) : todosQuery.isError ? (
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("personal.home.todoUnavailable")}
          </p>
        ) : counts ? (
          <>
            <div
              className="personal-todo-compact flex flex-wrap gap-x-4 gap-y-1 text-[length:var(--exits-text-sm)]"
              data-testid="personal-todo-counts"
              role="list"
            >
              <Link
                to={todoAgendaTabHref("today")}
                className="text-foreground no-underline"
                data-testid="personal-todo-stat-today"
              >
                {t("personal.todo.countToday")}{" "}
                <span className="font-semibold tabular-nums">{counts.today}</span>
              </Link>
              <Link
                to={todoAgendaTabHref("overdue")}
                className="text-foreground no-underline"
                data-testid="personal-todo-stat-overdue"
              >
                {t("personal.todo.countOverdue")}{" "}
                <span className="font-semibold tabular-nums">{counts.overdue}</span>
              </Link>
              <Link
                to={todoAgendaTabHref("upcoming")}
                className="text-foreground no-underline"
                data-testid="personal-todo-stat-upcoming"
              >
                {t("personal.todo.countUpcoming")}{" "}
                <span className="font-semibold tabular-nums">{counts.upcoming}</span>
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
    </div>
  );
}
