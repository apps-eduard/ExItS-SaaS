import { useEffect, useMemo, useRef, useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { canManageInventory } from "@/access/pos-capabilities";
import { listCatalogProducts, getCatalogProduct } from "@/api/pos/pos-catalog-client";
import type { PosCatalogProductDto } from "@/api/pos/pos-catalog-types";
import {
  getInventoryProduct,
  listInventory,
  type PosInventoryAccountDto,
} from "@/api/pos/pos-inventory-client";
import { PosApiError } from "@/api/pos/pos-http";
import {
  createStockUse,
  STOCK_USE_REASONS,
  type StockUseReasonCode,
} from "@/api/pos/pos-stock-use-client";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { ExitsChipBar } from "@/components/exits/ExitsChipBar";
import { StickyActionBar } from "@/components/exits/FoundationStates";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { SearchField } from "@/components/exits/SearchField";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { isLikelyNetworkFailure } from "@/connectivity/network-failure";
import {
  businessUsageLabelKey,
  resolveBusinessUsage,
} from "@/features/catalog/product-business-usage";
import { stockUseReasonLabelKey } from "@/features/inventory/stock-use-labels";
import { useI18n } from "@/i18n/I18nProvider";
import { createSecureMutationId } from "@/lib/secure-mutation-id";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

type ProductFilter = "internal" | "all";

type DraftLine = {
  productId: string;
  name: string;
  uom: string;
  quantity: number;
  available: number;
};

type PickerRow = {
  productId: string;
  name: string;
  uom: string;
  onHand: number;
  usageLabel: string;
  isTracked: boolean;
};

export function StockUseCreatePage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const preselectProductId = searchParams.get("productId")?.trim() || null;
  const online = useBrowserOnline();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const allowManage = canManageInventory(sessionGrant);

  const [reason, setReason] = useState<StockUseReasonCode>("InternalOperations");
  const [notes, setNotes] = useState("");
  const [search, setSearch] = useState("");
  const [debounced, setDebounced] = useState("");
  const [productFilter, setProductFilter] = useState<ProductFilter>("internal");
  const [lines, setLines] = useState<DraftLine[]>([]);
  const [qtyByProduct, setQtyByProduct] = useState<Record<string, string>>({});
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [statusLocked, setStatusLocked] = useState(false);
  const stockUseIdRef = useRef<string | null>(null);
  const preselectDoneRef = useRef(false);

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

  const inventoryQuery = useQuery({
    queryKey: [
      "inventory",
      "stock-use-picker",
      workspace?.organizationId,
      workspace?.branchId,
      debounced,
    ],
    enabled: Boolean(workspace) && online && allowManage,
    queryFn: ({ signal }) =>
      listInventory(
        workspace!,
        { search: debounced || undefined, pageSize: 40 },
        signal,
      ),
  });

  const catalogQuery = useQuery({
    queryKey: [
      "catalog-products",
      "stock-use-picker",
      workspace?.organizationId,
      debounced,
      productFilter,
    ],
    enabled: Boolean(workspace) && online && allowManage,
    queryFn: ({ signal }) =>
      listCatalogProducts(
        workspace!,
        {
          search: debounced || undefined,
          status: "Active",
          pageSize: 40,
          canBeSold: productFilter === "internal" ? false : undefined,
        },
        signal,
      ),
  });

  const pickerRows = useMemo(() => {
    const inventoryById = new Map<string, PosInventoryAccountDto>();
    for (const item of inventoryQuery.data?.items ?? []) {
      inventoryById.set(item.productId, item);
    }

    const catalogById = new Map<string, PosCatalogProductDto>();
    for (const item of catalogQuery.data?.items ?? []) {
      catalogById.set(item.productId, item);
    }

    const ids = new Set<string>([
      ...inventoryById.keys(),
      ...catalogById.keys(),
    ]);

    const rows: PickerRow[] = [];
    for (const productId of ids) {
      const inv = inventoryById.get(productId);
      const cat = catalogById.get(productId);
      const isTracked = inv?.isTracked ?? cat?.isTracked === true;
      if (!isTracked) {
        continue;
      }

      const usage = resolveBusinessUsage(
        cat ?? {
          canBeSold: inv?.productStatus === "Active" ? true : undefined,
        },
      );
      if (productFilter === "internal" && usage !== "InternalUse") {
        continue;
      }

      const name = cat?.name ?? inv?.name ?? productId;
      const uom = cat?.unitOfMeasure ?? inv?.unitOfMeasure ?? "";
      rows.push({
        productId,
        name,
        uom,
        onHand: inv?.onHandQuantity ?? 0,
        usageLabel: t(businessUsageLabelKey(usage)),
        isTracked,
      });
    }

    rows.sort((a, b) => a.name.localeCompare(b.name));
    return rows;
  }, [inventoryQuery.data?.items, catalogQuery.data?.items, productFilter, t]);

  useEffect(() => {
    if (!workspace || !preselectProductId || preselectDoneRef.current || !allowManage || !online) {
      return;
    }
    let cancelled = false;
    void (async () => {
      try {
        const [inv, cat] = await Promise.all([
          getInventoryProduct(workspace, preselectProductId),
          getCatalogProduct(workspace, preselectProductId).catch(() => null),
        ]);
        if (cancelled || !inv.isTracked) {
          return;
        }
        preselectDoneRef.current = true;
        setLines((prev) => {
          if (prev.some((l) => l.productId === inv.productId)) {
            return prev;
          }
          return [
            ...prev,
            {
              productId: inv.productId,
              name: cat?.name ?? inv.name,
              uom: cat?.unitOfMeasure ?? inv.unitOfMeasure,
              quantity: 1,
              available: inv.onHandQuantity,
            },
          ];
        });
        setQtyByProduct((prev) => ({ ...prev, [inv.productId]: "1" }));
      } catch {
        // Preselect is best-effort; keep form usable.
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [workspace, preselectProductId, allowManage, online]);

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }

  function upsertLine(row: PickerRow, quantity: number) {
    setLines((prev) => {
      const without = prev.filter((l) => l.productId !== row.productId);
      return [
        ...without,
        {
          productId: row.productId,
          name: row.name,
          uom: row.uom,
          quantity,
          available: row.onHand,
        },
      ];
    });
  }

  function addOrUpdateFromPicker(row: PickerRow) {
    setError(null);
    const raw = qtyByProduct[row.productId] ?? "1";
    const qty = Number(raw);
    if (!Number.isFinite(qty) || qty <= 0) {
      setError(t("stockUse.invalidQuantity"));
      return;
    }
    if (qty > row.onHand) {
      setError(
        t("stockUse.onlyAvailable").replace("{quantity}", `${row.onHand} ${row.uom}`.trim()),
      );
      return;
    }
    const existing = lines.find((l) => l.productId === row.productId);
    const nextQty = existing ? existing.quantity + qty : qty;
    if (nextQty > row.onHand) {
      setError(
        t("stockUse.onlyAvailable").replace("{quantity}", `${row.onHand} ${row.uom}`.trim()),
      );
      return;
    }
    upsertLine(row, nextQty);
    setQtyByProduct((prev) => ({ ...prev, [row.productId]: "1" }));
  }

  function updateLineQty(productId: string, raw: string) {
    setQtyByProduct((prev) => ({ ...prev, [productId]: raw }));
    const qty = Number(raw);
    if (!Number.isFinite(qty) || qty <= 0) {
      return;
    }
    setLines((prev) =>
      prev.map((line) => (line.productId === productId ? { ...line, quantity: qty } : line)),
    );
  }

  function removeLine(productId: string) {
    setLines((prev) => prev.filter((l) => l.productId !== productId));
  }

  async function submit() {
    if (!workspace || !allowManage || !online || saving || statusLocked || lines.length === 0) {
      return;
    }
    for (const line of lines) {
      if (line.quantity <= 0) {
        setError(t("stockUse.invalidQuantity"));
        return;
      }
      if (line.quantity > line.available) {
        setError(
          t("stockUse.onlyAvailable").replace(
            "{quantity}",
            `${line.available} ${line.uom}`.trim(),
          ),
        );
        return;
      }
    }

    if (!stockUseIdRef.current) {
      const generated = createSecureMutationId();
      if (!generated.ok) {
        setError(t("stockUse.saveFailed"));
        return;
      }
      stockUseIdRef.current = generated.id;
    }
    const stockUseId = stockUseIdRef.current;
    setSaving(true);
    setError(null);
    try {
      const created = await createStockUse(workspace, {
        reason,
        notes: notes.trim() || null,
        stockUseId,
        lines: lines.map((line) => ({
          productId: line.productId,
          quantity: line.quantity,
        })),
      });
      stockUseIdRef.current = null;
      navigate(`/inventory/stock-use/${created.stockUseId}`, { replace: true });
    } catch (err) {
      if (isLikelyNetworkFailure(err)) {
        setError(t("checkout.confirmingTransaction"));
        try {
          const created = await createStockUse(workspace, {
            reason,
            notes: notes.trim() || null,
            stockUseId,
            lines: lines.map((line) => ({
              productId: line.productId,
              quantity: line.quantity,
            })),
          });
          stockUseIdRef.current = null;
          navigate(`/inventory/stock-use/${created.stockUseId}`, { replace: true });
          return;
        } catch (retryErr) {
          if (isLikelyNetworkFailure(retryErr)) {
            setStatusLocked(true);
            setError(t("checkout.transactionStatusUnknown"));
            return;
          }
          setError(
            retryErr instanceof PosApiError
              ? (retryErr.problem.detail ?? t("stockUse.saveFailed"))
              : t("stockUse.saveFailed"),
          );
          return;
        }
      }
      setError(
        err instanceof PosApiError
          ? (err.problem.detail ?? t("stockUse.saveFailed"))
          : t("stockUse.saveFailed"),
      );
    } finally {
      setSaving(false);
    }
  }

  return (
    <div
      className="stock-use-create-page exits-page flex min-w-0 flex-col gap-3 pb-4"
      data-testid="stock-use-create-page"
    >
      <PageHeader
        title={t("stockUse.recordTitle")}
        description={t("stockUse.notASale")}
        backTo="/inventory/stock-use"
        backLabel={t("stockUse.backList")}
        backTestId="page-header-back-stock-use"
      />

      {!online ? (
        <Card>
          <p className="m-0">{t("stockUse.offline")}</p>
        </Card>
      ) : null}
      {!allowManage ? (
        <Card>
          <p className="m-0">{t("stockUse.manageDenied")}</p>
        </Card>
      ) : null}

      {error ? <ErrorState title={t("stockUse.errorTitle")} detail={error} /> : null}

      <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
        {t("stockUse.reason")}
        <select
          className="min-h-11 rounded-md border border-border bg-background px-3"
          value={reason}
          onChange={(e) => setReason(e.target.value as StockUseReasonCode)}
          disabled={!allowManage || statusLocked}
          data-testid="stock-use-reason"
        >
          {STOCK_USE_REASONS.map((code) => (
            <option key={code} value={code}>
              {t(stockUseReasonLabelKey(code))}
            </option>
          ))}
        </select>
      </label>

      <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
        {t("stockUse.notes")}
        <textarea
          className="min-h-20 rounded-md border border-border bg-background px-3 py-2"
          value={notes}
          onChange={(e) => setNotes(e.target.value)}
          disabled={!allowManage || statusLocked}
          placeholder={t("stockUse.notesOptional")}
          data-testid="stock-use-notes"
        />
      </label>

      <section className="flex flex-col gap-2" data-testid="stock-use-draft-lines">
        <h2 className="m-0 text-[length:var(--exits-text-md)] font-medium">
          {t("stockUse.usedStock")}
        </h2>
        {lines.length === 0 ? (
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("stockUse.draftEmpty")}
          </p>
        ) : (
          <ul className="m-0 flex list-none flex-col gap-2 p-0">
            {lines.map((line) => (
              <li key={line.productId}>
                <Card className="flex flex-col gap-2 p-3">
                  <div className="font-medium">{line.name}</div>
                  <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                    {t("stockUse.available")}: {line.available} {line.uom}
                  </p>
                  <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
                    {t("stockUse.quantityUsed")}
                    <input
                      type="number"
                      min={0}
                      step="any"
                      className="min-h-11 rounded-md border border-border bg-background px-3"
                      value={qtyByProduct[line.productId] ?? String(line.quantity)}
                      onChange={(e) => updateLineQty(line.productId, e.target.value)}
                      disabled={statusLocked}
                      data-testid={`stock-use-line-qty-${line.productId}`}
                    />
                  </label>
                  <Button
                    type="button"
                    variant="ghost"
                    className="min-h-11 w-fit"
                    onClick={() => removeLine(line.productId)}
                    disabled={statusLocked}
                  >
                    {t("stockUse.removeLine")}
                  </Button>
                </Card>
              </li>
            ))}
          </ul>
        )}
      </section>

      <section className="flex flex-col gap-2">
        <h2 className="m-0 text-[length:var(--exits-text-md)] font-medium">
          {t("stockUse.addProduct")}
        </h2>
        <ExitsChipBar
          variant="filter"
          ariaLabel={t("stockUse.addProduct")}
          testId="stock-use-product-filter"
          items={[
            {
              key: "internal",
              label: t("stockUse.filterInternalUse"),
              state: productFilter === "internal" ? "active" : "idle",
              testId: "stock-use-filter-internal",
              onSelect: () => setProductFilter("internal"),
            },
            {
              key: "all",
              label: t("stockUse.filterAllStock"),
              state: productFilter === "all" ? "active" : "idle",
              testId: "stock-use-filter-all",
              onSelect: () => setProductFilter("all"),
            },
          ]}
        />
        <SearchField
          label={t("stockUse.searchProducts")}
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          onClear={() => setSearch("")}
          placeholder={t("stockUse.searchProducts")}
          data-testid="stock-use-product-search"
        />
        {pickerRows.length === 0 && (debounced || inventoryQuery.isSuccess) ? (
          <EmptyState
            title={t("stockUse.noProducts")}
            detail={t("stockUse.noProductsDetail")}
          />
        ) : null}
        <ul className="m-0 flex list-none flex-col gap-2 p-0" data-testid="stock-use-product-picker">
          {pickerRows.map((row) => (
            <li key={row.productId}>
              <Card className="flex flex-col gap-2 p-3">
                <div className="min-w-0">
                  <div className="font-medium">{row.name}</div>
                  <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                    {row.usageLabel} · {t("stockUse.available")}: {row.onHand} {row.uom}
                  </p>
                </div>
                <div className="flex flex-wrap items-end gap-2">
                  <label className="flex min-w-[5.5rem] flex-1 flex-col gap-1 text-[length:var(--exits-text-sm)]">
                    {t("stockUse.quantityUsed")}
                    <input
                      type="number"
                      min={0}
                      step="any"
                      className="min-h-11 rounded-md border border-border bg-background px-3"
                      value={qtyByProduct[row.productId] ?? "1"}
                      onChange={(e) =>
                        setQtyByProduct((prev) => ({
                          ...prev,
                          [row.productId]: e.target.value,
                        }))
                      }
                      disabled={statusLocked || !allowManage}
                      data-testid={`stock-use-picker-qty-${row.productId}`}
                    />
                  </label>
                  <Button
                    type="button"
                    className="min-h-11"
                    disabled={!allowManage || !online || statusLocked || row.onHand <= 0}
                    onClick={() => addOrUpdateFromPicker(row)}
                    data-testid={`stock-use-add-${row.productId}`}
                  >
                    {t("stockUse.addProduct")}
                  </Button>
                </div>
              </Card>
            </li>
          ))}
        </ul>
      </section>

      <StickyActionBar>
        <Button
          type="button"
          className="min-h-11 w-full"
          disabled={
            !allowManage ||
            !online ||
            saving ||
            statusLocked ||
            lines.length === 0
          }
          onClick={() => void submit()}
          data-testid="stock-use-submit"
        >
          {saving ? t("stockUse.recording") : t("stockUse.recordUse")}
        </Button>
      </StickyActionBar>
    </div>
  );
}
