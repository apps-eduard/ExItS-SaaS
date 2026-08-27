import { useEffect, useMemo, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { canManagePurchasing } from "@/access/pos-capabilities";
import { describePosApiError } from "@/access/pos-commercial-errors";
import { listCatalogProducts } from "@/api/pos/pos-catalog-client";
import type { PosCatalogProductDto } from "@/api/pos/pos-catalog-types";
import {
  createPurchaseOrder,
  getPurchaseOrder,
} from "@/api/pos/pos-purchase-orders-client";
import { listSuppliers } from "@/api/pos/pos-suppliers-client";
import { PosApiError } from "@/api/pos/pos-http";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/exits/EmptyState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { SearchField } from "@/components/exits/SearchField";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { useI18n } from "@/i18n/I18nProvider";
import { createSecureMutationId } from "@/lib/secure-mutation-id";
import { resolveAmbiguousMutationOutcome } from "@/runtime/ambiguous-mutation-outcome";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

type DraftLine = {
  productId: string;
  name: string;
  uom: string;
  orderedQty: number;
  unitPurchaseCost: number;
};

function todayIsoDate(): string {
  const d = new Date();
  const yyyy = d.getFullYear();
  const mm = String(d.getMonth() + 1).padStart(2, "0");
  const dd = String(d.getDate()).padStart(2, "0");
  return `${yyyy}-${mm}-${dd}`;
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
  const [lines, setLines] = useState<DraftLine[]>([]);
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

  const productsQuery = useQuery({
    queryKey: ["catalog-products", "po-create", workspace?.organizationId, debounced],
    enabled: Boolean(workspace) && online && allowManage && debounced.length > 0,
    queryFn: ({ signal }) =>
      listCatalogProducts(
        workspace!,
        { search: debounced, status: "Active", pageSize: 20 },
        signal,
      ),
  });

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }

  function addLine() {
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
    setLines((prev) => {
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

  async function submit() {
    if (!workspace || !allowManage || !online || saving || statusLocked) {
      return;
    }
    if (!supplierId) {
      setError(t("purchasing.supplierRequired"));
      return;
    }
    if (lines.length === 0) {
      setError(t("purchasing.linesRequired"));
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
        lines: lines.map((l) => ({
          productId: l.productId,
          orderedQty: l.orderedQty,
          unitPurchaseCost: l.unitPurchaseCost,
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
      const detail =
        err instanceof PosApiError
          ? (err.problem.detail ?? t("purchasing.saveFailed"))
          : t("purchasing.saveFailed");
      setError(detail);
    } finally {
      setSaving(false);
    }
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
        {t("purchasing.branch")}
        <input
          className="min-h-11 rounded-md border border-border bg-muted px-3"
          value={boundWorkspace?.branchName ?? boundWorkspace?.branchId ?? ""}
          readOnly
          data-testid="po-branch"
        />
      </label>

      <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
        {t("purchasing.supplier")}
        <select
          className="min-h-11 rounded-md border border-border bg-background px-3"
          value={supplierId}
          onChange={(e) => setSupplierId(e.target.value)}
          disabled={!allowManage || !online}
          data-testid="po-supplier"
        >
          <option value="">{t("purchasing.selectSupplier")}</option>
          {(suppliersQuery.data?.items ?? []).map((s) => (
            <option key={s.supplierId} value={s.supplierId}>
              {s.name}
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
                onClick={addLine}
                data-testid="po-add-line"
              >
                {t("purchasing.addLine")}
              </Button>
            </div>
          </div>
        ) : null}
      </section>

      <section aria-labelledby="po-lines-heading">
        <h2
          id="po-lines-heading"
          className="m-0 mb-2 text-[length:var(--exits-text-md)] font-medium"
        >
          {t("purchasing.lines")}
        </h2>
        {lines.length === 0 ? (
          <p className="m-0 text-muted">{t("purchasing.linesEmpty")}</p>
        ) : (
          <ul className="m-0 flex list-none flex-col gap-2 p-0">
            {lines.map((line) => (
              <li
                key={line.productId}
                className="rounded-md border border-border p-3"
                data-testid={`po-draft-line-${line.productId}`}
              >
                <div className="font-medium">{line.name}</div>
                <div className="text-[length:var(--exits-text-sm)] text-muted">
                  {line.orderedQty} {line.uom} · {line.unitPurchaseCost}
                </div>
                <Button
                  type="button"
                  variant="ghost"
                  className="mt-2 min-h-11"
                  onClick={() =>
                    setLines((prev) => prev.filter((l) => l.productId !== line.productId))
                  }
                >
                  {t("purchasing.removeLine")}
                </Button>
              </li>
            ))}
          </ul>
        )}
      </section>

      {error ? (
        <Card data-testid="po-create-error">
          <p className="m-0 text-destructive">{error}</p>
        </Card>
      ) : null}

      <Button
        type="button"
        className="min-h-11"
        disabled={!allowManage || !online || saving || statusLocked}
        onClick={() => void submit()}
        data-testid="po-create-submit"
      >
        {saving ? t("purchasing.saving") : t("purchasing.createOrder")}
      </Button>
    </div>
  );
}
