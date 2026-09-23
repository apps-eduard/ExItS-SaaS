import { useEffect, useMemo, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import {
  ArrowLeft,
  ArrowLeftRight,
  ArrowRightLeft,
  RotateCcw,
  Store,
} from "lucide-react";
import { canManageInventory } from "@/access/pos-capabilities";
import {
  listInventory,
  listProductLots,
  type PosInventoryAccountDto,
  type PosInventoryLotDto,
} from "@/api/pos/pos-inventory-client";
import { PosApiError } from "@/api/pos/pos-http";
import { createInventoryTransfer } from "@/api/pos/pos-inventory-transfer-client";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { ExitsModal } from "@/components/exits/ExitsModal";
import { ExitsSelect } from "@/components/exits/ExitsSelect";
import { LoadingState } from "@/components/exits/LoadingState";
import { Notice } from "@/components/exits/Notice";
import { PageHeader } from "@/components/exits/PageHeader";
import { ProductCategoryMultiSelect } from "@/components/exits/ProductCategoryMultiSelect";
import { SelectedItemsPanel } from "@/components/exits/ProductSelectionWorkspace";
import { ProductSelectionToolbar } from "@/components/exits/ProductSelectionView";
import { PRODUCT_SELECTION_TABLE_MIN_PX } from "@/components/exits/product-selection-view";
import { SearchField } from "@/components/exits/SearchField";
import { useResponsiveDataLayout } from "@/components/exits/useResponsiveDataLayout";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { PoDocumentSummary } from "@/features/purchasing/PoDocumentSummary";
import { parseTransferQuantity } from "@/features/inventory/inventory-transfer-labels";
import { resolveAvailableQuantity } from "@/features/inventory/inventory-reservation-display";
import {
  canAddTransferQuantity,
  evaluateTransferLineStock,
  lotDemandExcludingLine,
  productDemandExcludingLine,
  type TransferLineStockIssue,
} from "@/features/inventory/inventory-transfer-stock-guard";
import { InventoryTransferItemsView } from "@/features/inventory/InventoryTransferItemsView";
import { InventoryTransferProductSelection } from "@/features/inventory/InventoryTransferProductSelection";
import { useI18n } from "@/i18n/I18nProvider";
import { createSecureMutationId } from "@/lib/secure-mutation-id";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

/** Synthetic category filter id — not a real catalog category. */
const OUT_OF_STOCK_FILTER_ID = "__transfer_out_of_stock__";

type DraftLine = {
  key: string;
  productId: string;
  name: string;
  unitOfMeasure: string;
  quantity: number;
  tracksExpiration: boolean;
  isTracked: boolean;
  sourceLotId: string | null;
  lotNumber: string | null;
  expirationDate: string | null;
  availableQuantity: number;
  lotAvailableQuantity: number | null;
};

function lineKeyOf(line: { productId: string; sourceLotId: string | null }) {
  return `${line.productId}:${line.sourceLotId ?? "none"}`;
}

export function InventoryTransferCreatePage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const online = useBrowserOnline();
  const { boundWorkspace, sessionGrant, workspaces } = useWorkspace();
  const allowManage = canManageInventory(sessionGrant);

  const [destinationBranchId, setDestinationBranchId] = useState("");
  const [notes, setNotes] = useState("");
  const [search, setSearch] = useState("");
  const [debounced, setDebounced] = useState("");
  const [categoryIds, setCategoryIds] = useState<string[]>([]);
  const [lines, setLines] = useState<DraftLine[]>([]);
  const [lotByProduct, setLotByProduct] = useState<Record<string, string>>({});
  const [lotsCache, setLotsCache] = useState<Record<string, PosInventoryLotDto[]>>({});
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [finderOpen, setFinderOpen] = useState(false);
  const finderPanelId = "transfer-product-finder-panel";
  const operationIdRef = useRef<string | null>(null);
  const { layout: pickerLayout } = useResponsiveDataLayout({
    tableMinWidthPx: PRODUCT_SELECTION_TABLE_MIN_PX,
  });

  function openFinder() {
    setFinderOpen(true);
  }

  function closeFinder() {
    setFinderOpen(false);
  }

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

  const orgBranches = useMemo(() => {
    const org = workspaces.find((w) => w.organizationId === boundWorkspace?.organizationId);
    return (org?.branches ?? []).filter((b) => b.isActive);
  }, [workspaces, boundWorkspace?.organizationId]);

  const destinations = useMemo(
    () => orgBranches.filter((b) => b.branchId !== boundWorkspace?.branchId),
    [orgBranches, boundWorkspace?.branchId],
  );

  const multiBranch = orgBranches.length >= 2;
  const sourceName = boundWorkspace?.branchName ?? t("transfer.sourceBranch");

  const pickerQueryKey = [
    "inventory",
    "transfer-picker",
    workspace?.organizationId,
    workspace?.branchId,
    debounced,
  ] as const;

  const pickerQuery = useQuery({
    queryKey: pickerQueryKey,
    enabled: Boolean(workspace) && online && allowManage && multiBranch,
    queryFn: ({ signal }) =>
      listInventory(
        workspace!,
        { search: debounced || undefined, pageSize: 40, tracked: true },
        signal,
      ),
  });

  const availabilityByProduct = useMemo(() => {
    const map = new Map<string, number>();
    for (const row of pickerQuery.data?.items ?? []) {
      if (row.isTracked) {
        map.set(row.productId, Math.max(0, resolveAvailableQuantity(row)));
      }
    }
    return map;
  }, [pickerQuery.data?.items]);

  // Refresh line availability when source-branch inventory query updates.
  useEffect(() => {
    if (availabilityByProduct.size === 0) {
      return;
    }
    setLines((prev) =>
      prev.map((line) => {
        const nextAvailable = availabilityByProduct.get(line.productId);
        if (nextAvailable === undefined || nextAvailable === line.availableQuantity) {
          return line;
        }
        return { ...line, availableQuantity: nextAvailable };
      }),
    );
  }, [availabilityByProduct]);

  const pickerRows = useMemo(() => {
    const tracked = (pickerQuery.data?.items ?? []).filter((row) => row.isTracked);
    const addedProductIds = new Set(lines.map((line) => line.productId));
    const wantOutOfStock = categoryIds.includes(OUT_OF_STOCK_FILTER_ID);
    const realCategoryIds = categoryIds.filter((id) => id !== OUT_OF_STOCK_FILTER_ID);
    const realSelected = new Set(realCategoryIds);

    return tracked.filter((row) => {
      if (addedProductIds.has(row.productId)) {
        return false;
      }
      const outOfStock = resolveAvailableQuantity(row) <= 0;
      if (wantOutOfStock) {
        if (!outOfStock) {
          return false;
        }
        if (realSelected.size === 0) {
          return true;
        }
        return row.categoryId != null && realSelected.has(row.categoryId);
      }
      // Default: in-stock only. Optional real category filter.
      if (outOfStock) {
        return false;
      }
      if (realSelected.size === 0) {
        return true;
      }
      return row.categoryId != null && realSelected.has(row.categoryId);
    });
  }, [pickerQuery.data?.items, categoryIds, lines]);

  const transferCategoryOptions = useMemo(() => {
    const tracked = (pickerQuery.data?.items ?? []).filter((r) => r.isTracked);
    const addedProductIds = new Set(lines.map((line) => line.productId));
    const counts = new Map<string, { name: string; count: number }>();
    let outOfStockCount = 0;
    for (const row of tracked) {
      if (addedProductIds.has(row.productId)) {
        continue;
      }
      if (resolveAvailableQuantity(row) <= 0) {
        outOfStockCount += 1;
      }
      const id = row.categoryId?.trim();
      if (!id || resolveAvailableQuantity(row) <= 0) {
        // Real category counts reflect in-stock products (default table).
        continue;
      }
      const name = row.categoryName?.trim() || id;
      const existing = counts.get(id);
      if (existing) {
        existing.count += 1;
      } else {
        counts.set(id, { name, count: 1 });
      }
    }
    const categories = [...counts.entries()]
      .sort((a, b) => a[1].name.localeCompare(b[1].name, undefined, { sensitivity: "base" }))
      .map(([categoryId, meta]) => ({
        categoryId,
        name: meta.name,
        count: meta.count,
      }));
    return [
      {
        categoryId: OUT_OF_STOCK_FILTER_ID,
        name: t("transfer.outOfStock"),
        count: outOfStockCount,
      },
      ...categories,
    ];
  }, [pickerQuery.data?.items, lines, t]);

  function stockIssueMessage(issue: TransferLineStockIssue, available: number, uom: string): string {
    switch (issue) {
      case "untracked":
        return t("transfer.notTracked");
      case "out_of_stock":
      case "lot_out_of_stock":
        return t("transfer.outOfStock");
      case "over_stock":
      case "lot_over_stock":
        return t("transfer.onlyAvailableAtSource")
          .replace("{qty}", String(available))
          .replace("{uom}", uom)
          .replace("{branch}", sourceName);
      case "invalid_qty":
        return t("transfer.invalidQuantity");
    }
  }

  function formatAvailable(qty: number, uom: string) {
    return t("transfer.available").replace("{qty}", String(qty)).replace("{uom}", uom);
  }

  async function ensureLots(productId: string, tracksExpiration: boolean) {
    if (!workspace || !tracksExpiration || lotsCache[productId]) {
      return lotsCache[productId] ?? [];
    }
    try {
      const result = await listProductLots(workspace, productId, { pageSize: 50 });
      setLotsCache((prev) => ({ ...prev, [productId]: result.items }));
      return result.items;
    } catch {
      setLotsCache((prev) => ({ ...prev, [productId]: [] }));
      return [];
    }
  }

  async function addLine(row: PosInventoryAccountDto) {
    const tracksExpiration = row.tracksExpiration === true;
    const availableQuantity = Math.max(0, resolveAvailableQuantity(row));
    const lots = await ensureLots(row.productId, tracksExpiration);
    let sourceLotId: string | null = null;
    let lotNumber: string | null = null;
    let expirationDate: string | null = null;
    let lotAvailableQuantity: number | null = null;
    if (tracksExpiration) {
      const lotId = lotByProduct[row.productId]?.trim() || "";
      if (!lotId) {
        setError(t("transfer.lotRequired"));
        return;
      }
      const lot = lots.find((l) => l.lotId === lotId) ?? lotsCache[row.productId]?.find((l) => l.lotId === lotId);
      if (!lot) {
        setError(t("transfer.lotRequired"));
        return;
      }
      sourceLotId = lot.lotId;
      lotNumber = lot.lotNumber ?? null;
      expirationDate = lot.expirationDate ?? null;
      lotAvailableQuantity = Math.max(0, lot.quantityOnHand);
    }

    if (availableQuantity <= 0 || (tracksExpiration && (lotAvailableQuantity ?? 0) <= 0)) {
      setError(t("transfer.outOfStock"));
      return;
    }

    const key = lineKeyOf({ productId: row.productId, sourceLotId });
    const existing = lines.find((l) => l.key === key);
    // Each Add click adds one unit; adjust further on the draft line stepper.
    const qtyParsed = existing ? existing.quantity + 1 : 1;
    const existingProductDemand = productDemandExcludingLine(lines, row.productId, key);
    const existingLotDemand =
      sourceLotId != null ? lotDemandExcludingLine(lines, sourceLotId, key) : 0;
    const issue = canAddTransferQuantity({
      quantity: qtyParsed,
      availableQuantity,
      lotAvailableQuantity,
      tracksExpiration,
      existingProductDemand,
      existingLotDemand,
    });
    if (issue) {
      const cap =
        tracksExpiration && lotAvailableQuantity != null
          ? Math.min(availableQuantity, lotAvailableQuantity)
          : availableQuantity;
      setError(stockIssueMessage(issue, cap, row.unitOfMeasure));
      return;
    }

    setError(null);
    setLines((prev) => {
      const existingIndex = prev.findIndex((l) => l.key === key);
      if (existingIndex >= 0) {
        const next = [...prev];
        next[existingIndex] = {
          ...next[existingIndex],
          quantity: qtyParsed,
          availableQuantity,
          lotAvailableQuantity,
        };
        return next;
      }
      return [
        ...prev,
        {
          key,
          productId: row.productId,
          name: row.name,
          unitOfMeasure: row.unitOfMeasure,
          quantity: qtyParsed,
          tracksExpiration,
          isTracked: row.isTracked,
          sourceLotId,
          lotNumber,
          expirationDate,
          availableQuantity,
          lotAvailableQuantity,
        },
      ];
    });
  }

  function removeLine(key: string) {
    setLines((prev) => prev.filter((l) => l.key !== key));
  }

  function updateLineQuantity(key: string, raw: string) {
    const parsed = parseTransferQuantity(raw);
    if (parsed === "empty" || parsed === "invalid") {
      setLines((prev) =>
        prev.map((line) => (line.key === key ? { ...line, quantity: 0 } : line)),
      );
      setError(t("transfer.invalidQuantity"));
      return;
    }
    setLines((prev) => {
      const target = prev.find((l) => l.key === key);
      if (!target) {
        return prev;
      }
      const nextLine = { ...target, quantity: parsed };
      const issue = evaluateTransferLineStock(nextLine, prev);
      if (issue) {
        const cap =
          nextLine.lotAvailableQuantity != null
            ? Math.min(nextLine.availableQuantity, nextLine.lotAvailableQuantity)
            : nextLine.availableQuantity;
        setError(stockIssueMessage(issue, cap, nextLine.unitOfMeasure));
      } else {
        setError(null);
      }
      return prev.map((line) => (line.key === key ? nextLine : line));
    });
  }

  function resetForm() {
    if (saving) {
      return;
    }
    setDestinationBranchId("");
    setNotes("");
    setSearch("");
    setDebounced("");
    setCategoryIds([]);
    setLines([]);
    setLotByProduct({});
    setError(null);
    operationIdRef.current = null;
  }

  const lineIssues = useMemo(() => {
    const map = new Map<string, TransferLineStockIssue>();
    for (const line of lines) {
      const issue = evaluateTransferLineStock(line, lines);
      if (issue) {
        map.set(line.key, issue);
      }
    }
    return map;
  }, [lines]);

  const createBlockedReason = useMemo(() => {
    if (lines.length === 0) {
      return t("transfer.draftEmpty");
    }
    if (!destinationBranchId) {
      return t("transfer.destinationRequired");
    }
    for (const line of lines) {
      const issue = lineIssues.get(line.key);
      if (!issue) {
        continue;
      }
      const cap =
        line.lotAvailableQuantity != null
          ? Math.min(line.availableQuantity, line.lotAvailableQuantity)
          : line.availableQuantity;
      return stockIssueMessage(issue, cap, line.unitOfMeasure);
    }
    return null;
    // stockIssueMessage uses t/sourceName; intentional.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [lines, lineIssues, destinationBranchId, sourceName, t]);

  async function refreshAvailability() {
    await queryClient.invalidateQueries({ queryKey: ["inventory", "transfer-picker"] });
    setLotsCache({});
  }

  async function saveDraft() {
    if (!workspace || !boundWorkspace?.branchId || !allowManage || !online || saving) {
      return;
    }
    if (createBlockedReason) {
      setError(createBlockedReason);
      return;
    }
    if (destinationBranchId === boundWorkspace.branchId) {
      setError(t("transfer.sameBranch"));
      return;
    }
    if (!operationIdRef.current) {
      const generated = createSecureMutationId();
      if (!generated.ok) {
        setError(t("transfer.saveFailed"));
        return;
      }
      operationIdRef.current = generated.id;
    }
    setSaving(true);
    setError(null);
    try {
      const created = await createInventoryTransfer(workspace, {
        sourceBranchId: boundWorkspace.branchId,
        destinationBranchId,
        notes: notes.trim() || null,
        operationId: operationIdRef.current,
        lines: lines.map((line) => ({
          productId: line.productId,
          quantity: line.quantity,
          sourceLotId: line.sourceLotId,
        })),
      });
      operationIdRef.current = null;
      navigate(`/inventory/transfers/${created.transferId}`, {
        replace: true,
        state: { flash: "created" },
      });
    } catch (err) {
      const detail =
        err instanceof PosApiError
          ? (err.problem.detail ?? t("transfer.saveFailed"))
          : t("transfer.saveFailed");
      setError(detail);
      await refreshAvailability();
      // Keep entered lines for correction after server rejection.
    } finally {
      setSaving(false);
    }
  }

  const destinationOptions = useMemo(
    () => [
      { value: "", label: t("transfer.selectDestination") },
      ...destinations.map((branch) => ({
        value: branch.branchId,
        label: branch.secondaryLine
          ? `${branch.name} — ${branch.secondaryLine}`
          : branch.name,
      })),
    ],
    [destinations, t],
  );

  const unitCount = useMemo(
    () => lines.reduce((sum, line) => sum + line.quantity, 0),
    [lines],
  );

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }

  if (!allowManage) {
    return (
      <div className="exits-page flex min-w-0 flex-col gap-3" data-testid="transfer-create-denied">
        <PageHeader
          title={t("transfer.newTitle")}
          backTo="/inventory/transfers"
          backLabel={t("transfer.backList")}
          backTestId="page-header-back-transfers"
        />
        <ErrorState title={t("transfer.errorTitle")} detail={t("transfer.manageDenied")} />
      </div>
    );
  }

  if (!multiBranch) {
    return (
      <div className="exits-page flex min-w-0 flex-col gap-3" data-testid="transfer-create-single-branch">
        <PageHeader
          title={t("transfer.newTitle")}
          backTo="/inventory/transfers"
          backLabel={t("transfer.backList")}
          backTestId="page-header-back-transfers"
        />
        <EmptyState
          align="center"
          variant="setup"
          icon={<ArrowRightLeft className="size-5" strokeWidth={1.75} />}
          title={t("transfer.requiresTwoBranches")}
          detail={t("transfer.singleBranchDetail")}
        />
      </div>
    );
  }

  const createDisabled =
    !online || saving || Boolean(createBlockedReason) || lines.length === 0 || !destinationBranchId;

  return (
    <div
      className="inventory-transfer-create-page exits-page flex min-w-0 flex-col gap-4"
      data-testid="inventory-transfer-create-page"
    >
      <PageHeader
        title={t("transfer.newTitle")}
        description={t("transfer.newLede")}
        backTo="/inventory/transfers"
        backLabel={t("transfer.backList")}
        backTestId="page-header-back-transfers"
      />

      {!online ? (
        <Notice tone="warning" testId="transfer-offline-notice">
          {t("transfer.offline")}
        </Notice>
      ) : null}

      {error ? (
        <Notice tone="danger" testId="transfer-create-error">
          {error}
        </Notice>
      ) : null}

      {createBlockedReason && lines.length > 0 && destinationBranchId ? (
        <p
          className="m-0 text-[length:var(--exits-text-sm)] text-muted"
          data-testid="transfer-create-blocked-reason"
        >
          {createBlockedReason}
        </p>
      ) : null}

      <PoDocumentSummary
        className="po-document-summary--create transfer-create-summary"
        title={t("transfer.detailsTitle")}
        fields={[
          {
            key: "from",
            label: t("transfer.fromBranch"),
            value: (
              <span
                className="inline-flex min-w-0 items-center gap-2 font-semibold"
                data-testid="transfer-source-branch"
              >
                <Store className="size-4 shrink-0 text-primary" strokeWidth={1.75} aria-hidden />
                <span className="min-w-0 truncate">{sourceName}</span>
              </span>
            ),
          },
          {
            key: "to",
            label: t("transfer.toBranch"),
            value: (
              <ExitsSelect
                value={destinationBranchId}
                options={destinationOptions}
                onChange={setDestinationBranchId}
                searchable={destinations.length > 6}
                searchPlaceholder={t("transfer.selectDestination")}
                menuLabel={t("transfer.toBranch")}
                aria-label={t("transfer.toBranch")}
                testId="transfer-destination-branch"
                invalid={!destinationBranchId && lines.length > 0}
              />
            ),
          },
        ]}
        testId="transfer-create-details"
        footer={
          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            <span>
              {t("transfer.notes")}{" "}
              <span className="font-normal text-muted">({t("transfer.notesOptional")})</span>
            </span>
            <textarea
              className="min-h-16 rounded-md border border-border bg-background px-3 py-2"
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              maxLength={512}
              data-testid="transfer-notes"
            />
          </label>
        }
      />

      <div
        className="product-selection-workspace receive-stock-workspace transfer-create-workspace flex flex-col gap-3"
        data-testid="transfer-order-cart"
      >
        <SelectedItemsPanel
          title={t("transfer.items")}
          count={lines.length}
          headingId="transfer-draft-items-heading"
          addLabel={t("transfer.addProducts")}
          onAddClick={openFinder}
          finderOpen={finderOpen}
          finderPanelId={finderPanelId}
          emptyTitle={t("transfer.itemsEmpty")}
          emptyDetail={t("transfer.itemsEmptyDetail")}
          emptyTestId="transfer-selected-items-empty"
          addTestId="transfer-add-products-trigger"
          testId="transfer-draft-lines"
          summary={
            <div className="receive-stock-receipt__summary" data-testid="transfer-order-summary">
              <div className="receive-stock-receipt__summary-row">
                <span className="text-[length:var(--exits-text-sm)] text-muted">
                  {t("transfer.items")}
                </span>
                <span className="text-[length:var(--exits-text-sm)] tabular-nums">
                  {lines.length}
                </span>
              </div>
              <div className="receive-stock-receipt__summary-row">
                <span className="text-[length:var(--exits-text-sm)] text-muted">
                  {t("transfer.units")}
                </span>
                <span className="text-[length:var(--exits-text-md)] font-semibold tabular-nums">
                  {unitCount}
                </span>
              </div>
            </div>
          }
        >
          <InventoryTransferItemsView
            lines={lines.map((line) => ({
              key: line.key,
              name: line.name,
              quantity: line.quantity,
              unitOfMeasure: line.unitOfMeasure,
              availableQuantity: line.availableQuantity,
              lotAvailableQuantity: line.lotAvailableQuantity,
              lotNumber: line.lotNumber,
              expirationDate: line.expirationDate,
              hasIssue: Boolean(lineIssues.get(line.key)),
              onQtyChange: (next) => updateLineQuantity(line.key, String(next)),
              onRemove: () => removeLine(line.key),
            }))}
            formatAvailable={formatAvailable}
            t={t}
          />
        </SelectedItemsPanel>

        <ExitsModal
          open={finderOpen}
          onOpenChange={(open) => {
            if (!open) {
              closeFinder();
            }
          }}
          title={t("transfer.findProducts")}
          closeLabel={t("transfer.closeFindProducts")}
          testId="transfer-add-products"
          id={finderPanelId}
          size="lg"
          fullHeightOnCompact
          className="lg:max-h-[min(92dvh,48rem)] lg:max-w-3xl"
        >
          <div className="flex flex-col gap-3">
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
              {t("transfer.baseUomHint")}
            </p>
            <ProductSelectionToolbar
              className="transfer-product-selection__filters"
              testId="transfer-product-filters"
            >
              <ProductCategoryMultiSelect
                categories={transferCategoryOptions}
                selectedIds={categoryIds}
                onChange={setCategoryIds}
                placeholder={t("purchasing.categoriesPlaceholder")}
                selectedCountLabel={(count) =>
                  t("purchasing.categoriesSelected").replace("{count}", String(count))
                }
                selectAllLabel={t("purchasing.selectAllCategories")}
                clearAllLabel={t("purchasing.deselectAllCategories")}
                searchPlaceholder={t("catalog.searchCategories")}
                menuLabel={t("purchasing.categoryFilter")}
                aria-label={t("purchasing.categoryFilter")}
                testId="transfer-category-multiselect"
                className="transfer-category-multiselect"
              />
              <SearchField
                label={t("transfer.searchProducts")}
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                onClear={() => setSearch("")}
                placeholder={t("transfer.searchProducts")}
                data-testid="transfer-product-search"
                containerClassName="transfer-product-selection__search"
              />
            </ProductSelectionToolbar>
            {pickerQuery.isLoading ? <LoadingState label={t("transfer.loading")} /> : null}
            {!pickerQuery.isLoading && pickerRows.length === 0 ? (
              <EmptyState
                align="center"
                size="compact"
                icon={<ArrowLeftRight className="size-5" strokeWidth={1.75} />}
                title={t("transfer.noProducts")}
                detail={t("transfer.noProductsDetail")}
              />
            ) : null}
            {pickerRows.length > 0 ? (
              <InventoryTransferProductSelection
                layout={pickerLayout}
                products={pickerRows}
                lotByProduct={lotByProduct}
                lotsCache={lotsCache}
                online={online}
                formatAvailable={formatAvailable}
                onLotChange={(productId, lotId) =>
                  setLotByProduct((prev) => ({ ...prev, [productId]: lotId }))
                }
                onLotFocus={(productId) => void ensureLots(productId, true)}
                onAddProduct={(row) => void addLine(row)}
                t={t}
              />
            ) : null}
          </div>
        </ExitsModal>

        <div className="receive-stock-actions product-selection-workspace__actions">
          <div className="receive-stock-actions__primary">
            <Button
              type="button"
              intent="primary"
              appearance="ghost"
              className="font-semibold"
              disabled={saving}
              onClick={() => navigate("/inventory/transfers")}
              data-testid="transfer-cancel-create"
            >
              <ArrowLeft className="size-4 shrink-0 rtl:rotate-180" aria-hidden />
              {t("transfer.backList")}
            </Button>
            <Button
              type="button"
              variant="outline"
              disabled={saving}
              onClick={resetForm}
              data-testid="transfer-reset-create"
            >
              <RotateCcw className="size-4 shrink-0" aria-hidden />
              {t("transfer.resetCreate")}
            </Button>
            <Button
              type="button"
              disabled={createDisabled}
              onClick={() => void saveDraft()}
              data-testid="transfer-save-draft"
            >
              <ArrowRightLeft className="size-4 shrink-0" aria-hidden />
              {saving ? t("transfer.saving") : t("transfer.saveDraft")}
            </Button>
          </div>
        </div>
      </div>
    </div>
  );
}

