import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { getPersonalDashboard } from "@/api/platform/personal-dashboard-client";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { PageHeader } from "@/components/exits/PageHeader";
import { useI18n } from "@/i18n/I18nProvider";

export function PersonalHomePage() {
  const { t } = useI18n();
  const dashboardQuery = useQuery({
    queryKey: ["personal", "dashboard"],
    queryFn: ({ signal }) => getPersonalDashboard(signal),
  });

  if (dashboardQuery.isPending) {
    return <LoadingSkeleton label={t("personal.home.loading")} />;
  }

  if (dashboardQuery.isError) {
    return (
      <div className="flex flex-col gap-3">
        <ErrorState
          title={t("personal.home.loadErrorTitle")}
          detail={t("personal.home.loadErrorDetail")}
        />
        <Button
          type="button"
          className="min-h-11 w-fit"
          onClick={() => void dashboardQuery.refetch()}
        >
          {t("personal.home.retry")}
        </Button>
      </div>
    );
  }

  const dashboard = dashboardQuery.data;
  const hasActivity =
    dashboard.contactCount > 0 ||
    dashboard.activeRelationshipCount > 0 ||
    dashboard.totalLentBalance > 0 ||
    dashboard.totalBorrowedBalance > 0;

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="personal-home-page">
      <PageHeader title={t("personal.title")} description={t("personal.lede")} />

      <section
        aria-label={t("personal.home.utangSummary")}
        className="grid min-w-0 grid-cols-2 gap-3 sm:grid-cols-4"
        data-testid="personal-utang-summary"
      >
        <SummaryTile label={t("personal.home.owedToMe")} testId="personal-stat-lent">
          <MoneyDisplay amount={dashboard.totalLentBalance} />
        </SummaryTile>
        <SummaryTile label={t("personal.home.iOwe")} testId="personal-stat-borrowed">
          <MoneyDisplay amount={dashboard.totalBorrowedBalance} />
        </SummaryTile>
        <SummaryTile label={t("personal.home.people")} testId="personal-stat-people">
          <span className="text-[length:var(--exits-text-lg)] font-semibold">
            {dashboard.contactCount}
          </span>
        </SummaryTile>
        <SummaryTile label={t("personal.home.active")} testId="personal-stat-active">
          <span className="text-[length:var(--exits-text-lg)] font-semibold">
            {dashboard.activeRelationshipCount}
          </span>
        </SummaryTile>
      </section>

      <section aria-label={t("personal.home.quickActions")} data-testid="personal-quick-actions">
        <h2 className="m-0 mb-2 text-[length:var(--exits-text-sm)] font-semibold text-foreground">
          {t("personal.home.quickActions")}
        </h2>
        <div className="flex flex-wrap gap-2">
          <Button asChild className="min-h-11" data-testid="personal-qa-lent">
            <Link to="/personal/utang/lent">{t("personal.home.actionLent")}</Link>
          </Button>
          <Button asChild variant="ghost" className="min-h-11" data-testid="personal-qa-owe">
            <Link to="/personal/utang/owe">{t("personal.home.actionOwe")}</Link>
          </Button>
          <Button asChild variant="ghost" className="min-h-11" data-testid="personal-qa-people">
            <Link to="/personal/utang/people">{t("personal.home.actionPeople")}</Link>
          </Button>
          <Button asChild variant="ghost" className="min-h-11" data-testid="personal-qa-todo">
            <Link to="/personal/todo">{t("personal.home.actionTodo")}</Link>
          </Button>
        </div>
      </section>

      {!hasActivity ? (
        <EmptyState title={t("personal.emptyTitle")} detail={t("personal.emptyDetail")} />
      ) : null}

      <section aria-label={t("personal.home.todoSummary")} data-testid="personal-todo-summary">
        <h2 className="m-0 mb-1 text-[length:var(--exits-text-sm)] font-semibold text-foreground">
          {t("personal.home.todoSummary")}
        </h2>
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
          {t("personal.home.todoSoon")}
        </p>
      </section>
    </div>
  );
}

function SummaryTile({
  label,
  children,
  testId,
}: {
  label: string;
  children: React.ReactNode;
  testId: string;
}) {
  return (
    <div
      data-testid={testId}
      className="min-w-0 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 py-3"
    >
      <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">{label}</p>
      <div className="mt-1 min-w-0 truncate text-foreground">{children}</div>
    </div>
  );
}
