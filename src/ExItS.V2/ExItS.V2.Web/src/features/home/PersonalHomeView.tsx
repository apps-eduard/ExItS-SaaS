import { useState } from "react";
import { useTranslation } from "react-i18next";
import type { PersonalHomeData } from "@/api/home-personal-data";
import type { UtangAttentionItem } from "@/api/personal-utang";
import { Button } from "@/components/ui/button";
import { Avatar, AvatarFallback } from "@/components/ui/avatar";
import { DeferredNav } from "@/features/personal/DeferredNav";
import { MaterialIcon } from "@/features/personal/MaterialIcon";
import { MoneyDisplay } from "@/features/personal/MoneyDisplay";
import { PersonalBusinessSummary } from "@/features/personal/PersonalBusinessSummary";
import { PersonalGuideHomeCard } from "@/features/personal/PersonalGuideHomeCard";
import { PersonalTrackerCard } from "@/features/personal/PersonalTrackerCard";
import { ORG_EHG_QA_GLASS_CLASS_NAME } from "@/features/personal/org-ehg-qa-glass";
import { formatDate } from "@/lib/format";
import { cn } from "@/lib/utils";
import { WidgetMessage, isFailedWidget } from "./home-ui";

function greetingPeriodKey():
  | "greetingMorning"
  | "greetingAfternoon"
  | "greetingEvening" {
  const hour = new Date().getHours();
  if (hour < 12) return "greetingMorning";
  if (hour < 17) return "greetingAfternoon";
  return "greetingEvening";
}

function attentionTitle(
  item: UtangAttentionItem,
  t: (key: string) => string,
): string {
  if (item.kind === "pendingConfirmation") {
    return t("personal.home.attentionPending");
  }
  if (item.kind === "overdue") {
    if (item.displayName) {
      return t("personal.home.attentionOverdueOne").replace(
        "{name}",
        item.displayName,
      );
    }
    return t("personal.home.attentionOverdue");
  }
  if (item.displayName) {
    return t("personal.home.attentionDueSoonOne").replace(
      "{name}",
      item.displayName,
    );
  }
  return t("personal.home.attentionDueSoon");
}

