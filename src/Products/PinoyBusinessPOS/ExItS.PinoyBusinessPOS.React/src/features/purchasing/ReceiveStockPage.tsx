import { useEffect, useMemo, useRef, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { ArrowRight, Plus, Trash2 } from "lucide-react";
import { canManageInventory } from "@/access/pos-capabilities";
import {
  listCatalogCategories,
  listCatalogProducts,
} from "@/api/pos/pos-catalog-client";
import type { PosCatalogProductDto } from "@/api/pos/pos-catalog-types";
import { createDirectPurchaseReceipt } from "@/api/pos/pos-direct-purchase-receipts-client";
import { PosApiError } from "@/api/pos/pos-http";
import { listSuppliers } from "@/api/pos/pos-suppliers-client";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/exits/EmptyState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { pageBackNav } from "@/navigation/page-back-nav";
import { SearchField } from "@/components/exits/SearchField";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { isLikelyNetworkFailure } from "@/connectivity/network-failure";
import { ReceivePaymentSection } from "@/features/purchasing/ReceivePaymentSection";
import {
  directPurchaseCreditValidationKey,
  formatMoneyInput,
  parseMoneyInput,
  remainingCredit,
  roundMoney,
  validateReceivePaidNow,
  type ReceivePaymentMethodCode,
  type ReceivePaymentMode,
} from "@/features/purchasing/receive-payment";
import { useI18n } from "@/i18n/I18nProvider";
import { formatPeso } from "@/lib/format-money";
import { createSecureMutationId } from "@/lib/secure-mutation-id";
import { cn } from "@/lib/cn";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

const OTHER_SOURCE = "__other__";

type DraftLine = {
  productId: string;
  name: string;
  uom: string;
  tracksExpiration: boolean;
  quantity: number;
  unitCost: number;
  expiryDate: string | null;
  lotNumber: string | null;
};

type RowDraft = {
  qty: string;
  cost: string;
  expiry: string;
  lot: string;
};

function todayIsoDate(): string {
  const d = new Date();
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;
}

function emptyRowDraft(existing?: DraftLine): RowDraft {
  return {
    qty: existing ? String(existing.quantity) : "1",
    cost: existing ? String(existing.unitCost) : "",
    expiry: existing?.expiryDate ?? "",
    lot: existing?.lotNumber ?? "",
  };
}

export function ReceiveStockPage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const online = useBrowserOnline();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const allowManage = canManageInventory(sessionGrant);

  const [purchaseDate, setPurchaseDate] = useState(todayIsoDate);
  const [supplierChoice, setSupplierChoice] = useState("");
  const [sourceName, setSourceName] = useState("");
  const [referenceNumber, setReferenceNumber] = useState("");
  const [notes, setNotes] = useState("");
  const [search, setSearch] = useState("");
  const [debounced, setDebounced] = useState("");
  const [categoryId, setCategoryId] = useState("");
  const [lines, setLines] = useState<DraftLine[]>([]);
  const [rowDrafts, setRowDrafts] = useState<Record<string, RowDraft>>({});
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [reviewing, setReviewing] = useState(false);
  const [statusLocked, setStatusLocked] = useState(false);
  const [paidNowText, setPaidNowText] = useState("");
  const [dueDate, setDueDate] = useState("");
  const [paymentMode, setPaymentMode] = useState<ReceivePaymentMode>("paidInFull");
  const [paymentMethod, setPaymentMethod] = useState<ReceivePaymentMethodCode>("Cash");
  const [paidNowTouched, setPaidNowTouched] = useState(false);
  const idempotencyKeyRef = useRef<string | null>(null);
  const draftBranchIdRef = useRef<string | null>(null);

  const supplierId =
    supplierChoice && supplierChoice !== OTHER_SOURCE ? supplierChoice : "";
  const useOtherSource = supplierChoice === OTHER_SOURCE;
  const allowSupplierCredit = Boolean(supplierId.trim());

  useEffect(() => {
    const currentBranchId = boundWorkspace?.branchId ?? null;
    if (!currentBranchId) {
      return;
    }
    if (draftBranchIdRef.current === null) {
      draftBranchIdRef.current = currentBranchId;
      return;
    }
    if (draftBranchIdRef.current === currentBranchId) {
      return;
    }
    const hadDraft =
      lines.length > 0 ||
      supplierChoice.trim().length > 0 ||
      sourceName.trim().length > 0 ||
      referenceNumber.trim().length > 0 ||
      notes.trim().length > 0;
    draftBranchIdRef.current = currentBranchId;
    idempotencyKeyRef.current = null;
    setLines([]);
    setSupplierChoice("");
    setSourceName("");
    setReferenceNumber("");
    setNotes("");
    setRowDrafts({});
    setReviewing(false);
    setStatusLocked(false);
    if (hadDraft) {
      setError(t("purchasing.branchSwitchDraftReset"));
    }
  }, [
    boundWorkspace?.branchId,
    lines.length,
    notes,
    referenceNumber,
    sourceName,
    supplierChoice,
    t,
  ]);

  useEffect(() => {
    const handle = window.setTimeout(() => setDebounced(search.trim()), 250);
    return () => window.clearTimeout(handle);
  }, [search]);

  const estimatedTotal = useMemo(
    () => roundMoney(lines.reduce((sum, line) => sum + line.quantity * line.unitCost, 0)),
    [lines],
  );

  useEffect(() => {
    if (!allowSupplierCredit && paymentMode === "supplierCredit") {
      setPaymentMode("paidInFull");
      setPaidNowTouched(false);
      setDueDate("");
    }
  }, [allowSupplierCredit, paymentMode]);

  useEffect(() => {
    if (paymentMode === "paidInFull") {
      setPaidNowText(formatMoneyInput(estimatedTotal));
      setDueDate("");
      return;
    }
    if (!paidNowTouched) {
      setPaidNowText(formatMoneyInput(estimatedTotal));
    }
  }, [estimatedTotal, paidNowTouched, paymentMode]);

  const paidNowValue = parseMoneyInput(paidNowText);
  const effectivePaidNow =
    paymentMode === "paidInFull" ? estimatedTotal : paidNowValue;

  function onPaymentModeChange(mode: ReceivePaymentMode) {
    if (mode === "supplierCredit" && !allowSupplierCredit) {
      return;
    }
    setPaymentMode(mode);
    setPaidNowTouched(false);
    if (mode === "paidInFull") {
      setPaidNowText(formatMoneyInput(estimatedTotal));
      setDueDate("");
    }
  }

  function validatePayment(): number | null {
    const paidNow =
      paymentMode === "paidInFull" ? estimatedTotal : parseMoneyInput(paidNowText);
    const paidError = validateReceivePaidNow(estimatedTotal, paidNow);
    if (paidError) {
      setError(t(paidError));
      return null;
    }
    const creditKey = directPurchaseCreditValidationKey(
      supplierId,
      estimatedTotal,
      paidNow!,
    );
    if (creditKey) {
      setError(t(creditKey));
      return null;
    }
    if (paidNow! > 0 && !paymentMethod) {
      setError(t("purchasing.paymentMethodRequired"));
      return null;
    }
    return paidNow;
  }

  const workspace = useMemo(
    () =>
      boundWorkspace?.branchId
        ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
        : null,
    [boundWorkspace],
  );

  const suppliersQuery = useQuery({
    queryKey: ["suppliers", "direct-buy", workspace?.organizationId],
    enabled: Boolean(workspace) && online && allowManage,
    queryFn: ({ signal }) => listSuppliers(workspace!, { status: "Active", pageSize: 100 }, signal),
  });

  const categoriesQuery = useQuery({
    queryKey: ["catalog-categories", "direct-buy", workspace?.organizationId],
    enabled: Boolean(workspace) && online && allowManage,
    queryFn: ({ signal }) =>
      listCatalogCategories(workspace!, { status: "Active", pageSize: 50 }, signal),
  });

  const productsQuery = useQuery({
    queryKey: [
      "catalog-products",
      "direct-buy",
      workspace?.organizationId,
      debounced,
      categoryId,
    ],
    enabled: Boolean(workspace) && online && allowManage && (debounced.length > 0 || categoryId.length > 0),
    queryFn: ({ signal }) =>
      listCatalogProducts(
        workspace!,
        {
          search: debounced || undefined,
          categoryId: categoryId || undefined,
          status: "Active",
          pageSize: 20,
        },
        signal,
      ),
  });

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }

  function rowDraftFor(product: PosCatalogProductDto): RowDraft {
    const existing = lines.find((l) => l.productId === product.productId);
    return rowDrafts[product.productId] ?? emptyRowDraft(existing);
  }

  function patchRowDraft(productId: string, patch: Partial<RowDraft>) {
    setRowDrafts((prev) => {
      const existingLine = lines.find((l) => l.productId === productId);
      const base = prev[productId] ?? emptyRowDraft(existingLine);
      return { ...prev, [productId]: { ...base, ...patch } };
    });
  }

  function addProductRow(product: PosCatalogProductDto) {
    if (product.isTracked === false) {
      setError(t("purchasing.receiveStockNotTracked"));
      return;
    }
    const draft = rowDraftFor(product);
    const qty = Number(draft.qty);
    const cost = Number(draft.cost);
    if (!Number.isFinite(qty) || qty <= 0 || !Number.isFinite(cost) || cost <= 0) {
      setError(t("purchasing.invalidLine"));
      return;
    }
    const tracksExpiration = product.tracksExpiration === true;
    if (tracksExpiration && !draft.expiry.trim()) {
      setError(t("purchasing.expiryRequired"));
      return;
    }
    const line: DraftLine = {
      productId: product.productId,
      name: product.name,
      uom: product.unitOfMeasure,
      tracksExpiration,
      quantity: qty,
      unitCost: cost,
      expiryDate: tracksExpiration ? draft.expiry.trim() : null,
      lotNumber: tracksExpiration && draft.lot.trim() ? draft.lot.trim() : null,
    };
    setLines((prev) => {
      const without = prev.filter((l) => l.productId !== line.productId);
      return [...without, line];
    });
    setRowDrafts((prev) => {
      const next = { ...prev };
      delete next[product.productId];
      return next;
    });
    setError(null);
  }

  function removeLine(productId: string) {
    setLines((prev) => prev.filter((l) => l.productId !== productId));
  }

  async function confirm() {
    if (!workspace || !allowManage || !online || saving || statusLocked || lines.length === 0) {
      return;
    }
    const paidNow = validatePayment();
    if (paidNow === null) {
      return;
    }
    if (!idempotencyKeyRef.current) {
      const generated = createSecureMutationId();
      if (!generated.ok) {
        setError(t("purchasing.directSaveFailed"));
        return;
      }
      idempotencyKeyRef.current = generated.id;
    }
    const idempotencyKey = idempotencyKeyRef.current;
    const resolvedSupplierId = supplierId.trim() || null;
    const resolvedSourceName = useOtherSource
      ? sourceName.trim() || null
      : resolvedSupplierId
        ? (suppliersQuery.data?.items.find((s) => s.supplierId === resolvedSupplierId)?.name ??
          null)
        : sourceName.trim() || null;
    const paymentFields = {
      paidNow,
      dueDate:
        remainingCredit(estimatedTotal, paidNow) > 0 && dueDate.trim()
          ? dueDate.trim()
          : null,
      paymentMethodAtReceipt: paidNow > 0 ? paymentMethod : null,
    };
    const payload = {
      purchaseDate,
      supplierId: resolvedSupplierId,
      sourceName: resolvedSourceName,
      referenceNumber: referenceNumber.trim() || null,
      notes: notes.trim() || null,
      idempotencyKey,
      lines: lines.map((line) => ({
        productId: line.productId,
        quantity: line.quantity,
        unitCost: line.unitCost,
        expiryDate: line.expiryDate,
        lotNumber: line.lotNumber,
      })),
      ...paymentFields,
    };
    setSaving(true);
    setError(null);
    try {
      const receipt = await createDirectPurchaseReceipt(workspace, payload);
      idempotencyKeyRef.current = null;
      navigate(`/purchasing/direct-purchases/${receipt.directPurchaseReceiptId}`, {
        replace: true,
      });
    } catch (err) {
      // No GET-by-idempotency-key API. Sticky key makes a same-payload retry safe;
      // if transport is still down, lock the form instead of inviting a new key.
      if (isLikelyNetworkFailure(err)) {
        setError(t("checkout.confirmingTransaction"));
        try {
          const receipt = await createDirectPurchaseReceipt(workspace, payload);
          idempotencyKeyRef.current = null;
          navigate(`/purchasing/direct-purchases/${receipt.directPurchaseReceiptId}`, {
            replace: true,
          });
          return;
        } catch (retryErr) {
          if (isLikelyNetworkFailure(retryErr)) {
            setStatusLocked(true);
            setError(t("checkout.transactionStatusUnknown"));
            return;
          }
          setError(
            retryErr instanceof PosApiError
              ? (retryErr.problem.detail ?? t("purchasing.directSaveFailed"))
              : t("purchasing.directSaveFailed"),
          );
          return;
        }
      }
      setError(
        err instanceof PosApiError
          ? (err.problem.detail ?? t("purchasing.directSaveFailed"))
          : t("purchasing.directSaveFailed"),
      );
    } finally {
      setSaving(false);
    }
  }

  const categories = categoriesQuery.data?.items ?? [];
  const showProductResults = debounced.length > 0 || categoryId.length > 0;
  const productItems = productsQuery.data?.items ?? [];
  const reviewDisabled = lines.length === 0 || !allowManage || !online;

  return (
    <div
      className="receive-stock-page exits-page mx-auto flex w-full max-w-[56rem] min-w-0 flex-col gap-3"
      data-testid="receive-stock-page"
    >
      <PageHeader
        title={t("purchasing.receiveStock")}
        subtitle={boundWorkspace?.branchName || undefined}
        description={t("purchasing.receiveStockHelper")}
        backTo={pageBackNav.purchasing.to}
        backLabel={t(pageBackNav.purchasing.labelKey)}
        backTestId="page-header-back-purchasing"
      />
      {boundWorkspace?.branchName ? (
        <span className="sr-only" data-testid="direct-purchase-receiving-branch">
          {t("purchasing.receivingIntoBranch").replace("{name}", boundWorkspace.branchName)}
        </span>
      ) : null}
      {!online ? (
        <Card>
          <p className="m-0">{t("purchasing.offline")}</p>
        </Card>
      ) : null}
      {!allowManage ? (
        <Card>
          <p className="m-0">{t("purchasing.inventoryManageDenied")}</p>
        </Card>
      ) : null}

      {!reviewing ? (
        <>
          <section
            className="flex min-w-0 flex-col gap-2"
            data-testid="direct-purchase-details"
            aria-labelledby="direct-purchase-details-heading"
          >
            <h2
              id="direct-purchase-details-heading"
              className="m-0 text-[length:var(--exits-text-sm)] font-semibold text-foreground"
            >
              {t("purchasing.purchaseDetails")}
            </h2>
            <div className="grid min-w-0 grid-cols-1 gap-2 sm:grid-cols-3">
              <label className="flex min-w-0 flex-col gap-1 text-[length:var(--exits-text-sm)]">
                <span className="sr-only">{t("purchasing.purchaseDate")}</span>
                <input
                  type="date"
                  className="h-[var(--exits-control-height)] min-h-[var(--exits-control-height)] rounded-md border border-border bg-background px-3"
                  value={purchaseDate}
                  onChange={(e) => setPurchaseDate(e.target.value)}
                  data-testid="direct-purchase-date"
                  aria-label={t("purchasing.purchaseDate")}
                />
              </label>
              <label className="flex min-w-0 flex-col gap-1 text-[length:var(--exits-text-sm)]">
                <span className="sr-only">{t("purchasing.boughtFrom")}</span>
                <select
                  className="exits-select h-[var(--exits-control-height)]"
                  value={supplierChoice}
                  onChange={(e) => {
                    const next = e.target.value;
                    setSupplierChoice(next);
                    if (next && next !== OTHER_SOURCE) {
                      const match = suppliersQuery.data?.items.find(
                        (s) => s.supplierId === next,
                      );
                      if (match) {
                        setSourceName(match.name);
                      }
                    } else if (next !== OTHER_SOURCE) {
                      setSourceName("");
                    }
                  }}
                  data-testid="direct-supplier"
                  aria-label={t("purchasing.boughtFrom")}
                >
                  <option value="">{t("purchasing.boughtFrom")}</option>
                  {(suppliersQuery.data?.items ?? []).map((s) => (
                    <option key={s.supplierId} value={s.supplierId}>
                      {s.name}
                    </option>
                  ))}
                  <option value={OTHER_SOURCE}>{t("purchasing.useAnotherSource")}</option>
                </select>
              </label>
              <label className="flex min-w-0 flex-col gap-1 text-[length:var(--exits-text-sm)]">
                <span className="sr-only">{t("purchasing.reference")}</span>
                <input
                  className="h-[var(--exits-control-height)] min-h-[var(--exits-control-height)] rounded-md border border-border bg-background px-3"
                  value={referenceNumber}
                  onChange={(e) => setReferenceNumber(e.target.value)}
                  placeholder={t("purchasing.reference")}
                  data-testid="direct-reference"
                  aria-label={t("purchasing.reference")}
                />
              </label>
            </div>
            {useOtherSource ? (
              <label className="flex min-w-0 flex-col gap-1 text-[length:var(--exits-text-sm)]">
                <span className="text-muted">{t("purchasing.sourceName")}</span>
                <input
                  className="h-[var(--exits-control-height)] min-h-[var(--exits-control-height)] rounded-md border border-border bg-background px-3"
                  value={sourceName}
                  onChange={(e) => setSourceName(e.target.value)}
                  placeholder={t("purchasing.sourcePlaceholder")}
                  data-testid="direct-source-name"
                />
              </label>
            ) : null}
            <label className="flex min-w-0 flex-col gap-1 text-[length:var(--exits-text-sm)]">
              <span className="sr-only">{t("purchasing.notesOptional")}</span>
              <textarea
                className="min-h-0 rounded-md border border-border bg-background px-3 py-2"
                rows={2}
                value={notes}
                onChange={(e) => setNotes(e.target.value)}
                placeholder={t("purchasing.notesOptional")}
                data-testid="direct-notes"
                aria-label={t("purchasing.notesOptional")}
              />
            </label>
          </section>

          <section
            className="flex min-w-0 flex-col gap-2"
            data-testid="direct-add-products"
            aria-labelledby="direct-add-products-heading"
          >
            <h2
              id="direct-add-products-heading"
              className="m-0 text-[length:var(--exits-text-sm)] font-semibold text-foreground"
            >
              {t("purchasing.addProducts")}
            </h2>
            <SearchField
              label={t("purchasing.productSearch")}
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              onClear={() => setSearch("")}
              placeholder={t("purchasing.productSearch")}
              data-testid="direct-product-search"
            />
            {categories.length > 0 ? (
              <div
                className="flex min-w-0 flex-wrap gap-1.5"
                role="group"
                aria-label={t("purchasing.categoryFilter")}
                data-testid="direct-category-filters"
              >
                <button
                  type="button"
                  className={cn(
                    "rounded-md border px-2.5 py-1 text-[length:var(--exits-text-xs)]",
                    !categoryId
                      ? "border-primary bg-primary/10 text-foreground"
                      : "border-border bg-background text-muted",
                  )}
                  onClick={() => setCategoryId("")}
                >
                  {t("purchasing.categoryAll")}
                </button>
                {categories.map((category) => (
                  <button
                    type="button"
                    key={category.categoryId}
                    className={cn(
                      "rounded-md border px-2.5 py-1 text-[length:var(--exits-text-xs)]",
                      categoryId === category.categoryId
                        ? "border-primary bg-primary/10 text-foreground"
                        : "border-border bg-background text-muted",
                    )}
                    onClick={() =>
                      setCategoryId((prev) =>
                        prev === category.categoryId ? "" : category.categoryId,
                      )
                    }
                  >
                    {category.name}
                  </button>
                ))}
              </div>
            ) : null}

            {showProductResults && productsQuery.isFetching ? (
              <LoadingState label={t("loading.label")} />
            ) : null}

            {showProductResults &&
            !productsQuery.isFetching &&
            productItems.length === 0 ? (
              <EmptyState
                title={t("purchasing.noProducts")}
                detail={t("purchasing.noProductsDetail")}
                action={
                  <Button asChild variant="secondary" data-testid="direct-add-new-product">
                    <Link to="/catalog/products/new">{t("purchasing.addNewProduct")}</Link>
                  </Button>
                }
              />
            ) : null}

            <ul
              className="m-0 flex list-none flex-col gap-2 p-0"
              data-testid="direct-product-results"
            >
              {productItems.map((product) => {
                const draft = rowDraftFor(product);
                const tracksExpiration = product.tracksExpiration === true;
                return (
                  <li key={product.productId}>
                    <article
                      className="rounded-md border border-border bg-background px-3 py-2.5"
                      data-testid={`direct-product-${product.productId}`}
                    >
                      <div className="flex min-w-0 items-baseline justify-between gap-2">
                        <p className="m-0 min-w-0 font-medium leading-snug">{product.name}</p>
                        <p className="m-0 shrink-0 text-[length:var(--exits-text-sm)] text-muted">
                          {product.unitOfMeasure}
                        </p>
                      </div>
                      <div className="mt-2 flex min-w-0 flex-col gap-2 sm:flex-row sm:flex-wrap sm:items-end">
                        <label className="flex min-w-0 flex-1 flex-col gap-0.5 text-[length:var(--exits-text-xs)] text-muted sm:max-w-[7rem]">
                          {t("purchasing.qtyShort")}
                          <input
                            className="h-[var(--exits-control-height)] min-h-[var(--exits-control-height)] rounded-md border border-border bg-background px-2 text-[length:var(--exits-text-sm)] text-foreground"
                            value={draft.qty}
                            onChange={(e) =>
                              patchRowDraft(product.productId, { qty: e.target.value })
                            }
                            inputMode="decimal"
                            data-testid={`direct-line-qty-${product.productId}`}
                          />
                        </label>
                        <label className="flex min-w-0 flex-1 flex-col gap-0.5 text-[length:var(--exits-text-xs)] text-muted sm:max-w-[9rem]">
                          {t("purchasing.costShort")}
                          <input
                            className="h-[var(--exits-control-height)] min-h-[var(--exits-control-height)] rounded-md border border-border bg-background px-2 text-[length:var(--exits-text-sm)] text-foreground"
                            value={draft.cost}
                            onChange={(e) =>
                              patchRowDraft(product.productId, { cost: e.target.value })
                            }
                            inputMode="decimal"
                            placeholder="0.00"
                            data-testid={`direct-line-cost-${product.productId}`}
                          />
                        </label>
                        {tracksExpiration ? (
                          <>
                            <label className="flex min-w-0 flex-1 flex-col gap-0.5 text-[length:var(--exits-text-xs)] text-muted sm:max-w-[10rem]">
                              {t("purchasing.expiryDate")}
                              <input
                                type="date"
                                className="h-[var(--exits-control-height)] min-h-[var(--exits-control-height)] rounded-md border border-border bg-background px-2 text-[length:var(--exits-text-sm)] text-foreground"
                                value={draft.expiry}
                                onChange={(e) =>
                                  patchRowDraft(product.productId, {
                                    expiry: e.target.value,
                                  })
                                }
                                data-testid={`direct-line-expiry-${product.productId}`}
                              />
                            </label>
                            <label className="flex min-w-0 flex-1 flex-col gap-0.5 text-[length:var(--exits-text-xs)] text-muted sm:max-w-[9rem]">
                              {t("purchasing.lotNumber")}
                              <input
                                className="h-[var(--exits-control-height)] min-h-[var(--exits-control-height)] rounded-md border border-border bg-background px-2 text-[length:var(--exits-text-sm)] text-foreground"
                                value={draft.lot}
                                onChange={(e) =>
                                  patchRowDraft(product.productId, { lot: e.target.value })
                                }
                                data-testid={`direct-line-lot-${product.productId}`}
                              />
                            </label>
                          </>
                        ) : null}
                        <Button
                          type="button"
                          className="w-full sm:ml-auto sm:w-auto"
                          onClick={() => addProductRow(product)}
                          data-testid={`direct-add-${product.productId}`}
                        >
                          <Plus className="size-4" aria-hidden />
                          {t("purchasing.addProduct")}
                        </Button>
                      </div>
                    </article>
                  </li>
                );
              })}
            </ul>
          </section>

          <section
            className="flex min-w-0 flex-col gap-2 border-t border-border pt-3"
            data-testid="direct-receipt-items"
            aria-labelledby="direct-receipt-items-heading"
          >
            <h2
              id="direct-receipt-items-heading"
              className="m-0 text-[length:var(--exits-text-sm)] font-semibold text-foreground"
            >
              {t("purchasing.receiptItems")}
            </h2>
            {lines.length === 0 ? (
              <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                {t("purchasing.draftEmpty")}
              </p>
            ) : (
              <>
                <ul className="m-0 flex list-none flex-col gap-1.5 p-0">
                  {lines.map((line) => {
                    const lineTotal = roundMoney(line.quantity * line.unitCost);
                    return (
                      <li
                        key={line.productId}
                        className="flex min-w-0 items-start justify-between gap-2 rounded-md border border-border px-3 py-2"
                        data-testid={`direct-receipt-line-${line.productId}`}
                      >
                        <div className="min-w-0 flex-1">
                          <p className="m-0 font-medium leading-snug">{line.name}</p>
                          <p className="m-0 mt-0.5 text-[length:var(--exits-text-sm)] text-muted tabular-nums">
                            {line.quantity} {line.uom} × {formatPeso(line.unitCost)}
                            {line.expiryDate ? ` · ${line.expiryDate}` : ""}
                          </p>
                        </div>
                        <div className="flex shrink-0 items-center gap-1">
                          <span className="text-[length:var(--exits-text-sm)] font-medium tabular-nums">
                            {formatPeso(lineTotal)}
                          </span>
                          <Button
                            type="button"
                            variant="ghost"
                            size="icon"
                            aria-label={t("purchasing.removeLine")}
                            onClick={() => removeLine(line.productId)}
                            data-testid={`direct-remove-${line.productId}`}
                          >
                            <Trash2 className="size-4" aria-hidden />
                          </Button>
                        </div>
                      </li>
                    );
                  })}
                </ul>
                <div className="flex items-center justify-end gap-2 pt-1">
                  <span className="text-[length:var(--exits-text-sm)] text-muted">
                    {t("purchasing.receiptTotal")}
                  </span>
                  <span
                    className="text-[length:var(--exits-text-md)] font-semibold tabular-nums"
                    data-testid="direct-receipt-total"
                  >
                    {formatPeso(estimatedTotal)}
                  </span>
                </div>
              </>
            )}
          </section>

          <div className="flex flex-wrap items-center justify-between gap-2 pt-1">
            <Button
              type="button"
              variant="ghost"
              onClick={() => navigate(pageBackNav.purchasing.to)}
              data-testid="direct-cancel"
            >
              {t("purchasing.cancel")}
            </Button>
            <Button
              type="button"
              disabled={reviewDisabled}
              onClick={() => {
                if (validatePayment() === null) {
                  return;
                }
                setError(null);
                setReviewing(true);
              }}
              data-testid="direct-review"
            >
              {t("purchasing.reviewDirect")}
              <ArrowRight className="size-4" aria-hidden />
            </Button>
          </div>
        </>
      ) : (
        <Card data-testid="direct-review-sheet">
          <p className="mt-0">{t("purchasing.willIncreaseStock")}</p>
          <ul className="m-0 flex list-none flex-col gap-2 p-0">
            {lines.map((line) => (
              <li key={line.productId}>
                {line.name}: {line.quantity} @ {formatPeso(line.unitCost)}
                {line.expiryDate ? ` · exp ${line.expiryDate}` : ""}
              </li>
            ))}
          </ul>
          <p className="mb-0 mt-2 text-right font-semibold tabular-nums">
            {t("purchasing.receiptTotal")} {formatPeso(estimatedTotal)}
          </p>
          <ReceivePaymentSection
            estimatedTotal={estimatedTotal}
            mode={paymentMode}
            onModeChange={onPaymentModeChange}
            paidNowText={paidNowText}
            onPaidNowChange={(value) => {
              setPaidNowTouched(true);
              setPaidNowText(value);
            }}
            paymentMethod={paymentMethod}
            onPaymentMethodChange={setPaymentMethod}
            dueDate={dueDate}
            onDueDateChange={setDueDate}
            paidNowValue={effectivePaidNow}
            allowSupplierCredit={allowSupplierCredit}
            disabled={saving || statusLocked}
          />
          <div className="mt-3 flex flex-wrap justify-between gap-2">
            <Button
              type="button"
              variant="ghost"
              onClick={() => setReviewing(false)}
            >
              {t("purchasing.backToReceipt")}
            </Button>
            <Button
              type="button"
              disabled={saving || statusLocked}
              onClick={() => void confirm()}
              data-testid="direct-confirm"
            >
              {saving ? t("purchasing.saving") : t("purchasing.confirmDirect")}
            </Button>
          </div>
        </Card>
      )}

      {error ? (
        <Card data-testid="direct-error">
          <p className="m-0 text-destructive">{error}</p>
        </Card>
      ) : null}
    </div>
  );
}
