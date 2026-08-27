import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useInfiniteQuery } from "@tanstack/react-query";
import { listExpiringLots, type PosExpiringLotDto } from "@/api/pos/pos-inventory-client";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { ExitsChipBar } from "@/components/exits/ExitsChipBar";
import { LoadingState } from "@/components/exits/LoadingState";
import { BackgroundRefreshIndicator } from "@/components/exits/loading/BackgroundRefreshIndicator";
import { PageHeader } from "@/components/exits/PageHeader";
import { pageBackNav } from "@/navigation/page-back-nav";
import { SearchField } from "@/components/exits/SearchField";
import {
  EXPIRY_WINDOWS,
  addLocalDays,
  formatLocalDateOnly,
  type ExpiryWindowCode,
  resolveLotExpiryLabel,
  type LotExpiryLabel,
} from "@/features/inventory/inventory-lot-status";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";
import { BranchRequiredPanel } from "@/features/workspace/BranchRequiredPanel";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

const EXPIRING_PAGE_SIZE = 50;

function formatLotStatus(lot: PosExpiringLotDto, t: ReturnType<typeof useI18n>["t"]): string {
  const label = resolveLotExpiryLabel(lot.expiryStatus, lot.expirationDate);
  switch (label.kind) {
    case "expired":
      return t("inventory.statusExpired");
    case "expiresToday":
      return t("inventory.statusExpiresToday");
    case "expiresInDays":
      return t("inventory.statusExpiresInDays").replace("{days}", String(label.days));
    case "ok":
      return t("inventory.statusOk");
    default:
      return label.status;
  }
}

function statusTone(label: LotExpiryLabel): "expired" | "today" | "near" | "ok" {
  switch (label.kind) {
    case "expired":
      return "expired";
    case "expiresToday":
      return "today";
    case "expiresInDays":
      return "near";
    default:
      return "ok";
  }
}

function windowLabel(code: ExpiryWindowCode, t: ReturnType<typeof useI18n>["t"]): string {
  switch (code) {
    case "Expired":
      return t("inventory.windowExpired");
    case "Days7":
      return t("inventory.windowDays7");
    case "Days14":
      return t("inventory.windowDays14");
    case "Days30":
      return t("inventory.windowDays30");
    case "Custom":
      return t("inventory.windowCustom");
  }
}

