import {
  AlertCircle,
  Building2,
  CheckSquare,
  ChevronDown,
  ChevronRight,
  Clock3,
  HandCoins,
  RefreshCw,
  Store,
  UserPlus,
  Users,
  Wallet,
  X,
} from "lucide-react";
import { AnimatePresence, motion, useReducedMotion } from "motion/react";
import { useState } from "react";
import { useTranslation } from "react-i18next";
import type { PersonalHomeData } from "@/api/home-personal-data";
import { dismissGuideHomeCard } from "@/api/personal-guide";
import type { UtangAttentionItem } from "@/api/personal-utang";
import { Button } from "@/components/ui/button";
import { formatCurrency, formatDate, formatNumber } from "@/lib/format";
import { cn } from "@/lib/utils";
import { MotionList, MotionListItem, MotionListPresence } from "@/motion/MotionList";
import { MotionReveal } from "@/motion/MotionReveal";
import { springSoft, tweenFast } from "@/motion/presets";
import { WidgetMessage, isFailedWidget } from "./home-ui";
import { UnavailableButton } from "./UnavailableAction";
import "./personal-home.css";

function greetingPeriodKey():
  | "periodMorning"
  | "periodAfternoon"
  | "periodEvening" {
  const hour = new Date().getHours();
  if (hour < 12) return "periodMorning";
  if (hour < 18) return "periodAfternoon";
  return "periodEvening";
}

function AmountRow({
  label,
  amount,
  tone,
}: {
  label: string;
  amount: number;
  tone: "owe" | "lent";
}) {
  return (
    <UnavailableButton
      className={cn(
        "personal-home__amount",
        tone === "lent" ? "personal-home__amount--lent" : "personal-home__amount--owe",
      )}
    >
      <span className="personal-home__amount-label">{label}</span>
      <span className="personal-home__amount-value">
        {formatCurrency(amount)}
      </span>
    </UnavailableButton>
  );
}

function attentionTone(kind: UtangAttentionItem["kind"]): string {
  if (kind === "pendingConfirmation") return "personal-home__attention-row--pending";
  if (kind === "overdue") return "personal-home__attention-row--overdue";
  return "personal-home__attention-row--dueSoon";
}

function AttentionIcon({ kind }: { kind: UtangAttentionItem["kind"] }) {
  if (kind === "overdue") {
    return <AlertCircle className="size-3.5" aria-hidden />;
  }
  if (kind === "dueSoon") {
    return <Clock3 className="size-3.5" aria-hidden />;
  }
  return <CheckSquare className="size-3.5" aria-hidden />;
}

