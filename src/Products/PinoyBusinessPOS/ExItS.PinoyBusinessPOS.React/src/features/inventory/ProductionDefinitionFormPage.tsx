import { useEffect, useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { canManageInventory } from "@/access/pos-capabilities";
import { listCatalogProducts, getCatalogProduct } from "@/api/pos/pos-catalog-client";
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
  draftQuantityParts,
  formatMaterialAvailableCaption,
  formatMaterialAvailableCell,
  formatMaterialAvailableStock,
  isEligibleProductionMaterial,
  isWeightMaterial,
  materialBaseUomLabel,
  normalizeMaterialQuantityInput,
  resolveMaterialEntryMode,
  type ProductionMaterialDraft,
} from "@/features/inventory/production-material-uom";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";
import { formatQuantityDisplay, type WeightInputUnit } from "@/cart/sell-cart-helpers";
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
  /** Ingredient picker collapsed by default; opened via + Add ingredient. */
  const [materialPickerOpen, setMaterialPickerOpen] = useState(false);
  /** Catalog snapshots for selected rows (availability / edit sheet). */
  const [materialCatalogById, setMaterialCatalogById] = useState<
    Record<string, PosCatalogProductDto>
  >({});

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
      const catalogById: Record<string, PosCatalogProductDto> = {};
      for (const component of def.components) {
        let productName = component.materialProductId;
        let displayUom = "";
        try {
          const product = await getCatalogProduct(workspace!, component.materialProductId);
          catalogById[product.productId] = product;
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
        setMaterialCatalogById(catalogById);
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
    ],
    enabled: Boolean(workspace) && online && allowManage && materialPickerOpen,
    queryFn: ({ signal }) =>
      listCatalogProducts(
        workspace!,
        {
          search: debouncedMaterial || undefined,
          status: "Active",
          canBeUsedAsIngredient: true,
          page: materialPage,
          pageSize: 40,
        },
        signal,
      ),
  });

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

  const selectedIds = useMemo(
    () => new Set(materials.map((m) => m.materialProductId)),
    [materials],
  );

  const materialCandidates = useMemo(() => {
    return materialPages.filter((p) => {
      if (p.productId === outputProductId) {
        return false;
      }
      if (selectedIds.has(p.productId)) {
        return false;
      }
      // Strict: Active + CanBeUsedAsIngredient + tracked. Never fall back to full catalog.
      return isEligibleProductionMaterial(p);
    });
  }, [materialPages, outputProductId, selectedIds]);

  const materialTotalCount = materialPickerQuery.data?.totalCount ?? 0;
  const canLoadMoreMaterials = materialPages.length < materialTotalCount;
  const noEligibleIngredients =
    materialPickerQuery.isSuccess &&
    !debouncedMaterial &&
    materialTotalCount === 0;

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
    // Recipe draft must never convert catalog products. Only already-eligible materials.
    if (!editing && !isEligibleProductionMaterial(product)) {
      setError(t("production.setups.materialNotEligible"));
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

    // Side-effect free: new materials must already be eligible. Never mutate catalog.
    if (!qtySheetEditing && !isEligibleProductionMaterial(sourceProduct)) {
      setError(t("production.setups.materialNotEligible"));
      setQtySheetProduct(null);
      setQtySheetEditing(false);
      return;
    }

    setMaterials((prev) => [
      ...prev.filter((m) => m.materialProductId !== draft.materialProductId),
      draft,
    ]);
    setMaterialCatalogById((prev) => ({
      ...prev,
      [sourceProduct.productId]: sourceProduct,
    }));
    setQtySheetProduct(null);
    setQtySheetEditing(false);
    setMaterialPickerOpen(false);
    setMaterialSearch("");
    setError(null);
  }

  function removeMaterial(productId: string) {
    setMaterials((prev) => prev.filter((m) => m.materialProductId !== productId));
    setMaterialCatalogById((prev) => {
      const next = { ...prev };
      delete next[productId];
      return next;
    });
  }

  function resolveMaterialProduct(materialProductId: string): PosCatalogProductDto | null {
    return (
      materialCatalogById[materialProductId] ??
      materialPages.find((p) => p.productId === materialProductId) ??
      null
    );
  }

  function openEditMaterial(material: ProductionMaterialDraft) {
    const fromCache = resolveMaterialProduct(material.materialProductId);
    if (fromCache) {
      openMaterialSheet(fromCache, true);
      return;
    }
    void getCatalogProduct(workspace!, material.materialProductId).then((product) => {
      setMaterialCatalogById((prev) => ({ ...prev, [product.productId]: product }));
      openMaterialSheet(product, true);
    });
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
      if (isEdit) {
        navigate(`/inventory/production/setups/${saved.productionDefinitionId}`, { replace: true });
      } else {
        // First-time recipe: stock stays 0 until Produce — take the user there next.
        navigate(
          `/inventory/production/produce?definitionId=${saved.productionDefinitionId}`,
          { replace: true },
        );
      }
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
          <div data-testid="production-setup-selected-materials">
            {/* Desktop / wide tablet: compact semantic table */}
            <div className="hidden overflow-x-auto md:block">
              <table
                className="w-full min-w-[28rem] border-collapse text-left text-[length:var(--exits-text-sm)]"
                data-testid="production-setup-materials-table"
              >
                <thead>
                  <tr className="border-b border-border text-muted">
                    <th scope="col" className="py-1.5 pr-2 font-medium">
                      {t("production.setups.tableIngredient")}
                    </th>
                    <th scope="col" className="py-1.5 pr-2 text-right font-medium">
                      {t("production.setups.tableQty")}
                    </th>
                    <th scope="col" className="py-1.5 pr-2 font-medium">
                      {t("production.setups.tableUnit")}
                    </th>
                    <th scope="col" className="py-1.5 pr-2 text-right font-medium">
                      {t("production.setups.tableAvailable")}
                    </th>
                    <th scope="col" className="py-1.5 font-medium">
                      {t("production.setups.tableActions")}
                    </th>
                  </tr>
                </thead>
                <tbody>
                  {materials.map((material) => {
                    const parts = draftQuantityParts(material);
                    const product = resolveMaterialProduct(material.materialProductId);
                    const available = formatMaterialAvailableCell(product);
                    return (
                      <tr
                        key={material.materialProductId}
                        className="border-b border-border/60"
                        data-testid={`production-setup-selected-${material.materialProductId}`}
                      >
                        <td className="max-w-[12rem] truncate py-1.5 pr-2 font-medium">
                          {material.name}
                        </td>
                        <td
                          className="py-1.5 pr-2 text-right tabular-nums"
                          data-testid={`production-setup-qty-${material.materialProductId}`}
                        >
                          {parts.qty}
                        </td>
                        <td
                          className="py-1.5 pr-2"
                          data-testid={`production-setup-unit-${material.materialProductId}`}
                        >
                          {parts.unit}
                        </td>
                        <td
                          className="py-1.5 pr-2 text-right tabular-nums text-muted"
                          data-testid={`production-setup-available-${material.materialProductId}`}
                        >
                          {available}
                        </td>
                        <td className="py-1.5">
                          <div className="flex flex-wrap gap-1">
                            <Button
                              type="button"
                              variant="ghost"
                              className="h-7 px-2"
                              disabled={!allowManage}
                              data-testid={`production-setup-edit-${material.materialProductId}`}
                              onClick={() => openEditMaterial(material)}
                            >
                              {t("production.setups.editMaterial")}
                            </Button>
                            <Button
                              type="button"
                              variant="ghost"
                              className="h-7 px-2"
                              disabled={!allowManage}
                              data-testid={`production-setup-remove-${material.materialProductId}`}
                              onClick={() => removeMaterial(material.materialProductId)}
                            >
                              {t("production.setups.removeMaterial")}
                            </Button>
                          </div>
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>

            {/* Mobile / narrow: compact stacked rows */}
            <ul
              className="m-0 flex list-none flex-col gap-1.5 p-0 md:hidden"
              data-testid="production-setup-materials-mobile"
            >
              {materials.map((material) => {
                const parts = draftQuantityParts(material);
                const product = resolveMaterialProduct(material.materialProductId);
                const stock = product ? formatMaterialAvailableStock(product) : null;
                const availableLabel = stock
                  ? t("production.setups.availableShort")
                      .replace("{qty}", formatQuantityDisplay(stock.qty))
                      .replace("{uom}", stock.uom)
                  : null;
                return (
                  <li
                    key={material.materialProductId}
                    className="rounded-md border border-border px-2.5 py-2"
                    data-testid={`production-setup-selected-mobile-${material.materialProductId}`}
                  >
                    <div className="font-medium leading-tight">{material.name}</div>
                    <div className="mt-0.5 flex flex-wrap items-baseline justify-between gap-x-2 gap-y-0.5">
                      <span
                        className="text-[length:var(--exits-text-sm)] tabular-nums"
                        data-testid={`production-setup-qty-mobile-${material.materialProductId}`}
                      >
                        {parts.qty} {parts.unit}
                      </span>
                      {availableLabel ? (
                        <span
                          className="text-[length:var(--exits-text-xs)] text-muted"
                          data-testid={`production-setup-available-mobile-${material.materialProductId}`}
                        >
                          {availableLabel}
                        </span>
                      ) : null}
                    </div>
                    <div className="mt-1.5 flex flex-wrap gap-1">
                      <Button
                        type="button"
                        variant="outline"
                        className="h-7 px-2"
                        disabled={!allowManage}
                        data-testid={`production-setup-edit-mobile-${material.materialProductId}`}
                        onClick={() => openEditMaterial(material)}
                      >
                        {t("production.setups.editMaterial")}
                      </Button>
                      <Button
                        type="button"
                        variant="ghost"
                        className="h-7 px-2"
                        disabled={!allowManage}
                        data-testid={`production-setup-remove-mobile-${material.materialProductId}`}
                        onClick={() => removeMaterial(material.materialProductId)}
                      >
                        {t("production.setups.removeMaterial")}
                      </Button>
                    </div>
                  </li>
                );
              })}
            </ul>
          </div>
        )}

        {!materialPickerOpen ? (
          <Button
            type="button"
            variant="outline"
            className="w-fit"
            disabled={!allowManage || !online}
            data-testid="production-setup-add-ingredient"
            onClick={() => setMaterialPickerOpen(true)}
          >
            {t("production.setups.addIngredient")}
          </Button>
        ) : (
          <div
            className="flex flex-col gap-2 rounded-md border border-border p-3"
            data-testid="production-setup-ingredient-picker"
          >
            <div className="flex flex-wrap items-center justify-between gap-2">
              <h3 className="m-0 text-[length:var(--exits-text-sm)] font-medium">
                {t("production.setups.addIngredient")}
              </h3>
              <Button
                type="button"
                variant="ghost"
                className="h-7 px-2"
                data-testid="production-setup-close-ingredient-picker"
                onClick={() => {
                  setMaterialPickerOpen(false);
                  setMaterialSearch("");
                }}
              >
                {t("production.setups.closeIngredientPicker")}
              </Button>
            </div>

            <SearchField
              label={t("production.setups.searchMaterial")}
              value={materialSearch}
              onChange={(e) => setMaterialSearch(e.target.value)}
              onClear={() => setMaterialSearch("")}
              placeholder={t("production.setups.searchMaterialPlaceholder")}
              data-testid="production-setup-material-search"
            />

            {noEligibleIngredients ? (
              <div
                className="flex flex-col gap-2"
                data-testid="production-setup-no-eligible-ingredients"
              >
                <EmptyState
                  title={t("production.setups.noProductionIngredientsYet")}
                  detail={t("production.setups.noProductionIngredientsDetail")}
                />
                <Button
                  type="button"
                  variant="outline"
                  className="w-fit"
                  data-testid="production-setup-manage-products"
                  onClick={() => navigate("/catalog")}
                >
                  {t("production.setups.manageProducts")}
                </Button>
              </div>
            ) : null}

            {!noEligibleIngredients &&
            materialCandidates.length === 0 &&
            materialPickerQuery.isSuccess ? (
              <EmptyState
                title={t("production.setups.noEligibleMaterials")}
                detail={t("production.setups.noProductsDetail")}
              />
            ) : null}

            {materialPickerQuery.isLoading && materialPages.length === 0 ? (
              <LoadingState label={t("production.loading")} />
            ) : null}

            {!noEligibleIngredients ? (
              <ul
                className="m-0 grid max-h-64 list-none grid-cols-1 gap-1.5 overflow-y-auto p-0"
                data-testid="production-setup-material-browser"
              >
                {materialCandidates.map((product) => {
                  const available = formatMaterialAvailableCaption(
                    product,
                    t("transfer.available"),
                  );
                  return (
                    <li key={product.productId}>
                      <button
                        type="button"
                        disabled={!allowManage || !online || saving}
                        data-testid={`production-setup-material-${product.productId}`}
                        data-selected="false"
                        className={cn(
                          "production-material-pick flex w-full flex-col gap-0.5 rounded-md border border-border bg-surface p-2.5 text-left",
                        )}
                        onClick={() => openMaterialSheet(product, false)}
                      >
                        <span className="min-w-0 font-medium">{product.name}</span>
                        <span className="text-[length:var(--exits-text-sm)] text-muted">
                          {available ?? materialBaseUomLabel(product)}
                        </span>
                      </button>
                    </li>
                  );
                })}
              </ul>
            ) : null}

            {!noEligibleIngredients && canLoadMoreMaterials ? (
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
          </div>
        )}
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
