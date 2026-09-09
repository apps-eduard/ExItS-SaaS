import { useEffect, useMemo, useRef, useState } from "react";
import { PackageMinus, Plus, Trash2 } from "lucide-react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { canManageInventory } from "@/access/pos-capabilities";
import {
  listCatalogProducts,
  getCatalogProduct,
  listCatalogCategories,
  listCatalogBrands,
} from "@/api/pos/pos-catalog-client";
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
import { ErrorState } from "@/components/exits/ErrorState";
import { ExitsChipBar } from "@/components/exits/ExitsChipBar";
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
import {
  canAddStockUseQuantity,
  convertDisplayBetweenWeightUnits,
  displayQtyFromCanonical,
  entryUnitLabel,
  isStockUseWeightProduct,
  parseStockUseEntryQuantity,
  type WeightInputUnit,
} from "@/features/inventory/stock-use-quantity";
import { useMediaMin } from "@/hooks/useMediaQuery";
import { useI18n } from "@/i18n/I18nProvider";
import { createSecureMutationId } from "@/lib/secure-mutation-id";
import { cn } from "@/lib/cn";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

type ProductFilter = "internal" | "all";

type DraftLine = {
  productId: string;
  name: string;
  uom: string;
  /** Canonical inventory quantity (kg for weight products). */
  quantity: number;
  available: number;
  isWeight: boolean;
  weightInputUnit: WeightInputUnit | null;
};

type PickerRow = {
  productId: string;
  name: string;
  uom: string;
  onHand: number;
  usageLabel: string;
  isTracked: boolean;
  isWeight: boolean;
};