function PersonalHomeView({
  data,
  loading,
  reveal = true,
  onRetry,
  onGuideDismissed,
}: {
  data: PersonalHomeData | null;
  loading: boolean;
  reveal?: boolean;
  onRetry: () => void;
  onGuideDismissed: () => void;
}) {
  const { t } = useTranslation();
  const reduced = useReducedMotion();
  const [periodKey] = useState(greetingPeriodKey);
  const [todayLabel] = useState(() =>
    formatDate(new Date(), undefined, {
      weekday: "short",
      month: "short",
      day: "numeric",
    }),
  );
  const [metricsOpen, setMetricsOpen] = useState(false);
  const firstName = (data?.profile.displayName ?? "").split(/\s+/)[0] || "there";

  const hasAttention =
    !loading &&
    !!data &&
    data.attention.status === "ready" &&
    (data.attention.data?.items.length ?? 0) > 0;

  const hasBusinesses =
    !loading && !!data && data.businesses.status === "ready";

  return (
    <div className="personal-home">
      <MotionReveal index={0} animate={reveal}>
        <header className="personal-home__header">
          <div className="personal-home__header-meta">
            <p className="personal-home__kicker">{todayLabel}</p>
            <Button
              type="button"
              size="icon"
              variant="ghost"
              className="personal-home__refresh"
              onClick={onRetry}
              disabled={loading && !data}
              aria-label={t("homeData.refresh")}
            >
              <RefreshCw className="size-3.5" aria-hidden />
            </Button>
          </div>
          <h1 className="personal-home__greeting">
            {loading
              ? t("auth.loadingSession")
              : t("home.greeting", {
                  period: t(`home.${periodKey}`),
                  name: firstName,
                })}
          </h1>
        </header>
      </MotionReveal>

      {data?.showGuide ? (
        <MotionReveal index={1} animate={reveal}>
          <div className="personal-home__guide">
            <div className="personal-home__guide-copy">
              <p className="personal-home__guide-title">{t("home.guideTitle")}</p>
              <p className="personal-home__guide-body">{t("home.guideBody")}</p>
            </div>
            <UnavailableButton className="personal-home__guide-cta">
              {t("home.guideContinue")}
            </UnavailableButton>
            <Button
              type="button"
              size="icon"
              variant="ghost"
              className="size-7 shrink-0"
              aria-label={t("common.close")}
              onClick={() => {
                dismissGuideHomeCard(data.profile.userId);
                onGuideDismissed();
              }}
            >
              <X className="size-3.5" />
            </Button>
          </div>
        </MotionReveal>
      ) : null}

      <div className="personal-home__layout">
        <div className="personal-home__main">
          <MotionReveal index={2} animate={reveal}>
            <section
              className="personal-home__hero"
              aria-labelledby="ph-tracker-title"
            >
              <h2 id="ph-tracker-title" className="personal-home__hero-title">
                {t("home.trackerTitle")}
              </h2>

              {loading || !data ? (
                <WidgetMessage status="loading" />
              ) : isFailedWidget(data.tracker) ? (
                <WidgetMessage
                  status={data.tracker.status}
                  message={data.tracker.message}
                  onRetry={onRetry}
                />
              ) : (
                <>
                  <div className="personal-home__scopes">
                    <div className="personal-home__scope">
                      <p className="personal-home__scope-title">
                        {t("home.myRecord")}
                      </p>
                      <div className="personal-home__amounts">
                        <AmountRow
                          label={t("home.iOwed")}
                          amount={data.tracker.data!.myBorrowed}
                          tone="owe"
                        />
                        <AmountRow
                          label={t("home.iLent")}
                          amount={data.tracker.data!.myLent}
                          tone="lent"
                        />
                      </div>
                    </div>
                    <div className="personal-home__scope">
                      <p className="personal-home__scope-title">
                        {t("home.sharedWithMe")}
                      </p>
                      <div className="personal-home__amounts">
                        <AmountRow
                          label={t("home.iOwed")}
                          amount={data.tracker.data!.sharedBorrowed}
                          tone="owe"
                        />
                        <AmountRow
                          label={t("home.iLent")}
                          amount={data.tracker.data!.sharedLent}
                          tone="lent"
                        />
                      </div>
                    </div>
                  </div>

                  <button
                    type="button"
                    className="personal-home__disclosure"
                    aria-expanded={metricsOpen}
                    onClick={() => setMetricsOpen((open) => !open)}
                  >
                    <span>
                      {metricsOpen
                        ? t("home.trackerHideDetails")
                        : t("home.trackerShowDetails")}
                    </span>
                    <motion.span
                      animate={
                        reduced ? undefined : { rotate: metricsOpen ? 180 : 0 }
                      }
                      transition={tweenFast}
                      className="inline-flex"
                    >
                      <ChevronDown className="size-3.5" aria-hidden />
                    </motion.span>
                  </button>

                  <AnimatePresence initial={false}>
                    {metricsOpen ? (
                      <motion.div
                        key="tracker-metrics"
                        initial={
                          reduced ? false : { height: 0, opacity: 0, y: -3 }
                        }
                        animate={{ height: "auto", opacity: 1, y: 0 }}
                        exit={
                          reduced
                            ? undefined
                            : { height: 0, opacity: 0, y: -2 }
                        }
                        transition={springSoft}
                        className="personal-home__metrics-shell"
                      >
                        <div className="personal-home__metrics">
                          <UnavailableButton className="personal-home__metric">
                            <strong>
                              {formatNumber(data.tracker.data!.contactCount)}
                            </strong>
                            <span>{t("home.metricFriends")}</span>
                          </UnavailableButton>
                          <UnavailableButton className="personal-home__metric">
                            <strong>
                              {formatNumber(data.tracker.data!.activeMine)}
                            </strong>
                            <span>{t("home.myRecord")}</span>
                          </UnavailableButton>
                          <UnavailableButton className="personal-home__metric">
                            <strong>
                              {formatNumber(data.tracker.data!.activeShared)}
                            </strong>
                            <span>{t("home.sharedWithMe")}</span>
                          </UnavailableButton>
                        </div>
                      </motion.div>
                    ) : null}
                  </AnimatePresence>
                </>
              )}
            </section>
          </MotionReveal>

          {hasAttention ? (
            <MotionReveal index={3} animate={reveal}>
              <section className="personal-home__section">
                <h2 className="personal-home__label">
                  {t("home.needsAttentionTitle")}
                </h2>
                <MotionListPresence>
                  <MotionList className="personal-home__attention">
                    {data!.attention.data!.items.map((item, index) => (
                      <MotionListItem key={item.key} id={item.key} index={index}>
                        <UnavailableButton
                          className={cn(
                            "personal-home__attention-row",
                            attentionTone(item.kind),
                          )}
                        >
                          <span
                            className="personal-home__attention-icon"
                            aria-hidden
                          >
                            <AttentionIcon kind={item.kind} />
                          </span>
                          <div className="personal-home__attention-meta">
                            <p className="personal-home__attention-title">
                              {item.displayName ??
                                t(`home.utangAttention.${item.kind}`)}
                            </p>
                            <p className="personal-home__attention-status">
                              {t(`home.utangAttention.${item.kind}`)}
                            </p>
                          </div>
                          <span className="personal-home__attention-count">
                            {formatNumber(item.count)}
                          </span>
                          <ChevronRight
                            className="personal-home__attention-chevron size-3.5"
                            aria-hidden
                          />
                        </UnavailableButton>
                      </MotionListItem>
                    ))}
                  </MotionList>
                </MotionListPresence>
              </section>
            </MotionReveal>
          ) : null}

          <MotionReveal index={4} animate={reveal}>
            <section className="personal-home__section">
              <div className="personal-home__section-head">
                <h2 className="personal-home__label">{t("home.todoTitle")}</h2>
                <UnavailableButton className="personal-home__todo-add">
                  {t("home.todoAdd")}
                </UnavailableButton>
              </div>
              {loading || !data ? (
                <WidgetMessage status="loading" />
              ) : isFailedWidget(data.todos) ? (
                <WidgetMessage
                  status={data.todos.status}
                  message={data.todos.message}
                  onRetry={onRetry}
                />
              ) : (
                <div className="personal-home__todo" role="group">
                  {(
                    [
                      ["today", data.todos.data!.today],
                      ["upcoming", data.todos.data!.upcoming],
                      ["overdue", data.todos.data!.overdue],
                    ] as const
                  ).map(([key, count], index) => (
                    <UnavailableButton
                      key={key}
                      className={cn(
                        "personal-home__todo-cell",
                        key === "overdue" && count > 0
                          ? "personal-home__todo-cell--warn"
                          : count === 0 && "personal-home__todo-cell--calm",
                        index > 0 && "personal-home__todo-cell--divided",
                      )}
                    >
                      <strong>{formatNumber(count)}</strong>
                      <span>{t(`home.todo.${key}`)}</span>
                    </UnavailableButton>
                  ))}
                </div>
              )}
            </section>
          </MotionReveal>
        </div>

        <div className="personal-home__aside">
          {hasBusinesses ? (
            <MotionReveal index={5} animate={reveal}>
              <section className="personal-home__section">
                <h2 className="personal-home__label">
                  {t("home.businessesTitle")}
                </h2>
                {data!.businesses.data!.items.length === 0 ? (
                  <p className="personal-home__empty">
                    {t("home.businessesEmpty")}
                  </p>
                ) : (
                  <div className="personal-home__list">
                    {data!.businesses.data!.items.map((biz) => (
                      <div
                        key={`${biz.kind}-${biz.organizationId}`}
                        className="personal-home__list-row"
                      >
                        <span className="personal-home__biz-mark">
                          <Building2 className="size-3.5" aria-hidden />
                        </span>
                        <div className="personal-home__biz-copy">
                          <strong>{biz.displayName}</strong>
                          <span>
                            {biz.roleDisplay}
                            {biz.planDisplayName
                              ? ` · ${biz.planDisplayName}`
                              : ""}
                          </span>
                        </div>
                        <UnavailableButton className="personal-home__deferred">
                          {biz.kind === "owned"
                            ? t("home.openBusiness")
                            : t("home.staffLogin")}
                          <span className="sr-only">
                            {t("homeData.quickActionUnavailable")}
                          </span>
                        </UnavailableButton>
                      </div>
                    ))}
                  </div>
                )}
              </section>
            </MotionReveal>
          ) : null}

          <MotionReveal index={6} animate={reveal}>
            <section className="personal-home__section">
              <h2 className="personal-home__label">
                {t("home.storesToPayTitle")}
              </h2>
              {loading || !data ? (
                <WidgetMessage status="loading" />
              ) : isFailedWidget(data.storesToPay) ? (
                <WidgetMessage
                  status={data.storesToPay.status}
                  message={data.storesToPay.message}
                  onRetry={onRetry}
                />
              ) : data.storesToPay.status === "empty" ||
                data.storesToPay.data!.storeCount === 0 ? (
                <p className="personal-home__empty">{t("homeData.zeroStores")}</p>
              ) : (
                <>
                  <div className="personal-home__store-stats">
                    <span>
                      <strong>
                        {formatNumber(data.storesToPay.data!.storeCount)}
                      </strong>{" "}
                      {t("home.storesCount")}
                    </span>
                    <span>
                      <strong>
                        {formatNumber(data.storesToPay.data!.activeCount)}
                      </strong>{" "}
                      {t("home.storesActive")}
                    </span>
                  </div>
                  {data.storesToPay.data!.preview.length > 0 ? (
                    <MotionListPresence>
                      <MotionList className="personal-home__list">
                        {data.storesToPay.data!.preview.map((row, index) => (
                          <MotionListItem
                            key={`${row.organizationId}-${row.businessCustomerId}`}
                            id={`${row.organizationId}-${row.businessCustomerId}`}
                            index={index}
                          >
                            <UnavailableButton className="personal-home__list-row">
                              <span className="min-w-0 flex-1 truncate text-sm font-medium">
                                {row.displayName}
                              </span>
                              <span className="personal-home__store-amount">
                                {formatCurrency(
                                  row.outstandingBalance,
                                  undefined,
                                  row.currency,
                                )}
                              </span>
                            </UnavailableButton>
                          </MotionListItem>
                        ))}
                      </MotionList>
                    </MotionListPresence>
                  ) : (
                    <p className="personal-home__empty">
                      {t("homeData.zeroStoreBalances")}
                    </p>
                  )}
                </>
              )}
            </section>
          </MotionReveal>

          <MotionReveal index={7} animate={reveal}>
            <section className="personal-home__section">
              <h2 className="personal-home__label">
                {t("home.quickActionsTitle")}
              </h2>
              <div className="personal-home__qa">
                <UnavailableButton className="personal-home__qa-primary">
                  <span className="personal-home__qa-primary-icon">
                    <Building2 className="size-4" aria-hidden />
                  </span>
                  <span className="personal-home__qa-primary-copy">
                    <strong>{t("home.qaStartBusiness")}</strong>
                    <em>{t("homeData.quickActionUnavailable")}</em>
                  </span>
                </UnavailableButton>
                <div className="personal-home__qa-secondary">
                  {(
                    [
                      { id: "lent", label: t("home.qaILent"), icon: HandCoins },
                      { id: "owe", label: t("home.qaIOwe"), icon: Wallet },
                      { id: "stores", label: t("home.qaStores"), icon: Store },
                      {
                        id: "people",
                        label: t("home.qaAddPeople"),
                        icon: UserPlus,
                      },
                    ] as const
                  ).map((action) => {
                    const Icon = action.icon;
                    return (
                      <UnavailableButton
                        key={action.id}
                        className="personal-home__qa-item"
                      >
                        <span className="personal-home__qa-icon">
                          <Icon className="size-3.5" aria-hidden />
                        </span>
                        <span>{action.label}</span>
                      </UnavailableButton>
                    );
                  })}
                </div>
              </div>
            </section>
          </MotionReveal>
        </div>
      </div>

      <MotionReveal index={8} animate={reveal}>
        <p className="personal-home__footnote">
          <Users className="size-3.5 shrink-0" aria-hidden />
          {t("home.personalNavHint")}
        </p>
      </MotionReveal>
    </div>
  );
}

export { PersonalHomeView };
