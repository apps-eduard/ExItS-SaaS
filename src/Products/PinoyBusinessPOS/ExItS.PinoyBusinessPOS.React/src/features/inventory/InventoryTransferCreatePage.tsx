import { useEffect, useMemo, useRef, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
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
  getInventoryProduct,
  listInventory,
  listProductLots,
  type PosInventoryAccountDto,
  type PosInventoryLotDto,
} from "@/api/pos/pos-inventory-client";
import { PosApiError } from "@/api/pos/pos-http";
import {
  createInventoryTransfer,
  getInventoryTransfer,
  updateInventoryTransfer,
  type InventoryTransferDto,
  type InventoryTransferLineDto,
} from "@/api/pos/pos-inventory-transfer-client";
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
import {
  allocateTransferLotsFefo,
  resolveChangeLotsMaxQuantity,
  resolveTransferableAvailableQuantity,
  selectTransferEligibleLots,
  sumTransferLotAllocationQty,
  sumTransferPhysicalLotQuantity,
  type TransferLotAllocationSlice,
} from "@/features/inventory/inventory-transfer-fefo-allocate";
import { parseTransferQuantity } from "@/features/inventory/inventory-transfer-labels";
import { resolveAvailableQuantity } from "@/features/inventory/inventory-reservation-display";
import {
  canAddTransferQuantity,
  evaluateTransferDraftProduct,
  maxTransferableQuantity,
  type TransferDraftStockProduct,
  type TransferLineStockIssue,
} from "@/features/inventory/inventory-transfer-stock-guard";
import { InventoryTransferItemsView } from "@/features/inventory/InventoryTransferItemsView";
import { InventoryTransferProductSelection } from "@/features/inventory/InventoryTransferProductSelection";
import { TransferChangeLotsDialog } from "@/features/inventory/TransferChangeLotsDialog";
import { useI18n } from "@/i18n/I18nProvider";
import { createSecureMutationId } from "@/lib/secure-mutation-id";
import { isWarehouseBranch } from "@/features/branches/branch-type";
import { listSupplyRoutes } from "@/api/pos/pos-supply-routes-client";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

/** Synthetic category filter id — not a real catalog category. */
const OUT_OF_STOCK_FILTER_ID = "__transfer_out_of_stock__";

type AllocationMode = "auto" | "manual";

type DraftProduct = {
  key: string;
  productId: string;
  name: string;
  sku: string | null;
  unitOfMeasure: string;
  quantity: number;
  unitCost: number | null;
  tracksExpiration: boolean;
  isTracked: boolean;
  availableQuantity: number;
  eligibleLotQuantity: number | null;
  allocationMode: AllocationMode;
  allocations: TransferLotAllocationSlice[];
};

function eligibleLotQtyFromLots(lots: readonly PosInventoryLotDto[]): number {
  // Draft / line max includes expired physical stock (Change lots may move it).
  return sumTransferPhysicalLotQuantity(lots);
}

function transferableForPicker(
  row: PosInventoryAccountDto,
  lotsCache: Record<string, PosInventoryLotDto[]>,
): number {
  return resolveTransferableAvailableQuantity({
    branchAvailable: Math.max(0, resolveAvailableQuantity(row)),
    tracksExpiration: row.tracksExpiration === true,
    lots: lotsCache[row.productId],
  });
}

function toStockProduct(draft: DraftProduct): TransferDraftStockProduct {
  return {
    key: draft.key,
    productId: draft.productId,
    quantity: draft.quantity,
    availableQuantity: draft.availableQuantity,
    eligibleLotQuantity: draft.eligibleLotQuantity,
    tracksExpiration: draft.tracksExpiration,
    isTracked: draft.isTracked,
    allocations: draft.allocations.map((row) => ({
      lotId: row.lotId,
      quantity: row.quantity,
      lotAvailableQuantity: row.lotAvailableQuantity,
    })),
  };
}

