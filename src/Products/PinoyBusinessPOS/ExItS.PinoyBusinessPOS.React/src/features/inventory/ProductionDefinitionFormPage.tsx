import { useEffect, useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { Check } from "lucide-react";
import { canManageInventory } from "@/access/pos-capabilities";
import { listCatalogProducts, getCatalogProduct, updateCatalogProduct } from "@/api/pos/pos-catalog-client";
import type { PosCatalogProductDto } from "@/api/pos/pos-catalog-types";
import {
  DEFAULT_CATALOG_UNIT_OF_MEASURE,
  type PosUnitOfMeasureCode,
} from "@/api/pos/pos-catalog-options";
import { PosApiError } from "@/api/pos/pos-http";
import {
  createProductionDefinition,
  getProductionDefinition,
  updateProductionDefinition,
} from "@/api/pos/pos-production-client";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { StickyActionBar } from "@/components/exits/FoundationStates";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { SearchField } from "@/components/exits/SearchField";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { ProductionMaterialQuantitySheet } from "@/features/inventory/ProductionMaterialQuantitySheet";
import { ProductionRecipeOutputSheet } from "@/features/inventory/ProductionRecipeOutputSheet";
import {
  activeMaterialUnits,
  draftQuantityLine,
  formatMaterialAvailableCaption,
  isEligibleProductionMaterial,
  isWeightMaterial,
  materialBaseUomLabel,
  normalizeMaterialQuantityInput,
  resolveMaterialEntryMode,
  type ProductionMaterialDraft,
} from "@/features/inventory/production-material-uom";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";
import type { WeightInputUnit } from "@/cart/sell-cart-helpers";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

type OutputPickerTab = "produced" | "all";

function isEligibleProductionOutput(product: PosCatalogProductDto, tab: OutputPickerTab): boolean {
  if (product.isProduced !== true) {
    return false;
  }
  void tab;
  return true;
}

const YIELD_UOM_OPTIONS: PosUnitOfMeasureCode[] = [
  "Piece",
  "Kilogram",
  "Gram",
  "Liter",
  "Pack",
  "Box",
];

async function ensureCanBeUsedAsIngredient(
  workspace: { organizationId: string; branchId: string },
  product: PosCatalogProductDto,
): Promise<PosCatalogProductDto> {
  if (product.canBeUsedAsIngredient === true) {
    return product;
  }
  return updateCatalogProduct(workspace, product.productId, {
    name: product.name,
    unitOfMeasure: product.unitOfMeasure,
    sellingPrice: product.sellingPrice,
    description: product.description ?? null,
    sku: product.sku ?? null,
    barcode: product.barcode ?? null,
    categoryId: product.categoryId ?? null,
    brandId: product.brandId ?? null,
    sellingMode: product.sellingMode,
    canBeSold: product.canBeSold ?? true,
    canBeUsedAsIngredient: true,
    isProduced: product.isProduced ?? false,
    expectedUpdatedAtUtc: product.updatedAtUtc,
    tracksExpiration: product.tracksExpiration ?? false,
    expirationWarningDays: product.expirationWarningDays ?? null,
  });
}

export function ProductionDefinitionFormPage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const { definitionId } = useParams<{ definitionId: string }>();
  const isEdit = Boolean(definitionId);
  const online = useBrowserOnline();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const allowManage = canManageInventory(sessionGrant);

  const [name, setName] = useState("");
  const [outputProductId, setOutputProductId] = useState<string | null>(null);
  const [outputProduct, setOutputProduct] = useState<PosCatalogProductDto | null>(null);
  const [outputName, setOutputName] = useState("");
  const [outputQuantityRaw, setOutputQuantityRaw] = useState(isEdit ? "1" : "100");
  const [outputProductUnitId, setOutputProductUnitId] = useState<string | null>(null);
  const [outputWeightUnit, setOutputWeightUnit] = useState<WeightInputUnit>("kg");
  /** Create-mode yield UOM before an output product exists. */
  const [draftYieldUom, setDraftYieldUom] = useState<PosUnitOfMeasureCode | string>(
    DEFAULT_CATALOG_UNIT_OF_MEASURE,
  );
  const [outputPickerTab, setOutputPickerTab] = useState<OutputPickerTab>("produced");
  const [materials, setMaterials] = useState<ProductionMaterialDraft[]>([]);
  const [outputSearch, setOutputSearch] = useState("");
  const [materialSearch, setMaterialSearch] = useState("");
  const [debouncedOutput, setDebouncedOutput] = useState("");
  const [debouncedMaterial, setDebouncedMaterial] = useState("");
  const [materialPage, setMaterialPage] = useState(1);
  const [materialPages, setMaterialPages] = useState<PosCatalogProductDto[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [hydrated, setHydrated] = useState(!isEdit);
  const [qtySheetProduct, setQtySheetProduct] = useState<PosCatalogProductDto | null>(null);
  const [qtySheetEditing, setQtySheetEditing] = useState(false);
  const [outputSheetOpen, setOutputSheetOpen] = useState(false);
  /** When true, browse Active catalog and enable ingredient capability on add. */
  const [browseCatalogIngredients, setBrowseCatalogIngredients] = useState(false);
  const [enablingIngredient, setEnablingIngredient] = useState(false);

  useEffect(() => {
    const handle = window.setTimeout(() => setDebouncedOutput(outputSearch.trim()), 250);
    return () => window.clearTimeout(handle);
  }, [outputSearch]);

  useEffect(() => {
    const handle = window.setTimeout(() => {
      setDebouncedMaterial(materialSearch.trim());
      setMaterialPage(1);
      setMaterialPages([]);
    }, 250);
    return () => window.clearTimeout(handle);
  }, [materialSearch]);

  const workspace = useMemo(
    () =>
      boundWorkspace?.branchId
        ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
        : null,
    [boundWorkspace],
  );

  const existingQuery = useQuery({
    queryKey: ["production-definition", workspace?.organizationId, definitionId],
    enabled: Boolean(workspace) && Boolean(definitionId) && online && isEdit,
    queryFn: ({ signal }) => getProductionDefinition(workspace!, definitionId!, signal),
  });

  useEffect(() => {
    if (!existingQuery.data || hydrated) {
      return;
    }
    let cancelled = false;
    void (async () => {
      const def = existingQuery.data;
      setName(def.name);
      setOutputProductId(def.outputProductId);
      try {
        const output = await getCatalogProduct(workspace!, def.outputProductId);
        if (!cancelled) {
          setOutputProduct(output);
          setOutputName(output.name);
          setDraftYieldUom(output.unitOfMeasure || DEFAULT_CATALOG_UNIT_OF_MEASURE);
          const mode = resolveMaterialEntryMode(output);
          if (mode === "weight") {
            setOutputWeightUnit("kg");
            setOutputQuantityRaw(String(def.outputQuantityEntered));
            setOutputProductUnitId(null);
          } else if (mode === "unit") {
            const units = activeMaterialUnits(output);
            const unitId =
              def.outputProductUnitId ??
              units.find((u) => u.unitId === def.outputProductUnitId)?.unitId ??
              units[0]?.unitId ??
              null;
            setOutputProductUnitId(unitId);
            setOutputQuantityRaw(String(def.outputQuantityEntered));
          } else {
            setOutputProductUnitId(def.outputProductUnitId ?? null);
            setOutputQuantityRaw(String(def.outputQuantityEntered));
          }
        }
      } catch {
        if (!cancelled) {
          setOutputName(def.outputProductId);
          setOutputQuantityRaw(String(def.outputQuantityEntered));
          setOutputProductUnitId(def.outputProductUnitId ?? null);
        }
      }
      const loaded: ProductionMaterialDraft[] = [];
      for (const component of def.components) {
        let productName = component.materialProductId;
        let displayUom = "";
        try {
          const product = await getCatalogProduct(workspace!, component.materialProductId);
          productName = product.name;
          const unit = (product.units ?? []).find((u) => u.unitId === component.productUnitId);
          displayUom =
            unit?.shortLabel ||
            unit?.displayName ||
            materialBaseUomLabel(product) ||
            product.unitOfMeasure;
        } catch {
          // keep id fallback
        }
        loaded.push({
          materialProductId: component.materialProductId,
          name: productName,
          quantity: component.quantityEntered,
          displayUom: displayUom || "qty",
          productUnitId: component.productUnitId ?? null,
          weightInputUnit: null,
        });
      }
      if (!cancelled) {
        setMaterials(loaded);
        setHydrated(true);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [existingQuery.data, hydrated, workspace]);

  const showOutputPicker = isEdit && !outputProductId;

  const outputPickerQuery = useQuery({
    queryKey: [
      "catalog-products",
      "production-output-picker",
      workspace?.organizationId,
      debouncedOutput,
      outputPickerTab,
    ],
    enabled: Boolean(workspace) && online && allowManage && showOutputPicker,
    queryFn: ({ signal }) =>
      listCatalogProducts(
        workspace!,
        { search: debouncedOutput || undefined, status: "Active", pageSize: 40 },
        signal,
      ),
  });

  const materialPickerQuery = useQuery({
    queryKey: [
      "catalog-products",
      "production-material-picker",
      workspace?.organizationId,
      workspace?.branchId,
      debouncedMaterial,
      materialPage,
      browseCatalogIngredients,
    ],
    enabled: Boolean(workspace) && online && allowManage,
    queryFn: ({ signal }) =>
      listCatalogProducts(
        workspace!,
        {
          search: debouncedMaterial || undefined,
          status: "Active",
          canBeUsedAsIngredient: browseCatalogIngredients ? undefined : true,
          page: materialPage,
          pageSize: 40,
        },
        signal,
      ),
  });

  useEffect(() => {
    setMaterialPage(1);
    setMaterialPages([]);
  }, [browseCatalogIngredients]);
  useEffect(() => {
    const pageItems = materialPickerQuery.data?.items;
    if (!pageItems) {
      return;
    }
    setMaterialPages((prev) => {
      if (materialPage === 1) {
        return pageItems;
      }
      const seen = new Set(prev.map((p) => p.productId));
      return [...prev, ...pageItems.filter((p) => !seen.has(p.productId))];
    });
  }, [materialPage, materialPickerQuery.data?.items]);

  const outputCandidates = useMemo(() => {
    const items = outputPickerQuery.data?.items ?? [];
    return items.filter((p) => isEligibleProductionOutput(p, outputPickerTab));
  }, [outputPickerQuery.data?.items, outputPickerTab]);

  const materialCandidates = useMemo(() => {
    return materialPages.filter((p) => {
      if (p.productId === outputProductId) {
        return false;
      }
      if (browseCatalogIngredients) {
        return true;
      }
      return isEligibleProductionMaterial(p);
    });
  }, [materialPages, outputProductId, browseCatalogIngredients]);

  // If the org has no tagged ingredients yet, surface catalog browse automatically.
  useEffect(() => {
    if (
      !browseCatalogIngredients &&
      materialPickerQuery.isSuccess &&
      (materialPickerQuery.data?.totalCount ?? 0) === 0 &&
      !debouncedMaterial
    ) {
      setBrowseCatalogIngredients(true);
    }
  }, [
    browseCatalogIngredients,
    materialPickerQuery.isSuccess,
    materialPickerQuery.data?.totalCount,
    debouncedMaterial,
  ]);
  const selectedIds = useMemo(
    () => new Set(materials.map((m) => m.materialProductId)),
    [materials],
  );

  const materialTotalCount = materialPickerQuery.data?.totalCount ?? 0;
  const canLoadMoreMaterials = materialPages.length < materialTotalCount;

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }
  if (isEdit && (existingQuery.isLoading || !hydrated)) {
    return <LoadingState label={t("production.loading")} />;
  }
  if (isEdit && (existingQuery.isError || (!existingQuery.data && existingQuery.isFetched))) {
    return (
      <ErrorState title={t("production.errorTitle")} detail={t("production.setups.notFound")} />
    );
  }

  function selectOutput(product: PosCatalogProductDto) {
    setOutputProductId(product.productId);
    setOutputProduct(product);
    setOutputName(product.name);
    setDraftYieldUom(product.unitOfMeasure || DEFAULT_CATALOG_UNIT_OF_MEASURE);
    setOutputSearch("");
    const mode = resolveMaterialEntryMode(product);
    if (mode === "weight") {
      setOutputWeightUnit("kg");
      setOutputQuantityRaw(isEdit ? "1" : outputQuantityRaw || "100");
      setOutputProductUnitId(null);
    } else if (mode === "unit") {
      const units = activeMaterialUnits(product);
      setOutputProductUnitId(units[0]?.unitId ?? null);
      setOutputQuantityRaw(isEdit ? "1" : outputQuantityRaw || "100");
    } else {
      setOutputProductUnitId(null);
      setOutputQuantityRaw(isEdit ? "1" : outputQuantityRaw || "100");
    }
    setMaterials((prev) => prev.filter((m) => m.materialProductId !== product.productId));
    setError(null);
  }

  function clearOutput() {
    if (!isEdit) {
      setOutputProductId(null);
      setOutputProduct(null);
      setOutputName("");
      setOutputProductUnitId(null);
      setOutputWeightUnit("kg");
      return;
    }
    setOutputProductId(null);
    setOutputProduct(null);
    setOutputName("");
    setOutputQuantityRaw("1");
    setOutputProductUnitId(null);
    setOutputWeightUnit("kg");
  }

  function openMaterialSheet(product: PosCatalogProductDto, editing: boolean) {
    if (product.productId === outputProductId) {
      setError(t("production.setups.materialAsOutputForbidden"));
      return;
    }
    setQtySheetProduct(product);
    setQtySheetEditing(editing);
    setError(null);
  }

  async function confirmMaterial(draft: ProductionMaterialDraft) {
    if (!workspace) {
      return;
    }
    if (draft.materialProductId === outputProductId) {
      setError(t("production.setups.materialAsOutputForbidden"));
      setQtySheetProduct(null);
      setQtySheetEditing(false);
      return;
    }
    const alreadySelected = materials.some(
      (m) => m.materialProductId === draft.materialProductId,
    );
    if (!qtySheetEditing && alreadySelected) {
      setError(t("production.setups.duplicateMaterial"));
      setQtySheetProduct(null);
      setQtySheetEditing(false);
      return;
    }

    let sourceProduct =
      qtySheetProduct && qtySheetProduct.productId === draft.materialProductId
        ? qtySheetProduct
        : materialPages.find((p) => p.productId === draft.materialProductId) ?? null;

    if (!sourceProduct) {
      try {
        sourceProduct = await getCatalogProduct(workspace, draft.materialProductId);
      } catch {
        setError(t("production.setups.saveFailed"));
        return;
      }
    }

    try {
      if (sourceProduct.canBeUsedAsIngredient !== true) {
        setEnablingIngredient(true);
        sourceProduct = await ensureCanBeUsedAsIngredient(workspace, sourceProduct);
      }
      setMaterials((prev) => [
        ...prev.filter((m) => m.materialProductId !== draft.materialProductId),
        draft,
      ]);
      setQtySheetProduct(null);
      setQtySheetEditing(false);
      setError(null);
    } catch (err) {
      setError(
        err instanceof PosApiError
          ? (err.problem.detail ?? t("production.recipes.enableIngredientFailed"))
          : t("production.recipes.enableIngredientFailed"),
      );
    } finally {
      setEnablingIngredient(false);
    }
  }

  function removeMaterial(productId: string) {
    setMaterials((prev) => prev.filter((m) => m.materialProductId !== productId));
  }

  function validateRecipeDraft(): boolean {
    const trimmedName = name.trim();
    if (!trimmedName) {
      setError(t("production.setups.needName"));
      return false;
    }
    const qty = Number(outputQuantityRaw);
    if (!Number.isFinite(qty) || qty <= 0) {
      setError(t("production.setups.invalidQuantity"));
      return false;
    }
    if (materials.length === 0) {
      setError(t("production.setups.needMaterials"));
      return false;
    }
    for (const material of materials) {
      if (material.quantity <= 0) {
        setError(t("production.setups.invalidQuantity"));
        return false;
      }
    }
    return true;
  }

  async function persistDefinition(linkedOutput: PosCatalogProductDto) {
    if (!workspace || !allowManage || !online) {
      return;
    }
    const trimmedName = name.trim();
    const normalizedOutput = normalizeMaterialQuantityInput({
      product: linkedOutput,
      rawValue: Number(outputQuantityRaw),
      weightUnit: outputWeightUnit,
      productUnitId: outputProductUnitId,
    });
    // When product was just created with matching base UOM, accept raw qty if normalize fails
    // only due to missing units (Piece base).
    let outputQuantity = Number(outputQuantityRaw);
    let outUnitId: string | null = outputProductUnitId;
    if (normalizedOutput.ok) {
      outputQuantity = normalizedOutput.quantity;
      outUnitId = normalizedOutput.productUnitId ?? null;
    } else if (!Number.isFinite(outputQuantity) || outputQuantity <= 0) {
      setError(t("production.setups.invalidQuantity"));
      return;
    }

    setSaving(true);
    setError(null);
    try {
      const body = {
        name: trimmedName,
        outputProductId: linkedOutput.productId,
        outputQuantity,
        outputProductUnitId: outUnitId,
        components: materials.map((m, index) => ({
          materialProductId: m.materialProductId,
          quantity: m.quantity,
          productUnitId: m.productUnitId ?? null,
          sortOrder: index,
        })),
      };
      const saved = isEdit
        ? await updateProductionDefinition(workspace, definitionId!, body)
        : await createProductionDefinition(workspace, body);
      navigate(`/inventory/production/setups/${saved.productionDefinitionId}`, { replace: true });
    } catch (err) {
      setError(
        err instanceof PosApiError
          ? (err.problem.detail ?? t("production.setups.saveFailed"))
          : t("production.setups.saveFailed"),
      );
    } finally {
      setSaving(false);
      setOutputSheetOpen(false);
    }
  }

  async function submit() {
    if (!workspace || !allowManage || !online || saving) {
      return;
    }
    if (!validateRecipeDraft()) {
      return;
    }

    if (!isEdit && (!outputProductId || !outputProduct)) {
      setOutputSheetOpen(true);
      setError(null);
      return;
    }

    if (!outputProductId || !outputProduct) {
      setError(t("production.setups.needOutput"));
      return;
    }

    await persistDefinition(outputProduct);
  }

  const editingDraft = qtySheetProduct
    ? materials.find((m) => m.materialProductId === qtySheetProduct.productId) ?? null
    : null;

  return (
    <div
      className="production-definition-form-page exits-page flex min-w-0 flex-col gap-3 pb-4"
      data-testid="production-definition-form-page"
    >
      <PageHeader
        title={isEdit ? t("production.setups.edit") : t("production.setups.new")}
        description={
          isEdit ? t("production.setups.formLede") : t("production.recipes.formLedeFirstTime")
        }
        backTo={
          isEdit
            ? `/inventory/production/setups/${definitionId}`
            : "/inventory/production/setups"
        }
        backLabel={t("production.backSetups")}
        backTestId="page-header-back-production-setups"
      />

      {!online ? (
        <Card>
          <p className="m-0">{t("production.offline")}</p>
        </Card>
      ) : null}
      {!allowManage ? (
        <Card>
          <p className="m-0">{t("production.manageDenied")}</p>
        </Card>
      ) : null}

      {error ? <ErrorState title={t("production.errorTitle")} detail={error} /> : null}

      <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
        {t("production.setups.name")}
        <input
          className="rounded-md border border-border bg-background px-3"
          value={name}
          onChange={(e) => setName(e.target.value)}
          disabled={!allowManage}
          data-testid="production-setup-name"
        />
      </label>

      <section className="flex flex-col gap-2" data-testid="production-setup-output">
        <h2 className="m-0 text-[length:var(--exits-text-md)] font-medium">
          {isEdit
            ? t("production.setups.outputProduct")
            : t("production.recipes.standardYield")}
        </h2>

        {!isEdit ? (
          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            {t("production.recipes.standardYield")}
            <div className="flex flex-wrap items-stretch gap-2">
              <input
                type="number"
                min={0}
                step="any"
                className="min-w-0 flex-1 rounded-md border border-border bg-background px-3 tabular-nums"
                value={outputQuantityRaw}
                onChange={(e) => setOutputQuantityRaw(e.target.value)}
                disabled={!allowManage}
                data-testid="production-setup-output-qty"
              />
              <select
                className="rounded-md border border-border bg-background px-2"
                value={draftYieldUom}
                onChange={(e) => setDraftYieldUom(e.target.value)}
                disabled={!allowManage}
                data-testid="production-recipe-yield-uom"
                aria-label={t("production.recipes.baseUnit")}
              >
                {YIELD_UOM_OPTIONS.map((code) => (
                  <option key={code} value={code}>
                    {code}
                  </option>
                ))}
              </select>
            </div>
          </label>
        ) : null}

        {isEdit && outputProductId && outputProduct ? (
          <Card className="flex flex-col gap-2 p-3">
            <div className="font-medium">{outputName}</div>
            <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
              {t("production.setups.outputQuantity")}
              <div className="flex flex-wrap items-stretch gap-2">
                <input
                  type="number"
                  min={0}
                  step="any"
                  className="min-w-0 flex-1 rounded-md border border-border bg-background px-3 tabular-nums"
                  value={outputQuantityRaw}
                  onChange={(e) => setOutputQuantityRaw(e.target.value)}
                  disabled={!allowManage}
                  data-testid="production-setup-output-qty"
                />
                {isWeightMaterial(outputProduct) ? (
                  <select
                    className="rounded-md border border-border bg-background px-2"
                    value={outputWeightUnit}
                    onChange={(e) => setOutputWeightUnit(e.target.value as WeightInputUnit)}
                    disabled={!allowManage}
                    data-testid="production-setup-output-weight-unit"
                    aria-label={t("sell.weightUnit")}
                  >
                    <option value="kg">{t("sell.weightUnitKg")}</option>
                    <option value="g">{t("sell.weightUnitG")}</option>
                  </select>
                ) : resolveMaterialEntryMode(outputProduct) === "unit" ? (
                  <select
                    className="rounded-md border border-border bg-background px-2"
                    value={outputProductUnitId ?? ""}
                    onChange={(e) => setOutputProductUnitId(e.target.value || null)}
                    disabled={!allowManage}
                    data-testid="production-setup-output-unit"
                    aria-label={t("production.setups.outputQuantity")}
                  >
                    {activeMaterialUnits(outputProduct).map((unit) => (
                      <option key={unit.unitId} value={unit.unitId}>
                        {unit.shortLabel || unit.displayName}
                      </option>
                    ))}
                  </select>
                ) : (
                  <span
                    className="inline-flex items-center rounded-md border border-border bg-[var(--exits-surface-muted)] px-3 text-[length:var(--exits-text-sm)]"
                    data-testid="production-setup-output-base-uom"
                  >
                    {outputProduct.unitOfMeasure}
                  </span>
                )}
              </div>
            </label>
            <Button
              type="button"
              variant="ghost"
              className="w-fit"
              disabled={!allowManage}
              onClick={clearOutput}
            >
              {t("production.setups.changeProduct")}
            </Button>
          </Card>
        ) : null}

        {showOutputPicker ? (
          <>
            <div
              className="flex flex-wrap gap-2"
              role="tablist"
              aria-label={t("production.setups.outputProduct")}
              data-testid="production-setup-output-tabs"
            >
              <Button
                type="button"
                variant={outputPickerTab === "produced" ? "default" : "outline"}
                data-testid="production-setup-output-tab-produced"
                onClick={() => setOutputPickerTab("produced")}
              >
                {t("production.setups.outputTabProduced")}
              </Button>
              <Button
                type="button"
                variant={outputPickerTab === "all" ? "default" : "outline"}
                data-testid="production-setup-output-tab-all"
                onClick={() => setOutputPickerTab("all")}
              >
                {t("production.setups.outputTabAll")}
              </Button>
            </div>
            <SearchField
              label={t("production.setups.searchOutput")}
              value={outputSearch}
              onChange={(e) => setOutputSearch(e.target.value)}
              onClear={() => setOutputSearch("")}
              placeholder={t("production.setups.searchOutput")}
              data-testid="production-setup-output-search"
            />
            {outputCandidates.length === 0 && outputPickerQuery.isSuccess ? (
              <EmptyState
                title={t("production.setups.noProducts")}
                detail={t("production.setups.noProductsDetail")}
              />
            ) : null}
            <ul
              className="m-0 flex list-none flex-col gap-2 p-0"
              data-testid="production-setup-output-browser"
            >
              {outputCandidates.map((product) => (
                <li key={product.productId}>
                  <Card className="flex flex-wrap items-center justify-between gap-2 p-3">
                    <div className="min-w-0">
                      <div className="font-medium">{product.name}</div>
                      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                        {materialBaseUomLabel(product)}
                      </p>
                    </div>
                    <Button
                      type="button"
                      disabled={!allowManage || !online}
                      onClick={() => selectOutput(product)}
                    >
                      {t("production.setups.selectProduct")}
                    </Button>
                  </Card>
                </li>
              ))}
            </ul>
          </>
        ) : null}

        {!isEdit ? (
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("production.recipes.outputCreatedOnSaveHint")}
          </p>
        ) : null}
      </section>

      <section className="flex flex-col gap-2" data-testid="production-setup-materials">
        <div className="flex flex-wrap items-baseline justify-between gap-2">
          <h2 className="m-0 text-[length:var(--exits-text-md)] font-medium">
            {t("production.setups.ingredients")}
          </h2>
          <p
            className="m-0 text-[length:var(--exits-text-sm)] text-muted"
            data-testid="production-setup-selected-count"
          >
            {t("production.setups.selectedCount").replace("{count}", String(materials.length))}
          </p>
        </div>

        {materials.length === 0 ? (
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("production.setups.draftEmpty")}
          </p>
        ) : (
          <div className="flex flex-col gap-2" data-testid="production-setup-selected-materials">
            <h3 className="m-0 text-[length:var(--exits-text-sm)] font-medium">
              {t("production.setups.selectedMaterials")}
            </h3>
            <ul className="m-0 flex list-none flex-col gap-2 p-0">
              {materials.map((material) => (
                <li key={material.materialProductId}>
                  <Card
                    className="flex flex-col gap-2 p-3"
                    data-testid={`production-setup-selected-${material.materialProductId}`}
                  >
                    <div className="font-medium">{material.name}</div>
                    <p className="m-0 text-[length:var(--exits-text-sm)]">
                      {draftQuantityLine(material)}
                    </p>
                    <div className="flex flex-wrap gap-2">
                      <Button
                        type="button"
                        variant="outline"
                        className="w-fit"
                        disabled={!allowManage}
                        data-testid={`production-setup-edit-${material.materialProductId}`}
                        onClick={() => {
                          const fromList = materialPages.find(
                            (p) => p.productId === material.materialProductId,
                          );
                          if (fromList) {
                            openMaterialSheet(fromList, true);
                            return;
                          }
                          void getCatalogProduct(workspace, material.materialProductId).then(
                            (product) => openMaterialSheet(product, true),
                          );
                        }}
                      >
                        {t("production.setups.editMaterial")}
                      </Button>
                      <Button
                        type="button"
                        variant="ghost"
                        className="w-fit"
                        disabled={!allowManage}
                        data-testid={`production-setup-remove-${material.materialProductId}`}
                        onClick={() => removeMaterial(material.materialProductId)}
                      >
                        {t("production.setups.removeMaterial")}
                      </Button>
                    </div>
                  </Card>
                </li>
              ))}
            </ul>
          </div>
        )}

        <SearchField
          label={t("production.setups.searchMaterial")}
          value={materialSearch}
          onChange={(e) => setMaterialSearch(e.target.value)}
          onClear={() => setMaterialSearch("")}
          placeholder={t("production.setups.searchMaterialPlaceholder")}
          data-testid="production-setup-material-search"
        />

        <div className="flex flex-wrap items-center gap-2">
          <Button
            type="button"
            variant={browseCatalogIngredients ? "default" : "outline"}
            disabled={!allowManage || !online}
            data-testid="production-setup-browse-catalog-ingredients"
            onClick={() => setBrowseCatalogIngredients((v) => !v)}
          >
            {browseCatalogIngredients
              ? t("production.recipes.showTaggedIngredientsOnly")
              : t("production.recipes.browseCatalogToEnable")}
          </Button>
          {browseCatalogIngredients ? (
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
              {t("production.recipes.browseCatalogHint")}
            </p>
          ) : null}
        </div>

        {materialCandidates.length === 0 && materialPickerQuery.isSuccess ? (
          <EmptyState
            title={t("production.setups.noEligibleMaterials")}
            detail={
              debouncedMaterial
                ? t("production.setups.noProductsDetail")
                : browseCatalogIngredients
                  ? t("production.setups.noProductsDetail")
                  : t("production.setups.noEligibleMaterialsDetail")
            }
          />
        ) : null}

        {materialPickerQuery.isLoading && materialPages.length === 0 ? (
          <LoadingState label={t("production.loading")} />
        ) : null}

        <ul
          className="m-0 grid list-none grid-cols-1 gap-2 p-0"
          data-testid="production-setup-material-browser"
        >
          {materialCandidates.map((product) => {
            const selected = selectedIds.has(product.productId);
            const available = formatMaterialAvailableCaption(product, t("transfer.available"));
            const needsEnable = product.canBeUsedAsIngredient !== true;
            return (
              <li key={product.productId}>
                <button
                  type="button"
                  disabled={!allowManage || !online || saving || enablingIngredient}
                  data-testid={`production-setup-material-${product.productId}`}
                  data-selected={selected ? "true" : "false"}
                  className={cn(
                    "production-material-pick flex w-full flex-col gap-1 rounded-md border p-3 text-left",
                    selected
                      ? "border-primary bg-[var(--exits-surface-muted)]"
                      : "border-border bg-surface",
                  )}
                  onClick={() => openMaterialSheet(product, selected)}
                >
                  <span className="flex items-start justify-between gap-2">
                    <span className="min-w-0 font-medium">{product.name}</span>
                    {selected ? (
                      <span
                        className="inline-flex shrink-0 items-center gap-1 text-[length:var(--exits-text-xs)] font-semibold text-[var(--exits-primary)]"
                        data-testid={`production-setup-material-selected-badge-${product.productId}`}
                      >
                        <Check className="size-3.5" aria-hidden />
                        {t("production.setups.materialSelected")}
                      </span>
                    ) : needsEnable ? (
                      <span className="shrink-0 text-[length:var(--exits-text-xs)] text-muted">
                        {t("production.recipes.willEnableIngredient")}
                      </span>
                    ) : null}
                  </span>
                  <span className="text-[length:var(--exits-text-sm)] text-muted">
                    {available ?? materialBaseUomLabel(product)}
                  </span>
                </button>
              </li>
            );
          })}
        </ul>

        {canLoadMoreMaterials ? (
          <Button
            type="button"
            variant="outline"
            disabled={materialPickerQuery.isFetching}
            data-testid="production-setup-material-load-more"
            onClick={() => setMaterialPage((page) => page + 1)}
          >
            {t("production.setups.loadMoreMaterials")}
          </Button>
        ) : null}
      </section>

      {!isEdit ? (
        <label
          className="flex items-center gap-2 text-[length:var(--exits-text-sm)]"
          data-testid="production-recipe-save-as-recipe"
        >
          <input type="checkbox" checked disabled readOnly />
          {t("production.recipes.saveAsRecipe")}
        </label>
      ) : null}

      <ProductionMaterialQuantitySheet
        open={Boolean(qtySheetProduct)}
        product={qtySheetProduct}
        initialDraft={qtySheetEditing ? editingDraft : null}
        onCancel={() => {
          setQtySheetProduct(null);
          setQtySheetEditing(false);
        }}
        onConfirm={confirmMaterial}
      />

      {workspace ? (
        <ProductionRecipeOutputSheet
          open={outputSheetOpen}
          workspace={workspace}
          recipeName={name}
          standardYieldQty={Number(outputQuantityRaw) || 0}
          standardYieldUom={draftYieldUom}
          materials={materials}
          onCancel={() => setOutputSheetOpen(false)}
          onLinked={(result) => {
            setOutputProductId(result.outputProduct.productId);
            setOutputProduct(result.outputProduct);
            setOutputName(result.outputProduct.name);
            setDraftYieldUom(
              result.outputProduct.unitOfMeasure || draftYieldUom || DEFAULT_CATALOG_UNIT_OF_MEASURE,
            );
            void persistDefinition(result.outputProduct);
          }}
        />
      ) : null}

      <StickyActionBar>
        <Button
          type="button"
          className="w-full"
          disabled={!allowManage || !online || saving}
          onClick={() => void submit()}
          data-testid="production-setup-save"
        >
          {saving ? t("production.setups.saving") : t("production.setups.save")}
        </Button>
      </StickyActionBar>
    </div>
  );
}