function PersonalHomeView({
  data,
  loading,
  onRetry,
  onGuideDismissed,
}: {
  data: PersonalHomeData | null;
  loading: boolean;
  onRetry: () => void;
  onGuideDismissed: () => void;
}) {
  const { t } = useTranslation();
  const [periodKey] = useState(greetingPeriodKey);
  const [dateChipLabel] = useState(() =>
    formatDate(new Date(), undefined, {
      weekday: "short",
      month: "short",
      day: "numeric",
      year: "numeric",
    }),
  );

  if (loading && !data) {
    return (
      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted" data-testid="personal-home-loading">
        {t("personal.home.loading")}
      </p>
    );
  }

  if (!data) {
    return (
      <div className="personal-page exits-page flex min-w-0 flex-col gap-3">
        <p className="m-0 font-semibold">{t("personal.home.loadErrorTitle")}</p>
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
          {t("personal.home.loadErrorDetail")}
        </p>
        <Button type="button" className="personal-error-retry w-full" onClick={onRetry}>
          <MaterialIcon name="refresh" className="size-4 shrink-0" />
          {t("personal.home.retry")}
        </Button>
      </div>
    );
  }

  const displayName = data.profile.displayName;
  const qaClass = (identity: string) =>
    cn(
      "personal-qa-glass",
      `personal-qa-glass--${identity}`,
      ORG_EHG_QA_GLASS_CLASS_NAME,
    );

  return (
    <div
      className="personal-shell personal-page personal-home-page exits-page flex min-w-0 flex-col gap-3"
      data-testid="personal-home-page"
    >
      <header className="personal-home-hero" data-testid="personal-home-welcome">
        <div className="personal-home-hero__main min-w-0">
          <span className="personal-home-hero__avatar-wrap" aria-hidden>
            <span className="personal-home-hero__avatar" data-testid="personal-home-avatar">
              <Avatar className="personal-home-hero__avatar-fallback size-full">
                <AvatarFallback>{data.profile.initials}</AvatarFallback>
              </Avatar>
            </span>
          </span>
          <div className="personal-home-hero__copy min-w-0">
            <p className="personal-home-hero__greeting m-0">
              {t(`personal.home.${periodKey}`)}
            </p>
            <h1 className="personal-home-hero__name m-0 min-w-0 truncate">
              {displayName}{" "}
              <span className="personal-home-hero__wave" aria-hidden>
                👋
              </span>
            </h1>
          </div>
          <span className="personal-home-hero__info relative inline-flex shrink-0">
            <Button
              type="button"
              variant="ghost"
              size="icon"
              className="shrink-0"
              aria-label={t("personal.info.open")}
              title={t("personal.home.heroSubtitle")}
              data-testid="personal-home-info"
            >
              <MaterialIcon name="info" className="size-5" />
            </Button>
          </span>
        </div>
        <aside className="personal-home-hero__date" data-testid="personal-home-date-chip">
          <span className="personal-home-hero__date-icon" aria-hidden>
            <MaterialIcon name="today" className="size-5" />
          </span>
          <span className="personal-home-hero__date-copy">
            <strong className="personal-home-hero__date-label">{dateChipLabel}</strong>
            <span className="personal-home-hero__date-tail">
              {t("personal.home.dateChipTail")}
            </span>
          </span>
        </aside>
      </header>

      {data.showGuide ? (
        <PersonalGuideHomeCard
          userId={data.profile.userId}
          onDismissed={onGuideDismissed}
        />
      ) : null}

      <div className="personal-home-layout" data-testid="personal-home-layout">
        <div className="personal-home-layout__main flex min-w-0 flex-col gap-3">
          {data.businesses.status === "ready" && data.businesses.data ? (
            <PersonalBusinessSummary items={data.businesses.data.items} />
          ) : null}

          {isFailedWidget(data.tracker) ? (
            <WidgetMessage status={data.tracker.status} message={data.tracker.message} />
          ) : data.tracker.status === "ready" && data.tracker.data ? (
            <PersonalTrackerCard dashboard={data.tracker.data} metricsOutline />
          ) : null}

          <section
            className="catalog-form-section exits-animate-panel personal-section personal-stores-to-pay gap-3 max-md:gap-1.5"
            aria-label={t("personal.home.storesToPay")}
            data-testid="personal-stores-to-pay"
          >
            <div className="personal-stores-to-pay__header">
              <h2
                className="personal-stores-to-pay__title"
                data-testid="personal-stores-to-pay-ribbon"
              >
                <span
                  className="personal-stores-to-pay__pennant"
                  aria-hidden
                  data-testid="personal-stores-to-pay-pennant"
                >
                  <MaterialIcon
                    name="store"
                    className="personal-stores-to-pay__pennant-icon size-[1.05rem] shrink-0"
                  />
                </span>
                {t("personal.home.storesToPay")}
              </h2>
            </div>
            {isFailedWidget(data.storesToPay) ? (
              <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                {t("personal.home.storesUnavailable")}
              </p>
            ) : data.storesToPay.status === "ready" ||
              data.storesToPay.status === "empty" ? (
              <>
                <div
                  className="personal-stores-to-pay__metrics"
                  data-testid="personal-stores-to-pay-meta"
                >
                  <DeferredNav
                    className={qaClass("stores")}
                    testId="personal-stat-stores"
                    data-recipe="ext org-ehg qa-glass stores md icon-soft card-soft trail-count no-shadow no-icon-shadow"
                  >
                    <span className="personal-qa-glass__icon personal-qa-glass__icon--soft" aria-hidden>
                      <MaterialIcon
                        name="store"
                        className="personal-stores-to-pay__pennant-icon size-[0.95rem] shrink-0"
                      />
                    </span>
                    <span className="personal-qa-glass__label">
                      {t("personal.home.stores")}
                    </span>
                    <strong className="personal-qa-glass__badge tabular-nums">
                      {data.storesToPay.data?.storeCount ?? 0}
                    </strong>
                  </DeferredNav>
                  <DeferredNav
                    className={qaClass("stores")}
                    testId="personal-stat-stores-active"
                    data-recipe="ext org-ehg qa-glass stores md icon-soft card-soft trail-count no-shadow no-icon-shadow"
                  >
                    <span className="personal-qa-glass__icon personal-qa-glass__icon--soft" aria-hidden>
                      <MaterialIcon
                        name="account_balance_wallet"
                        className="personal-stores-to-pay__pennant-icon size-[0.95rem] shrink-0"
                      />
                    </span>
                    <span className="personal-qa-glass__label">
                      {t("personal.home.active")}
                    </span>
                    <strong className="personal-qa-glass__badge tabular-nums">
                      {data.storesToPay.data?.activeCount ?? 0}
                    </strong>
                  </DeferredNav>
                </div>
                {(data.storesToPay.data?.preview.length ?? 0) > 0 ? (
                  <ul
                    className="personal-stores-to-pay-list m-0 grid list-none gap-2 p-0"
                    data-testid="personal-stores-to-pay-list"
                  >
                    {data.storesToPay.data!.preview.map((store) => (
                      <li key={`${store.organizationId}:${store.businessCustomerId}`}>
                        <DeferredNav
                          className="personal-stores-to-pay-row personal-home-infodata-card personal-home-infodata-card--row personal-qa-glass--stores personal-qa-glass--md personal-qa-glass--icon-soft personal-qa-glass--no-icon-shadow"
                          testId={`personal-store-row-${store.organizationId}`}
                          data-shell="org-ehg"
                          data-recipe="ext org-ehg qa-glass stores md icon-soft no-icon-shadow"
                        >
                          <span className="personal-qa-glass__icon personal-qa-glass__icon--soft" aria-hidden>
                            <MaterialIcon
                              name="store"
                              className="personal-stores-to-pay__pennant-icon size-[0.95rem] shrink-0"
                            />
                          </span>
                          <span className="personal-home-infodata-card__body">
                            <span className="personal-home-infodata-card__title truncate">
                              {store.displayName}
                            </span>
                          </span>
                          <span className="personal-stores-to-pay-row__amount shrink-0 tabular-nums">
                            <MoneyDisplay
                              amount={store.outstandingBalance}
                              currency={store.currency}
                            />
                          </span>
                        </DeferredNav>
                      </li>
                    ))}
                  </ul>
                ) : (
                  <p
                    className="personal-stores-empty m-0 text-[length:var(--exits-text-sm)] text-muted"
                    data-testid="personal-stores-to-pay-empty"
                  >
                    {(data.storesToPay.data?.storeCount ?? 0) === 0
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
          {data.attention.status === "ready" &&
          (data.attention.data?.items.length ?? 0) > 0 ? (
            <section
              className="catalog-form-section exits-animate-panel personal-section personal-needs-attention gap-2 max-md:gap-1.5"
              aria-label={t("personal.home.needsAttention")}
              data-testid="personal-needs-attention"
            >
              <div className="personal-needs-attention__header">
                <h2
                  className="personal-needs-attention__title"
                  data-testid="personal-needs-attention-ribbon"
                >
                  <span
                    className="personal-needs-attention__pennant"
                    aria-hidden
                    data-testid="personal-needs-attention-pennant"
                  >
                    <MaterialIcon
                      name="notifications"
                      className="personal-needs-attention__pennant-icon size-[1.05rem] shrink-0"
                    />
                  </span>
                  {t("personal.home.needsAttention")}
                </h2>
                <DeferredNav
                  className="personal-needs-attention__view-all"
                  testId="personal-attention-view-all"
                >
                  {t("personal.home.viewAllUtang")}
                </DeferredNav>
              </div>
              <ul className="personal-needs-attention__list m-0 grid list-none gap-2 p-0">
                {data.attention.data!.items.map((item) => (
                  <li key={item.key}>
                    <DeferredNav
                      className={cn(qaClass("warn"), "personal-attention-row")}
                      testId={`personal-attention-${item.kind}`}
                      data-shell="org-ehg"
                      data-recipe="ext org-ehg qa-glass warn md icon-soft card-soft trail-count no-shadow no-icon-shadow"
                    >
                      <span className="personal-qa-glass__icon personal-qa-glass__icon--soft" aria-hidden>
                        <MaterialIcon name="notifications" className="size-[0.95rem] shrink-0" />
                      </span>
                      <span className="personal-qa-glass__label personal-attention-row__title">
                        {attentionTitle(item, t)}
                      </span>
                      <strong className="personal-qa-glass__badge tabular-nums" aria-hidden>
                        {item.count}
                      </strong>
                    </DeferredNav>
                  </li>
                ))}
              </ul>
            </section>
          ) : null}

          <section
            className="catalog-form-section exits-animate-panel personal-section personal-quick-actions gap-3 max-md:gap-1.5"
            aria-label={t("personal.home.quickActions")}
            data-testid="personal-quick-actions"
          >
            <div className="personal-quick-actions__header">
              <h2
                className="personal-quick-actions__title"
                data-testid="personal-quick-actions-ribbon"
              >
                <span
                  className="personal-quick-actions__pennant"
                  aria-hidden
                  data-testid="personal-quick-actions-pennant"
                >
                  <MaterialIcon
                    name="bolt"
                    className="personal-quick-actions__pennant-icon size-[1.05rem] shrink-0"
                  />
                </span>
                {t("personal.home.quickActions")}
              </h2>
            </div>
            <div className="personal-quick-actions__cards" data-testid="personal-quick-actions-cards">
              {(
                [
                  ["start", "business_center", "actionStartBusiness", "personal-qa-start-business"],
                  ["lent", "arrow_upward", "actionLent", "personal-qa-lent"],
                  ["owe", "arrow_downward", "actionOwe", "personal-qa-owe"],
                  ["stores", "payments", "actionStores", "personal-qa-stores"],
                  ["people", "person_add", "actionPeople", "personal-qa-people"],
                ] as const
              ).map(([identity, icon, labelKey, testId]) => (
                <DeferredNav
                  key={testId}
                  className={qaClass(identity)}
                  testId={testId}
                  data-recipe={`ext org-ehg qa-glass ${identity} md icon-soft card-soft trail-chevron no-shadow no-icon-shadow`}
                >
                  <span className="personal-qa-glass__icon personal-qa-glass__icon--soft" aria-hidden>
                    <MaterialIcon
                      name={icon}
                      className="personal-quick-actions__pennant-icon size-[0.95rem] shrink-0"
                    />
                  </span>
                  <span className="personal-qa-glass__label">
                    {t(`personal.home.${labelKey}`)}
                  </span>
                  <MaterialIcon name="chevron_right" className="personal-qa-glass__chevron" />
                </DeferredNav>
              ))}
            </div>
          </section>

          <section
            className="catalog-form-section exits-animate-panel personal-section personal-todo-summary gap-3 max-md:gap-1.5"
            aria-label={t("personal.home.todoSummary")}
            data-testid="personal-todo-summary"
          >
            <div className="personal-todo-summary__header">
              <h2
                className="personal-todo-summary__title"
                data-testid="personal-todo-summary-ribbon"
              >
                <span
                  className="personal-todo-summary__pennant"
                  aria-hidden
                  data-testid="personal-todo-summary-pennant"
                >
                  <MaterialIcon
                    name="checklist"
                    className="personal-todo-summary__pennant-icon size-[1.05rem] shrink-0"
                  />
                </span>
                {t("personal.home.todoSummary")}
              </h2>
              <DeferredNav
                className="personal-todo-summary__view-all"
                testId="personal-todo-view-all"
              >
                {t("personal.home.viewAllTodo")}
              </DeferredNav>
            </div>
            {isFailedWidget(data.todos) ? (
              <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                {t("personal.home.todoUnavailable")}
              </p>
            ) : data.todos.status === "ready" && data.todos.data ? (
              <div className="personal-todo-cards" data-testid="personal-todo-counts" role="list">
                <DeferredNav
                  className={qaClass("todo")}
                  testId="personal-todo-add"
                  data-recipe="ext org-ehg qa-glass todo md icon-soft card-soft trail-chevron no-shadow no-icon-shadow"
                >
                  <span className="personal-qa-glass__icon personal-qa-glass__icon--soft" aria-hidden>
                    <MaterialIcon
                      name="playlist_add"
                      className="personal-todo-summary__pennant-icon size-[0.95rem] shrink-0"
                    />
                  </span>
                  <span className="personal-qa-glass__label">
                    {t("personal.home.actionTodo")}
                  </span>
                  <MaterialIcon name="chevron_right" className="personal-qa-glass__chevron" />
                </DeferredNav>
                {(
                  [
                    ["today", "today", "countToday", data.todos.data.today, "personal-todo-stat-today", "todo"],
                    [
                      "upcoming",
                      "event_upcoming",
                      "countUpcoming",
                      data.todos.data.upcoming,
                      "personal-todo-stat-upcoming",
                      "todo",
                    ],
                    [
                      "overdue",
                      "event_busy",
                      "countOverdue",
                      data.todos.data.overdue,
                      "personal-todo-stat-overdue",
                      "todo-overdue",
                    ],
                  ] as const
                ).map(([, icon, labelKey, count, testId, identity]) => (
                  <DeferredNav
                    key={testId}
                    className={qaClass(identity)}
                    testId={testId}
                    data-recipe={`ext org-ehg qa-glass ${identity} md icon-soft card-soft trail-count no-shadow no-icon-shadow`}
                  >
                    <span className="personal-qa-glass__icon personal-qa-glass__icon--soft" aria-hidden>
                      <MaterialIcon
                        name={icon}
                        className="personal-todo-summary__pennant-icon size-[0.95rem] shrink-0"
                      />
                    </span>
                    <span className="personal-qa-glass__label">
                      {t(`personal.todo.${labelKey}`)}
                    </span>
                    <strong className="personal-qa-glass__badge tabular-nums">{count}</strong>
                  </DeferredNav>
                ))}
              </div>
            ) : null}
          </section>
        </aside>
      </div>
    </div>
  );
}

export { PersonalHomeView };