export function StockUseCreatePage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const preselectProductId = searchParams.get("productId")?.trim() || null;
  const online = useBrowserOnline();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const allowManage = canManageInventory(sessionGrant);
  const isLargeScreen = useMediaMin(1024);

  const [reason, setReason] = useState<StockUseReasonCode>("InternalOperations");
  const [notes, setNotes] = useState("");
  const [search, setSearch] = useState("");
  const [debounced, setDebounced] = useState("");
  const [productFilter, setProductFilter] = useState<ProductFilter>("internal");
  const [categoryId, setCategoryId] = useState("");
  const [brandId, setBrandId] = useState("");
  const [lines, setLines] = useState<DraftLine[]>([]);
  const [qtyByProduct, setQtyByProduct] = useState<Record<string, string>>({});
  const [weightUnitByProduct, setWeightUnitByProduct] = useState<
    Record<string, WeightInputUnit>
  >({});
  const [focusedQtyProductId, setFocusedQtyProductId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [statusLocked, setStatusLocked] = useState(false);
  const stockUseIdRef = useRef<string | null>(null);
  const preselectDoneRef = useRef(false);
  const productsScrollRef = useRef<HTMLDivElement | null>(null);

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
        { search: debounced || undefined, pageSize: 40, tracked: true },
        signal,
      ),
  });

  const categoriesQuery = useQuery({
    queryKey: ["catalog-categories", "stock-use-picker", workspace?.organizationId],
    enabled: Boolean(workspace) && online && allowManage,
    queryFn: ({ signal }) =>
      listCatalogCategories(workspace!, { status: "Active", pageSize: 100 }, signal),
  });

  const brandsQuery = useQuery({
    queryKey: ["catalog-brands", "stock-use-picker", workspace?.organizationId],
    enabled: Boolean(workspace) && online && allowManage,
    queryFn: ({ signal }) =>
      listCatalogBrands(workspace!, { status: "Active", pageSize: 100 }, signal),
  });

  const catalogQuery = useQuery({
    queryKey: [
      "catalog-products",
      "stock-use-picker",
      workspace?.organizationId,
      debounced,
      productFilter,
      categoryId,
      brandId,
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
          categoryId: categoryId || undefined,
          brandId: brandId || undefined,
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

    const ids = new Set<string>([...inventoryById.keys(), ...catalogById.keys()]);

    const rows: PickerRow[] = [];
    for (const productId of ids) {
      const inv = inventoryById.get(productId);
      const cat = catalogById.get(productId);
      const isTracked = inv?.isTracked ?? cat?.isTracked === true;
      if (!isTracked) {
        continue;
      }

      if (categoryId || brandId) {
        if (!cat) {
          continue;
        }
        if (categoryId && cat.categoryId !== categoryId) {
          continue;
        }
        if (brandId && cat.brandId !== brandId) {
          continue;
        }
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
        isWeight: isStockUseWeightProduct({
          unitOfMeasure: uom,
          sellingMode: cat?.sellingMode,
        }),
      });
    }

    rows.sort((a, b) => a.name.localeCompare(b.name));
    return rows;
  }, [
    inventoryQuery.data?.items,
    catalogQuery.data?.items,
    productFilter,
    categoryId,
    brandId,
    t,
  ]);

  const selectedIds = useMemo(() => new Set(lines.map((l) => l.productId)), [lines]);

  const availablePickerRows = useMemo(
    () => pickerRows.filter((row) => !selectedIds.has(row.productId)),
    [pickerRows, selectedIds],
  );

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
        const uom = cat?.unitOfMeasure ?? inv.unitOfMeasure;
        const isWeight = isStockUseWeightProduct({
          unitOfMeasure: uom,
          sellingMode: cat?.sellingMode,
        });
        setLines((prev) => {
          if (prev.some((l) => l.productId === inv.productId)) {
            return prev;
          }
          return [
            ...prev,
            {
              productId: inv.productId,
              name: cat?.name ?? inv.name,
              uom,
              quantity: 0,
              available: inv.onHandQuantity,
              isWeight,
              weightInputUnit: isWeight ? "kg" : null,
            },
          ];
        });
        setQtyByProduct((prev) => ({ ...prev, [inv.productId]: "" }));
        if (isWeight) {
          setWeightUnitByProduct((prev) => ({ ...prev, [inv.productId]: "kg" }));
        }
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

  if (!allowManage) {
    return (
      <div className="exits-page flex min-w-0 flex-col gap-3" data-testid="stock-use-create-denied">
        <PageHeader
          title={t("stockUse.recordTitle")}
          backTo="/inventory/stock-use"
          backLabel={t("stockUse.backList")}
          backTestId="page-header-back-stock-use"
        />
        <ErrorState title={t("stockUse.errorTitle")} detail={t("stockUse.manageDenied")} />
      </div>
    );
  }

  function weightUnitFor(productId: string, isWeight: boolean): WeightInputUnit {
    if (!isWeight) {
      return "kg";
    }
    return weightUnitByProduct[productId] ?? "kg";
  }

  function clearPendingEntry(productId: string) {
    setQtyByProduct((prev) => {
      const next = { ...prev };
      delete next[productId];
      return next;
    });
    setWeightUnitByProduct((prev) => {
      const next = { ...prev };
      delete next[productId];
      return next;
    });
    setFocusedQtyProductId((current) => (current === productId ? null : current));
  }

  function addFromPicker(row: PickerRow) {
    setError(null);
    if (selectedIds.has(row.productId)) {
      return;
    }
    const weightUnit = weightUnitFor(row.productId, row.isWeight);
    const raw = qtyByProduct[row.productId] ?? "";
    if (
      !canAddStockUseQuantity({
        raw,
        isWeight: row.isWeight,
        weightUnit,
        available: row.onHand,
      })
    ) {
      const parsed = parseStockUseEntryQuantity({
        raw,
        isWeight: row.isWeight,
        weightUnit,
      });
      if (!parsed.ok) {
        if (parsed.reason !== "blank") {
          setError(t("stockUse.invalidQuantity"));
        }
        return;
      }
      setError(
        t("stockUse.onlyAvailable").replace(
          "{quantity}",
          `${row.onHand} ${row.isWeight ? "kg" : row.uom}`.trim(),
        ),
      );
      return;
    }

    const parsed = parseStockUseEntryQuantity({
      raw,
      isWeight: row.isWeight,
      weightUnit,
    });
    if (!parsed.ok) {
      return;
    }

    setLines((prev) => [
      ...prev.filter((l) => l.productId !== row.productId),
      {
        productId: row.productId,
        name: row.name,
        uom: row.uom,
        quantity: parsed.quantity,
        available: row.onHand,
        isWeight: row.isWeight,
        weightInputUnit: row.isWeight ? weightUnit : null,
      },
    ]);
    setQtyByProduct((prev) => ({
      ...prev,
      [row.productId]: displayQtyFromCanonical({
        quantity: parsed.quantity,
        isWeight: row.isWeight,
        weightUnit,
      }),
    }));
    if (row.isWeight) {
      setWeightUnitByProduct((prev) => ({ ...prev, [row.productId]: weightUnit }));
    }
    productsScrollRef.current?.scrollTo({ top: 0 });
  }

  function updateLineQty(productId: string, raw: string) {
    setQtyByProduct((prev) => ({ ...prev, [productId]: raw }));
    const line = lines.find((l) => l.productId === productId);
    if (!line) {
      return;
    }
    const weightUnit = weightUnitFor(productId, line.isWeight);
    const parsed = parseStockUseEntryQuantity({
      raw,
      isWeight: line.isWeight,
      weightUnit,
    });
    if (!parsed.ok) {
      return;
    }
    setLines((prev) =>
      prev.map((item) =>
        item.productId === productId ? { ...item, quantity: parsed.quantity } : item,
      ),
    );
  }

  function updateLineWeightUnit(productId: string, nextUnit: WeightInputUnit) {
    const line = lines.find((l) => l.productId === productId);
    const currentUnit = weightUnitFor(productId, true);
    const currentRaw = qtyByProduct[productId] ?? "";
    const converted = convertDisplayBetweenWeightUnits(currentRaw, currentUnit, nextUnit);
    setWeightUnitByProduct((prev) => ({ ...prev, [productId]: nextUnit }));
    setQtyByProduct((prev) => ({ ...prev, [productId]: converted }));
    if (!line) {
      return;
    }
    const parsed = parseStockUseEntryQuantity({
      raw: converted,
      isWeight: true,
      weightUnit: nextUnit,
    });
    if (!parsed.ok) {
      setLines((prev) =>
        prev.map((item) =>
          item.productId === productId ? { ...item, weightInputUnit: nextUnit } : item,
        ),
      );
      return;
    }
    setLines((prev) =>
      prev.map((item) =>
        item.productId === productId
          ? { ...item, quantity: parsed.quantity, weightInputUnit: nextUnit }
          : item,
      ),
    );
  }

  function updatePickerWeightUnit(productId: string, nextUnit: WeightInputUnit) {
    const currentUnit = weightUnitFor(productId, true);
    const currentRaw = qtyByProduct[productId] ?? "";
    const converted = convertDisplayBetweenWeightUnits(currentRaw, currentUnit, nextUnit);
    setWeightUnitByProduct((prev) => ({ ...prev, [productId]: nextUnit }));
    setQtyByProduct((prev) => ({ ...prev, [productId]: converted }));
  }

  function removeLine(productId: string) {
    setLines((prev) => prev.filter((l) => l.productId !== productId));
    clearPendingEntry(productId);
  }

  function inlineQtyHint(input: {
    productId: string;
    raw: string;
    isWeight: boolean;
    available: number;
    uom: string;
  }): string | null {
    const weightUnit = weightUnitFor(input.productId, input.isWeight);
    const trimmed = input.raw.trim();
    if (trimmed === "") {
      return null;
    }
    const parsed = parseStockUseEntryQuantity({
      raw: input.raw,
      isWeight: input.isWeight,
      weightUnit,
    });
    if (!parsed.ok) {
      return t("stockUse.invalidQuantity");
    }
    if (parsed.quantity > input.available) {
      return t("stockUse.onlyAvailable").replace(
        "{quantity}",
        `${input.available} ${input.isWeight ? "kg" : input.uom}`.trim(),
      );
    }
    return null;
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
            `${line.available} ${line.isWeight ? "kg" : line.uom}`.trim(),
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

  const productRowClass =
    "stock-use-product-row flex flex-col gap-2 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 py-2.5 sm:flex-row sm:items-center sm:justify-between sm:gap-2";
  const actionIconClass = "stock-use-action-btn shrink-0";
  const actionIconSoftClass = "stock-use-action-btn stock-use-action-btn--soft shrink-0";
  const qtyInputClass = "exits-input stock-use-qty-input tabular-nums";
  const pickerLoading = inventoryQuery.isLoading || catalogQuery.isLoading;

  function renderWeightUnitSegment(input: {
    productId: string;
    value: WeightInputUnit;
    visible: boolean;
    disabled?: boolean;
    onChange: (unit: WeightInputUnit) => void;
  }) {
    return (
      <div
        className={cn(
          "stock-use-weight-unit",
          input.visible ? "stock-use-weight-unit--visible" : "stock-use-weight-unit--reserved",
        )}
        role="radiogroup"
        aria-label={t("sell.weightUnit")}
        aria-hidden={!input.visible}
        data-testid={`stock-use-weight-unit-${input.productId}`}
      >
        {(["kg", "g"] as const).map((code) => (
          <button
            key={code}
            type="button"
            role="radio"
            aria-checked={input.value === code}
            className={cn(
              "stock-use-weight-unit__option",
              input.value === code && "stock-use-weight-unit__option--active",
            )}
            disabled={input.disabled || !input.visible}
            tabIndex={input.visible ? 0 : -1}
            onMouseDown={(e) => e.preventDefault()}
            onClick={() => input.onChange(code)}
            data-testid={`stock-use-weight-unit-${code}-${input.productId}`}
          >
            {code}
          </button>
        ))}
      </div>
    );
  }

  function renderQtyEntry(input: {
    productId: string;
    isWeight: boolean;
    uom: string;
    available: number;
    value: string;
    disabled?: boolean;
    testId: string;
    onChange: (raw: string) => void;
    onWeightUnitChange?: (unit: WeightInputUnit) => void;
  }) {
    const weightUnit = weightUnitFor(input.productId, input.isWeight);
    const showWeightUnit =
      input.isWeight &&
      (focusedQtyProductId === input.productId || weightUnit === "g" || input.value.trim() !== "");
    const hint = inlineQtyHint({
      productId: input.productId,
      raw: input.value,
      isWeight: input.isWeight,
      available: input.available,
      uom: input.uom,
    });
    const unitLabel = entryUnitLabel({
      isWeight: input.isWeight,
      weightUnit,
      uom: input.uom,
    });

    return (
      <div className="stock-use-qty-entry">
        <div className="stock-use-qty-entry__controls">
          <input
            type="number"
            min={0}
            step="any"
            className={qtyInputClass}
            value={input.value}
            placeholder=""
            onChange={(e) => input.onChange(e.target.value)}
            onFocus={() => setFocusedQtyProductId(input.productId)}
            onBlur={() =>
              setFocusedQtyProductId((current) =>
                current === input.productId ? null : current,
              )
            }
            disabled={input.disabled}
            aria-label={t("stockUse.quantityUsed")}
            data-testid={input.testId}
          />
          {input.isWeight ? (
            renderWeightUnitSegment({
              productId: input.productId,
              value: weightUnit,
              visible: showWeightUnit,
              disabled: input.disabled,
              onChange: (unit) => input.onWeightUnitChange?.(unit),
            })
          ) : (
            <span className="stock-use-qty-entry__uom" aria-hidden>
              {unitLabel}
            </span>
          )}
        </div>
        {hint ? (
          <p className="stock-use-qty-entry__hint m-0" role="status">
            {hint}
          </p>
        ) : null}
      </div>
    );
  }

  return (
    <div
      className="stock-use-create-page exits-page mx-auto flex h-full min-h-0 w-full max-w-[80rem] min-w-0 flex-col gap-2.5 overflow-hidden"
      data-testid="stock-use-create-page"
    >
      <div className="stock-use-create-chrome flex shrink-0 min-w-0 flex-col gap-2.5">
        <PageHeader
          title={t("stockUse.recordTitle")}
          description={t("stockUse.notASale")}
          backTo="/inventory/stock-use"
          backLabel={t("stockUse.backList")}
          backTestId="page-header-back-stock-use"
        />

        {!online ? (
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{t("stockUse.offline")}</p>
        ) : null}

        {error ? (
          <p
            className="m-0 text-[length:var(--exits-text-sm)] text-danger"
            role="alert"
            data-testid="stock-use-create-error"
          >
            {error}
          </p>
        ) : null}

        <section
          className="catalog-form-section stock-use-section exits-animate-panel"
          data-testid="stock-use-create-fields"
        >
          <div className="stock-use-create-meta grid min-w-0 grid-cols-1 gap-2.5 sm:grid-cols-[minmax(12rem,18rem)_minmax(0,1fr)] sm:items-start">
            <label className="flex min-w-0 flex-col gap-1">
              <span className="text-[length:var(--exits-text-sm)] font-medium text-muted">
                {t("stockUse.reason")}
              </span>
              <select
                className="exits-select catalog-form-select"
                value={reason}
                onChange={(e) => setReason(e.target.value as StockUseReasonCode)}
                disabled={statusLocked}
                data-testid="stock-use-reason"
              >
                {STOCK_USE_REASONS.map((code) => (
                  <option key={code} value={code}>
                    {t(stockUseReasonLabelKey(code))}
                  </option>
                ))}
              </select>
            </label>

            <label className="flex min-w-0 flex-col gap-1">
              <span className="text-[length:var(--exits-text-sm)] font-medium text-muted">
                {t("stockUse.notes")}{" "}
                <span className="font-normal">({t("stockUse.notesOptional")})</span>
              </span>
              <textarea
                className="exits-input stock-use-notes-input"
                value={notes}
                onChange={(e) => setNotes(e.target.value)}
                disabled={statusLocked}
                placeholder={t("stockUse.notesOptional")}
                maxLength={512}
                rows={1}
                data-testid="stock-use-notes"
              />
            </label>
          </div>
        </section>
      </div>

      <section
        className="catalog-form-section stock-use-section stock-use-section--products exits-animate-panel"
        data-testid="stock-use-draft-lines"
      >
        <div className="flex shrink-0 flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
          <h2 className="catalog-form-section__title m-0 flex min-w-0 items-baseline gap-2">
            <span>{t("stockUse.usedStock")}</span>
            {lines.length > 0 ? (
              <span className="text-[length:var(--exits-text-sm)] font-medium text-muted">
                ({lines.length})
              </span>
            ) : null}
          </h2>
          <Button
            type="button"
            variant="default"
            className="w-full shrink-0 sm:w-auto"
            disabled={!online || saving || statusLocked || lines.length === 0}
            onClick={() => void submit()}
            data-testid="stock-use-submit"
          >
            <PackageMinus className="size-4 shrink-0" aria-hidden />
            {saving ? t("stockUse.recording") : t("stockUse.recordUse")}
          </Button>
        </div>

        <div
          ref={productsScrollRef}
          className="stock-use-create-products-scroll"
          data-testid="stock-use-products-scroll"
        >
          {lines.length === 0 ? (
            <p className="stock-use-empty m-0">{t("stockUse.draftEmpty")}</p>
          ) : isLargeScreen ? (
            <div
              className="stock-use-create-table-shell min-w-0 overflow-x-auto"
              data-testid="stock-use-selected-table"
            >
              <table className="stock-use-create-table w-full min-w-[36rem] border-collapse text-left text-[length:var(--exits-text-sm)]">
                <thead>
                  <tr className="stock-use-create-table__head border-b border-border">
                    <th className="whitespace-nowrap px-3 py-2.5 text-[length:var(--exits-text-xs)] font-medium text-muted">
                      {t("stockCount.product")}
                    </th>
                    <th className="whitespace-nowrap px-3 py-2.5 text-right text-[length:var(--exits-text-xs)] font-medium text-muted">
                      {t("stockUse.available")}
                    </th>
                    <th className="whitespace-nowrap px-3 py-2.5 text-right text-[length:var(--exits-text-xs)] font-medium text-muted">
                      {t("stockUse.quantityUsed")}
                    </th>
                    <th className="whitespace-nowrap px-3 py-2.5 text-right text-[length:var(--exits-text-xs)] font-medium text-muted">
                      {t("catalog.col.actions")}
                    </th>
                  </tr>
                </thead>
                <tbody>
                  {lines.map((line) => {
                    const weightUnit = weightUnitFor(line.productId, line.isWeight);
                    const displayUom = entryUnitLabel({
                      isWeight: line.isWeight,
                      weightUnit,
                      uom: line.uom,
                    });
                    return (
                      <tr
                        key={line.productId}
                        className="stock-use-create-table__row border-b border-border"
                        data-testid={`stock-use-line-${line.productId}`}
                      >
                        <td className="max-w-[18rem] px-3 py-2.5 align-middle">
                          <span className="block truncate font-semibold text-foreground">
                            {line.name}
                          </span>
                          <span className="mt-0.5 block text-[length:var(--exits-text-xs)] text-muted">
                            {line.isWeight ? "kg" : line.uom}
                          </span>
                        </td>
                        <td className="whitespace-nowrap px-3 py-2.5 align-middle text-right tabular-nums text-muted">
                          {line.available}
                          {line.isWeight ? " kg" : ""}
                        </td>
                        <td className="px-3 py-2.5 align-middle">
                          <div className="flex justify-end">
                            {renderQtyEntry({
                              productId: line.productId,
                              isWeight: line.isWeight,
                              uom: line.uom,
                              available: line.available,
                              value:
                                qtyByProduct[line.productId] ??
                                (line.quantity > 0
                                  ? displayQtyFromCanonical({
                                      quantity: line.quantity,
                                      isWeight: line.isWeight,
                                      weightUnit,
                                    })
                                  : ""),
                              disabled: statusLocked,
                              testId: `stock-use-line-qty-${line.productId}`,
                              onChange: (raw) => updateLineQty(line.productId, raw),
                              onWeightUnitChange: (unit) =>
                                updateLineWeightUnit(line.productId, unit),
                            })}
                          </div>
                          {line.isWeight && line.quantity > 0 ? (
                            <p className="m-0 mt-1 text-right text-[length:var(--exits-text-xs)] text-muted">
                              {displayQtyFromCanonical({
                                quantity: line.quantity,
                                isWeight: true,
                                weightUnit,
                              })}{" "}
                              {displayUom}
                            </p>
                          ) : null}
                        </td>
                        <td className="px-3 py-2.5 align-middle">
                          <div className="flex justify-end">
                            <Button
                              type="button"
                              variant="outline"
                              size="icon"
                              className={actionIconSoftClass}
                              aria-label={t("stockUse.removeLine")}
                              onClick={() => removeLine(line.productId)}
                              disabled={statusLocked}
                              data-testid={`stock-use-remove-${line.productId}`}
                            >
                              <Trash2 className="size-4 shrink-0" aria-hidden />
                            </Button>
                          </div>
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          ) : (
            <ul className="m-0 flex list-none flex-col gap-1.5 p-0">
              {lines.map((line) => {
                const weightUnit = weightUnitFor(line.productId, line.isWeight);
                return (
                  <li
                    key={line.productId}
                    className={productRowClass}
                    data-testid={`stock-use-line-${line.productId}`}
                  >
                    <div className="min-w-0 flex-1">
                      <p className="m-0 font-medium leading-snug">{line.name}</p>
                      <p className="m-0 mt-0.5 text-[length:var(--exits-text-sm)] text-muted">
                        {line.isWeight ? "kg" : line.uom} · {t("stockUse.available")}:{" "}
                        {line.available}
                        {line.isWeight ? " kg" : ""}
                      </p>
                      <div className="mt-2">
                        {renderQtyEntry({
                          productId: line.productId,
                          isWeight: line.isWeight,
                          uom: line.uom,
                          available: line.available,
                          value:
                            qtyByProduct[line.productId] ??
                            (line.quantity > 0
                              ? displayQtyFromCanonical({
                                  quantity: line.quantity,
                                  isWeight: line.isWeight,
                                  weightUnit,
                                })
                              : ""),
                          disabled: statusLocked,
                          testId: `stock-use-line-qty-${line.productId}`,
                          onChange: (raw) => updateLineQty(line.productId, raw),
                          onWeightUnitChange: (unit) =>
                            updateLineWeightUnit(line.productId, unit),
                        })}
                      </div>
                    </div>
                    <Button
                      type="button"
                      variant="outline"
                      size="icon"
                      className={actionIconSoftClass}
                      aria-label={t("stockUse.removeLine")}
                      onClick={() => removeLine(line.productId)}
                      disabled={statusLocked}
                      data-testid={`stock-use-remove-${line.productId}`}
                    >
                      <Trash2 className="size-4 shrink-0" aria-hidden />
                    </Button>
                  </li>
                );
              })}
            </ul>
          )}

          <div className="stock-use-picker flex min-w-0 flex-col gap-2 border-t border-border pt-2.5">
            <div className="stock-use-create-toolbar flex min-w-0 flex-col gap-2">
              <h3 className="m-0 text-[length:var(--exits-text-sm)] font-semibold text-foreground">
                {t("stockUse.addProduct")}
              </h3>

              <div className="stock-use-create-filters flex min-w-0 flex-col gap-2 sm:flex-row sm:flex-wrap sm:items-center">
                <SearchField
                  label={t("stockUse.searchProducts")}
                  value={search}
                  onChange={(e) => setSearch(e.target.value)}
                  onClear={() => setSearch("")}
                  placeholder={t("stockUse.searchProducts")}
                  containerClassName="min-w-0 w-full sm:min-w-[12rem] sm:flex-1 sm:basis-[14rem]"
                  data-testid="stock-use-product-search"
                />
                <ExitsChipBar
                  variant="filter"
                  ariaLabel={t("stockUse.addProduct")}
                  testId="stock-use-product-filter"
                  className="shrink-0"
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
                <label className="flex min-w-0 flex-1 basis-[10rem] flex-col gap-1 sm:max-w-[14rem]">
                  <span className="sr-only">{t("catalog.category")}</span>
                  <select
                    className="exits-select catalog-form-select"
                    value={categoryId}
                    onChange={(e) => setCategoryId(e.target.value)}
                    disabled={statusLocked}
                    data-testid="stock-use-filter-category"
                  >
                    <option value="">{t("catalog.allCategories")}</option>
                    {(categoriesQuery.data?.items ?? []).map((category) => (
                      <option key={category.categoryId} value={category.categoryId}>
                        {category.name}
                      </option>
                    ))}
                  </select>
                </label>
                <label className="flex min-w-0 flex-1 basis-[10rem] flex-col gap-1 sm:max-w-[14rem]">
                  <span className="sr-only">{t("catalog.brand")}</span>
                  <select
                    className="exits-select catalog-form-select"
                    value={brandId}
                    onChange={(e) => setBrandId(e.target.value)}
                    disabled={statusLocked}
                    data-testid="stock-use-filter-brand"
                  >
                    <option value="">{t("catalog.allBrands")}</option>
                    {(brandsQuery.data?.items ?? []).map((brand) => (
                      <option key={brand.brandId} value={brand.brandId}>
                        {brand.name}
                      </option>
                    ))}
                  </select>
                </label>
              </div>
            </div>

            {pickerLoading ? <LoadingState label={t("stockUse.loading")} /> : null}

            {!pickerLoading && availablePickerRows.length === 0 ? (
              <p className="stock-use-empty m-0">{t("stockUse.noProductsDetail")}</p>
            ) : null}

            {availablePickerRows.length > 0 ? (
              isLargeScreen ? (
                <div
                  className="stock-use-create-table-shell min-w-0 overflow-x-auto"
                  data-testid="stock-use-product-picker"
                >
                  <table className="stock-use-create-table w-full min-w-[40rem] border-collapse text-left text-[length:var(--exits-text-sm)]">
                    <thead>
                      <tr className="stock-use-create-table__head border-b border-border">
                        <th className="whitespace-nowrap px-3 py-2.5 text-[length:var(--exits-text-xs)] font-medium text-muted">
                          {t("stockCount.product")}
                        </th>
                        <th className="whitespace-nowrap px-3 py-2.5 text-[length:var(--exits-text-xs)] font-medium text-muted">
                          {t("catalog.col.scope")}
                        </th>
                        <th className="whitespace-nowrap px-3 py-2.5 text-right text-[length:var(--exits-text-xs)] font-medium text-muted">
                          {t("stockUse.available")}
                        </th>
                        <th className="whitespace-nowrap px-3 py-2.5 text-right text-[length:var(--exits-text-xs)] font-medium text-muted">
                          {t("stockUse.quantityUsed")}
                        </th>
                        <th className="whitespace-nowrap px-3 py-2.5 text-right text-[length:var(--exits-text-xs)] font-medium text-muted">
                          {t("catalog.col.actions")}
                        </th>
                      </tr>
                    </thead>
                    <tbody>
                      {availablePickerRows.map((row) => {
                        const weightUnit = weightUnitFor(row.productId, row.isWeight);
                        const raw = qtyByProduct[row.productId] ?? "";
                        const canAdd = canAddStockUseQuantity({
                          raw,
                          isWeight: row.isWeight,
                          weightUnit,
                          available: row.onHand,
                        });
                        return (
                          <tr
                            key={row.productId}
                            className="stock-use-create-table__row border-b border-border"
                          >
                            <td className="max-w-[16rem] px-3 py-2.5 align-middle">
                              <span className="block truncate font-semibold text-foreground">
                                {row.name}
                              </span>
                              <span className="mt-0.5 block text-[length:var(--exits-text-xs)] text-muted">
                                {row.isWeight ? "kg" : row.uom}
                              </span>
                            </td>
                            <td className="whitespace-nowrap px-3 py-2.5 align-middle text-muted">
                              {row.usageLabel}
                            </td>
                            <td className="whitespace-nowrap px-3 py-2.5 align-middle text-right tabular-nums text-muted">
                              {row.onHand}
                              {row.isWeight ? " kg" : ""}
                            </td>
                            <td className="px-3 py-2.5 align-middle">
                              <div className="flex justify-end">
                                {renderQtyEntry({
                                  productId: row.productId,
                                  isWeight: row.isWeight,
                                  uom: row.uom,
                                  available: row.onHand,
                                  value: raw,
                                  disabled: statusLocked,
                                  testId: `stock-use-picker-qty-${row.productId}`,
                                  onChange: (value) =>
                                    setQtyByProduct((prev) => ({
                                      ...prev,
                                      [row.productId]: value,
                                    })),
                                  onWeightUnitChange: (unit) =>
                                    updatePickerWeightUnit(row.productId, unit),
                                })}
                              </div>
                            </td>
                            <td className="px-3 py-2.5 align-middle">
                              <div className="flex justify-end">
                                <Button
                                  type="button"
                                  variant="default"
                                  size="icon"
                                  className={actionIconClass}
                                  disabled={!online || statusLocked || row.onHand <= 0 || !canAdd}
                                  aria-label={t("stockUse.addProduct")}
                                  onClick={() => addFromPicker(row)}
                                  data-testid={`stock-use-add-${row.productId}`}
                                >
                                  <Plus className="size-4 shrink-0" aria-hidden />
                                </Button>
                              </div>
                            </td>
                          </tr>
                        );
                      })}
                    </tbody>
                  </table>
                </div>
              ) : (
                <ul
                  className="m-0 flex list-none flex-col gap-1.5 p-0"
                  data-testid="stock-use-product-picker"
                >
                  {availablePickerRows.map((row) => {
                    const weightUnit = weightUnitFor(row.productId, row.isWeight);
                    const raw = qtyByProduct[row.productId] ?? "";
                    const canAdd = canAddStockUseQuantity({
                      raw,
                      isWeight: row.isWeight,
                      weightUnit,
                      available: row.onHand,
                    });
                    return (
                      <li key={row.productId} className={productRowClass}>
                        <div className="min-w-0 flex-1">
                          <p className="m-0 font-medium leading-snug">{row.name}</p>
                          <p className="m-0 mt-0.5 text-[length:var(--exits-text-sm)] text-muted">
                            {row.usageLabel} · {t("stockUse.available")}: {row.onHand}{" "}
                            {row.isWeight ? "kg" : row.uom}
                          </p>
                          <div className="mt-2">
                            {renderQtyEntry({
                              productId: row.productId,
                              isWeight: row.isWeight,
                              uom: row.uom,
                              available: row.onHand,
                              value: raw,
                              disabled: statusLocked,
                              testId: `stock-use-picker-qty-${row.productId}`,
                              onChange: (value) =>
                                setQtyByProduct((prev) => ({
                                  ...prev,
                                  [row.productId]: value,
                                })),
                              onWeightUnitChange: (unit) =>
                                updatePickerWeightUnit(row.productId, unit),
                            })}
                          </div>
                        </div>
                        <Button
                          type="button"
                          variant="default"
                          size="icon"
                          className={actionIconClass}
                          disabled={!online || statusLocked || row.onHand <= 0 || !canAdd}
                          aria-label={t("stockUse.addProduct")}
                          onClick={() => addFromPicker(row)}
                          data-testid={`stock-use-add-${row.productId}`}
                        >
                          <Plus className="size-4 shrink-0" aria-hidden />
                        </Button>
                      </li>
                    );
                  })}
                </ul>
              )
            ) : null}
          </div>
        </div>
      </section>
    </div>
  );
}
