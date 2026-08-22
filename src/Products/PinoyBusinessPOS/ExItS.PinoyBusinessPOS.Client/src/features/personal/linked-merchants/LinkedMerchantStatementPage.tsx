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
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { useBrowserOnline } from "@/connectivity/browser-online";
import {
  formatLinkedCustomerActivityMeta,
  formatLinkedCustomerActivityTitle,
} from "@/features/personal/linked-merchants/format-linked-customer-activity";
import { useI18n } from "@/i18n/I18nProvider";

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
  const title = formatLinkedCustomerActivityTitle(item);
  const meta = formatLinkedCustomerActivityMeta(item);

  if (item.sourceSaleId && item.hasDetails) {
    return (
      <Link
        to={`/personal/linked-merchants/${organizationId}/${businessCustomerId}/receipts/${item.sourceSaleId}`}
        className="flex min-h-11 items-center gap-2 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 py-2 no-underline text-foreground transition-colors hover:bg-[var(--exits-surface-muted)]"
        data-testid="linked-merchant-activity-receipt-link"
      >
        <span className="min-w-0 flex-1">
          <span className="block truncate text-[length:var(--exits-text-sm)] font-semibold">
            {title}
          </span>
          <span className="block truncate text-[length:var(--exits-text-xs)] text-muted">
            {meta} · {openReceiptLabel}
          </span>
        </span>
        <ChevronRight className="size-4 shrink-0 text-muted" aria-hidden />
      </Link>
    );
  }

  return (
    <div
      className="rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 py-2"
      data-testid="linked-merchant-activity-row"
    >
      <p className="m-0 truncate text-[length:var(--exits-text-sm)] font-semibold">{title}</p>
      <p className="m-0 truncate text-[length:var(--exits-text-xs)] text-muted">{meta}</p>
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
      <div className="flex min-w-0 flex-col gap-4" data-testid="linked-merchant-statement-page">
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
      <div className="flex min-w-0 flex-col gap-4" data-testid="linked-merchant-statement-page">
        <ErrorState title={t("personal.merchantStatement.errorTitle")} detail={state.detail} />
        <Button type="button" className="min-h-11 w-fit" onClick={() => void loadInitial()}>
          {t("orders.retry")}
        </Button>
      </div>
    );
  }

  const { summary, openDebt, openDebtHasMore, recent, recentHasMore } = state;

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="linked-merchant-statement-page">
      <Button asChild variant="ghost" className="min-h-11 w-fit">
        <Link to="/personal/linked-merchants">{t("personal.merchantStatement.backToStores")}</Link>
      </Button>
      <PageHeader
        title={summary.merchantDisplayName ?? t("personal.merchantStatement.title")}
        description={t("personal.merchantStatement.lede")}
      />

      <Card data-testid="linked-merchant-outstanding">
        <p className="m-0 text-[length:var(--exits-text-xs)] font-semibold uppercase tracking-wide text-muted">
          {t("personal.merchantStatement.outstandingLabel")}
        </p>
        <p className="mb-0 mt-1 text-[length:var(--exits-text-xl)] font-semibold tabular-nums">
          {summary.outstandingBalance.toFixed(2)} {summary.currency}
        </p>
        <p className="mb-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
          {summary.customerDisplayName}
        </p>
      </Card>

      {summary.outstandingBalance > 0 ? (
        <section className="flex flex-col gap-2">
          <h2 className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
            {t("personal.merchantStatement.openDebtSection")}
          </h2>
          {openDebt.length === 0 ? (
            <EmptyState
              title={t("personal.merchantStatement.openDebtEmptyTitle")}
              detail={t("personal.merchantStatement.openDebtEmptyDetail")}
            />
          ) : (
            <div className="flex flex-col gap-2">
              {openDebt.map((item) => (
                <ActivityRow
                  key={item.activityId}
                  item={item}
                  organizationId={organizationId}
                  businessCustomerId={businessCustomerId}
                  openReceiptLabel={t("personal.merchantStatement.openReceipt")}
                />
              ))}
              {openDebtHasMore ? (
                <Button
                  type="button"
                  className="min-h-11 w-fit"
                  disabled={busyOpenDebt}
                  data-testid="linked-merchant-open-debt-load-more"
                  onClick={() => void loadMoreOpenDebt()}
                >
                  {t("personal.merchantStatement.loadMore")}
                </Button>
              ) : null}
            </div>
          )}
        </section>
      ) : null}

      <section className="flex flex-col gap-2">
        <h2 className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
          {t("personal.merchantStatement.recentSection")}
        </h2>
        {recent.length === 0 ? (
          <EmptyState
            title={t("personal.merchantStatement.recentEmptyTitle")}
            detail={t("personal.merchantStatement.recentEmptyDetail")}
          />
        ) : (
          <div className="flex flex-col gap-2">
            {recent.map((item) => (
              <ActivityRow
                key={item.activityId}
                item={item}
                organizationId={organizationId}
                businessCustomerId={businessCustomerId}
                openReceiptLabel={t("personal.merchantStatement.openReceipt")}
              />
            ))}
            {recentHasMore ? (
              <Button
                type="button"
                className="min-h-11 w-fit"
                disabled={busyRecent}
                data-testid="linked-merchant-recent-load-more"
                onClick={() => void loadMoreRecent()}
              >
                {t("personal.merchantStatement.loadMore")}
              </Button>
            ) : null}
          </div>
        )}
      </section>

      <section className="flex flex-col gap-2">
        <h2 className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
          {t("personal.merchantStatement.olderSection")}
        </h2>
        {olderLocked ? (
          <Card data-testid="linked-merchant-older-locked">
            <p className="m-0 text-[length:var(--exits-text-sm)]">
              {t("personal.merchantStatement.historyLocked")}
            </p>
            <Button asChild className="mt-3 min-h-11 w-fit">
              <Link to="/personal/rewards">{t("personal.merchantStatement.historyUnlock")}</Link>
            </Button>
          </Card>
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
          <div className="flex flex-col gap-2">
            {olderItems.map((item) => (
              <ActivityRow
                key={item.activityId}
                item={item}
                organizationId={organizationId}
                businessCustomerId={businessCustomerId}
                openReceiptLabel={t("personal.merchantStatement.openReceipt")}
              />
            ))}
            {olderHasMore ? (
              <Button
                type="button"
                className="min-h-11 w-fit"
                disabled={busyOlder}
                data-testid="linked-merchant-older-load-more"
                onClick={() => void loadMoreOlder()}
              >
                {t("personal.merchantStatement.loadMore")}
              </Button>
            ) : null}
          </div>
        )}
      </section>
    </div>
  );
}
