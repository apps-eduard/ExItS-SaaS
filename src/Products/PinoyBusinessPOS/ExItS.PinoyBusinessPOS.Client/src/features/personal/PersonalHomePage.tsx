import { useQuery } from "@tanstack/react-query";
import {
  Activity,
  AlertCircle,
  CalendarClock,
  CircleDot,
  HandCoins,
  Home,
  ListPlus,
  ListTodo,
  RefreshCw,
  UserPlus,
  Wallet,
} from "lucide-react";
import { getPersonalDashboard } from "@/api/platform/personal-dashboard-client";
import {
  listPersonalTodos,
  summarizeTodoCounts,
  todoAgendaTabHref,
} from "@/api/platform/personal-todo-client";
import { Button } from "@/components/ui/button";
import { ActionTileGrid } from "@/components/exits/ActionTileGrid";
import { ExitsChipBar } from "@/components/exits/ExitsChipBar";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { PageHeader } from "@/components/exits/PageHeader";
import { DashboardMetricCard } from "@/features/reports/DashboardMetricCards";
import { useI18n } from "@/i18n/I18nProvider";

export function PersonalHomePage() {
  const { t } = useI18n();
  const dashboardQuery = useQuery({
    queryKey: ["personal", "dashboard"],
    queryFn: ({ signal }) => getPersonalDashboard(signal),
  });
  const todosQuery = useQuery({
    queryKey: ["personal", "todos"],
    queryFn: ({ signal }) => listPersonalTodos(signal),
  });

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
  const hasActivity =
    dashboard.contactCount > 0 ||
    dashboard.activeRelationshipCount > 0 ||
    dashboard.totalLentBalance > 0 ||
    dashboard.totalBorrowedBalance > 0;

  const counts = todosQuery.isSuccess ? summarizeTodoCounts(todosQuery.data) : null;

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
        aria-label={t("personal.home.utangSummary")}
        data-testid="personal-utang-summary"
      >
        <h2 className="catalog-form-section__title text-muted">{t("personal.home.utangSummary")}</h2>
        <div className="personal-summary-grid personal-summary-grid--utang" role="list">
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
          <DashboardMetricCard
            label={t("personal.home.people")}
            icon={UserPlus}
            testId="personal-stat-people"
            to="/personal/utang/people"
          >
            {dashboard.contactCount}
          </DashboardMetricCard>
          <DashboardMetricCard
            label={t("personal.home.active")}
            icon={Activity}
            testId="personal-stat-active"
            to="/personal/utang"
          >
            {dashboard.activeRelationshipCount}
          </DashboardMetricCard>
        </div>
      </section>

      <section
        className="catalog-form-section exits-animate-panel personal-section gap-3"
        aria-label={t("personal.home.quickActions")}
        data-testid="personal-quick-actions"
      >
        <h2 className="catalog-form-section__title text-muted">{t("personal.home.quickActions")}</h2>
        <ActionTileGrid
          tiles={[
            {
              key: "lent",
              label: t("personal.home.actionLent"),
              icon: HandCoins,
              testId: "personal-qa-lent",
              to: "/personal/utang/lent",
              primary: true,
            },
            {
              key: "owe",
              label: t("personal.home.actionOwe"),
              icon: Wallet,
              testId: "personal-qa-owe",
              to: "/personal/utang/owe",
            },
            {
              key: "people",
              label: t("personal.home.actionPeople"),
              icon: UserPlus,
              testId: "personal-qa-people",
              to: "/personal/utang/people",
            },
            {
              key: "todo",
              label: t("personal.home.actionTodo"),
              icon: ListTodo,
              testId: "personal-qa-todo",
              to: "/personal/todo?add=1",
            },
          ]}
        />
      </section>

      {!hasActivity ? (
        <div className="exits-animate-panel">
          <EmptyState title={t("personal.emptyTitle")} detail={t("personal.emptyDetail")} />
        </div>
      ) : null}

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
              className="personal-summary-grid personal-summary-grid--todo"
              data-testid="personal-todo-counts"
              role="list"
            >
              <DashboardMetricCard
                label={t("personal.todo.countToday")}
                icon={ListTodo}
                tone="emphasis"
                testId="personal-todo-stat-today"
                to={todoAgendaTabHref("today")}
              >
                {counts.today}
              </DashboardMetricCard>
              <DashboardMetricCard
                label={t("personal.todo.countUpcoming")}
                icon={CalendarClock}
                testId="personal-todo-stat-upcoming"
                to={todoAgendaTabHref("upcoming")}
              >
                {counts.upcoming}
              </DashboardMetricCard>
              <DashboardMetricCard
                label={t("personal.todo.countOverdue")}
                icon={AlertCircle}
                tone={counts.overdue > 0 ? "attention" : "default"}
                testId="personal-todo-stat-overdue"
                to={todoAgendaTabHref("overdue")}
              >
                {counts.overdue}
              </DashboardMetricCard>
              <DashboardMetricCard
                label={t("personal.todo.countOpen")}
                icon={CircleDot}
                testId="personal-todo-stat-open"
                to={todoAgendaTabHref("open")}
              >
                {counts.open}
              </DashboardMetricCard>
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
