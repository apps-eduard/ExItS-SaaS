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
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { PageHeader } from "@/components/exits/PageHeader";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { PersonalCommerceNav } from "@/features/customer-ordering/PersonalCommerceNav";
import { CommerceLoadMore } from "@/features/customer-ordering/personal-commerce-ui";
import { useLinkedMerchantShopContext } from "@/features/customer-ordering/useLinkedMerchantShopContext";
import {
  formatLinkedCustomerActivityAmount,
  formatLinkedCustomerActivityLabel,
  formatLinkedCustomerActivityMeta,
  formatLinkedCustomerActivityReference,
} from "@/features/personal/linked-merchants/format-linked-customer-activity";
import { MerchantStatementStatusPanel } from "@/features/personal/linked-merchants/MerchantStatementStatusPanel";
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
  const label = formatLinkedCustomerActivityLabel(item);
  const title = label ?? formatLinkedCustomerActivityReference(item);
  const subtitle = label ? formatLinkedCustomerActivityReference(item) : null;
  const meta = formatLinkedCustomerActivityMeta(item);
  const amount = formatLinkedCustomerActivityAmount(item);

  const content = (
    <>
      <div className="pc-activity-row__main">
        <span className="pc-activity-row__title">{title}</span>
        {subtitle ? <span className="pc-activity-row__meta">{subtitle}</span> : null}
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

  const merchantContextQuery = useLinkedMerchantShopContext(organizationId, Boolean(organizationId));
  const shopTo = organizationId ? `/personal/linked-merchants/${organizationId}/shop` : null;
  const storeName =
    merchantContextQuery.data?.organizationDisplayName ?? t("personal.merchantStatement.title");
  const relationshipLabel = merchantContextQuery.data?.customerDisplayName ?? null;

  const pageShell =
    "personal-page personal-commerce-page linked-merchant-statement-page exits-page flex min-w-0 flex-col gap-3";

  function statementPageHeader(title?: string) {
    return (
      <PageHeader
        title={title ?? storeName}
        description={t("personal.merchantStatement.lede")}
        backTo={personalPageBackNav.merchants.to}
        backLabel={t(personalPageBackNav.merchants.labelKey)}
        backTestId="page-header-back-merchant-statement"
      />
    );
  }

  function statementStatusShell(
    variant: "notFound" | "forbidden" | "error" | "offline",
    detail?: string,
    options?: { onRetry?: () => void; includeShop?: boolean },
  ) {
    return (
      <div className={pageShell} data-testid="linked-merchant-statement-page">
        {statementPageHeader()}
        <PersonalCommerceNav active="stores" />
        <MerchantStatementStatusPanel
          variant={variant}
          storeName={storeName}
          relationshipLabel={relationshipLabel}
          detail={detail}
          onRetry={options?.onRetry}
          shopTo={options?.includeShop === false ? null : shopTo}
        />
      </div>
    );
  }

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
    return (
      <div className={pageShell} data-testid="linked-merchant-statement-page">
        {statementPageHeader()}
        <PersonalCommerceNav active="stores" />
        <LoadingSkeleton label={t("loading.label")} />
      </div>
    );
  }

  if (state.kind === "offline") {
    return statementStatusShell("offline", undefined, { onRetry: () => void loadInitial() });
  }

  if (state.kind === "forbidden") {
    return statementStatusShell("forbidden", state.detail, { includeShop: false });
  }

  if (state.kind === "notFound") {
    return statementStatusShell("notFound", state.detail, { onRetry: () => void loadInitial() });
  }

  if (state.kind === "error") {
    return statementStatusShell("error", state.detail, { onRetry: () => void loadInitial() });
  }

  const { summary, openDebt, openDebtHasMore, recent, recentHasMore } = state;
  const hasNoActivity =
    summary.outstandingBalance <= 0 && openDebt.length === 0 && recent.length === 0;

  return (
    <div className={pageShell} data-testid="linked-merchant-statement-page">
      {statementPageHeader(summary.merchantDisplayName ?? undefined)}
      <PersonalCommerceNav active="stores" />

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

      {hasNoActivity ? (
        <MerchantStatementStatusPanel
          variant="empty"
          storeName={summary.merchantDisplayName ?? storeName}
          relationshipLabel={summary.customerDisplayName}
          shopTo={shopTo}
        />
      ) : (
        <>
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
        </>
      )}
    </div>
  );
}