function expandDraftToRequestLines(drafts: readonly DraftProduct[]) {
  const lines: { productId: string; quantity: number; sourceLotId: string | null }[] = [];
  for (const draft of drafts) {
    if (draft.tracksExpiration) {
      for (const slice of draft.allocations) {
        if (!(slice.quantity > 0)) {
          continue;
        }
        lines.push({
          productId: draft.productId,
          quantity: slice.quantity,
          sourceLotId: slice.lotId,
        });
      }
    } else {
      lines.push({
        productId: draft.productId,
        quantity: draft.quantity,
        sourceLotId: null,
      });
    }
  }
  return lines;
}

function groupTransferLinesByProduct(
  lines: readonly InventoryTransferLineDto[],
): Map<string, InventoryTransferLineDto[]> {
  const groups = new Map<string, InventoryTransferLineDto[]>();
  for (const line of lines) {
    const existing = groups.get(line.productId);
    if (existing) {
      existing.push(line);
    } else {
      groups.set(line.productId, [line]);
    }
  }
  return groups;
}

function draftFromTransferLines(
  productLines: readonly InventoryTransferLineDto[],
  lots: readonly PosInventoryLotDto[],
  availableQuantity: number,
): DraftProduct {
  const first = productLines[0]!;
  const tracksExpiration = productLines.some((line) => Boolean(line.sourceLotId));
  const quantity = productLines.reduce((sum, line) => sum + line.sentQty, 0);
  const lotById = new Map(lots.map((lot) => [lot.lotId, lot]));
  const allocations: TransferLotAllocationSlice[] = tracksExpiration
    ? productLines
        .filter((line) => line.sourceLotId && line.sentQty > 0)
        .map((line) => {
          const lot = lotById.get(line.sourceLotId!);
          return {
            lotId: line.sourceLotId!,
            quantity: line.sentQty,
            lotAvailableQuantity: lot ? Math.max(0, lot.quantityOnHand) : 0,
            expirationDate: lot?.expirationDate ?? line.expirationDate ?? "",
            lotNumber: lot?.lotNumber ?? line.lotNumber ?? null,
          };
        })
    : [];

  return {
    key: first.productId,
    productId: first.productId,
    name: first.productName,
    sku: first.sku ?? null,
    unitOfMeasure: first.unitOfMeasure,
    quantity,
    unitCost: first.unitCostSnapshot ?? null,
    tracksExpiration,
    isTracked: true,
    availableQuantity,
    eligibleLotQuantity: tracksExpiration ? eligibleLotQtyFromLots(lots) : null,
    // Preserve saved lot picks; auto would re-FEFO and may diverge from the draft.
    allocationMode: tracksExpiration ? "manual" : "auto",
    allocations,
  };
}

function allocateAuto(
  lots: readonly PosInventoryLotDto[],
  quantity: number,
): TransferLotAllocationSlice[] | null {
  const result = allocateTransferLotsFefo(selectTransferEligibleLots(lots), quantity);
  return result.ok ? result.allocations : null;
}

function refreshAllocationAvailability(
  allocations: readonly TransferLotAllocationSlice[],
  lots: readonly PosInventoryLotDto[],
): TransferLotAllocationSlice[] {
  const byId = new Map(lots.map((lot) => [lot.lotId, lot]));
  return allocations.map((row) => {
    const lot = byId.get(row.lotId);
    return {
      ...row,
      lotAvailableQuantity: lot ? Math.max(0, lot.quantityOnHand) : 0,
      lotNumber: lot?.lotNumber ?? row.lotNumber,
      expirationDate: lot?.expirationDate ?? row.expirationDate,
    };
  });
}

