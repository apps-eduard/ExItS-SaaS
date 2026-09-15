import { useEffect, useMemo, useRef, useState } from "react";
import { Link, useNavigate, useSearchParams } from "react-router-dom";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { BookOpen, Check, ClipboardList, PackageSearch, Plus, Store } from "lucide-react";
import { canManageCatalog, canManagePurchasing } from "@/access/pos-capabilities";
import { describePosApiError } from "@/access/pos-commercial-errors";
import { listCatalogProducts } from "@/api/pos/pos-catalog-client";
import type { PosCatalogProductDto } from "@/api/pos/pos-catalog-types";
import {
  classifyCatalogReadiness,
  createBuyerProductAndLink,
  getConnectedOrderStock,
  linkProduct,
  listLinks,
  searchExposedCatalog,
  type CatalogProductReadinessItem,
  type SupplierProductExposure,
} from "@/api/pos/pos-connected-suppliers-client";
import {
  createPurchaseOrder,
  getPurchaseOrder,
} from "@/api/pos/pos-purchase-orders-client";
import {
  isConnectedSupplier,
  listSuppliers,
  type PosSupplier,
} from "@/api/pos/pos-suppliers-client";
import { PosApiError } from "@/api/pos/pos-http";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/exits/EmptyState";
import { LoadingState } from "@/components/exits/LoadingState";
import { MoneyDisplay, QuantityStepper } from "@/components/exits/MoneyQuantity";
import { Notice } from "@/components/exits/Notice";
import { PageHeader } from "@/components/exits/PageHeader";
import { SearchField } from "@/components/exits/SearchField";
import { StatusChip } from "@/components/exits/StatusChip";
import { UnderlineTabBar } from "@/components/exits/UnderlineTabBar";
import { useResponsiveDataLayout } from "@/components/exits/useResponsiveDataLayout";
import { useBrowserOnline } from "@/connectivity/browser-online";
import {
  applyConnectedQuantityDelta,
  buildConnectedCategoryOptions,
  buildConnectedReadyProducts,
  connectedCategoryFilterFromValues,
  connectedCategoryFilterToValues,
  emptyConnectedCategoryFilter,
  filterConnectedReadyProducts,
  filterItemsByConnectedCategory,
  formatSupplierAvailabilityLabel,
  formatUnitOfMeasureLabel,
  isConnectedCategoryFilterActive,
  mergeConnectedStock,
  orderSubtotal,
  orderUnitCount,
  requestedExceedsSupplierAvailability,
  resolveConnectedCategoryId,
  retainCompatibleDraftLines,
  type ConnectedPoCategoryFilter,
  type ConnectedPoDraftLine,
  type ConnectedPoReadyProduct,
} from "@/features/purchasing/purchase-order-create-connected";
import { ProductCategoryMultiSelect } from "@/components/exits/ProductCategoryMultiSelect";
import {
  ProductFinderPanel,
  SelectedItemsPanel,
} from "@/components/exits/ProductSelectionWorkspace";
import { PRODUCT_SELECTION_TABLE_MIN_PX } from "@/components/exits/product-selection-view";
import { RESPONSIVE_DATA_TABLE_MIN_LG } from "@/components/exits/responsive-data-view";
import { PurchaseOrderLinkedProductsFinder } from "@/features/purchasing/PurchaseOrderLinkedProductsFinder";
import { PurchaseOrderItemsView } from "@/features/purchasing/PurchaseOrderItemsView";
import { PoDocumentSummary } from "@/features/purchasing/PoDocumentSummary";
import {
  CONNECTED_PO_PAYMENT_OPTIONS,
  resolvePoUtangEligibility,
  type ConnectedPoPaymentMethodCode,
} from "@/features/purchasing/po-payment-methods";
import { getBusinessCustomerCreditPolicy } from "@/api/pos/pos-business-credit-policy-client";
import {
  isBulkConnectSelectable,
  partitionBulkConnectSelection,
} from "@/features/suppliers/connected-catalog-bulk";
import {
  countByUserState,
  filterReadinessItems,
  mapBackendStatusToUserState,
  type CatalogReadinessFilter,
} from "@/features/suppliers/connected-catalog-readiness";
import { useI18n } from "@/i18n/I18nProvider";
import { formatPeso } from "@/lib/format-money";
import { createSecureMutationId } from "@/lib/secure-mutation-id";
import { resolveAmbiguousMutationOutcome } from "@/runtime/ambiguous-mutation-outcome";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

type ExternalDraftLine = {
  productId: string;
  name: string;
  uom: string;
  orderedQty: number;
  unitPurchaseCost: number;
};

/** PO ordering tabs — Shared Catalog readiness without All; default Linked. */
type PoCatalogSetupFilter = Exclude<CatalogReadinessFilter, "all">;

function todayIsoDate(): string {
  const d = new Date();
  const yyyy = d.getFullYear();
  const mm = String(d.getMonth() + 1).padStart(2, "0");
  const dd = String(d.getDate()).padStart(2, "0");
  return `${yyyy}-${mm}-${dd}`;
}

async function loadAllExposedCatalog(
  workspace: { organizationId: string; branchId: string },
  relationshipId: string,
  signal?: AbortSignal,
): Promise<SupplierProductExposure[]> {
  const pageSize = 50;
  let page = 1;
  const items: SupplierProductExposure[] = [];
  while (true) {
    const result = await searchExposedCatalog(
      workspace,
      relationshipId,
      { page, pageSize },
      signal,
    );
    items.push(...result.items);
    if (items.length >= result.totalCount || result.items.length === 0) {
      break;
    }
    page += 1;
    if (page > 40) {
      break;
    }
  }
  return items;
}