export function InventoryExpirationPage() {
  const { t } = useI18n();
  const { boundWorkspace } = useWorkspace();
  const [windowCode, setWindowCode] = useState<ExpiryWindowCode>("Days30");
  const [customFrom, setCustomFrom] = useState(() => formatLocalDateOnly());
  const [customTo, setCustomTo] = useState(() => addLocalDays(formatLocalDateOnly(), 30));
  const [search, setSearch] = useState("");
  const [debounced, setDebounced] = useState("");

  useEffect(() => {
    const handle = window.setTimeout(() => setDebounced(search.trim()), 250);
    return () => window.clearTimeout(handle);
  }, [search]);

  const workspace = useMemo(
    () =>
      boundWorkspace?.branchId
        ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
        : null,
    [boundWorkspace],
  );

  const customRangeValid =
    windowCode !== "Custom" ||
    (Boolean(customFrom) && Boolean(customTo) && customFrom <= customTo);

  const query = useInfiniteQuery({
    queryKey: [
      "inventory",
      "expiring",
      workspace?.organizationId,
      workspace?.branchId,
      windowCode,
      windowCode === "Custom" ? customFrom : null,
      windowCode === "Custom" ? customTo : null,
      debounced,
    ],
    enabled: Boolean(workspace) && customRangeValid,
    initialPageParam: 1,
    queryFn: ({ pageParam, signal }) =>
      listExpiringLots(
        workspace!,
        {
          window: windowCode,
          fromDate: windowCode === "Custom" ? customFrom : undefined,
          toDate: windowCode === "Custom" ? customTo : undefined,
          search: debounced || undefined,
          page: pageParam,
          pageSize: EXPIRING_PAGE_SIZE,
        },
        signal,
      ),
    getNextPageParam: (lastPage) => {
      const loaded = lastPage.page * lastPage.pageSize;
      return loaded < lastPage.totalCount ? lastPage.page + 1 : undefined;
    },
  });

  const items = useMemo(() => {
    const seen = new Set<string>();
    const merged: PosExpiringLotDto[] = [];
    for (const page of query.data?.pages ?? []) {
      for (const lot of page.items) {
        if (seen.has(lot.lotId)) {
          continue;
        }
        seen.add(lot.lotId);
        merged.push(lot);
      }
    }
    return merged;
  }, [query.data]);

  const counts = query.data?.pages[0];

  if (!workspace) {
    return <BranchRequiredPanel title={t("inventory.expirationTitle")} />;
  }

  return (
    <div
      className="inventory-expiration-page exits-page flex min-w-0 flex-col gap-3"
      data-testid="inventory-expiration-page"
    >
      <PageHeader
        title={t("inventory.expirationTitle")}
        description={t("inventory.expirationLede")}
        backTo={pageBackNav.inventory.to}
        backLabel={t(pageBackNav.inventory.labelKey)}
        backTestId="page-header-back-inventory"
      />

      <div className="inventory-expiry-window flex min-w-0 flex-col gap-1.5">
        <span
          id="inventory-expiry-window-label"
          className="inventory-expiry-window__label text-[length:var(--exits-text-sm)] font-medium text-muted"
        >
          {t("inventory.expiryWindow")}
        </span>
        <ExitsChipBar
          variant="filter"
          ariaLabel={t("inventory.expiryWindow")}
          testId="inventory-expiry-window"
          items={EXPIRY_WINDOWS.map((code) => ({
            key: code,
            label: windowLabel(code, t),
            state: windowCode === code ? "active" : "idle",
            testId: `inventory-expiry-window-${code}`,
            onSelect: () => setWindowCode(code),
          }))}
        />

        {windowCode === "Custom" ? (
          <div
            className="inventory-expiry-custom"
            data-testid="inventory-expiry-custom-range"
          >
            <label className="inventory-expiry-custom__field">
              <span className="inventory-expiry-custom__label">{t("inventory.customFrom")}</span>
              <input
                type="date"
                className="inventory-expiry-custom__input"
                value={customFrom}
                max={customTo || undefined}
                data-testid="inventory-expiry-custom-from"
                onChange={(event) => setCustomFrom(event.target.value)}
              />
            </label>
            <label className="inventory-expiry-custom__field">
              <span className="inventory-expiry-custom__label">{t("inventory.customTo")}</span>
              <input
                type="date"
                className="inventory-expiry-custom__input"
                value={customTo}
                min={customFrom || undefined}
                data-testid="inventory-expiry-custom-to"
                onChange={(event) => setCustomTo(event.target.value)}
              />
            </label>
            {!customRangeValid ? (
              <p className="inventory-expiry-custom__hint m-0 text-[length:var(--exits-text-xs)] text-[var(--exits-danger)]">
                {t("inventory.customRangeInvalid")}
              </p>
            ) : null}
          </div>
        ) : null}
      </div>

      <SearchField
        label={t("inventory.searchExpiring")}
        value={search}
        onChange={(event) => setSearch(event.target.value)}
        onClear={() => setSearch("")}
        placeholder={t("inventory.searchExpiring")}
        containerClassName="inventory-expiration-page__search exits-page__search"
      />

      {query.isSuccess && counts ? (
        <div
          className="inventory-expiry-counts flex min-w-0 flex-wrap gap-2"
          data-testid="inventory-expiry-counts"
        >
          <span className="inventory-expiry-counts__stat inventory-expiry-counts__stat--expired">
            {t("inventory.expiryCountExpired").replace("{count}", String(counts.expiredCount))}
          </span>
          <span className="inventory-expiry-counts__stat inventory-expiry-counts__stat--near">
            {t("inventory.expiryCountNear").replace("{count}", String(counts.nearExpiryCount))}
          </span>
        </div>
      ) : null}

      {query.isLoading ? <LoadingState label={t("loading.label")} /> : null}
      {query.isFetching && !query.isLoading && query.data ? (
        <BackgroundRefreshIndicator active label={t("loading.updating")} />
      ) : null}
      {query.isError ? (
        <ErrorState title={t("error.title")} detail={(query.error as Error).message} />
      ) : null}
      {query.isSuccess && items.length === 0 ? (
        <EmptyState
          title={t("inventory.expirationEmpty")}
          detail={t("inventory.expirationEmptyDetail")}
        />
      ) : null}

      <ul
        className="inventory-expiring-list m-0 flex list-none flex-col gap-2 p-0"
        data-testid="inventory-expiring-list"
      >
        {items.map((lot) => {
          const label = resolveLotExpiryLabel(lot.expiryStatus, lot.expirationDate);
          const tone = statusTone(label);
          const statusText = formatLotStatus(lot, t);
          return (
            <li key={lot.lotId}>
              <Link
                className="inventory-expiring-lot block min-w-0 no-underline"
                to={`/inventory/${lot.productId}`}
                data-testid={`expiring-lot-${lot.lotId}`}
              >
                <div className="inventory-expiring-lot__row">
                  <span className="inventory-expiring-lot__name min-w-0 truncate font-semibold text-foreground">
                    {lot.productName}
                  </span>
                  <span
                    className={cn(
                      "inventory-expiring-lot__badge shrink-0",
                      `inventory-expiring-lot__badge--${tone}`,
                    )}
                  >
                    {statusText}
                  </span>
                </div>
                <span className="inventory-expiring-lot__meta block truncate text-[length:var(--exits-text-sm)] text-muted">
                  {lot.expirationDate}
                  {lot.lotNumber ? ` · ${lot.lotNumber}` : ""} · {t("inventory.onHand")}:{" "}
                  {lot.quantityOnHand}
                </span>
              </Link>
            </li>
          );
        })}
      </ul>

      {query.hasNextPage ? (
        <Button
          type="button"
          variant="ghost"
          className="min-h-11 w-fit"
          disabled={query.isFetchingNextPage}
          onClick={() => void query.fetchNextPage()}
          data-testid="inventory-expiring-load-more"
        >
          {query.isFetchingNextPage ? t("inventory.loadingMore") : t("inventory.loadMore")}
        </Button>
      ) : null}
    </div>
  );
}