export function InventoryTransferCreatePage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const { transferId: editTransferId } = useParams<{ transferId?: string }>();
  const isEditMode = Boolean(editTransferId);
  const queryClient = useQueryClient();
  const online = useBrowserOnline();
  const { boundWorkspace, sessionGrant, workspaces } = useWorkspace();
  const allowManage = canManageInventory(sessionGrant);

  const [destinationBranchId, setDestinationBranchId] = useState("");
  const [notes, setNotes] = useState("");
  const [search, setSearch] = useState("");
  const [debounced, setDebounced] = useState("");
  const [categoryIds, setCategoryIds] = useState<string[]>([]);
  const [lines, setLines] = useState<DraftProduct[]>([]);
  const [lotsCache, setLotsCache] = useState<Record<string, PosInventoryLotDto[]>>({});
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [finderOpen, setFinderOpen] = useState(false);
  const [changeLotsProductId, setChangeLotsProductId] = useState<string | null>(null);
  const [editHydrated, setEditHydrated] = useState(!isEditMode);
  const [editLoadError, setEditLoadError] = useState<string | null>(null);
  const [linkedStockRequestId, setLinkedStockRequestId] = useState<string | null>(null);
  const finderPanelId = "transfer-product-finder-panel";
  const operationIdRef = useRef<string | null>(null);
  const isStockRequestShipment = Boolean(linkedStockRequestId);
  const { layout: pickerLayout } = useResponsiveDataLayout({
    tableMinWidthPx: PRODUCT_SELECTION_TABLE_MIN_PX,
  });

  function openFinder() {
    if (isStockRequestShipment) {
      return;
    }
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

  const isWarehouseSource = isWarehouseBranch(boundWorkspace?.branchType);

  const coverageQuery = useQuery({
    queryKey: [
      "supply-routes",
      workspace?.organizationId,
      "transfer-coverage",
      workspace?.branchId,
    ],
    enabled: Boolean(workspace) && online && allowManage && isWarehouseSource && !isEditMode,
    queryFn: ({ signal }) => listSupplyRoutes(workspace!, signal),
  });

  const destinations = useMemo(() => {
    const others = orgBranches.filter((b) => b.branchId !== boundWorkspace?.branchId);
    if (!isWarehouseSource || isEditMode) {
      return others;
    }
    const routes = coverageQuery.data ?? [];
    const allowed = new Set(
      routes
        .filter(
          (route) =>
            route.isActive && route.sourceLocationId === boundWorkspace?.branchId,
        )
        .map((route) => route.destinationLocationId),
    );
    return others.filter((branch) => allowed.has(branch.branchId));
  }, [
    orgBranches,
    boundWorkspace?.branchId,
    isWarehouseSource,
    isEditMode,
    coverageQuery.data,
  ]);

  useEffect(() => {
    if (isEditMode || !destinationBranchId) {
      return;
    }
    if (isWarehouseSource && coverageQuery.isLoading) {
      return;
    }
    if (destinations.some((branch) => branch.branchId === destinationBranchId)) {
      return;
    }
    setDestinationBranchId("");
  }, [
    isEditMode,
    destinationBranchId,
    destinations,
    isWarehouseSource,
    coverageQuery.isLoading,
  ]);

  const multiBranch = orgBranches.length >= 2;
  const sourceName = boundWorkspace?.branchName ?? t("transfer.sourceBranch");
  const coveragePending = isWarehouseSource && !isEditMode && coverageQuery.isLoading;
  const coverageFailed = isWarehouseSource && !isEditMode && coverageQuery.isError;
  const noCoveredDestinations =
    isWarehouseSource &&
    !isEditMode &&
    !coveragePending &&
    !coverageFailed &&
    multiBranch &&
    destinations.length === 0;

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
      const branchAvailable = Math.max(0, resolveAvailableQuantity(row));
      const transferable = transferableForPicker(row, lotsCache);
      const outOfStock = transferable <= 0;
      const expiredOnlyPhysical =
        row.tracksExpiration === true &&
        lotsCache[row.productId] != null &&
        branchAvailable > 0 &&
        transferable <= 0;
      if (wantOutOfStock) {
        if (!outOfStock) {
          return false;
        }
        if (realSelected.size === 0) {
          return true;
        }
        return row.categoryId != null && realSelected.has(row.categoryId);
      }
      // Default: transferable in-stock, or expired-only physical (show as unavailable).
      if (outOfStock && !expiredOnlyPhysical) {
        return false;
      }
      if (realSelected.size === 0) {
        return true;
      }
      return row.categoryId != null && realSelected.has(row.categoryId);
    });
  }, [pickerQuery.data?.items, categoryIds, lines, lotsCache]);

  const transferCategoryOptions = useMemo(() => {
    const tracked = (pickerQuery.data?.items ?? []).filter((r) => r.isTracked);
    const addedProductIds = new Set(lines.map((line) => line.productId));
    const counts = new Map<string, { name: string; count: number }>();
    let outOfStockCount = 0;
    for (const row of tracked) {
      if (addedProductIds.has(row.productId)) {
        continue;
      }
      if (transferableForPicker(row, lotsCache) <= 0) {
        outOfStockCount += 1;
      }
      const id = row.categoryId?.trim();
      if (!id || transferableForPicker(row, lotsCache) <= 0) {
        // Real category counts reflect transferable in-stock products (default table).
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
  }, [pickerQuery.data?.items, lines, lotsCache, t]);

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
      case "allocation_mismatch":
        return t("transfer.allocationMismatch");
      case "invalid_qty":
        return t("transfer.invalidQuantity");
    }
  }

  function formatAvailable(qty: number, uom: string) {
    const unit = uom.trim();
    return unit ? `${qty}/${unit}` : String(qty);
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

  // Load existing draft when editing.
  useEffect(() => {
    if (!isEditMode || !editTransferId || !workspace || !boundWorkspace?.branchId) {
      return;
    }
    if (!online || !allowManage) {
      return;
    }

    let cancelled = false;
    setEditHydrated(false);
    setEditLoadError(null);

    async function hydrate(transfer: InventoryTransferDto) {
      if (transfer.status !== "Draft") {
        setEditLoadError(t("transfer.draftNoEdit"));
        return;
      }
      if (transfer.sourceBranchId !== boundWorkspace!.branchId) {
        setEditLoadError(t("transfer.draftNoEdit"));
        return;
      }

      setDestinationBranchId(transfer.destinationBranchId);
      setNotes(transfer.notes?.trim() ?? "");
      setLinkedStockRequestId(transfer.stockRequestId?.trim() || null);

      const groups = groupTransferLinesByProduct(transfer.lines);
      const nextLots: Record<string, PosInventoryLotDto[]> = {};
      const drafts: DraftProduct[] = [];
      for (const [, productLines] of groups) {
        const tracksExpiration = productLines.some((line) => Boolean(line.sourceLotId));
        const productId = productLines[0]!.productId;
        let lots: PosInventoryLotDto[] = [];
        if (tracksExpiration) {
          try {
            const result = await listProductLots(workspace!, productId, { pageSize: 50 });
            lots = result.items;
            nextLots[productId] = lots;
          } catch {
            nextLots[productId] = [];
          }
        }

        // Never leave edit lines at availableQuantity 0 by default — picker refresh only
        // covers products on the current inventory page and can miss draft lines.
        let availableQuantity = 0;
        try {
          const account = await getInventoryProduct(workspace!, productId);
          availableQuantity = Math.max(0, resolveAvailableQuantity(account));
        } catch {
          availableQuantity = 0;
        }

        drafts.push(draftFromTransferLines(productLines, lots, availableQuantity));
      }

      if (cancelled) {
        return;
      }
      setLotsCache((prev) => ({ ...prev, ...nextLots }));
      setLines(drafts);
      setEditHydrated(true);
    }

    void getInventoryTransfer(workspace, editTransferId)
      .then((transfer) => {
        if (cancelled) {
          return;
        }
        return hydrate(transfer);
      })
      .catch((err) => {
        if (cancelled) {
          return;
        }
        const detail =
          err instanceof PosApiError
            ? (err.problem.detail ?? t("transfer.loadFailed"))
            : t("transfer.loadFailed");
        setEditLoadError(detail);
      });

    return () => {
      cancelled = true;
    };
  }, [
    isEditMode,
    editTransferId,
    workspace,
    boundWorkspace?.branchId,
    online,
    allowManage,
    t,
  ]);

  // Prefetch lot counts for expiry products visible in the finder.
  useEffect(() => {
    if (!finderOpen || !workspace) {
      return;
    }
    for (const row of pickerRows) {
      if (row.tracksExpiration === true && !lotsCache[row.productId]) {
        void ensureLots(row.productId, true);
      }
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps -- prefetch when finder rows change
  }, [finderOpen, pickerRows, workspace]);

  async function addProduct(row: PosInventoryAccountDto) {
    const tracksExpiration = row.tracksExpiration === true;
    const availableQuantity = Math.max(0, resolveAvailableQuantity(row));
    const lots = await ensureLots(row.productId, tracksExpiration);
    const eligibleLotQuantity = tracksExpiration ? eligibleLotQtyFromLots(lots) : null;
    const maxQty = maxTransferableQuantity({
      availableQuantity,
      eligibleLotQuantity,
      tracksExpiration,
    });

    if (maxQty <= 0) {
      setError(t("transfer.outOfStock"));
      return;
    }

    const existing = lines.find((l) => l.productId === row.productId);
    const qtyParsed = existing ? existing.quantity + 1 : 1;

    let allocations: TransferLotAllocationSlice[] = [];
    if (tracksExpiration) {
      const auto = allocateAuto(lots, qtyParsed);
      if (!auto) {
        setError(
          stockIssueMessage(
            "lot_over_stock",
            maxQty,
            row.unitOfMeasure,
          ),
        );
        return;
      }
      allocations = auto;
    }

    const issue = canAddTransferQuantity({
      quantity: qtyParsed,
      availableQuantity: maxQty,
      lotAvailableQuantity: tracksExpiration ? eligibleLotQuantity : null,
      tracksExpiration,
      existingProductDemand: 0,
      existingLotDemand: 0,
    });
    if (issue) {
      setError(stockIssueMessage(issue, maxQty, row.unitOfMeasure));
      return;
    }

    const unitCost =
      row.unitCost != null && Number.isFinite(row.unitCost) && row.unitCost > 0
        ? row.unitCost
        : null;

    setError(null);
    setLines((prev) => {
      const existingIndex = prev.findIndex((l) => l.productId === row.productId);
      if (existingIndex >= 0) {
        const current = prev[existingIndex]!;
        const nextAllocations =
          tracksExpiration && current.allocationMode === "auto"
            ? allocations
            : tracksExpiration
              ? current.allocations
              : [];
        const next = [...prev];
        next[existingIndex] = {
          ...current,
          quantity: qtyParsed,
          availableQuantity,
          eligibleLotQuantity,
          unitCost: unitCost ?? current.unitCost,
          allocations: nextAllocations,
          allocationMode:
            tracksExpiration && current.allocationMode === "manual" ? "manual" : "auto",
        };
        return next;
      }
      return [
        ...prev,
        {
          key: row.productId,
          productId: row.productId,
          name: row.name,
          sku: row.sku?.trim() || null,
          unitOfMeasure: row.unitOfMeasure,
          quantity: qtyParsed,
          unitCost,
          tracksExpiration,
          isTracked: row.isTracked,
          availableQuantity,
          eligibleLotQuantity,
          allocationMode: "auto",
          allocations,
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
        prev.map((line) => (line.key === key ? { ...line, quantity: 0, allocations: [] } : line)),
      );
      setError(t("transfer.invalidQuantity"));
      return;
    }
    setLines((prev) => {
      const target = prev.find((l) => l.key === key);
      if (!target) {
        return prev;
      }
      let nextAllocations = target.allocations;
      let nextMode = target.allocationMode;
      if (target.tracksExpiration) {
        if (target.allocationMode === "auto") {
          const lots = lotsCache[target.productId] ?? [];
          const auto = allocateAuto(lots, parsed);
          nextAllocations = auto ?? [];
          nextMode = "auto";
        }
        // Manual mode: keep existing allocations; validation surfaces mismatch.
      }
      const nextLine: DraftProduct = {
        ...target,
        quantity: parsed,
        allocations: nextAllocations,
        allocationMode: nextMode,
      };
      const issue = evaluateTransferDraftProduct(toStockProduct(nextLine));
      if (issue) {
        const cap = maxTransferableQuantity(nextLine);
        setError(stockIssueMessage(issue, cap, nextLine.unitOfMeasure));
      } else {
        setError(null);
      }
      return prev.map((line) => (line.key === key ? nextLine : line));
    });
  }

  function applyLotAllocation(
    productId: string,
    allocations: TransferLotAllocationSlice[],
    mode: AllocationMode,
  ) {
    const total = sumTransferLotAllocationQty(allocations);
    const lots = lotsCache[productId] ?? [];
    const physicalLotQuantity = sumTransferPhysicalLotQuantity(lots);
    setLines((prev) =>
      prev.map((line) => {
        if (line.productId !== productId) {
          return line;
        }
        const nextLine: DraftProduct = {
          ...line,
          quantity: total,
          allocations,
          allocationMode: mode,
          // Allow total that includes expired lots (physical move).
          eligibleLotQuantity: line.tracksExpiration ? physicalLotQuantity : null,
        };
        const issue = evaluateTransferDraftProduct(toStockProduct(nextLine));
        if (issue) {
          const cap = maxTransferableQuantity(nextLine);
          setError(stockIssueMessage(issue, cap, nextLine.unitOfMeasure));
        } else {
          setError(null);
        }
        return nextLine;
      }),
    );
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
    setError(null);
    setChangeLotsProductId(null);
    operationIdRef.current = null;
  }

  const lineIssues = useMemo(() => {
    const map = new Map<string, TransferLineStockIssue>();
    for (const line of lines) {
      const issue = evaluateTransferDraftProduct(toStockProduct(line));
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
      const cap = maxTransferableQuantity(line);
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

  async function refetchLotsAndRecalcFefo() {
    if (!workspace) {
      return;
    }
    const nextCache: Record<string, PosInventoryLotDto[]> = {};
    for (const line of lines) {
      if (!line.tracksExpiration) {
        continue;
      }
      try {
        const result = await listProductLots(workspace, line.productId, { pageSize: 50 });
        nextCache[line.productId] = result.items;
      } catch {
        nextCache[line.productId] = [];
      }
    }
    setLotsCache(nextCache);
    setLines((prev) =>
      prev.map((line) => {
        if (!line.tracksExpiration) {
          return line;
        }
        const lots = nextCache[line.productId] ?? [];
        const eligibleLotQuantity = eligibleLotQtyFromLots(lots);
        if (line.allocationMode === "auto") {
          const auto = allocateAuto(lots, line.quantity);
          return {
            ...line,
            eligibleLotQuantity,
            allocations: auto ?? [],
          };
        }
        return {
          ...line,
          eligibleLotQuantity,
          allocations: refreshAllocationAvailability(line.allocations, lots),
        };
      }),
    );
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
      const requestLines = expandDraftToRequestLines(lines);
      const saved =
        isEditMode && editTransferId
          ? await updateInventoryTransfer(workspace, editTransferId, {
              notes: notes.trim() || null,
              operationId: operationIdRef.current,
              lines: requestLines,
            })
          : await createInventoryTransfer(workspace, {
              sourceBranchId: boundWorkspace.branchId,
              destinationBranchId,
              notes: notes.trim() || null,
              operationId: operationIdRef.current,
              lines: requestLines,
            });
      operationIdRef.current = null;
      void queryClient.invalidateQueries({ queryKey: ["inventory", "transfers"] });
      navigate(`/inventory/transfers/${saved.transferId}`, {
        replace: true,
        state: { flash: isEditMode ? "updated" : "created" },
      });
    } catch (err) {
      const detail =
        err instanceof PosApiError
          ? (err.problem.detail ?? t("transfer.saveFailed"))
          : t("transfer.saveFailed");
      const errorCode =
        err instanceof PosApiError ? (err.problem.errorCode ?? "").toLowerCase() : "";
      // Only remap true lot identity / concurrency failures — not ordinary insufficient stock.
      const looksLikeLotConcurrency =
        errorCode.includes("lot_mismatch") ||
        errorCode.includes("lotmismatch") ||
        /lot (was not found|does not belong|mismatch)/i.test(detail);
      if (errorCode.includes("expired") || /expired.*transfer|lot.*expired/i.test(detail)) {
        setError(
          /dispatch|review the lot allocation before dispatching/i.test(detail)
            ? t("transfer.dispatchLotsExpired")
            : t("transfer.lotExpiredCannotTransfer"),
        );
      } else if (errorCode.includes("destination_not_covered")) {
        setError(t("transfer.destinationNotCovered"));
      } else if (looksLikeLotConcurrency) {
        setError(t("transfer.lotStockChanged"));
      } else {
        setError(detail);
      }
      await refreshAvailability();
      await refetchLotsAndRecalcFefo();
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

  const changeLotsLine = changeLotsProductId
    ? lines.find((l) => l.productId === changeLotsProductId) ?? null
    : null;

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }

  if (!allowManage) {
    return (
      <div className="exits-page flex min-w-0 flex-col gap-3" data-testid="transfer-create-denied">
        <PageHeader
          title={isEditMode ? t("transfer.editTitle") : t("transfer.newTitle")}
          backTo={isEditMode && editTransferId ? `/inventory/transfers/${editTransferId}` : "/inventory/transfers"}
          backLabel={isEditMode ? t("transfer.backToTransfer") : t("transfer.backList")}
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
          title={isEditMode ? t("transfer.editTitle") : t("transfer.newTitle")}
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

  if (coveragePending) {
    return <LoadingState label={t("session.loading")} />;
  }

  if (coverageFailed) {
    return (
      <div className="exits-page flex min-w-0 flex-col gap-3" data-testid="transfer-create-coverage-error">
        <PageHeader
          title={isEditMode ? t("transfer.editTitle") : t("transfer.newTitle")}
          backTo="/inventory/transfers"
          backLabel={t("transfer.backList")}
          backTestId="page-header-back-transfers"
        />
        <ErrorState
          title={t("transfer.errorTitle")}
          detail={t("transfer.coverageLoadFailed")}
        />
      </div>
    );
  }

  if (noCoveredDestinations) {
    return (
      <div
        className="exits-page flex min-w-0 flex-col gap-3"
        data-testid="transfer-create-no-covered-destinations"
      >
        <PageHeader
          title={isEditMode ? t("transfer.editTitle") : t("transfer.newTitle")}
          backTo="/inventory/transfers"
          backLabel={t("transfer.backList")}
          backTestId="page-header-back-transfers"
        />
        <EmptyState
          align="center"
          variant="setup"
          icon={<Store className="size-5" strokeWidth={1.75} />}
          title={t("transfer.noCoveredDestinationsTitle")}
          detail={t("transfer.noCoveredDestinationsDetail")}
        />
      </div>
    );
  }

  if (isEditMode && editLoadError) {
    return (
      <div className="exits-page flex min-w-0 flex-col gap-3" data-testid="transfer-edit-load-error">
        <PageHeader
          title={t("transfer.editTitle")}
          backTo={editTransferId ? `/inventory/transfers/${editTransferId}` : "/inventory/transfers"}
          backLabel={t("transfer.backToTransfer")}
          backTestId="page-header-back-transfers"
        />
        <ErrorState title={t("transfer.errorTitle")} detail={editLoadError} />
      </div>
    );
  }

  if (isEditMode && !editHydrated) {
    return <LoadingState label={t("session.loading")} />;
  }

  const createDisabled =
    !online || saving || Boolean(createBlockedReason) || lines.length === 0 || !destinationBranchId;

  const pageBackTo =
    isEditMode && editTransferId ? `/inventory/transfers/${editTransferId}` : "/inventory/transfers";
  const editPageTitle = isStockRequestShipment
    ? t("transfer.editShipmentTitle")
    : t("transfer.editTitle");
  const editPageLede = isStockRequestShipment
    ? t("transfer.editShipmentLede")
    : t("transfer.editLede");
  const pageTitle = isEditMode ? editPageTitle : t("transfer.newTitle");
  const pageDescription = isEditMode ? editPageLede : t("transfer.newLede");

  return (
    <div
      className="inventory-transfer-create-page exits-page flex min-w-0 flex-col gap-4"
      data-testid={isEditMode ? "inventory-transfer-edit-page" : "inventory-transfer-create-page"}
      data-edit-mode={isStockRequestShipment ? "stock-request-shipment" : isEditMode ? "direct" : undefined}
    >
      <PageHeader
        title={pageTitle}
        description={pageDescription}
        backTo={pageBackTo}
        backLabel={isEditMode ? t("transfer.backToTransfer") : t("transfer.backList")}
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
            value: isEditMode ? (
              <span
                className="inline-flex min-w-0 items-center gap-2 font-semibold"
                data-testid="transfer-destination-branch-locked"
              >
                <Store className="size-4 shrink-0 text-primary" strokeWidth={1.75} aria-hidden />
                <span className="min-w-0 truncate">
                  {orgBranches.find((b) => b.branchId === destinationBranchId)?.name ??
                    destinations.find((b) => b.branchId === destinationBranchId)?.name ??
                    destinationBranchId}
                </span>
              </span>
            ) : (
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
          emptyTitle={
            isStockRequestShipment
              ? t("transfer.shipmentItemsEmpty")
              : t("transfer.itemsEmpty")
          }
          emptyDetail={
            isStockRequestShipment
              ? t("transfer.shipmentItemsEmptyDetail")
              : t("transfer.itemsEmptyDetail")
          }
          emptyTestId="transfer-selected-items-empty"
          addTestId="transfer-add-products-trigger"
          testId="transfer-draft-lines"
          hideAddButton={isStockRequestShipment}
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
              sku: line.sku,
              quantity: line.quantity,
              unitOfMeasure: line.unitOfMeasure,
              availableQuantity: line.availableQuantity,
              maxQuantity: maxTransferableQuantity(line),
              unitCost: line.unitCost,
              tracksExpiration: line.tracksExpiration,
              allocationMode: line.allocationMode,
              allocations: line.allocations,
              hasIssue: Boolean(lineIssues.get(line.key)),
              onQtyChange: (next) => updateLineQuantity(line.key, String(next)),
              onRemove: () => removeLine(line.key),
              onChangeLots: line.tracksExpiration
                ? () => setChangeLotsProductId(line.productId)
                : null,
            }))}
            formatAvailable={formatAvailable}
            t={t}
            lineActionMode={isStockRequestShipment ? "none" : "remove"}
          />
        </SelectedItemsPanel>

        {isStockRequestShipment ? null : (
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
                lotsCache={lotsCache}
                online={online}
                formatAvailable={formatAvailable}
                onAddProduct={(row) => void addProduct(row)}
                t={t}
              />
            ) : null}
          </div>
        </ExitsModal>
        )}

        {changeLotsLine ? (
          <TransferChangeLotsDialog
            open
            productName={changeLotsLine.name}
            unitOfMeasure={changeLotsLine.unitOfMeasure}
            transferQuantity={changeLotsLine.quantity}
            maxTransferableQuantity={resolveChangeLotsMaxQuantity({
              branchAvailable: changeLotsLine.availableQuantity,
              tracksExpiration: changeLotsLine.tracksExpiration,
              lots: lotsCache[changeLotsLine.productId],
            })}
            lots={lotsCache[changeLotsLine.productId] ?? []}
            initialAllocations={changeLotsLine.allocations}
            onOpenChange={(open) => {
              if (!open) {
                setChangeLotsProductId(null);
              }
            }}
            onApply={(allocations, mode) =>
              applyLotAllocation(changeLotsLine.productId, allocations, mode)
            }
            t={t}
          />
        ) : null}

        <div className="receive-stock-actions product-selection-workspace__actions">
          <div className="receive-stock-actions__primary">
            <Button
              type="button"
              intent="primary"
              appearance="ghost"
              className="font-semibold"
              disabled={saving}
              onClick={() => navigate(pageBackTo)}
              data-testid="transfer-cancel-create"
            >
              <ArrowLeft className="size-4 shrink-0 rtl:rotate-180" aria-hidden />
              {isEditMode ? t("transfer.backToTransfer") : t("transfer.backList")}
            </Button>
            {!isEditMode ? (
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
            ) : null}
            <Button
              type="button"
              disabled={createDisabled}
              onClick={() => void saveDraft()}
              data-testid="transfer-save-draft"
            >
              <ArrowRightLeft className="size-4 shrink-0" aria-hidden />
              {saving
                ? t("transfer.saving")
                : isEditMode
                  ? t("transfer.saveChanges")
                  : t("transfer.saveDraft")}
            </Button>
          </div>
        </div>
      </div>
    </div>
  );
}
