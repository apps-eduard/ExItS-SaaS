import { useEffect, useMemo, useRef, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { ArrowRight, AlertTriangle, ClipboardList, PackagePlus, Plus, Trash2, X } from "lucide-react";
import { canManageInventory } from "@/access/pos-capabilities";
import {
  listCatalogCategories,
  listCatalogProducts,
} from "@/api/pos/pos-catalog-client";
import type { PosCatalogProductDto } from "@/api/pos/pos-catalog-types";
import { createDirectPurchaseReceipt } from "@/api/pos/pos-direct-purchase-receipts-client";
import {
  listDirectPurchases,
  type DirectPurchaseHistoryItem,
} from "@/api/pos/pos-direct-purchases-client";
import { PosApiError } from "@/api/pos/pos-http";
import { listSuppliers } from "@/api/pos/pos-suppliers-client";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { CountBadge } from "@/components/exits/CountChip";
import { EmptyState } from "@/components/exits/EmptyState";
import {
  ExitsTable,
  ExitsTableActions,
  ExitsTableBody,
  ExitsTableCell,
  ExitsTableContainer,
  ExitsTableHead,
  ExitsTableHeader,
  ExitsTableMobile,
  ExitsTableMobileRow,
  ExitsTablePagination,
  ExitsTableRow,
} from "@/components/exits/ExitsTable";
import { FilterChip } from "@/components/exits/FilterChip";
import { LoadingState } from "@/components/exits/LoadingState";
import { QuantityStepper } from "@/components/exits/MoneyQuantity";
import { Notice } from "@/components/exits/Notice";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/exits/StatusChip";
import { useToast } from "@/components/exits/ToastProvider";
import { pageBackNav } from "@/navigation/page-back-nav";
import { SearchField } from "@/components/exits/SearchField";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { isLikelyNetworkFailure } from "@/connectivity/network-failure";
import { ReceiveCategoryMultiSelect } from "@/features/purchasing/ReceiveCategoryMultiSelect";
import {
  hasReceiveCostMarginWarning,
  receiveCostMarginKind,
  resolveReceiveEffectiveSellingPrice,
  type ReceiveMarginWarningFlash,
} from "@/features/purchasing/receive-cost-margin";
import { ReceivePaymentSection } from "@/features/purchasing/ReceivePaymentSection";
import { normalizeMoneyAmountTyping } from "@/lib/money-input";
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
import { cn } from "@/lib/cn";
import { formatPeso } from "@/lib/format-money";
import {
  clampQuantityToPrecision,
  isValidQuantity,
  maxQuantityDecimals,
  quantityInputMinimum,
  quantityStepperWholeStep,
} from "@/lib/quantity-rules";
import { createSecureMutationId } from "@/lib/secure-mutation-id";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

const OTHER_SOURCE = "__other__";
const RECENT_COMPLETED_PAGE_SIZE = 8;
const PRODUCT_PAGE_SIZE_OPTIONS = [10, 25, 50, 100] as const;

type DraftLine = {
  productId: string;
  name: string;
  sku: string | null;
  uom: string;
  sellingMode: string;
  tracksExpiration: boolean;
  quantity: number;
  unitCost: number;
  /** Branch-effective catalog selling price at add time (margin comparison source). */
  effectiveSellingPrice: number;
  sellingPrice: number;
  costInput: string;
  expiryDate: string;
  lotNumber: string;
};

function todayIsoDate(): string {
  const d = new Date();
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;
}

function historyRowHref(item: DirectPurchaseHistoryItem): string {
  return item.sourceType === "B2B"
    ? `/purchasing/direct-purchases/b2b/${item.sourceId}`
    : `/purchasing/direct-purchases/${item.sourceId}`;
}

function formatHistoryDate(item: DirectPurchaseHistoryItem): string {
  if (item.purchaseDate) return item.purchaseDate;
  return new Date(item.occurredAtUtc).toISOString().slice(0, 10);
}

function lineIsComplete(line: DraftLine): boolean {
  if (!(line.quantity > 0 && line.unitCost > 0)) return false;
  if (line.tracksExpiration && !line.expiryDate.trim()) return false;
  return true;
}

function lineHasCostMarginWarning(line: DraftLine): boolean {
  return hasReceiveCostMarginWarning(line.unitCost, line.effectiveSellingPrice);
}

function prefersReducedMotion(): boolean {
  return (
    typeof window !== "undefined" &&
    typeof window.matchMedia === "function" &&
    window.matchMedia("(prefers-reduced-motion: reduce)").matches
  );
}

export function ReceiveStockPage() {
  const { t } = useI18n();
  const { showToast } = useToast();
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
  const [categoryIds, setCategoryIds] = useState<string[]>([]);
  const [lines, setLines] = useState<DraftLine[]>([]);
  const [finderOpen, setFinderOpen] = useState(false);
  const [highlightProductId, setHighlightProductId] = useState<string | null>(null);
  const [focusCostProductId, setFocusCostProductId] = useState<string | null>(null);
  const [trackedOnly, setTrackedOnly] = useState(false);
  const [productPage, setProductPage] = useState(1);
  const [productPageSize, setProductPageSize] = useState<number>(PRODUCT_PAGE_SIZE_OPTIONS[0]);
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
  const finderPanelId = "direct-find-products-panel";
  const productSearchInputId = "direct-product-search-input";

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
    setFinderOpen(false);
    setHighlightProductId(null);
    setSupplierChoice("");
    setSourceName("");
    setReferenceNumber("");
    setNotes("");
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

  useEffect(() => {
    if (!finderOpen) {
      return;
    }
    const frame = window.requestAnimationFrame(() => {
      const input = document.getElementById(productSearchInputId);
      if (input instanceof HTMLInputElement) {
        input.focus();
      }
    });
    return () => window.cancelAnimationFrame(frame);
  }, [finderOpen]);

  useEffect(() => {
    if (!highlightProductId) {
      return;
    }
    const reduceMotion = prefersReducedMotion();
    if (reduceMotion) {
      setHighlightProductId(null);
      return;
    }
    const handle = window.setTimeout(() => setHighlightProductId(null), 1200);
    return () => window.clearTimeout(handle);
  }, [highlightProductId]);

  useEffect(() => {
    if (!focusCostProductId) {
      return;
    }
    const productId = focusCostProductId;
    const frame = window.requestAnimationFrame(() => {
      const costInput = document.querySelector(
        `[data-testid="direct-line-cost-${productId}"]`,
      );
      if (costInput instanceof HTMLInputElement) {
        costInput.focus();
        costInput.select();
      }
      setFocusCostProductId(null);
    });
    return () => window.cancelAnimationFrame(frame);
  }, [focusCostProductId]);

  const estimatedTotal = useMemo(
    () => roundMoney(lines.reduce((sum, line) => sum + line.quantity * line.unitCost, 0)),
    [lines],
  );

  const linesValid = useMemo(
    () => lines.length > 0 && lines.every(lineIsComplete),
    [lines],
  );

  const marginWarningLines = useMemo(
    () => lines.filter(lineHasCostMarginWarning),
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

  // Category filter is applied client-side so every category can show a stable count (incl. 0).
  const productsQuery = useQuery({
    queryKey: [
      "catalog-products",
      "direct-buy",
      workspace?.organizationId,
      debounced,
    ],
    enabled: Boolean(workspace) && online && allowManage,
    queryFn: ({ signal }) =>
      listCatalogProducts(
        workspace!,
        {
          search: debounced || undefined,
          status: "Active",
          pageSize: 100,
        },
        signal,
      ),
  });

  const recentCompletedQuery = useQuery({
    queryKey: ["direct-purchases", "receive-stock-recent", workspace?.organizationId],
    enabled: Boolean(workspace) && online,
    queryFn: ({ signal }) =>
      listDirectPurchases(
        workspace!,
        {
          status: "Completed",
          page: 1,
          pageSize: RECENT_COMPLETED_PAGE_SIZE,
        },
        signal,
      ),
  });

  const categories = categoriesQuery.data?.items ?? [];
  const categoryNameById = useMemo(() => {
    const map = new Map<string, string>();
    for (const category of categories) {
      map.set(category.categoryId, category.name);
    }
    return map;
  }, [categories]);
  const rawProductItems = productsQuery.data?.items ?? [];
  const categoriesWithCounts = useMemo(() => {
    const counts = new Map<string, number>();
    for (const category of categories) {
      counts.set(category.categoryId, 0);
    }
    for (const product of rawProductItems) {
      if (product.categoryId == null || !counts.has(product.categoryId)) {
        continue;
      }
      counts.set(product.categoryId, (counts.get(product.categoryId) ?? 0) + 1);
    }
    return categories.map((category) => ({
      categoryId: category.categoryId,
      name: category.name,
      count: counts.get(category.categoryId) ?? 0,
    }));
  }, [categories, rawProductItems]);
  const addedProductIds = useMemo(
    () => new Set(lines.map((line) => line.productId)),
    [lines],
  );
  const productItems = useMemo(() => {
    const categoryFiltered =
      categoryIds.length === 0
        ? rawProductItems
        : rawProductItems.filter(
            (product) => product.categoryId != null && categoryIds.includes(product.categoryId),
          );
    const available = categoryFiltered.filter(
      (product) => !addedProductIds.has(product.productId),
    );
    if (trackedOnly) {
      return available.filter((product) => product.isTracked !== false);
    }
    return available;
  }, [addedProductIds, categoryIds, rawProductItems, trackedOnly]);

  const productTotal = productItems.length;
  const productPageCount = Math.max(1, Math.ceil(productTotal / productPageSize) || 1);
  const safeProductPage = Math.min(Math.max(productPage, 1), productPageCount);
  const pagedProductItems = useMemo(() => {
    const start = (safeProductPage - 1) * productPageSize;
    return productItems.slice(start, start + productPageSize);
  }, [productItems, productPageSize, safeProductPage]);

  useEffect(() => {
    setProductPage(1);
  }, [debounced, categoryIds, trackedOnly, addedProductIds]);

  useEffect(() => {
    if (productPage !== safeProductPage) {
      setProductPage(safeProductPage);
    }
  }, [productPage, safeProductPage]);

  const recentItems = recentCompletedQuery.data?.items ?? [];
  const recentTotal = recentCompletedQuery.data?.totalCount ?? 0;
  const reviewDisabled = !linesValid || !allowManage || !online;
  const hasActiveFilters =
    categoryIds.length > 0 || debounced.length > 0 || trackedOnly;

  function openFinder() {
    setFinderOpen(true);
  }

  function closeFinder() {
    setFinderOpen(false);
  }

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }

  function addProductRow(product: PosCatalogProductDto) {
    if (product.isTracked === false) {
      showToast({
        title: t("purchasing.inventoryTrackingRequired"),
        description: t("purchasing.inventoryTrackingRequiredDetail").replace(
          "{name}",
          product.name,
        ),
        tone: "error",
        action: {
          label: t("inventory.enable"),
          href: `/inventory/${product.productId}`,
        },
      });
      return;
    }
    if (lines.some((line) => line.productId === product.productId)) {
      return;
    }
    const tracksExpiration = product.tracksExpiration === true;
    const effectiveSelling = resolveReceiveEffectiveSellingPrice(product);
    const catalogSelling = effectiveSelling > 0 ? effectiveSelling : 0;
    const line: DraftLine = {
      productId: product.productId,
      name: product.name,
      sku: product.sku ?? null,
      uom: product.unitOfMeasure,
      sellingMode: product.sellingMode ?? "PerItem",
      tracksExpiration,
      quantity: 1,
      unitCost: 0,
      effectiveSellingPrice: catalogSelling,
      sellingPrice: catalogSelling,
      costInput: "",
      expiryDate: "",
      lotNumber: "",
    };
    setLines((prev) => [...prev, line]);
    setError(null);
    setFocusCostProductId(product.productId);
    if (!prefersReducedMotion()) {
      setHighlightProductId(product.productId);
    }
  }

  function patchLine(
    productId: string,
    patch: Partial<Pick<DraftLine, "costInput" | "expiryDate" | "lotNumber" | "quantity">>,
  ) {
    setLines((prev) =>
      prev.map((line) => {
        if (line.productId !== productId) return line;
        const next = { ...line, ...patch };
        if (patch.quantity !== undefined) {
          const precision = maxQuantityDecimals(line.uom, line.sellingMode);
          const minQty = quantityInputMinimum(line.uom, line.sellingMode);
          if (
            Number.isFinite(patch.quantity) &&
            patch.quantity >= minQty &&
            isValidQuantity(patch.quantity, line.uom, line.sellingMode)
          ) {
            next.quantity = clampQuantityToPrecision(patch.quantity, precision);
          }
        }
        if (patch.costInput !== undefined) {
          const cost = parseMoneyInput(patch.costInput);
          next.unitCost = cost !== null && cost > 0 ? cost : 0;
        }
        return next;
      }),
    );
  }

  function removeLine(productId: string) {
    setLines((prev) => prev.filter((l) => l.productId !== productId));
  }

  function marginWarningFlash(affected: DraftLine[]): ReceiveMarginWarningFlash | null {
    if (affected.length === 0) {
      return null;
    }
    return {
      count: affected.length,
      productId: affected.length === 1 ? affected[0].productId : null,
    };
  }

  function startReview() {
    if (!linesValid) {
      const incomplete = lines.find((line) => !lineIsComplete(line));
      if (incomplete?.tracksExpiration && !incomplete.expiryDate.trim()) {
        setError(t("purchasing.expiryRequired"));
      } else {
        setError(t("purchasing.invalidLine"));
      }
      return;
    }
    if (validatePayment() === null) {
      return;
    }
    setError(null);
    setReviewing(true);
  }

  async function confirm() {
    if (!workspace || !allowManage || !online || saving || statusLocked || !linesValid) {
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
        expiryDate: line.tracksExpiration ? line.expiryDate.trim() : null,
        lotNumber:
          line.tracksExpiration && line.lotNumber.trim() ? line.lotNumber.trim() : null,
      })),
      ...paymentFields,
    };
    setSaving(true);
    setError(null);
    const marginAffected = lines.filter(lineHasCostMarginWarning);
    try {
      const receipt = await createDirectPurchaseReceipt(workspace, payload);
      idempotencyKeyRef.current = null;
      const flash = marginWarningFlash(marginAffected);
      navigate(`/purchasing/direct-purchases/${receipt.directPurchaseReceiptId}`, {
        replace: true,
        state: flash ? { receiveMarginWarning: flash } : undefined,
      });
    } catch (err) {
      // No GET-by-idempotency-key API. Sticky key makes a same-payload retry safe;
      // if transport is still down, lock the form instead of inviting a new key.
      if (isLikelyNetworkFailure(err)) {
        setError(t("checkout.confirmingTransaction"));
        try {
          const receipt = await createDirectPurchaseReceipt(workspace, payload);
          idempotencyKeyRef.current = null;
          const flash = marginWarningFlash(marginAffected);
          navigate(`/purchasing/direct-purchases/${receipt.directPurchaseReceiptId}`, {
            replace: true,
            state: flash ? { receiveMarginWarning: flash } : undefined,
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

  return (
    <div
      className="receive-stock-page exits-page flex min-w-0 flex-col gap-3"
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
        <Notice tone="warning" testId="direct-offline">
          {t("purchasing.offline")}
        </Notice>
      ) : null}
      {!allowManage ? (
        <Notice tone="danger" testId="direct-manage-denied">
          {t("purchasing.inventoryManageDenied")}
        </Notice>
      ) : null}
      {error ? (
        <Notice tone="danger" testId="direct-error">
          {error}
        </Notice>
      ) : null}

      {!reviewing ? (
        <>
          <Card
            as="section"
            padding="compact"
            className="receive-stock-section receive-stock-details"
            data-testid="direct-purchase-details"
            aria-labelledby="direct-purchase-details-heading"
          >
            <h2
              id="direct-purchase-details-heading"
              className="receive-stock-section__title m-0"
            >
              {t("purchasing.purchaseDetails")}
            </h2>
            <div className="receive-stock-details__grid">
              <label className="receive-stock-field">
                <span className="receive-stock-field__label">{t("purchasing.purchaseDate")}</span>
                <input
                  type="date"
                  className="exits-input"
                  value={purchaseDate}
                  onChange={(e) => setPurchaseDate(e.target.value)}
                  data-testid="direct-purchase-date"
                  aria-label={t("purchasing.purchaseDate")}
                />
              </label>
              <label className="receive-stock-field">
                <span className="receive-stock-field__label">{t("purchasing.boughtFrom")}</span>
                <select
                  className="exits-select catalog-form-select"
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
              <label className="receive-stock-field">
                <span className="receive-stock-field__label">{t("purchasing.reference")}</span>
                <input
                  className="exits-input"
                  value={referenceNumber}
                  onChange={(e) => setReferenceNumber(e.target.value)}
                  placeholder={t("purchasing.reference")}
                  data-testid="direct-reference"
                  aria-label={t("purchasing.reference")}
                />
              </label>
            </div>
            {useOtherSource ? (
              <label className="receive-stock-field">
                <span className="receive-stock-field__label">{t("purchasing.sourceName")}</span>
                <input
                  className="exits-input"
                  value={sourceName}
                  onChange={(e) => setSourceName(e.target.value)}
                  placeholder={t("purchasing.sourcePlaceholder")}
                  data-testid="direct-source-name"
                />
              </label>
            ) : null}
            <label className="receive-stock-field">
              <span className="receive-stock-field__label">{t("purchasing.notesOptional")}</span>
              <textarea
                className="exits-input receive-stock-notes"
                rows={2}
                value={notes}
                onChange={(e) => setNotes(e.target.value)}
                data-testid="direct-notes"
                aria-label={t("purchasing.notesOptional")}
              />
            </label>
          </Card>

          <div className="receive-stock-workspace">
            <Card
              as="section"
              padding="compact"
              className="receive-stock-section receive-stock-receipt"
              data-testid="direct-receipt-items"
              aria-labelledby="direct-receipt-items-heading"
            >
              <div className="receive-stock-section__header">
                <h2
                  id="direct-receipt-items-heading"
                  className="receive-stock-section__title m-0 flex items-center gap-2"
                >
                  <span>{t("purchasing.receiptItems")}</span>
                  <CountBadge count={lines.length} tone="primary" />
                </h2>
                <Button
                  type="button"
                  onClick={openFinder}
                  aria-expanded={finderOpen}
                  aria-controls={finderPanelId}
                  data-testid="direct-add-products-trigger"
                >
                  <Plus className="size-4" aria-hidden />
                  {t("purchasing.addProducts")}
                </Button>
              </div>

              {lines.length === 0 ? (
                <EmptyState
                  align="center"
                  size="compact"
                  icon={<PackagePlus className="size-5" strokeWidth={1.75} />}
                  title={t("purchasing.draftEmpty")}
                  detail={t("purchasing.draftEmptyDetailLeft")}
                  testId="direct-receipt-empty"
                />
              ) : (
                <>
                  <ExitsTableContainer
                    data-testid="direct-receipt-table"
                    className="receive-stock-receipt-table"
                  >
                    <ExitsTable>
                      <ExitsTableHeader>
                        <ExitsTableRow>
                          <ExitsTableHead cellAlign="text">
                            {t("purchasing.receiveProduct")}
                          </ExitsTableHead>
                          <ExitsTableHead cellAlign="text">{t("purchasing.qtyShort")}</ExitsTableHead>
                          <ExitsTableHead cellAlign="text">
                            {t("purchasing.costShort")}
                          </ExitsTableHead>
                          <ExitsTableHead cellAlign="text">
                            {t("purchasing.sellingPriceShort")}
                          </ExitsTableHead>
                          <ExitsTableHead
                            cellAlign="text"
                            className="receive-stock-receipt-table__expiry-col"
                          >
                            {t("purchasing.expiryDate")}
                          </ExitsTableHead>
                          <ExitsTableHead cellAlign="text">
                            {t("purchasing.lotNumber")}
                          </ExitsTableHead>
                          <ExitsTableHead cellAlign="numeric">
                            {t("purchasing.lineTotal")}
                          </ExitsTableHead>
                          <ExitsTableHead
                            cellAlign="center"
                            colSize="actions"
                            className="receive-stock-receipt-table__action-col"
                          >
                            {t("purchasing.action")}
                          </ExitsTableHead>
                        </ExitsTableRow>
                      </ExitsTableHeader>
                      <ExitsTableBody>
                        {lines.map((line) => {
                          const lineTotal = roundMoney(line.quantity * line.unitCost);
                          const highlighted = highlightProductId === line.productId;
                          const qtyInvalid = !(line.quantity > 0);
                          const costInvalid = !(line.unitCost > 0);
                          const expiryInvalid =
                            line.tracksExpiration && !line.expiryDate.trim();
                          const qtyPrecision = maxQuantityDecimals(line.uom, line.sellingMode);
                          const qtyMin = quantityInputMinimum(line.uom, line.sellingMode);
                          const marginKind = receiveCostMarginKind(
                            line.unitCost,
                            line.effectiveSellingPrice,
                          );
                          const marginLabel =
                            marginKind === "zeroMargin"
                              ? t("purchasing.costZeroMarginWarning")
                              : marginKind === "negativeMargin"
                                ? t("purchasing.costNegativeMarginWarning")
                                : null;
                          const sellingDisplay =
                            line.effectiveSellingPrice > 0
                              ? formatMoneyInput(line.effectiveSellingPrice)
                              : "0.00";
                          return (
                            <ExitsTableRow
                              key={line.productId}
                              className={cn(
                                highlighted && "receive-stock-receipt-row--highlight",
                              )}
                              data-testid={`direct-receipt-line-${line.productId}`}
                            >
                              <ExitsTableCell cellAlign="text">
                                <div className="font-medium leading-snug">{line.name}</div>
                                {line.sku ? (
                                  <div className="text-[length:var(--exits-text-xs)] text-muted">
                                    {line.sku}
                                  </div>
                                ) : null}
                              </ExitsTableCell>
                              <ExitsTableCell cellAlign="text">
                                <QuantityStepper
                                  compact
                                  value={line.quantity}
                                  onChange={(next) =>
                                    patchLine(line.productId, { quantity: next })
                                  }
                                  min={qtyMin}
                                  step={quantityStepperWholeStep()}
                                  precision={qtyPrecision}
                                  unit={line.uom}
                                  invalid={qtyInvalid}
                                  decreaseLabel={t("purchasing.decreaseQty")}
                                  increaseLabel={t("purchasing.increaseQty")}
                                  ariaLabel={t("purchasing.qtyShort")}
                                  valueTestId={`direct-line-qty-${line.productId}`}
                                  className="receive-qty-stepper"
                                />
                              </ExitsTableCell>
                              <ExitsTableCell cellAlign="text">
                                <input
                                  className="exits-input receive-cost-input tabular-nums"
                                  value={line.costInput}
                                  onChange={(e) =>
                                    patchLine(line.productId, {
                                      costInput: normalizeMoneyAmountTyping(e.target.value),
                                    })
                                  }
                                  onBlur={(e) => {
                                    const parsed = parseMoneyInput(e.target.value);
                                    if (parsed !== null) {
                                      patchLine(line.productId, {
                                        costInput: formatMoneyInput(parsed),
                                      });
                                    }
                                  }}
                                  inputMode="decimal"
                                  placeholder="0.00"
                                  autoComplete="off"
                                  aria-invalid={costInvalid || undefined}
                                  aria-required
                                  aria-label={t("purchasing.costShort")}
                                  data-testid={`direct-line-cost-${line.productId}`}
                                />
                              </ExitsTableCell>
                              <ExitsTableCell cellAlign="text">
                                <div className="receive-stock-selling-cell">
                                  <span
                                    className="receive-stock-selling-readonly tabular-nums"
                                    aria-label={t("purchasing.sellingPriceShort")}
                                    data-testid={`direct-line-selling-${line.productId}`}
                                  >
                                    {sellingDisplay}
                                  </span>
                                  {marginLabel ? (
                                    <span
                                      className="receive-stock-margin-warning"
                                      title={marginLabel}
                                      role="img"
                                      aria-label={marginLabel}
                                      data-testid={`direct-line-margin-warning-${line.productId}`}
                                      data-margin={marginKind}
                                    >
                                      <AlertTriangle aria-hidden strokeWidth={2} />
                                    </span>
                                  ) : null}
                                </div>
                              </ExitsTableCell>
                              <ExitsTableCell
                                cellAlign="text"
                                className="receive-stock-receipt-table__expiry-col"
                              >
                                {line.tracksExpiration ? (
                                  <input
                                    type="date"
                                    className="exits-input receive-stock-expiry-input"
                                    value={line.expiryDate}
                                    onChange={(e) =>
                                      patchLine(line.productId, {
                                        expiryDate: e.target.value,
                                      })
                                    }
                                    aria-invalid={expiryInvalid || undefined}
                                    aria-required
                                    aria-label={t("purchasing.expiryDate")}
                                    data-testid={`direct-line-expiry-${line.productId}`}
                                  />
                                ) : (
                                  <span className="text-muted">—</span>
                                )}
                              </ExitsTableCell>
                              <ExitsTableCell cellAlign="text">
                                {line.tracksExpiration ? (
                                  <input
                                    className="exits-input receive-stock-lot-input"
                                    value={line.lotNumber}
                                    onChange={(e) =>
                                      patchLine(line.productId, {
                                        lotNumber: e.target.value,
                                      })
                                    }
                                    aria-label={t("purchasing.lotNumber")}
                                    data-testid={`direct-line-lot-${line.productId}`}
                                  />
                                ) : (
                                  <span className="text-muted">—</span>
                                )}
                              </ExitsTableCell>
                              <ExitsTableCell cellAlign="numeric">
                                <span className="font-semibold tabular-nums">
                                  {formatPeso(lineTotal)}
                                </span>
                              </ExitsTableCell>
                              <ExitsTableCell
                                cellAlign="center"
                                colSize="actions"
                                className="receive-stock-receipt-table__action-col"
                              >
                                <ExitsTableActions className="justify-center">
                                  <Button
                                    type="button"
                                    variant="destructive"
                                    size="icon"
                                    aria-label={t("purchasing.removeNamed").replace(
                                      "{name}",
                                      line.name,
                                    )}
                                    onClick={() => removeLine(line.productId)}
                                    data-testid={`direct-remove-${line.productId}`}
                                  >
                                    <Trash2 className="size-4" aria-hidden />
                                  </Button>
                                </ExitsTableActions>
                              </ExitsTableCell>
                            </ExitsTableRow>
                          );
                        })}
                      </ExitsTableBody>
                    </ExitsTable>
                  </ExitsTableContainer>
                  <div className="receive-stock-receipt__summary">
                    <div className="receive-stock-receipt__summary-row">
                      <span className="text-[length:var(--exits-text-sm)] text-muted">
                        {t("purchasing.receiptItems")}
                      </span>
                      <span className="text-[length:var(--exits-text-sm)] tabular-nums">
                        {lines.length}
                      </span>
                    </div>
                    <div className="receive-stock-receipt__summary-row">
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
                  </div>
                </>
              )}
            </Card>

            {finderOpen ? (
              <Card
                as="section"
                padding="compact"
                id={finderPanelId}
                className="receive-stock-section receive-stock-add receive-stock-finder"
                data-testid="direct-add-products"
                aria-labelledby="direct-add-products-heading"
              >
                <div className="receive-stock-section__header">
                  <h2
                    id="direct-add-products-heading"
                    className="receive-stock-section__title m-0"
                  >
                    {t("purchasing.findProducts")}
                  </h2>
                  <Button
                    type="button"
                    variant="ghost"
                    onClick={closeFinder}
                    data-testid="direct-close-finder"
                    aria-label={t("purchasing.closeFindProducts")}
                  >
                    <X className="size-4" aria-hidden />
                    {t("purchasing.closeFindProducts")}
                  </Button>
                </div>

                <div className="receive-stock-finder__filters">
                  {categories.length > 0 ? (
                    <ReceiveCategoryMultiSelect
                      categories={categoriesWithCounts}
                      selectedIds={categoryIds}
                      onChange={setCategoryIds}
                      label={t("purchasing.categories")}
                      placeholder={t("purchasing.categoriesPlaceholder")}
                      selectedCountLabel={(count) =>
                        t("purchasing.categoriesSelected").replace("{count}", String(count))
                      }
                      selectAllLabel={t("purchasing.selectAllCategories")}
                      deselectAllLabel={t("purchasing.deselectAllCategories")}
                      searchPlaceholder={t("catalog.searchCategories")}
                    />
                  ) : null}
                  <div className="receive-stock-finder__search-row">
                    <SearchField
                      id={productSearchInputId}
                      label={t("purchasing.productSearch")}
                      value={search}
                      onChange={(e) => setSearch(e.target.value)}
                      onClear={() => setSearch("")}
                      placeholder={t("purchasing.productSearch")}
                      testId="direct-product-search"
                      containerClassName="receive-stock-finder__search"
                    />
                    <FilterChip
                      selected={trackedOnly}
                      onClick={() => setTrackedOnly((prev) => !prev)}
                      data-testid="direct-tracking-filter-chip"
                      aria-label={t("purchasing.trackedOnly")}
                    >
                      {t("purchasing.trackedOnly")}
                    </FilterChip>
                  </div>
                </div>

                {productsQuery.isFetching ? <LoadingState label={t("loading.label")} /> : null}

                {!productsQuery.isFetching && productItems.length === 0 ? (
                  <EmptyState
                    align="center"
                    size="compact"
                    variant={hasActiveFilters ? "filtered" : "default"}
                    icon={<ClipboardList className="size-5" strokeWidth={1.75} />}
                    title={
                      hasActiveFilters
                        ? t("purchasing.noMatchingProducts")
                        : t("purchasing.noProducts")
                    }
                    detail={
                      hasActiveFilters
                        ? t("purchasing.noMatchingProductsDetail")
                        : t("purchasing.noProductsDetail")
                    }
                    action={
                      hasActiveFilters ? undefined : (
                        <Button asChild variant="secondary" data-testid="direct-add-new-product">
                          <Link to="/catalog/products/new">{t("purchasing.addNewProduct")}</Link>
                        </Button>
                      )
                    }
                    testId="direct-product-empty"
                  />
                ) : null}

                {!productsQuery.isFetching && productItems.length > 0 ? (
                  <ExitsTableContainer
                    data-testid="direct-product-results"
                    className="receive-stock-product-table"
                  >
                    <ExitsTable>
                      <ExitsTableHeader>
                        <ExitsTableRow>
                          <ExitsTableHead cellAlign="text">
                            {t("purchasing.receiveProduct")}
                          </ExitsTableHead>
                          <ExitsTableHead cellAlign="text">
                            {t("purchasing.category")}
                          </ExitsTableHead>
                          <ExitsTableHead cellAlign="text">
                            {t("purchasing.inventoryTracking")}
                          </ExitsTableHead>
                          <ExitsTableHead
                            cellAlign="center"
                            colSize="actions"
                            className="receive-stock-product-table__action-col"
                          >
                            {t("purchasing.action")}
                          </ExitsTableHead>
                        </ExitsTableRow>
                      </ExitsTableHeader>
                      <ExitsTableBody>
                        {pagedProductItems.map((product) => {
                          const notTracked = product.isTracked === false;
                          const categoryName =
                            product.categoryId != null
                              ? (categoryNameById.get(product.categoryId) ?? "—")
                              : "—";
                          return (
                            <ExitsTableRow
                              key={product.productId}
                              data-testid={`direct-product-${product.productId}`}
                            >
                              <ExitsTableCell cellAlign="text">
                                <div className="font-medium leading-snug">{product.name}</div>
                                {product.sku ? (
                                  <div className="text-[length:var(--exits-text-xs)] text-muted">
                                    {product.sku}
                                  </div>
                                ) : null}
                              </ExitsTableCell>
                              <ExitsTableCell cellAlign="text">{categoryName}</ExitsTableCell>
                              <ExitsTableCell cellAlign="text">
                                <StatusChip tone={notTracked ? "neutral" : "primary"}>
                                  {notTracked
                                    ? t("inventory.notTracked")
                                    : t("inventory.tracked")}
                                </StatusChip>
                              </ExitsTableCell>
                              <ExitsTableCell
                                cellAlign="center"
                                colSize="actions"
                                className="receive-stock-product-table__action-col"
                              >
                                <ExitsTableActions className="justify-center">
                                  <Button
                                    type="button"
                                    size="icon"
                                    shape="round"
                                    variant={notTracked ? "secondary" : "default"}
                                    onClick={() => addProductRow(product)}
                                    data-testid={`direct-add-${product.productId}`}
                                    aria-label={
                                      notTracked
                                        ? t("purchasing.inventoryTrackingRequired")
                                        : t("purchasing.addProduct")
                                    }
                                    title={
                                      notTracked
                                        ? t("purchasing.inventoryTrackingRequired")
                                        : t("purchasing.addProduct")
                                    }
                                  >
                                    <Plus className="size-4" aria-hidden />
                                  </Button>
                                </ExitsTableActions>
                              </ExitsTableCell>
                            </ExitsTableRow>
                          );
                        })}
                      </ExitsTableBody>
                    </ExitsTable>
                    <ExitsTablePagination
                      page={safeProductPage}
                      pageSize={productPageSize}
                      total={productTotal}
                      pageSizeOptions={PRODUCT_PAGE_SIZE_OPTIONS}
                      onPageChange={setProductPage}
                      onPageSizeChange={(size) => {
                        setProductPageSize(size);
                        setProductPage(1);
                      }}
                      rowsPerPageLabel={t("exitsTable.rowsPerPage")}
                      previousLabel={t("exitsTable.previous")}
                      nextLabel={t("exitsTable.next")}
                      rangeLabel={t("exitsTable.range")}
                    />
                  </ExitsTableContainer>
                ) : null}
              </Card>
            ) : null}
          </div>

          <div className="receive-stock-actions">
            <Button
              type="button"
              variant="destructive"
              onClick={() => navigate(pageBackNav.purchasing.to)}
              data-testid="direct-cancel"
            >
              {t("purchasing.cancel")}
            </Button>
            <Button
              type="button"
              disabled={reviewDisabled}
              onClick={startReview}
              data-testid="direct-review"
            >
              {t("purchasing.reviewDirect")}
              <ArrowRight className="size-4" aria-hidden />
            </Button>
          </div>

          <Card
            as="section"
            padding="compact"
            className="receive-stock-section receive-stock-history"
            data-testid="direct-recent-completed"
            aria-labelledby="direct-recent-completed-heading"
          >
            <div className="receive-stock-history__header">
              <h2
                id="direct-recent-completed-heading"
                className="receive-stock-section__title m-0 flex items-center gap-2"
              >
                <span>{t("purchasing.recentCompletedReceipts")}</span>
                {recentTotal > 0 ? <CountBadge count={recentTotal} tone="neutral" /> : null}
              </h2>
              <Button asChild variant="ghost" data-testid="direct-view-all-purchases">
                <Link to="/purchasing/direct-purchases">
                  {t("purchasing.viewAllDirectPurchases")}
                  <ArrowRight className="size-4" aria-hidden />
                </Link>
              </Button>
            </div>

            {recentCompletedQuery.isFetching ? (
              <LoadingState label={t("loading.label")} />
            ) : null}

            {!recentCompletedQuery.isFetching && recentItems.length === 0 ? (
              <EmptyState
                align="center"
                size="compact"
                icon={<ClipboardList className="size-5" strokeWidth={1.75} />}
                title={t("purchasing.completedReceiptsEmpty")}
                detail={t("purchasing.completedReceiptsEmptyDetail")}
                testId="direct-recent-empty"
              />
            ) : null}

            {!recentCompletedQuery.isFetching && recentItems.length > 0 ? (
              <ExitsTableContainer data-testid="direct-recent-table">
                <ExitsTable>
                  <ExitsTableHeader>
                    <ExitsTableRow>
                      <ExitsTableHead cellAlign="text">
                        {t("purchasing.directColReference")}
                      </ExitsTableHead>
                      <ExitsTableHead cellAlign="text">
                        {t("purchasing.directColDate")}
                      </ExitsTableHead>
                      <ExitsTableHead cellAlign="text">
                        {t("purchasing.directColSeller")}
                      </ExitsTableHead>
                      <ExitsTableHead cellAlign="text">
                        {t("purchasing.directColItems")}
                      </ExitsTableHead>
                      <ExitsTableHead cellAlign="numeric">
                        {t("purchasing.directColTotal")}
                      </ExitsTableHead>
                      <ExitsTableHead cellAlign="text">{t("purchasing.view")}</ExitsTableHead>
                    </ExitsTableRow>
                  </ExitsTableHeader>
                  <ExitsTableBody>
                    {recentItems.map((item) => (
                      <ExitsTableRow
                        key={`${item.sourceType}-${item.sourceId}`}
                        interactive
                        data-testid={`direct-recent-row-${item.sourceId}`}
                        onClick={() => navigate(historyRowHref(item))}
                      >
                        <ExitsTableCell cellAlign="text" className="font-medium">
                          {item.referenceNumber}
                        </ExitsTableCell>
                        <ExitsTableCell cellAlign="text">{formatHistoryDate(item)}</ExitsTableCell>
                        <ExitsTableCell cellAlign="text">{item.sellerDisplayName}</ExitsTableCell>
                        <ExitsTableCell cellAlign="text" className="tabular-nums">
                          {item.lineCount}
                        </ExitsTableCell>
                        <ExitsTableCell cellAlign="numeric" className="tabular-nums">
                          {formatPeso(item.totalAmount)}
                        </ExitsTableCell>
                        <ExitsTableCell cellAlign="text">
                          <Button
                            asChild
                            variant="ghost"
                            size="icon"
                            onClick={(e) => e.stopPropagation()}
                            data-testid={`direct-recent-view-${item.sourceId}`}
                          >
                            <Link to={historyRowHref(item)} aria-label={t("purchasing.view")}>
                              <ArrowRight className="size-4" aria-hidden />
                            </Link>
                          </Button>
                        </ExitsTableCell>
                      </ExitsTableRow>
                    ))}
                  </ExitsTableBody>
                </ExitsTable>
                <ExitsTableMobile>
                  {recentItems.map((item) => (
                    <ExitsTableMobileRow
                      key={`m-${item.sourceType}-${item.sourceId}`}
                      data-testid={`direct-recent-mobile-${item.sourceId}`}
                      onClick={() => navigate(historyRowHref(item))}
                    >
                      <div className="exits-table-mobile__title-row">
                        <p className="exits-table-mobile__title">{item.referenceNumber}</p>
                        <span className="tabular-nums font-semibold">
                          {formatPeso(item.totalAmount)}
                        </span>
                      </div>
                      <p className="exits-table-mobile__meta">
                        {formatHistoryDate(item)} · {item.sellerDisplayName}
                      </p>
                      <p className="exits-table-mobile__math">
                        {t("purchasing.directColItems")}: {item.lineCount}
                      </p>
                    </ExitsTableMobileRow>
                  ))}
                </ExitsTableMobile>
              </ExitsTableContainer>
            ) : null}
          </Card>
        </>
      ) : (
        <Card data-testid="direct-review-sheet" className="receive-stock-review" padding="compact">
          <Notice tone="info" testId="direct-review-notice">
            {t("purchasing.willIncreaseStock")}
          </Notice>
          {marginWarningLines.length > 0 ? (
            <Notice
              tone="warning"
              title={t("purchasing.sellingPriceNeedsReview")}
              testId="direct-review-margin-warning"
            >
              {marginWarningLines.length === 1
                ? t("purchasing.sellingPriceNeedsReviewDetail")
                : t("purchasing.sellingPriceNeedsReviewDetailMany").replace(
                    "{count}",
                    String(marginWarningLines.length),
                  )}
            </Notice>
          ) : null}
          <div className="receive-stock-review__meta">
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
              {t("purchasing.purchaseDate")}: {purchaseDate}
            </p>
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
              {t("purchasing.boughtFrom")}:{" "}
              {useOtherSource
                ? sourceName.trim() || t("purchasing.sourceEmpty")
                : suppliersQuery.data?.items.find((s) => s.supplierId === supplierId)?.name ||
                  t("purchasing.sourceEmpty")}
            </p>
            {referenceNumber.trim() ? (
              <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                {t("purchasing.reference")}: {referenceNumber.trim()}
              </p>
            ) : null}
            {notes.trim() ? (
              <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                {t("purchasing.notesOptional")}: {notes.trim()}
              </p>
            ) : null}
          </div>
          <h3 className="receive-stock-section__title m-0 flex items-center gap-2">
            <span>{t("purchasing.receiptItems")}</span>
            <CountBadge count={lines.length} tone="primary" />
          </h3>
          <ul className="m-0 flex list-none flex-col gap-2 p-0">
            {lines.map((line) => (
              <li key={line.productId} className="receive-stock-review__line">
                <span className="font-medium">{line.name}</span>
                <span className="tabular-nums text-muted">
                  {line.quantity} {line.uom} × {formatPeso(line.unitCost)}
                  {` · ${t("purchasing.sellingPriceShort")} ${formatPeso(line.sellingPrice)}`}
                </span>
                <span className="tabular-nums font-semibold">
                  {formatPeso(roundMoney(line.quantity * line.unitCost))}
                </span>
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
          <div className="receive-stock-actions receive-stock-actions--review">
            <Button
              type="button"
              variant="ghost"
              disabled={saving || statusLocked}
              onClick={() => setReviewing(false)}
              data-testid="direct-back-edit"
            >
              {t("returns.backToEdit")}
            </Button>
            <Button
              type="button"
              disabled={saving || statusLocked}
              onClick={() => void confirm()}
              data-testid="direct-confirm"
            >
              {saving ? t("purchasing.saving") : t("purchasing.receiveStockConfirm")}
            </Button>
          </div>
        </Card>
      )}
    </div>
  );
}
