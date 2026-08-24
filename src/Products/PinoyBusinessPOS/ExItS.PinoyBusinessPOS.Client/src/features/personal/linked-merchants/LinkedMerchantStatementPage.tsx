import { useCallback, useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { ChevronRight } from "lucide-react";
import {
  getLinkedCustomerStatement,
  isExtendedHistoryRequiredError,
  listLinkedCustomerOpenDebtActivity,
  listLinkedCustomerOlderActivity,
  listLinkedCustomerRecentActivity,
  type LinkedCustomerActivityItem,
  type LinkedCustomerStatementSummary,
} from "@/api/pos/pos-linked-customers-client";
import { PosApiError } from "@/api/pos/pos-http";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { CommerceLoadMore } from "@/features/customer-ordering/personal-commerce-ui";
import {
  formatLinkedCustomerActivityAmount,
  formatLinkedCustomerActivityMeta,
} from "@/features/personal/linked-merchants/format-linked-customer-activity";
import { cn } from "@/lib/cn";
import { useI18n } from "@/i18n/I18nProvider";
import { personalPageBackNav } from "@/navigation/page-back-nav";

type LoadState =
  | { kind: "loading" }
  | { kind: "offline" }
  | { kind: "forbidden"; detail: string }
  | { kind: "notFound"; detail: string }
  | { kind: "error"; detail: string }
  | {
      kind: "ready";
      summary: LinkedCustomerStatementSummary;
      openDebt: LinkedCustomerActivityItem[];
      openDebtHasMore: boolean;
      recent: LinkedCustomerActivityItem[];
      recentHasMore: boolean;
    };

function ActivityRow({
  item,
  organizationId,
  businessCustomerId,
  openReceiptLabel,
}: {
  item: LinkedCustomerActivityItem;
  organizationId: string;
  businessCustomerId: string;
  openReceiptLabel: string;
}) {
  const title = item.referenceNumber;
  const meta = formatLinkedCustomerActivityMeta(item);
  const amount = formatLinkedCustomerActivityAmount(item);

  const content = (
    <>
      <div className="pc-activity-row__main">
        <span className="pc-activity-row__title">{title}</span>
        <span className="pc-activity-row__meta">
          {meta}
          {item.sourceSaleId && item.hasDetails ? ` · ${openReceiptLabel}` : ""}
        </span>
      </div>
      {amount ? (
        <span
          className={cn(
            "pc-activity-row__amount",
            amount.kind === "charge" && "pc-activity-row__amount--charge",
            amount.kind === "payment" && "pc-activity-row__amount--payment",
            amount.kind === "neutral" && "pc-activity-row__amount--neutral",
          )}
        >
          {amount.text}
        </span>
      ) : null}
      {item.sourceSaleId && item.hasDetails ? (
        <ChevronRight className="size-4 shrink-0 text-muted" aria-hidden />
      ) : null}
    </>
  );

  if (item.sourceSaleId && item.hasDetails) {
    return (
      <Link
        to={`/personal/linked-merchants/${organizationId}/${businessCustomerId}/receipts/${item.sourceSaleId}`}
        className="pc-activity-row pc-activity-row--clickable"
        data-testid="linked-merchant-activity-receipt-link"
      >
        {content}
      </Link>
    );
  }

  return (
    <div className="pc-activity-row" data-testid="linked-merchant-activity-row">
      {content}
    </div>
  );
}

export function LinkedMerchantStatementPage() {
  const { t } = useI18n();
  const online = useBrowserOnline();
  const { organizationId = "", businessCustomerId = "" } = useParams<{
    organizationId: string;
    businessCustomerId: string;
  }>();
  const [state, setState] = useState<LoadState>({ kind: "loading" });
  const [openDebtPage, setOpenDebtPage] = useState(1);
  const [recentPage, setRecentPage] = useState(1);
  const [olderPage, setOlderPage] = useState(1);
  const [olderItems, setOlderItems] = useState<LinkedCustomerActivityItem[]>([]);
  const [olderHasMore, setOlderHasMore] = useState(false);
  const [olderLocked, setOlderLocked] = useState(false);
  const [olderLoadAttempted, setOlderLoadAttempted] = useState(false);
  const [busyOpenDebt, setBusyOpenDebt] = useState(false);
  const [busyRecent, setBusyRecent] = useState(false);
  const [busyOlder, setBusyOlder] = useState(false);

  const pageShell =
    "personal-page personal-commerce-page linked-merchant-statement-page exits-page flex min-w-0 flex-col gap-3";

  const loadInitial = useCallback(async () => {
    if (!organizationId || !businessCustomerId) {
      setState({ kind: "notFound", detail: t("personal.merchantStatement.missing") });
      return;
    }
    if (!online) {
      setState({ kind: "offline" });
      return;
    }

    setState({ kind: "loading" });
    setOlderItems([]);
    setOlderHasMore(false);
    setOlderLocked(false);
    setOlderLoadAttempted(false);
    setOpenDebtPage(1);
    setRecentPage(1);
    setOlderPage(1);

    try {
      const summary = await getLinkedCustomerStatement(organizationId, businessCustomerId);
      let openDebt: LinkedCustomerActivityItem[] = [];
      let openDebtHasMore = false;

      if (summary.outstandingBalance > 0) {
        const open = await listLinkedCustomerOpenDebtActivity(organizationId, businessCustomerId, {
          page: 1,
          pageSize: 10,
        });
        openDebt = open.items;
        openDebtHasMore = open.hasMore;
      }

      const recent = await listLinkedCustomerRecentActivity(organizationId, businessCustomerId, {
        page: 1,
        pageSize: 10,
      });

      setState({
        kind: "ready",
        summary,
        openDebt,
        openDebtHasMore,
        recent: recent.items,
        recentHasMore: recent.hasMore,
      });
    } catch (err) {
      if (err instanceof PosApiError) {
        if (err.status === 403) {
          setState({ kind: "forbidden", detail: err.message });
          return;
        }
        if (err.status === 404) {
          setState({ kind: "notFound", detail: err.message });
          return;
        }
      }
      setState({
        kind: "error",
        detail: err instanceof Error ? err.message : t("personal.merchantStatement.loadFailed"),
      });
    }
  }, [businessCustomerId, online, organizationId, t]);

  useEffect(() => {
    void loadInitial();
  }, [loadInitial]);

  async function loadMoreOpenDebt() {
    if (state.kind !== "ready" || busyOpenDebt || !state.openDebtHasMore) {
      return;
    }
    setBusyOpenDebt(true);
    try {
      const nextPage = openDebtPage + 1;
      const open = await listLinkedCustomerOpenDebtActivity(organizationId, businessCustomerId, {
        page: nextPage,
        pageSize: 10,
      });
      setOpenDebtPage(nextPage);
      setState({
        ...state,
        openDebt: [...state.openDebt, ...open.items],
        openDebtHasMore: open.hasMore,
      });
    } finally {
      setBusyOpenDebt(false);
    }
  }

  async function loadMoreRecent() {
    if (state.kind !== "ready" || busyRecent || !state.recentHasMore) {
      return;
    }
    setBusyRecent(true);
    try {
      const nextPage = recentPage + 1;
      const recent = await listLinkedCustomerRecentActivity(organizationId, businessCustomerId, {
        page: nextPage,
        pageSize: 10,
      });
      setRecentPage(nextPage);
      setState({
        ...state,
        recent: [...state.recent, ...recent.items],
        recentHasMore: recent.hasMore,
      });
    } finally {
      setBusyRecent(false);
    }
  }

  async function loadOlder() {
    if (busyOlder) {
      return;
    }
    setBusyOlder(true);
    setOlderLocked(false);
    try {
      const older = await listLinkedCustomerOlderActivity(organizationId, businessCustomerId, {
        page: 1,
        pageSize: 10,
      });
      setOlderLoadAttempted(true);
      setOlderPage(1);
      setOlderItems(older.items);
      setOlderHasMore(older.hasMore);
    } catch (err) {
      setOlderLoadAttempted(true);
      if (isExtendedHistoryRequiredError(err)) {
        setOlderLocked(true);
        return;
      }
    } finally {
      setBusyOlder(false);
    }
  }

  async function loadMoreOlder() {
    if (busyOlder || !olderHasMore) {
      return;
    }
    setBusyOlder(true);
    try {
      const nextPage = olderPage + 1;
      const older = await listLinkedCustomerOlderActivity(organizationId, businessCustomerId, {
        page: nextPage,
        pageSize: 10,
      });
      setOlderPage(nextPage);
      setOlderItems((prev) => [...prev, ...older.items]);
      setOlderHasMore(older.hasMore);
    } finally {
      setBusyOlder(false);
    }
  }

  if (state.kind === "loading") {
    return <LoadingState label={t("loading.label")} />;
  }

  if (state.kind === "offline") {
    return (
      <div className={pageShell} data-testid="linked-merchant-statement-page">
        <PageHeader
          title={t("personal.merchantStatement.title")}
          backTo={personalPageBackNav.merchants.to}
          backLabel={t(personalPageBackNav.merchants.labelKey)}
          backTestId="page-header-back-merchant-statement"
        />
        <ErrorState
          title={t("offline.internetRequiredTitle")}
          detail={t("offline.requiredHistory")}
        />
        <Button type="button" className="min-h-11 w-fit" onClick={() => void loadInitial()}>
          {t("orders.retry")}
        </Button>
      </div>
    );
  }

  if (state.kind === "forbidden") {
    return (
      <ErrorState
        title={t("personal.merchantStatement.deniedTitle")}
        detail={state.detail || t("personal.merchantStatement.denied")}
      />
    );
  }

  if (state.kind === "notFound") {
    return (
      <ErrorState
        title={t("personal.merchantStatement.missingTitle")}
        detail={state.detail || t("personal.merchantStatement.missing")}
      />
    );
  }

  if (state.kind === "error") {
    return (
      <div className={pageShell} data-testid="linked-merchant-statement-page">
        <PageHeader
          title={t("personal.merchantStatement.title")}
          backTo={personalPageBackNav.merchants.to}
          backLabel={t(personalPageBackNav.merchants.labelKey)}
          backTestId="page-header-back-merchant-statement"
        />
        <ErrorState title={t("personal.merchantStatement.errorTitle")} detail={state.detail} />
        <Button type="button" className="min-h-11 w-fit" onClick={() => void loadInitial()}>
          {t("orders.retry")}
        </Button>
      </div>
    );
  }

  const { summary, openDebt, openDebtHasMore, recent, recentHasMore } = state;

  return (
    <div className={pageShell} data-testid="linked-merchant-statement-page">
      <PageHeader
        title={summary.merchantDisplayName ?? t("personal.merchantStatement.title")}
        description={t("personal.merchantStatement.lede")}
        backTo={personalPageBackNav.merchants.to}
        backLabel={t(personalPageBackNav.merchants.labelKey)}
        backTestId="page-header-back-merchant-statement"
      />

      <section className="pc-balance-hero exits-animate-panel" data-testid="linked-merchant-outstanding">
        <p className="pc-balance-hero__label">{t("personal.merchantStatement.outstandingLabel")}</p>
        <p className="pc-balance-hero__amount">
          {summary.outstandingBalance.toFixed(2)} {summary.currency}
        </p>
        <p className="pc-balance-hero__context">{summary.customerDisplayName}</p>
        {summary.asOfUtc ? (
          <p className="pc-balance-hero__as-of">
            {new Date(summary.asOfUtc).toLocaleString()}
          </p>
        ) : null}
      </section>

      {summary.outstandingBalance > 0 ? (
        <section className="flex flex-col gap-3 exits-animate-panel">
          <h2 className="pc-section-heading">{t("personal.merchantStatement.openDebtSection")}</h2>
          {openDebt.length === 0 ? (
            <EmptyState
              title={t("personal.merchantStatement.openDebtEmptyTitle")}
              detail={t("personal.merchantStatement.openDebtEmptyDetail")}
            />
          ) : (
            <ul className="pc-activity-list">
              {openDebt.map((item) => (
                <li key={item.activityId}>
                  <ActivityRow
                    item={item}
                    organizationId={organizationId}
                    businessCustomerId={businessCustomerId}
                    openReceiptLabel={t("personal.merchantStatement.openReceipt")}
                  />
                </li>
              ))}
            </ul>
          )}
          {openDebt.length > 0 && openDebtHasMore ? (
            <CommerceLoadMore
              label={t("personal.merchantStatement.loadMore")}
              loadingLabel={t("loading.label")}
              busy={busyOpenDebt}
              testId="linked-merchant-open-debt-load-more"
              onClick={() => void loadMoreOpenDebt()}
            />
          ) : null}
        </section>
      ) : null}

      <section className="flex flex-col gap-3 exits-animate-panel">
        <h2 className="pc-section-heading">{t("personal.merchantStatement.recentSection")}</h2>
        {recent.length === 0 ? (
          <EmptyState
            title={t("personal.merchantStatement.recentEmptyTitle")}
            detail={t("personal.merchantStatement.recentEmptyDetail")}
          />
        ) : (
          <ul className="pc-activity-list">
            {recent.map((item) => (
              <li key={item.activityId}>
                <ActivityRow
                  item={item}
                  organizationId={organizationId}
                  businessCustomerId={businessCustomerId}
                  openReceiptLabel={t("personal.merchantStatement.openReceipt")}
                />
              </li>
            ))}
          </ul>
        )}
        {recent.length > 0 && recentHasMore ? (
          <CommerceLoadMore
            label={t("personal.merchantStatement.loadMore")}
            loadingLabel={t("loading.label")}
            busy={busyRecent}
            testId="linked-merchant-recent-load-more"
            onClick={() => void loadMoreRecent()}
          />
        ) : null}
      </section>

      <section className="flex flex-col gap-3 exits-animate-panel">
        <h2 className="pc-section-heading">{t("personal.merchantStatement.olderSection")}</h2>
        {olderLocked ? (
          <div data-testid="linked-merchant-older-locked" className="pc-empty-panel flex flex-col gap-3">
            <p className="m-0 text-[length:var(--exits-text-sm)]">
              {t("personal.merchantStatement.historyLocked")}
            </p>
            <Button asChild className="min-h-11 w-fit">
              <Link to="/personal/rewards">{t("personal.merchantStatement.historyUnlock")}</Link>
            </Button>
          </div>
        ) : olderItems.length === 0 && !olderLoadAttempted ? (
          <Button
            type="button"
            className="min-h-11 w-fit"
            disabled={busyOlder}
            data-testid="linked-merchant-older-load"
            onClick={() => void loadOlder()}
          >
            {t("personal.merchantStatement.olderLoad")}
          </Button>
        ) : olderItems.length === 0 ? (
          <EmptyState
            title={t("personal.merchantStatement.olderEmptyTitle")}
            detail={t("personal.merchantStatement.olderEmptyDetail")}
          />
        ) : (
          <>
            <ul className="pc-activity-list">
              {olderItems.map((item) => (
                <li key={item.activityId}>
                  <ActivityRow
                    item={item}
                    organizationId={organizationId}
                    businessCustomerId={businessCustomerId}
                    openReceiptLabel={t("personal.merchantStatement.openReceipt")}
                  />
                </li>
              ))}
            </ul>
            {olderHasMore ? (
              <CommerceLoadMore
                label={t("personal.merchantStatement.loadMore")}
                loadingLabel={t("loading.label")}
                busy={busyOlder}
                testId="linked-merchant-older-load-more"
                onClick={() => void loadMoreOlder()}
              />
            ) : null}
          </>
        )}
      </section>
    </div>
  );
}
