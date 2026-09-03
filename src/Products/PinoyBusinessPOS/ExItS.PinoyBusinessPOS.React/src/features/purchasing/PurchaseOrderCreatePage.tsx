import { useEffect, useMemo, useRef, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { canManagePurchasing } from "@/access/pos-capabilities";
import { describePosApiError } from "@/access/pos-commercial-errors";
import { listCatalogProducts } from "@/api/pos/pos-catalog-client";
import type { PosCatalogProductDto } from "@/api/pos/pos-catalog-types";
import {
  classifyCatalogReadiness,
  getConnectedOrderStock,
  listLinks,
  searchExposedCatalog,
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
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/exits/EmptyState";
import { LoadingState } from "@/components/exits/LoadingState";
import { MoneyDisplay, QuantityStepper } from "@/components/exits/MoneyQuantity";
import { PageHeader } from "@/components/exits/PageHeader";
import { SearchField } from "@/components/exits/SearchField";
import { UnderlineTabBar } from "@/components/exits/UnderlineTabBar";
import { useBrowserOnline } from "@/connectivity/browser-online";
import {
  applyConnectedQuantityDelta,
  buildConnectedReadyProducts,
  connectedLinesViolateStock,
  filterConnectedReadyProducts,
  formatLineMath,
  formatUnitPriceLabel,
  maxOrderablePurchaseQty,
  mergeConnectedStock,
  orderSubtotal,
  orderUnitCount,
  resolveSupplierAvailability,
  retainCompatibleDraftLines,
  type ConnectedPoDraftLine,
  type ConnectedPoReadyProduct,
} from "@/features/purchasing/purchase-order-create-connected";
import {
  countByUserState,
  filterReadinessItems,
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

function formatCompactPoPrice(amount: number): string {
  return `₱${amount.toLocaleString("en-PH", {
    minimumFractionDigits: amount % 1 === 0 ? 0 : 2,
    maximumFractionDigits: 2,
  })}`;
}

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
  const online = useBrowserOnline();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const allowManage = canManagePurchasing(sessionGrant);

  const [supplierId, setSupplierId] = useState("");
  const [orderDate, setOrderDate] = useState(todayIsoDate);
  const [notes, setNotes] = useState("");
  const [search, setSearch] = useState("");
  const [debounced, setDebounced] = useState("");
  const [readinessFilter, setReadinessFilter] = useState<PoCatalogSetupFilter>("linked");
  const [connectedLines, setConnectedLines] = useState<ConnectedPoDraftLine[]>([]);
  const [externalLines, setExternalLines] = useState<ExternalDraftLine[]>([]);
  const [qtyText, setQtyText] = useState("1");
  const [costText, setCostText] = useState("");
  const [selectedProduct, setSelectedProduct] = useState<PosCatalogProductDto | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [statusLocked, setStatusLocked] = useState(false);
  const purchaseOrderIdRef = useRef<string | null>(null);

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
      return buildConnectedReadyProducts(links, exposures.length > 0 ? exposures : null);
    },
  });

  const supplierProductIdsKey = useMemo(() => {
    const ids = (linkedProductsQuery.data ?? []).map((p) => p.supplierProductId);
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
      Boolean(linkedProductsQuery.data) &&
      (linkedProductsQuery.data?.length ?? 0) > 0,
    queryFn: async ({ signal }) => {
      const ids = [...new Set((linkedProductsQuery.data ?? []).map((p) => p.supplierProductId))];
      return getConnectedOrderStock(workspace!, relationshipId!, ids, signal);
    },
  });

  const readinessQuery = useQuery({
    queryKey: ["connected-suppliers", "readiness", relationshipId],
    enabled: Boolean(workspace) && online && allowManage && connected && Boolean(relationshipId),
    queryFn: ({ signal }) => classifyCatalogReadiness(workspace!, relationshipId!, signal),
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
    const base = linkedProductsQuery.data ?? [];
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
  const filteredConnected = useMemo(
    () => filterConnectedReadyProducts(readyProducts, debounced),
    [readyProducts, debounced],
  );
  const setupItems = useMemo(() => {
    if (!readinessQuery.data || readinessFilter === "linked") {
      return [];
    }
    return filterReadinessItems(readinessQuery.data.items, readinessFilter, debounced);
  }, [debounced, readinessFilter, readinessQuery.data]);

  const qtyByProductId = useMemo(() => {
    const map = new Map<string, number>();
    for (const line of connectedLines) {
      map.set(line.productId, line.orderedQty);
    }
    return map;
  }, [connectedLines]);

  const stockBlocksCreate = useMemo(
    () => connected && connectedLinesViolateStock(connectedLines, readyProducts),
    [connected, connectedLines, readyProducts],
  );

  useEffect(() => {
    if (!connected || !linkedProductsQuery.isSuccess) {
      return;
    }
    setConnectedLines((prev) => retainCompatibleDraftLines(prev, readyProducts));
  }, [connected, linkedProductsQuery.isSuccess, readyProducts]);

  useEffect(() => {
    if (!connected || !orderStockQuery.isSuccess) {
      return;
    }
    setConnectedLines((prev) => {
      let changed = false;
      const next = prev.map((line) => {
        const product = readyProducts.find((p) => p.buyerProductId === line.productId);
        if (!product) {
          return line;
        }
        const max = maxOrderablePurchaseQty(product);
        if (max == null) {
          return line;
        }
        if (max <= 0) {
          changed = true;
          return null;
        }
        if (line.orderedQty > max) {
          changed = true;
          return { ...line, orderedQty: max };
        }
        return line;
      });
      if (!changed) {
        return prev;
      }
      return next.filter((line): line is ConnectedPoDraftLine => line != null);
    });
  }, [connected, orderStockQuery.isSuccess, readyProducts]);

  function onSupplierChange(nextSupplierId: string) {
    setSupplierId(nextSupplierId);
    setSearch("");
    setDebounced("");
    setReadinessFilter("linked");
    setSelectedProduct(null);
    setQtyText("1");
    setCostText("");
    setError(null);
    setConnectedLines([]);
    setExternalLines([]);
  }

  const sharedCatalogHref = `/suppliers/${supplierId}/connected-catalog`;
  const sharedCatalogSetupHref = `${sharedCatalogHref}?setup=${readinessFilter}`;
  const showLinkedOrdering = readinessFilter === "linked";
  const connectedLoading =
    linkedProductsQuery.isLoading || readinessQuery.isLoading || orderStockQuery.isLoading;

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
    if (stockBlocksCreate) {
      setError(t("purchasing.stockBlocksCreate"));
      return;
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
        description={t("purchasing.createLede")}
        backTo="/purchasing/orders"
        backLabel={t("purchasing.backOrders")}
        backTestId="page-header-back-purchasing"
      />
      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
        {t("purchasing.ordersNoStock")}
      </p>
      {!online ? (
        <Card>
          <p className="m-0">{t("purchasing.offline")}</p>
        </Card>
      ) : null}
      {!allowManage ? (
        <Card>
          <p className="m-0">{t("purchasing.manageDenied")}</p>
        </Card>
      ) : null}

      <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
        {t("purchasing.receivingBranch")}
        <input
          className="min-h-11 rounded-md border border-border bg-muted px-3"
          value={boundWorkspace?.branchName ?? boundWorkspace?.branchId ?? ""}
          readOnly
          data-testid="po-branch"
        />
        <span className="text-muted">{t("purchasing.receivingBranchHelp")}</span>
      </label>

      <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
        {t("purchasing.supplier")}
        <select
          className="min-h-11 rounded-md border border-border bg-background px-3"
          value={supplierId}
          onChange={(e) => onSupplierChange(e.target.value)}
          disabled={!allowManage || !online}
          data-testid="po-supplier"
        >
          <option value="">{t("purchasing.selectSupplier")}</option>
          {(suppliersQuery.data?.items ?? []).map((s) => (
            <option key={s.supplierId} value={s.supplierId}>
              {s.supplierBranchName ? `${s.name} — ${s.supplierBranchName}` : s.name}
            </option>
          ))}
        </select>
      </label>

      <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
        {t("purchasing.orderDate")}
        <input
          type="date"
          className="min-h-11 rounded-md border border-border bg-background px-3"
          value={orderDate}
          onChange={(e) => setOrderDate(e.target.value)}
          disabled={!allowManage || !online}
          data-testid="po-order-date"
        />
      </label>

      <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
        {t("purchasing.notes")}
        <textarea
          className="min-h-20 rounded-md border border-border bg-background px-3 py-2"
          value={notes}
          onChange={(e) => setNotes(e.target.value)}
          disabled={!allowManage || !online}
        />
      </label>

      {supplierId && connected ? (
        <section className="flex flex-col gap-3" aria-labelledby="po-products-heading">
          <div className="flex flex-wrap items-end justify-between gap-2">
            <h2 id="po-products-heading" className="m-0 text-[length:var(--exits-text-md)] font-medium">
              {t("purchasing.orderProducts")}
            </h2>
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
              {showLinkedOrdering
                ? t("purchasing.connectedOrderingHelp")
                : t("purchasing.setupTabHelp")}
            </p>
          </div>
          <SearchField
            label={t("purchasing.productSearch")}
            value={search}
            onChange={(event) => setSearch(event.target.value)}
            onClear={() => setSearch("")}
            placeholder={t("purchasing.productSearch")}
            data-testid="po-product-search"
          />
          {readinessQuery.isSuccess || linkedProductsQuery.isSuccess ? (
            <UnderlineTabBar
              className="exits-chip-bar--scroll"
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
          {connectedLoading ? <LoadingState label={t("loading.label")} /> : null}

          {showLinkedOrdering ? (
            <>
              {linkedProductsQuery.isSuccess && readyProducts.length === 0 ? (
                <EmptyState
                  title={t("purchasing.noReadyProducts")}
                  detail={t("purchasing.noReadyProductsHelp")}
                  action={
                    <Button asChild className="min-h-11" data-testid="po-open-shared-catalog">
                      <Link to={sharedCatalogHref}>{t("purchasing.openSharedCatalog")}</Link>
                    </Button>
                  }
                />
              ) : null}
              {linkedProductsQuery.isSuccess &&
              readyProducts.length > 0 &&
              filteredConnected.length === 0 &&
              debounced ? (
                <EmptyState
                  title={t("purchasing.noProducts")}
                  detail={t("purchasing.noProductsDetail")}
                />
              ) : null}
              <ul className="m-0 grid list-none gap-2 p-0" data-testid="po-connected-product-list">
                {filteredConnected.map((product) => {
                  const qty = qtyByProductId.get(product.buyerProductId) ?? 0;
                  const availability = resolveSupplierAvailability(product);
                  const maxQty = maxOrderablePurchaseQty(product);
                  const atMax = maxQty != null && qty >= maxQty;
                  const cannotAdd =
                    availability.kind === "out_of_stock" || (maxQty != null && maxQty <= 0);
                  return (
                    <li key={product.buyerProductId}>
                      <Card
                        as="article"
                        className="grid gap-2 p-3"
                        data-testid={`po-connected-product-${product.buyerProductId}`}
                      >
                        <div className="flex min-w-0 items-start justify-between gap-3">
                          <div className="min-w-0">
                            <p className="m-0 font-semibold leading-snug">{product.productName}</p>
                            <p className="m-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
                              {t("purchasing.supplierSku")}:{" "}
                              {product.supplierSku ?? t("connected.noSku")}
                              {product.packageLabel ? ` · ${product.packageLabel}` : ""}
                            </p>
                            <p
                              className="m-0 mt-1 text-[length:var(--exits-text-sm)] text-muted"
                              data-testid={`po-stock-${product.buyerProductId}`}
                            >
                              {availability.kind === "out_of_stock"
                                ? t("purchasing.supplierOutOfStock")
                                : null}
                              {availability.kind === "available"
                                ? t("purchasing.supplierStockAvailable").replace(
                                    "{n}",
                                    String(availability.quantity),
                                  )
                                : null}
                              {availability.kind === "untracked"
                                ? t("purchasing.stockNotTracked")
                                : null}
                            </p>
                          </div>
                          <p className="m-0 shrink-0 text-[length:var(--exits-text-sm)] font-semibold tabular-nums">
                            {formatUnitPriceLabel(product.unitPurchaseCost, product.unitOfMeasure)}
                          </p>
                        </div>
                        {qty <= 0 ? (
                          <div className="flex justify-end">
                            <Button
                              type="button"
                              className="min-h-11"
                              data-testid={`po-add-${product.buyerProductId}`}
                              disabled={!allowManage || !online || saving || cannotAdd}
                              onClick={() => setConnectedQty(product, 1)}
                            >
                              {t("purchasing.addProduct")}
                            </Button>
                          </div>
                        ) : (
                          <div className="flex min-w-0 flex-wrap items-center justify-between gap-2">
                            <p
                              className="m-0 text-[length:var(--exits-text-sm)] font-medium tabular-nums"
                              data-testid={`po-line-math-${product.buyerProductId}`}
                            >
                              {formatLineMath(qty, product.unitPurchaseCost)}
                            </p>
                            <QuantityStepper
                              compact
                              value={qty}
                              valueTestId={`po-qty-${product.buyerProductId}`}
                              increaseLabel={t("purchasing.increaseQty")}
                              decreaseLabel={t("purchasing.decreaseQty")}
                              incrementDisabled={
                                !allowManage || !online || saving || atMax
                              }
                              onIncrement={() => setConnectedQty(product, qty + 1)}
                              onDecrement={() => setConnectedQty(product, qty - 1)}
                            />
                          </div>
                        )}
                      </Card>
                    </li>
                  );
                })}
              </ul>
            </>
          ) : (
            <>
              {readinessQuery.isSuccess && setupItems.length === 0 ? (
                <EmptyState
                  title={t("purchasing.noSetupProducts")}
                  detail={t("purchasing.noSetupProductsHelp")}
                  action={
                    <Button asChild className="min-h-11" data-testid="po-open-shared-catalog-setup">
                      <Link to={sharedCatalogSetupHref}>{t("purchasing.openSharedCatalog")}</Link>
                    </Button>
                  }
                />
              ) : null}
              <ul className="m-0 grid list-none gap-2 p-0" data-testid="po-setup-product-list">
                {setupItems.map((item) => (
                  <li key={item.exposureId}>
                    <Card
                      as="article"
                      className="grid gap-2 p-3"
                      data-testid={`po-setup-product-${item.exposureId}`}
                    >
                      <div className="flex min-w-0 items-start justify-between gap-3">
                        <div className="min-w-0">
                          <p className="m-0 font-semibold leading-snug">{item.supplierName}</p>
                          <p className="m-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
                            {t("purchasing.supplierSku")}: {item.supplierSku ?? t("connected.noSku")}
                            {" · "}
                            {item.unitOfMeasureCode}
                          </p>
                        </div>
                        <p className="m-0 shrink-0 text-[length:var(--exits-text-sm)] font-semibold tabular-nums">
                          {formatCompactPoPrice(item.poPrice)}
                        </p>
                      </div>
                      <Button asChild className="min-h-11 w-full" data-testid={`po-connect-${item.exposureId}`}>
                        <Link to={sharedCatalogSetupHref}>{t("purchasing.connectInSharedCatalog")}</Link>
                      </Button>
                    </Card>
                  </li>
                ))}
              </ul>
            </>
          )}
        </section>
      ) : null}

      {supplierId && !connected ? (
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
              title={t("purchasing.noProducts")}
              detail={t("purchasing.noProductsDetail")}
            />
          ) : null}
          <ul className="m-0 flex list-none flex-col gap-1 p-0">
            {(productsQuery.data?.items ?? []).map((p) => (
              <li key={p.productId}>
                <button
                  type="button"
                  className={`min-h-11 w-full rounded-md border px-3 text-left ${
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
            <div className="grid gap-2 sm:grid-cols-3">
              <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
                {t("purchasing.qty")}
                <input
                  className="min-h-11 rounded-md border border-border bg-background px-3"
                  value={qtyText}
                  onChange={(e) => setQtyText(e.target.value)}
                  data-testid="po-line-qty"
                />
              </label>
              <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
                {t("purchasing.unitCost")}
                <input
                  className="min-h-11 rounded-md border border-border bg-background px-3"
                  value={costText}
                  onChange={(e) => setCostText(e.target.value)}
                  data-testid="po-line-cost"
                />
              </label>
              <div className="flex items-end">
                <Button
                  type="button"
                  className="min-h-11 w-full"
                  onClick={addExternalLine}
                  data-testid="po-add-line"
                >
                  {t("purchasing.addLine")}
                </Button>
              </div>
            </div>
          ) : null}

          <section aria-labelledby="po-lines-heading">
            <h2
              id="po-lines-heading"
              className="m-0 mb-2 text-[length:var(--exits-text-md)] font-medium"
            >
              {t("purchasing.lines")}
            </h2>
            {externalLines.length === 0 ? (
              <p className="m-0 text-muted">{t("purchasing.linesEmpty")}</p>
            ) : (
              <ul className="m-0 flex list-none flex-col gap-2 p-0">
                {externalLines.map((line) => (
                  <li
                    key={line.productId}
                    className="rounded-md border border-border p-3"
                    data-testid={`po-draft-line-${line.productId}`}
                  >
                    <div className="font-medium">{line.name}</div>
                    <div className="text-[length:var(--exits-text-sm)] text-muted">
                      {line.orderedQty} {line.uom} · {formatPeso(line.unitPurchaseCost)}
                    </div>
                    <Button
                      type="button"
                      variant="ghost"
                      className="mt-2 min-h-11"
                      onClick={() =>
                        setExternalLines((prev) =>
                          prev.filter((l) => l.productId !== line.productId),
                        )
                      }
                    >
                      {t("purchasing.removeLine")}
                    </Button>
                  </li>
                ))}
              </ul>
            )}
          </section>
        </section>
      ) : null}

      {error ? (
        <Card data-testid="po-create-error">
          <p className="m-0 text-destructive">{error}</p>
        </Card>
      ) : null}

      {supplierId ? (
        <Card className="grid gap-3 p-3" data-testid="po-order-summary">
          <div className="flex flex-wrap items-center justify-between gap-2">
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
              {t("purchasing.draftSummary")
                .replace("{products}", String(activeLines.length))
                .replace("{units}", String(unitCount))}
            </p>
            <p className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
              {t("purchasing.subtotal")} <MoneyDisplay amount={subtotal} testId="po-subtotal" />
            </p>
          </div>
          <Button
            type="button"
            className="min-h-11 w-full"
            disabled={
              !allowManage ||
              !online ||
              saving ||
              statusLocked ||
              activeLines.length === 0 ||
              stockBlocksCreate
            }
            onClick={() => void submit()}
            data-testid="po-create-submit"
          >
            {saving ? t("purchasing.saving") : t("purchasing.createOrder")}
          </Button>
        </Card>
      ) : (
        <Button
          type="button"
          className="min-h-11"
          disabled
          data-testid="po-create-submit"
        >
          {t("purchasing.createOrder")}
        </Button>
      )}
    </div>
  );
}
