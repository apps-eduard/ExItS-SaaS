import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useInfiniteQuery } from "@tanstack/react-query";
import { listExpiringLots, type PosExpiringLotDto } from "@/api/pos/pos-inventory-client";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { SearchField } from "@/components/exits/SearchField";
import {
  EXPIRY_WINDOWS,
  type ExpiryWindowCode,
  resolveLotExpiryLabel,
} from "@/features/inventory/inventory-lot-status";
import { useI18n } from "@/i18n/I18nProvider";
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
  }
}

export function InventoryExpirationPage() {
  const { t } = useI18n();
  const { boundWorkspace } = useWorkspace();
  const [windowCode, setWindowCode] = useState<ExpiryWindowCode>("Days30");
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

  const query = useInfiniteQuery({
    queryKey: [
      "inventory",
      "expiring",
      workspace?.organizationId,
      workspace?.branchId,
      windowCode,
      debounced,
    ],
    enabled: Boolean(workspace),
    initialPageParam: 1,
    queryFn: ({ pageParam, signal }) =>
      listExpiringLots(
        workspace!,
        {
          window: windowCode,
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
    return <LoadingState label={t("session.loading")} />;
  }

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="inventory-expiration-page">
      <PageHeader
        title={t("inventory.expirationTitle")}
        description={t("inventory.expirationLede")}
      />
      <label className="flex flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold">
        {t("inventory.expiryWindow")}
        <select
          className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-background px-3 font-normal"
          value={windowCode}
          onChange={(e) => setWindowCode(e.target.value as ExpiryWindowCode)}
          data-testid="inventory-expiry-window"
        >
          {EXPIRY_WINDOWS.map((code) => (
            <option key={code} value={code}>
              {windowLabel(code, t)}
            </option>
          ))}
        </select>
      </label>
      <SearchField
        label={t("inventory.searchExpiring")}
        value={search}
        onChange={(event) => setSearch(event.target.value)}
        onClear={() => setSearch("")}
        placeholder={t("inventory.searchExpiring")}
      />
      {query.isLoading ? <LoadingState label={t("loading.label")} /> : null}
      {query.isError ? (
        <ErrorState title={t("error.title")} detail={(query.error as Error).message} />
      ) : null}
      {query.isSuccess && counts ? (
        <p
          className="m-0 text-[length:var(--exits-text-sm)] text-muted"
          data-testid="inventory-expiry-counts"
        >
          {t("inventory.expiryCounts")
            .replace("{expired}", String(counts.expiredCount))
            .replace("{near}", String(counts.nearExpiryCount))}
        </p>
      ) : null}
      {query.isSuccess && items.length === 0 ? (
        <EmptyState
          title={t("inventory.expirationEmpty")}
          detail={t("inventory.expirationEmptyDetail")}
        />
      ) : null}
      <ul className="m-0 flex list-none flex-col gap-2 p-0" data-testid="inventory-expiring-list">
        {items.map((lot) => (
          <li key={lot.lotId}>
            <Card className="p-3">
              <Link
                className="block min-w-0 text-foreground no-underline"
                to={`/inventory/${lot.productId}`}
                data-testid={`expiring-lot-${lot.lotId}`}
              >
                <span className="block truncate font-semibold">{lot.productName}</span>
                <span className="block truncate text-[length:var(--exits-text-sm)] text-muted">
                  {lot.expirationDate}
                  {lot.lotNumber ? ` · ${lot.lotNumber}` : ""} · {t("inventory.onHand")}:{" "}
                  {lot.quantityOnHand} · {formatLotStatus(lot, t)}
                </span>
              </Link>
            </Card>
          </li>
        ))}
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
      <Button asChild variant="ghost" className="min-h-11 w-fit">
        <Link to="/inventory">{t("inventory.backList")}</Link>
      </Button>
    </div>
  );
}