export function PurchaseOrderCreatePage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const online = useBrowserOnline();
  const queryClient = useQueryClient();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const allowManage = canManagePurchasing(sessionGrant);
  const allowCreate = allowManage && canManageCatalog(sessionGrant);
  const { layout: linkedProductsLayout } = useResponsiveDataLayout({
    tableMinWidthPx: PRODUCT_SELECTION_TABLE_MIN_PX,
  });
  const { layout: orderItemsLayout } = useResponsiveDataLayout({
    tableMinWidthPx: RESPONSIVE_DATA_TABLE_MIN_LG,
  });
  const [finderOpen, setFinderOpen] = useState(false);
  const finderPanelId = "po-product-finder-panel";

  const [supplierId, setSupplierId] = useState(
    () => searchParams.get("supplierId")?.trim() ?? "",
  );
  const [orderDate, setOrderDate] = useState(todayIsoDate);
  const [notes, setNotes] = useState("");
  const [search, setSearch] = useState("");
  const [debounced, setDebounced] = useState("");
  const [categoryFilter, setCategoryFilter] = useState<ConnectedPoCategoryFilter>(
    emptyConnectedCategoryFilter,
  );
  const [readinessFilter, setReadinessFilter] = useState<PoCatalogSetupFilter>("linked");
  const [connectedLines, setConnectedLines] = useState<ConnectedPoDraftLine[]>([]);
  const [externalLines, setExternalLines] = useState<ExternalDraftLine[]>([]);
  const [qtyText, setQtyText] = useState("1");
  const [costText, setCostText] = useState("");
  const [selectedProduct, setSelectedProduct] = useState<PosCatalogProductDto | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [statusLocked, setStatusLocked] = useState(false);
  const [setupSelected, setSetupSelected] = useState<Set<string>>(() => new Set());
  const [setupBusyKey, setSetupBusyKey] = useState<string | null>(null);
  const [setupBulkBusy, setSetupBulkBusy] = useState(false);
  const [paymentTerm, setPaymentTerm] = useState<ConnectedPoPaymentMethodCode | "">("");
  const purchaseOrderIdRef = useRef<string | null>(null);

  useEffect(() => {
    const handle = window.setTimeout(() => setDebounced(search.trim()), 250);
    return () => window.clearTimeout(handle);
  }, [search]);

  useEffect(() => {
    setSetupSelected(new Set());
  }, [debounced, readinessFilter, supplierId, categoryFilter]);

  useEffect(() => {
    setCategoryFilter(emptyConnectedCategoryFilter());
  }, [supplierId]);

  const workspace = useMemo(
    () =>
      boundWorkspace?.branchId
        ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
        : null,
    [boundWorkspace],
  );

  const suppliersQuery = useQuery({
    queryKey: ["suppliers", "po-create", workspace?.organizationId],
    enabled: Boolean(workspace) && online && allowManage,
    queryFn: ({ signal }) => listSuppliers(workspace!, { status: "Active", pageSize: 100 }, signal),
  });

  const selectedSupplier: PosSupplier | null = useMemo(() => {
    if (!supplierId) {
      return null;
    }
    return (suppliersQuery.data?.items ?? []).find((s) => s.supplierId === supplierId) ?? null;
  }, [supplierId, suppliersQuery.data]);

  const connected =
    selectedSupplier != null &&
    isConnectedSupplier(selectedSupplier) &&
    Boolean(selectedSupplier.connectedRelationshipId);
  const relationshipId = selectedSupplier?.connectedRelationshipId ?? null;

  const linkedProductsQuery = useQuery({
    queryKey: ["connected-suppliers", "po-links", relationshipId],
    enabled: Boolean(workspace) && online && allowManage && connected && Boolean(relationshipId),
    queryFn: async ({ signal }) => {
      const [links, exposures] = await Promise.all([
        listLinks(workspace!, relationshipId!, signal),
        loadAllExposedCatalog(workspace!, relationshipId!, signal).catch(() => [] as SupplierProductExposure[]),
      ]);
      const ready = buildConnectedReadyProducts(links, exposures.length > 0 ? exposures : null);
      const categoryBySupplierProductId = new Map<
        string,
        { categoryId: string | null; categoryName: string | null }
      >();
      for (const exposure of exposures) {
        const categoryName = exposure.categoryNameSnapshot?.trim() || null;
        categoryBySupplierProductId.set(exposure.productId, {
          categoryId: resolveConnectedCategoryId(null, categoryName),
          categoryName,
        });
      }
      for (const product of ready) {
        if (!categoryBySupplierProductId.has(product.supplierProductId)) {
          categoryBySupplierProductId.set(product.supplierProductId, {
            categoryId: product.categoryId,
            categoryName: product.categoryName,
          });
        }
      }
      return { ready, categoryBySupplierProductId };
    },
  });

  const supplierProductIdsKey = useMemo(() => {
    const ids = (linkedProductsQuery.data?.ready ?? []).map((p) => p.supplierProductId);
    return [...new Set(ids)].sort().join(",");
  }, [linkedProductsQuery.data]);

  const orderStockQuery = useQuery({
    queryKey: ["connected-suppliers", "order-stock", relationshipId, supplierProductIdsKey],
    enabled:
      Boolean(workspace) &&
      online &&
      allowManage &&
      connected &&
      Boolean(relationshipId) &&
      Boolean(linkedProductsQuery.data?.ready) &&
      (linkedProductsQuery.data?.ready.length ?? 0) > 0,
    queryFn: async ({ signal }) => {
      const ids = [
        ...new Set((linkedProductsQuery.data?.ready ?? []).map((p) => p.supplierProductId)),
      ];
      return getConnectedOrderStock(workspace!, relationshipId!, ids, signal);
    },
  });

  const readinessQuery = useQuery({
    queryKey: ["connected-suppliers", "readiness", relationshipId],
    enabled: Boolean(workspace) && online && allowManage && connected && Boolean(relationshipId),
    queryFn: ({ signal }) => classifyCatalogReadiness(workspace!, relationshipId!, signal),
  });

  const creditPolicyQuery = useQuery({
    queryKey: ["connected-suppliers", "credit-policy", relationshipId],
    enabled: Boolean(workspace) && online && allowManage && connected && Boolean(relationshipId),
    queryFn: ({ signal }) => getBusinessCustomerCreditPolicy(workspace!, relationshipId!, signal),
  });

  const productsQuery = useQuery({
    queryKey: ["catalog-products", "po-create", workspace?.organizationId, debounced],
    enabled:
      Boolean(workspace) &&
      online &&
      allowManage &&
      !connected &&
      Boolean(supplierId) &&
      debounced.length > 0,
    queryFn: ({ signal }) =>
      listCatalogProducts(
        workspace!,
        { search: debounced, status: "Active", pageSize: 20 },
        signal,
      ),
  });

  const readyProducts = useMemo(() => {
    const base = linkedProductsQuery.data?.ready ?? [];
    if (!orderStockQuery.data) {
      return base;
    }
    const map = new Map(
      orderStockQuery.data.items.map(
        (item) =>
          [
            item.supplierProductId,
            {
              isTracked: item.isTracked,
              availableBaseQuantity: item.availableBaseQuantity,
            },
          ] as const,
      ),
    );
    return mergeConnectedStock(base, map);
  }, [linkedProductsQuery.data, orderStockQuery.data]);
  const readinessCounts = useMemo(() => countByUserState(readinessQuery.data), [readinessQuery.data]);
  const categoryBySupplierProductId =
    linkedProductsQuery.data?.categoryBySupplierProductId ??
    new Map<string, { categoryId: string | null; categoryName: string | null }>();

  const categorySourceProducts = useMemo(() => {
    if (readinessFilter === "linked") {
      return readyProducts.map((product) => ({
        categoryId: product.categoryId,
        categoryName: product.categoryName,
      }));
    }
    const items = readinessQuery.data?.items ?? [];
    const filtered = filterReadinessItems(items, readinessFilter, "");
    return filtered.map((item) => {
      const meta = categoryBySupplierProductId.get(item.supplierProductId);
      return {
        categoryId: meta?.categoryId ?? null,
        categoryName: meta?.categoryName ?? null,
      };
    });
  }, [
    categoryBySupplierProductId,
    readinessFilter,
    readinessQuery.data,
    readyProducts,
  ]);

  const categoryOptions = useMemo(
    () =>
      buildConnectedCategoryOptions(categorySourceProducts, {
        noCategory: t("catalog.noCategory"),
      }),
    [categorySourceProducts, t],
  );

  const filteredConnected = useMemo(
    () => filterConnectedReadyProducts(readyProducts, debounced, categoryFilter),
    [readyProducts, debounced, categoryFilter],
  );
  /** Already on the PO — hide from Find products until removed. */
  const findConnectedProducts = useMemo(() => {
    const selected = new Set(connectedLines.map((line) => line.productId));
    return filteredConnected.filter((product) => !selected.has(product.buyerProductId));
  }, [connectedLines, filteredConnected]);
  const hasLinkedProductFilters =
    debounced.length > 0 || isConnectedCategoryFilterActive(categoryFilter);
  const setupItems = useMemo(() => {
    if (!readinessQuery.data || readinessFilter === "linked") {
      return [];
    }
    const bySearch = filterReadinessItems(
      readinessQuery.data.items,
      readinessFilter,
      debounced,
    );
    return filterItemsByConnectedCategory(bySearch, categoryFilter, (item) => {
      const meta = categoryBySupplierProductId.get(item.supplierProductId);
      return {
        categoryId: meta?.categoryId ?? null,
        categoryName: meta?.categoryName ?? null,
      };
    });
  }, [
    categoryBySupplierProductId,
    categoryFilter,
    debounced,
    readinessFilter,
    readinessQuery.data,
  ]);

  const selectableSetupItems = useMemo(
    () => setupItems.filter(isBulkConnectSelectable),
    [setupItems],
  );
  const allSetupSelectableSelected =
    selectableSetupItems.length > 0
    && selectableSetupItems.every((item) => setupSelected.has(item.exposureId));
  const setupBulkPartition = useMemo(
    () => partitionBulkConnectSelection(setupItems, setupSelected),
    [setupItems, setupSelected],
  );

  const qtyByProductId = useMemo(() => {
    const map = new Map<string, number>();
    for (const line of connectedLines) {
      map.set(line.productId, line.orderedQty);
    }
    return map;
  }, [connectedLines]);

  useEffect(() => {
    if (!connected || !linkedProductsQuery.isSuccess) {
      return;
    }
    setConnectedLines((prev) => retainCompatibleDraftLines(prev, readyProducts));
  }, [connected, linkedProductsQuery.isSuccess, readyProducts]);

  function onSupplierChange(nextSupplierId: string) {
    setSupplierId(nextSupplierId);
    setSearch("");
    setDebounced("");
    setCategoryFilter(emptyConnectedCategoryFilter());
    setReadinessFilter("linked");
    setSelectedProduct(null);
    setQtyText("1");
    setCostText("");
    setError(null);
    setConnectedLines([]);
    setExternalLines([]);
    setSetupSelected(new Set());
    setFinderOpen(false);
    setPaymentTerm("");
  }

  function openFinder() {
    setFinderOpen(true);
  }

  function closeFinder() {
    setFinderOpen(false);
  }

  const sharedCatalogHref = `/suppliers/${supplierId}/connected-catalog`;
  const sharedCatalogSetupHref = `${sharedCatalogHref}?setup=${readinessFilter}`;
  const showLinkedOrdering = readinessFilter === "linked";
  const connectedLoading =
    linkedProductsQuery.isLoading || readinessQuery.isLoading || orderStockQuery.isLoading;
  const setupActionBusy = setupBusyKey != null || setupBulkBusy;

  function toggleSetupSelected(exposureId: string) {
    setSetupSelected((current) => {
      const next = new Set(current);
      if (next.has(exposureId)) {
        next.delete(exposureId);
      } else {
        next.add(exposureId);
      }
      return next;
    });
  }

  function toggleSelectAllSetup() {
    setSetupSelected((current) => {
      const next = new Set(current);
      if (allSetupSelectableSelected) {
        for (const item of selectableSetupItems) {
          next.delete(item.exposureId);
        }
      } else {
        for (const item of selectableSetupItems) {
          next.add(item.exposureId);
        }
      }
      return next;
    });
  }

  async function refreshAfterSetupConnect() {
    await queryClient.invalidateQueries({ queryKey: ["connected-suppliers"] });
  }

  async function doSetupLink(exposureId: string, buyerProductId: string) {
    if (!workspace || !relationshipId || !allowManage) {
      return;
    }
    setSetupBusyKey(exposureId);
    setError(null);
    try {
      await linkProduct(workspace, relationshipId, { exposureId, buyerProductId });
      setSetupSelected((current) => {
        const next = new Set(current);
        next.delete(exposureId);
        return next;
      });
      await refreshAfterSetupConnect();
    } catch (err) {
      setError(
        err instanceof PosApiError
          ? (err.problem.detail ?? err.message)
          : t("connected.linkFailed"),
      );
    } finally {
      setSetupBusyKey(null);
    }
  }

  async function doSetupCreateAndLink(item: CatalogProductReadinessItem) {
    if (!workspace || !relationshipId || !allowCreate) {
      return;
    }
    setSetupBusyKey(`create-${item.exposureId}`);
    setError(null);
    try {
      await createBuyerProductAndLink(workspace, relationshipId, {
        exposureId: item.exposureId,
        name: item.supplierName,
        unitOfMeasure: item.unitOfMeasureCode,
        sellingPrice: Number.isFinite(item.poPrice) && item.poPrice > 0 ? item.poPrice : 0,
        businessUsage: "Resale",
      });
      setSetupSelected((current) => {
        const next = new Set(current);
        next.delete(item.exposureId);
        return next;
      });
      await refreshAfterSetupConnect();
    } catch (err) {
      setError(
        err instanceof PosApiError
          ? (err.problem.detail ?? err.message)
          : t("connected.createAndLinkFailed"),
      );
    } finally {
      setSetupBusyKey(null);
    }
  }

  async function runSetupBulkConfirmMatches() {
    if (!workspace || !relationshipId || !allowManage || setupBulkBusy) {
      return;
    }
    const targets = setupBulkPartition.confirmMatch;
    if (targets.length === 0) {
      return;
    }
    setSetupBulkBusy(true);
    setError(null);
    let ok = 0;
    let failed = 0;
    for (const item of targets) {
      const buyerProductId = item.candidateBuyerProductId;
      if (!buyerProductId) {
        failed += 1;
        continue;
      }
      try {
        await linkProduct(workspace, relationshipId, {
          exposureId: item.exposureId,
          buyerProductId,
        });
        ok += 1;
      } catch {
        failed += 1;
      }
    }
    setSetupSelected(new Set());
    setError(
      failed > 0
        ? t("connected.bulkConfirmResult")
            .replace("{ok}", String(ok))
            .replace("{failed}", String(failed))
        : null,
    );
    await refreshAfterSetupConnect();
    setSetupBulkBusy(false);
  }

  async function runSetupBulkAddAsNew() {
    if (!workspace || !relationshipId || !allowCreate || setupBulkBusy) {
      return;
    }
    const targets = setupBulkPartition.addAsNew;
    if (targets.length === 0) {
      return;
    }
    setSetupBulkBusy(true);
    setError(null);
    let ok = 0;
    let failed = 0;
    for (const item of targets) {
      try {
        await createBuyerProductAndLink(workspace, relationshipId, {
          exposureId: item.exposureId,
          name: item.supplierName,
          unitOfMeasure: item.unitOfMeasureCode,
          sellingPrice: Number.isFinite(item.poPrice) && item.poPrice > 0 ? item.poPrice : 0,
          businessUsage: "Resale",
        });
        ok += 1;
      } catch {
        failed += 1;
      }
    }
    setSetupSelected(new Set());
    setError(
      failed > 0
        ? t("connected.bulkAddAsNewResult")
            .replace("{ok}", String(ok))
            .replace("{failed}", String(failed))
        : null,
    );
    await refreshAfterSetupConnect();
    setSetupBulkBusy(false);
  }

  function setConnectedQty(product: ConnectedPoReadyProduct, nextQty: number) {
    const current = qtyByProductId.get(product.buyerProductId) ?? 0;
    const delta = nextQty - current;
    if (delta === 0) {
      return;
    }
    setConnectedLines((prev) => applyConnectedQuantityDelta(prev, product, delta));
    setError(null);
  }

  function addExternalLine() {
    if (!selectedProduct) {
      setError(t("purchasing.selectProduct"));
      return;
    }
    const qty = Number(qtyText);
    const cost = Number(costText);
    if (!Number.isFinite(qty) || qty <= 0 || !Number.isFinite(cost) || cost < 0) {
      setError(t("purchasing.invalidLine"));
      return;
    }
    setExternalLines((prev) => {
      const existing = prev.find((l) => l.productId === selectedProduct.productId);
      if (existing) {
        return prev.map((l) =>
          l.productId === selectedProduct.productId
            ? { ...l, orderedQty: qty, unitPurchaseCost: cost }
            : l,
        );
      }
      return [
        ...prev,
        {
          productId: selectedProduct.productId,
          name: selectedProduct.name,
          uom: selectedProduct.unitOfMeasure,
          orderedQty: qty,
          unitPurchaseCost: cost,
        },
      ];
    });
    setSelectedProduct(null);
    setQtyText("1");
    setCostText("");
    setSearch("");
    setError(null);
  }

  const activeLines = connected ? connectedLines : externalLines;
  const subtotal = orderSubtotal(activeLines);
  const utangEligibility = useMemo(
    () =>
      resolvePoUtangEligibility({
        connected,
        creditStatus: creditPolicyQuery.data?.status,
        availableCredit: creditPolicyQuery.data?.availableCredit,
        orderTotal: subtotal,
        canManagePurchasing: allowManage,
      }),
    [
      connected,
      creditPolicyQuery.data?.status,
      creditPolicyQuery.data?.availableCredit,
      subtotal,
      allowManage,
    ],
  );
  const selectedPaymentHelp = CONNECTED_PO_PAYMENT_OPTIONS.find((o) => o.code === paymentTerm)?.helpKey;
  const unitCount = orderUnitCount(activeLines);

  async function submit() {
    if (!workspace || !allowManage || !online || saving || statusLocked) {
      return;
    }
    if (!supplierId) {
      setError(t("purchasing.supplierRequired"));
      return;
    }
    if (activeLines.length === 0) {
      setError(t("purchasing.linesRequired"));
      return;
    }
    if (connected) {
      if (!paymentTerm) {
        setError(t("purchasing.poPaymentMethodRequired"));
        return;
      }
      if (paymentTerm === "Utang" && !utangEligibility.eligible) {
        setError(t(utangEligibility.reasonKey));
        return;
      }
    }
    setSaving(true);
    setError(null);
    try {
      if (!purchaseOrderIdRef.current) {
        const generated = createSecureMutationId();
        if (!generated.ok) {
          setError(t("purchasing.saveFailed"));
          return;
        }
        purchaseOrderIdRef.current = generated.id;
      }
      const purchaseOrderId = purchaseOrderIdRef.current;
      const po = await createPurchaseOrder(workspace, {
        purchaseOrderId,
        supplierId,
        orderDate,
        notes: notes.trim() || null,
        intendedReceivingBranchId: workspace.branchId ?? null,
        paymentTerm: connected ? paymentTerm || null : null,
        lines: activeLines.map((l) => ({
          productId: l.productId,
          orderedQty: l.orderedQty,
          unitPurchaseCost: l.unitPurchaseCost,
          purchaseUnitId:
            "purchaseUnitId" in l ? ((l as ConnectedPoDraftLine).purchaseUnitId ?? null) : null,
        })),
      });
      purchaseOrderIdRef.current = null;
      navigate(`/purchasing/${po.purchaseOrderId}`, { replace: true });
    } catch (err) {
      const purchaseOrderId = purchaseOrderIdRef.current;
      if (purchaseOrderId && workspace) {
        setError(t("checkout.confirmingTransaction"));
        const outcome = await resolveAmbiguousMutationOutcome({
          error: err,
          lookup: () => getPurchaseOrder(workspace, purchaseOrderId),
        });
        if (outcome.kind === "confirmed") {
          purchaseOrderIdRef.current = null;
          navigate(`/purchasing/${outcome.value.purchaseOrderId}`, { replace: true });
          return;
        }
        if (outcome.kind === "still_unknown") {
          setStatusLocked(true);
          setError(t("checkout.transactionStatusUnknown"));
          return;
        }
        if (outcome.kind === "not_found") {
          setError(describePosApiError(outcome.lookupError, t, "error.detail"));
          return;
        }
      }
      if (
        err instanceof PosApiError &&
        (err.errorCode === "pos.connected_supplier.out_of_stock" ||
          err.errorCode === "pos.connected_supplier.insufficient_stock")
      ) {
        void orderStockQuery.refetch();
        setError(err.problem.detail ?? t("purchasing.stockChanged"));
        return;
      }
      const detail =
        err instanceof PosApiError
          ? (err.problem.detail ?? t("purchasing.saveFailed"))
          : t("purchasing.saveFailed");
      setError(detail);
    } finally {
      setSaving(false);
    }
  }

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="purchase-order-create-page">
      <PageHeader
        title={t("purchasing.createTitle")}
        backTo="/purchasing/orders"
        backLabel={t("purchasing.backOrders")}
        backTestId="page-header-back-purchasing"
      />

      {allowManage ? (
        <Notice tone="info" testId="po-create-notice">
          {t("purchasing.ordersNoStock")}
        </Notice>
      ) : null}

      {!online ? (
        <Notice tone="warning">{t("purchasing.offline")}</Notice>
      ) : null}
      {!allowManage ? (
        <Notice tone="danger">{t("purchasing.manageDenied")}</Notice>
      ) : null}

      <PoDocumentSummary
        className="po-document-summary--create"
        counterpartyLabel={t("purchasing.seller")}
        counterpartyIcon={<Store className="size-5" strokeWidth={1.75} />}
        counterpartyName={
          selectedSupplier
            ? selectedSupplier.supplierBranchName
              ? `${selectedSupplier.name} — ${selectedSupplier.supplierBranchName}`
              : selectedSupplier.name
            : t("purchasing.selectSupplier")
        }
        fields={[
          {
            key: "receiving",
            label: t("purchasing.receivingBranch"),
            value: (
              <span data-testid="po-branch">
                <StatusChip tone="neutral">
                  {boundWorkspace?.branchName ?? boundWorkspace?.branchId ?? "—"}
                </StatusChip>
              </span>
            ),
          },
          {
            key: "supplier",
            label: t("purchasing.supplier"),
            value: (
              <select
                className="exits-select w-full"
                value={supplierId}
                onChange={(e) => onSupplierChange(e.target.value)}
                disabled={!allowManage || !online}
                data-testid="po-supplier"
                aria-label={t("purchasing.supplier")}
              >
                <option value="">{t("purchasing.selectSupplier")}</option>
                {(suppliersQuery.data?.items ?? []).map((s) => (
                  <option key={s.supplierId} value={s.supplierId}>
                    {s.supplierBranchName ? `${s.name} — ${s.supplierBranchName}` : s.name}
                  </option>
                ))}
              </select>
            ),
          },
          {
            key: "orderDate",
            label: t("purchasing.orderDate"),
            value: (
              <input
                type="date"
                className="exits-input w-full"
                value={orderDate}
                onChange={(e) => setOrderDate(e.target.value)}
                disabled={!allowManage || !online}
                data-testid="po-order-date"
                aria-label={t("purchasing.orderDate")}
              />
            ),
          },
        ]}
        testId="po-create-summary"
        footer={
          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            {t("purchasing.notes")}
            <textarea
              className="min-h-16 rounded-md border border-border bg-background px-3 py-2"
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              disabled={!allowManage || !online}
            />
          </label>
        }
      />

      {supplierId ? (
        <div className="product-selection-workspace receive-stock-workspace po-create-workspace flex flex-col gap-3" data-testid="po-order-cart">
          <SelectedItemsPanel
            title={t("purchasing.orderItems")}
            count={activeLines.length}
            headingId="po-order-items-heading"
            addLabel={t("purchasing.addProducts")}
            onAddClick={openFinder}
            finderOpen={finderOpen}
            finderPanelId={finderPanelId}
            emptyTitle={t("purchasing.orderItemsEmpty")}
            emptyDetail={t("purchasing.orderItemsEmptyHelp")}
            emptyTestId="po-connected-selected-items-empty"
            addTestId="po-add-products-trigger"
            testId="po-connected-selected-items"
            summary={
              <div className="receive-stock-receipt__summary" data-testid="po-order-summary">
                <div className="receive-stock-receipt__summary-row">
                  <span className="text-[length:var(--exits-text-sm)] text-muted">
                    {t("purchasing.items")}
                  </span>
                  <span className="text-[length:var(--exits-text-sm)] tabular-nums">
                    {activeLines.length}
                  </span>
                </div>
                <div className="receive-stock-receipt__summary-row">
                  <span className="text-[length:var(--exits-text-sm)] text-muted">
                    {t("purchasing.orderTotal")}
                  </span>
                  <span className="text-[length:var(--exits-text-md)] font-semibold tabular-nums">
                    <MoneyDisplay amount={subtotal} testId="po-subtotal" />
                  </span>
                </div>
                <p className="m-0 text-end text-[length:var(--exits-text-sm)] text-muted">
                  {t("purchasing.draftSummary")
                    .replace("{products}", String(activeLines.length))
                    .replace("{units}", String(unitCount))}
                </p>
              </div>
            }
          >
            {connected ? (
              <PurchaseOrderItemsView
                layout={orderItemsLayout}
                t={t}
                lines={connectedLines.map((line) => {
                  const product = readyProducts.find((p) => p.buyerProductId === line.productId);
                  const unitOfMeasure =
                    line.uom ||
                    (product
                      ? formatUnitOfMeasureLabel(
                          product.packageLabel || product.unitOfMeasure || "",
                        )
                      : "");
                  const availabilityLabel = product
                    ? formatSupplierAvailabilityLabel(product, t)
                    : null;
                  const overOrderWarning =
                    product && requestedExceedsSupplierAvailability(product, line.orderedQty)
                      ? t("purchasing.supplierOverOrderWarning")
                      : null;
                  return {
                    productId: line.productId,
                    name: line.name,
                    sku: product?.supplierSku,
                    orderedQty: line.orderedQty,
                    unitPurchaseCost: line.unitPurchaseCost,
                    availabilityLabel,
                    overOrderWarning,
                    quantityControl: product ? (
                      <QuantityStepper
                        compact
                        value={line.orderedQty}
                        onChange={(next) => setConnectedQty(product, next)}
                        unitOfMeasure={product.unitOfMeasure || line.uom}
                        sellingMode="PerItem"
                        unit={unitOfMeasure || undefined}
                        disabled={!allowManage || !online || saving}
                        decreaseLabel={t("purchasing.decreaseQty")}
                        increaseLabel={t("purchasing.increaseQty")}
                        ariaLabel={t("purchasing.qtyShort")}
                        valueTestId={`po-qty-${line.productId}`}
                        className="po-order-qty-stepper"
                      />
                    ) : (
                      <span className="tabular-nums">{line.orderedQty}</span>
                    ),
                    onRemove: product
                      ? () => setConnectedQty(product, 0)
                      : () =>
                          setConnectedLines((prev) =>
                            prev.filter((l) => l.productId !== line.productId),
                          ),
                  };
                })}
              />
            ) : (
              <PurchaseOrderItemsView
                layout={orderItemsLayout}
                t={t}
                lineTestIdPrefix="po-external-selected"
                lines={externalLines.map((line) => ({
                  productId: line.productId,
                  name: line.name,
                  orderedQty: line.orderedQty,
                  unitPurchaseCost: line.unitPurchaseCost,
                  quantityControl: (
                    <span className="tabular-nums">{line.orderedQty}</span>
                  ),
                  onRemove: () =>
                    setExternalLines((prev) =>
                      prev.filter((l) => l.productId !== line.productId),
                    ),
                }))}
              />
            )}
          </SelectedItemsPanel>
          {finderOpen ? (
            <ProductFinderPanel
              title={t("purchasing.supplierProducts")}
              titleIcon={<PackageSearch className="size-5" strokeWidth={1.75} />}
              headingId="po-find-products-heading"
              panelId={finderPanelId}
              closeLabel={t("purchasing.closeFindProducts")}
              onClose={closeFinder}
              closeTestId="po-close-finder"
              testId="po-add-products"
            >
              {connected ? (
                <div
                  className="flex flex-col gap-3"
                  aria-label={t("purchasing.orderProducts")}
                >
<p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {showLinkedOrdering
              ? t("purchasing.connectedOrderingHelp")
              : t("purchasing.setupTabHelp")}
          </p>
          <div className="po-setup-filters">
            <div className="po-setup-filter-row po-setup-filter-row--primary">
              {readinessQuery.isSuccess || linkedProductsQuery.isSuccess ? (
                <UnderlineTabBar
                  className="po-setup-filter-tabs"
                  ariaLabel={t("connected.readinessFilters")}
                  testId="po-readiness-filters"
                  activeKey={readinessFilter}
                  onChange={(key) => setReadinessFilter(key as PoCatalogSetupFilter)}
                  items={(
                    [
                      ["newProduct", readinessCounts.newProduct, "connected.filterNewProducts"],
                      ["checkMatch", readinessCounts.checkMatch, "connected.filterCheckMatch"],
                      ["attention", readinessCounts.attention, "connected.filterAttention"],
                      ["linked", readinessCounts.linked, "connected.filterLinked"],
                    ] as const
                  ).map(([value, count, key]) => ({
                    key: value,
                    label: t(key).replace("{count}", String(count)),
                    testId: `po-ready-${value}`,
                  }))}
                />
              ) : null}
              <Button
                asChild
                variant="outline"
                className="po-open-shared-catalog-btn shrink-0"
                data-testid="po-open-shared-catalog-setup-bar"
              >
                <Link to={showLinkedOrdering ? sharedCatalogHref : sharedCatalogSetupHref}>
                  <BookOpen className="size-4 shrink-0" aria-hidden />
                  {t("purchasing.openSharedCatalog")}
                </Link>
              </Button>
            </div>
            <div className="po-setup-filter-row po-setup-filter-row--search">
              {categoryOptions.length > 0 ? (
                <ProductCategoryMultiSelect
                  showSelectAll={false}
                  selectedIds={connectedCategoryFilterToValues(categoryFilter)}
                  categories={categoryOptions.map((option) => ({
                    categoryId: option.value,
                    name: option.label,
                    count: option.count ?? undefined,
                  }))}
                  onChange={(next) => setCategoryFilter(connectedCategoryFilterFromValues(next))}
                  placeholder={t("purchasing.categories")}
                  menuLabel={t("purchasing.categoryFilter")}
                  searchPlaceholder={t("catalog.searchCategories")}
                  clearAllLabel={t("purchasing.deselectAllCategories")}
                  selectedCountLabel={(count) =>
                    t("purchasing.categoriesTriggerCount").replace("{count}", String(count))
                  }
                  aria-label={t("purchasing.categoryFilter")}
                  testId="po-category-multiselect"
                  className="po-category-multiselect"
                />
              ) : null}
              <SearchField
                label={t("purchasing.productSearch")}
                value={search}
                onChange={(event) => setSearch(event.target.value)}
                onClear={() => setSearch("")}
                placeholder={t("purchasing.productSearch")}
                data-testid="po-product-search"
                containerClassName="po-setup-filter-search"
              />
            </div>
          </div>
          {connectedLoading ? <LoadingState label={t("loading.label")} /> : null}

          {showLinkedOrdering ? (
            <>
              {linkedProductsQuery.isSuccess && readyProducts.length === 0 ? (
                <EmptyState
              align="center"
              icon={<ClipboardList className="size-5" strokeWidth={1.75} />}
                  title={t("purchasing.noReadyProducts")}
                  detail={t("purchasing.noReadyProductsHelp")}
                  action={
                    <Button asChild data-testid="po-open-shared-catalog">
                      <Link to={sharedCatalogHref}>{t("purchasing.openSharedCatalog")}</Link>
                    </Button>
                  }
                />
              ) : null}
                      {linkedProductsQuery.isSuccess &&
                      readyProducts.length > 0 &&
                      filteredConnected.length === 0 &&
                      hasLinkedProductFilters ? (

                <EmptyState
                  align="center"
                  icon={<ClipboardList className="size-5" strokeWidth={1.75} />}
                  title={t("purchasing.noMatchingProducts")}
                  detail={t("purchasing.noMatchingProductsDetail")}
                  testId="po-connected-filter-empty"
                  action={
                    isConnectedCategoryFilterActive(categoryFilter) ? (
                      <Button
                        type="button"
                        variant="outline"
                        data-testid="po-clear-categories"
                        onClick={() => setCategoryFilter(emptyConnectedCategoryFilter())}
                      >
                        {t("purchasing.clearCategories")}
                      </Button>
                    ) : undefined
                  }
                />
              ) : null}
                        <PurchaseOrderLinkedProductsFinder
                          layout={linkedProductsLayout}
                          products={findConnectedProducts}
                          allowManage={allowManage}
                          online={online}
                          saving={saving}
                          onAddProduct={(product) => setConnectedQty(product, 1)}
                          t={t}
                        />
            </>
          ) : (
            <>
              {readinessQuery.isSuccess && setupItems.length === 0 ? (
                <EmptyState
              icon={<ClipboardList className="size-5" strokeWidth={1.75} />}
                  align="center"
                  title={t("purchasing.noSetupProducts")}
                  detail={t("purchasing.noSetupProductsHelp")}
                  action={
                    <Button
                      asChild
                      variant="outline"
                      className="po-open-shared-catalog-btn"
                      data-testid="po-open-shared-catalog-setup"
                    >
                      <Link to={sharedCatalogSetupHref}>
                        <BookOpen className="size-4 shrink-0" aria-hidden />
                        {t("purchasing.openSharedCatalog")}
                      </Link>
                    </Button>
                  }
                />
              ) : null}
              {selectableSetupItems.length > 0 ? (
                <div className="flex flex-wrap items-center gap-2">
                  <label
                    className="po-setup-select-all-mobile inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]"
                    data-testid="po-setup-select-bar"
                  >
                    <input
                      type="checkbox"
                      className="size-4"
                      checked={allSetupSelectableSelected}
                      disabled={setupBulkBusy}
                      data-testid="po-setup-select-all-mobile"
                      onChange={toggleSelectAllSetup}
                    />
                    {allSetupSelectableSelected
                      ? t("connected.deselectAllPage")
                      : t("connected.selectAllPage").replace(
                          "{count}",
                          String(selectableSetupItems.length),
                        )}
                  </label>
                </div>
              ) : null}
              <div
                className={
                  readinessFilter === "checkMatch" || readinessFilter === "attention"
                    ? "po-order-table po-order-table--setup po-order-table--setup-pair"
                    : "po-order-table po-order-table--setup"
                }
                data-testid="po-setup-product-list"
              >
                <div className="po-order-table__head">
                  <span className="po-order-table__check-head">
                    {selectableSetupItems.length > 0 ? (
                      <input
                        type="checkbox"
                        className="size-4"
                        checked={allSetupSelectableSelected}
                        disabled={setupBulkBusy}
                        aria-label={
                          allSetupSelectableSelected
                            ? t("connected.deselectAllPage")
                            : t("connected.selectAllPage").replace(
                                "{count}",
                                String(selectableSetupItems.length),
                              )
                        }
                        data-testid="po-setup-select-all"
                        onChange={toggleSelectAllSetup}
                      />
                    ) : null}
                  </span>
                  <span>{t("purchasing.colProduct")}</span>
                  <span>{t("purchasing.colSku")}</span>
                  <span>{t("purchasing.colUnit")}</span>
                  <span className="po-order-table__price-head">{t("purchasing.supplierPrice")}</span>
                  <span className="po-order-table__action-head">{t("purchasing.colAction")}</span>
                </div>
                <ul className="po-order-table__list">
                  {setupItems.map((item) => {
                    const state = mapBackendStatusToUserState(item.status);
                    const canSelect = isBulkConnectSelectable(item);
                    const isSelected = setupSelected.has(item.exposureId);
                    return (
                      <li key={item.exposureId}>
                        <div
                          className="po-order-table__row"
                          data-testid={`po-setup-product-${item.exposureId}`}
                        >
                          <span className="po-order-table__check">
                            {canSelect ? (
                              <input
                                type="checkbox"
                                className="size-4"
                                checked={isSelected}
                                disabled={setupBulkBusy}
                                aria-label={item.supplierName}
                                data-testid={`po-setup-select-${item.exposureId}`}
                                onChange={() => toggleSetupSelected(item.exposureId)}
                              />
                            ) : null}
                          </span>
                          <span className="po-order-table__product">
                            <span className="po-order-table__name">{item.supplierName}</span>
                          </span>
                          <span className="po-order-table__sku">
                            {item.supplierSku ?? t("connected.noSku")}
                          </span>
                          <span className="po-order-table__unit">
                            {formatUnitOfMeasureLabel(item.unitOfMeasureCode)}
                          </span>
                          <span className="po-order-table__price tabular-nums">
                            {formatPeso(item.poPrice)}
                          </span>
                          <span
                            className={
                              state === "checkMatch"
                                ? "po-order-table__action po-order-table__action--pair"
                                : "po-order-table__action po-order-table__action--stack"
                            }
                          >
                            {state === "newProduct" && allowCreate ? (
                              <Button
                                type="button"
                                variant="outline"
                                className="po-setup-connect-btn po-setup-connect-btn--add"
                                data-testid={`po-create-link-${item.exposureId}`}
                                disabled={setupActionBusy}
                                onClick={() => void doSetupCreateAndLink(item)}
                              >
                                <Plus className="size-3.5 shrink-0" aria-hidden />
                                {t("connected.createAndLink")}
                              </Button>
                            ) : null}
                            {state === "checkMatch" && allowManage && item.candidateBuyerProductId ? (
                              <Button
                                type="button"
                                variant="outline"
                                className="po-setup-connect-btn po-setup-connect-btn--confirm"
                                data-testid={`po-confirm-match-${item.exposureId}`}
                                disabled={setupActionBusy}
                                onClick={() =>
                                  void doSetupLink(item.exposureId, item.candidateBuyerProductId!)
                                }
                              >
                                <Check className="size-3.5 shrink-0" aria-hidden />
                                {t("connected.confirmMatch")}
                              </Button>
                            ) : null}
                            {state === "checkMatch" && allowCreate ? (
                              <Button
                                type="button"
                                variant="outline"
                                className="po-setup-connect-btn po-setup-connect-btn--new"
                                data-testid={`po-add-as-new-${item.exposureId}`}
                                disabled={setupActionBusy}
                                onClick={() => void doSetupCreateAndLink(item)}
                              >
                                <Plus className="size-3.5 shrink-0" aria-hidden />
                                {t("connected.addAsNew")}
                              </Button>
                            ) : null}
                            {state === "attention" ? (
                              <Button
                                asChild
                                variant="outline"
                                className="po-setup-connect-btn"
                                data-testid={`po-connect-${item.exposureId}`}
                              >
                                <Link to={sharedCatalogSetupHref}>
                                  {t("purchasing.connectInSharedCatalog")}
                                </Link>
                              </Button>
                            ) : null}
                          </span>
                        </div>
                      </li>
                    );
                  })}
                </ul>
              </div>
              {setupSelected.size > 0 ? (
                <div className="connected-share-bulk-bar" data-testid="po-setup-bulk-bar">
                  <span className="connected-share-bulk-bar__count">
                    {t("connected.bulkSelectedCount").replace(
                      "{count}",
                      String(setupSelected.size),
                    )}
                  </span>
                  {allowManage && setupBulkPartition.confirmMatch.length > 0 ? (
                    <Button
                      type="button"
                      variant="outline"
                      className="po-setup-connect-btn po-setup-connect-btn--confirm po-setup-bulk-btn"
                      disabled={setupBulkBusy}
                      data-testid="po-bulk-confirm-matches"
                      onClick={() => void runSetupBulkConfirmMatches()}
                    >
                      <Check className="size-3.5 shrink-0" aria-hidden />
                      {t("connected.bulkConfirmMatches").replace(
                        "{count}",
                        String(setupBulkPartition.confirmMatch.length),
                      )}
                    </Button>
                  ) : null}
                  {allowCreate && setupBulkPartition.addAsNew.length > 0 ? (
                    <Button
                      type="button"
                      variant="outline"
                      className="po-setup-connect-btn po-setup-connect-btn--new po-setup-bulk-btn"
                      disabled={setupBulkBusy}
                      data-testid="po-bulk-add-as-new"
                      onClick={() => void runSetupBulkAddAsNew()}
                    >
                      <Plus className="size-3.5 shrink-0" aria-hidden />
                      {t("connected.bulkAddAsNew").replace(
                        "{count}",
                        String(setupBulkPartition.addAsNew.length),
                      )}
                    </Button>
                  ) : null}
                  <Button
                    type="button"
                    variant="ghost"
                    disabled={setupBulkBusy}
                    data-testid="po-bulk-clear"
                    onClick={() => setSetupSelected(new Set())}
                  >
                    {t("connected.deselectAllPage")}
                  </Button>
                </div>
              ) : null}
            </>
          )}
                </div>
              ) : (
<section className="flex flex-col gap-2" aria-labelledby="po-products-heading">
          <h2 id="po-products-heading" className="m-0 text-[length:var(--exits-text-md)] font-medium">
            {t("purchasing.addProducts")}
          </h2>
          <SearchField
            label={t("purchasing.productSearch")}
            value={search}
            onChange={(event) => setSearch(event.target.value)}
            onClear={() => setSearch("")}
            placeholder={t("purchasing.productSearch")}
            data-testid="po-product-search"
          />
          {(productsQuery.data?.items ?? []).length === 0 && debounced ? (
            <EmptyState
              align="center"
              icon={<ClipboardList className="size-5" strokeWidth={1.75} />}
              title={t("purchasing.noProducts")}
              detail={t("purchasing.noProductsDetail")}
            />
          ) : null}
          <ul className="m-0 flex list-none flex-col gap-1 p-0">
            {(productsQuery.data?.items ?? []).map((p) => (
              <li key={p.productId}>
                <button
                  type="button"
                  className={` w-full rounded-md border px-3 text-left ${
                    selectedProduct?.productId === p.productId
                      ? "border-primary bg-muted"
                      : "border-border bg-background"
                  }`}
                  onClick={() => setSelectedProduct(p)}
                  data-testid={`po-product-${p.productId}`}
                >
                  {p.name}
                  {p.sku ? ` · ${p.sku}` : ""}
                  {p.barcode ? ` · ${p.barcode}` : ""}
                </button>
              </li>
            ))}
          </ul>
          {selectedProduct ? (
            <div className="flex flex-wrap items-end gap-3">
              <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
                {t("purchasing.qty")}
                <input
                  className="po-document-create-qty rounded-md border border-border bg-background px-3"
                  value={qtyText}
                  onChange={(e) => setQtyText(e.target.value)}
                  data-testid="po-line-qty"
                />
              </label>
              <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
                {t("purchasing.unitCost")}
                <input
                  className="po-document-create-cost rounded-md border border-border bg-background px-3"
                  value={costText}
                  onChange={(e) => setCostText(e.target.value)}
                  data-testid="po-line-cost"
                />
              </label>
              <Button type="button" onClick={addExternalLine} data-testid="po-add-line">
                {t("purchasing.addLine")}
              </Button>
            </div>
          ) : null}
        </section>
              )}
            </ProductFinderPanel>
          ) : null}

          {connected ? (
            <section
              className="po-payment-method-section flex flex-col gap-2 rounded-md border border-border p-3"
              data-testid="po-payment-method"
              aria-labelledby="po-payment-method-heading"
            >
              <h3 id="po-payment-method-heading" className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
                {t("purchasing.paymentMethod")}
              </h3>
              <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                {t("purchasing.paymentMethodIntendedHelp")}
              </p>
              <div
                className="flex flex-col gap-2"
                role="radiogroup"
                aria-label={t("purchasing.paymentMethod")}
              >
                {CONNECTED_PO_PAYMENT_OPTIONS.map((option) => {
                  const utangBlocked =
                    option.requiresUtangEligibility === true && !utangEligibility.eligible;
                  const disabled = !allowManage || utangBlocked;
                  return (
                    <label
                      key={option.code}
                      className={`flex cursor-pointer items-start gap-2 rounded-md border border-border px-3 py-2 ${
                        disabled ? "opacity-60" : ""
                      }`}
                    >
                      <input
                        type="radio"
                        name="po-payment-term"
                        value={option.code}
                        checked={paymentTerm === option.code}
                        disabled={disabled}
                        onChange={() => setPaymentTerm(option.code)}
                        data-testid={`po-payment-${option.code}`}
                      />
                      <span className="flex min-w-0 flex-col gap-0.5">
                        <span className="text-[length:var(--exits-text-sm)] font-medium">
                          {t(option.labelKey)}
                          {utangBlocked ? ` — ${t("purchasing.utang.unavailable")}` : null}
                        </span>
                        {utangBlocked ? (
                          <span className="text-[length:var(--exits-text-xs)] text-muted">
                            {t(utangEligibility.reasonKey)}
                          </span>
                        ) : null}
                      </span>
                    </label>
                  );
                })}
              </div>
              {selectedPaymentHelp ? (
                <Notice tone="info" testId="po-payment-help">
                  {t(selectedPaymentHelp)}
                </Notice>
              ) : null}
            </section>
          ) : null}

          <div className="receive-stock-actions product-selection-workspace__actions">
            <Button
              type="button"
              variant="destructive"
              onClick={() => navigate("/purchasing/orders")}
              data-testid="po-create-cancel"
            >
              {t("purchasing.cancel")}
            </Button>
            <Button
              type="button"
              disabled={
                !allowManage ||
                !online ||
                saving ||
                statusLocked ||
                activeLines.length === 0 ||
                (connected && !paymentTerm)
              }
              onClick={() => void submit()}
              data-testid="po-create-submit"
            >
              {saving ? t("purchasing.saving") : t("purchasing.createOrder")}
            </Button>
          </div>
        </div>
      ) : (
        <Button type="button" disabled data-testid="po-create-submit">
          {t("purchasing.createOrder")}
        </Button>
      )}

      {error ? (
        <Notice tone="danger" testId="po-create-error">
          {error}
        </Notice>
      ) : null}

    </div>
  );
}
