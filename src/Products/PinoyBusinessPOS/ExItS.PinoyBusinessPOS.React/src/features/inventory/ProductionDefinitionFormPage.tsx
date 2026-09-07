import { useEffect, useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { Check } from "lucide-react";
import { canManageInventory } from "@/access/pos-capabilities";
import { listCatalogProducts, getCatalogProduct } from "@/api/pos/pos-catalog-client";
import type { PosCatalogProductDto } from "@/api/pos/pos-catalog-types";
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
  // Backend requires IsProduced for production outputs.
  if (product.isProduced !== true) {
    return false;
  }
  // Tabs share eligibility today; "produced" is the default browse mode.
  void tab;
  return true;
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
  const [outputQuantityRaw, setOutputQuantityRaw] = useState("1");
  const [outputProductUnitId, setOutputProductUnitId] = useState<string | null>(null);
  const [outputWeightUnit, setOutputWeightUnit] = useState<WeightInputUnit>("kg");
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

  const outputPickerQuery = useQuery({
    queryKey: [
      "catalog-products",
      "production-output-picker",
      workspace?.organizationId,
      debouncedOutput,
      outputPickerTab,
    ],
    enabled: Boolean(workspace) && online && allowManage && !outputProductId,
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
    enabled: Boolean(workspace) && online && allowManage,
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

  const materialCandidates = useMemo(() => {
    return materialPages.filter((p) => {
      if (p.productId === outputProductId) {
        return false;
      }
      return isEligibleProductionMaterial(p);
    });
  }, [materialPages, outputProductId]);

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
    setOutputSearch("");
    const mode = resolveMaterialEntryMode(product);
    if (mode === "weight") {
      setOutputWeightUnit("kg");
      setOutputQuantityRaw("1");
      setOutputProductUnitId(null);
    } else if (mode === "unit") {
      const units = activeMaterialUnits(product);
      setOutputProductUnitId(units[0]?.unitId ?? null);
      setOutputQuantityRaw("1");
    } else {
      setOutputProductUnitId(null);
      setOutputQuantityRaw("1");
    }
    setMaterials((prev) => prev.filter((m) => m.materialProductId !== product.productId));
    setError(null);
  }

  function clearOutput() {
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

  function confirmMaterial(draft: ProductionMaterialDraft) {
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
    setMaterials((prev) => [
      ...prev.filter((m) => m.materialProductId !== draft.materialProductId),
      draft,
    ]);
    setQtySheetProduct(null);
    setQtySheetEditing(false);
    setError(null);
  }

  function removeMaterial(productId: string) {
    setMaterials((prev) => prev.filter((m) => m.materialProductId !== productId));
  }

  async function submit() {
    if (!workspace || !allowManage || !online || saving) {
      return;
    }
    const trimmedName = name.trim();
    if (!trimmedName) {
      setError(t("production.setups.needName"));
      return;
    }
    if (!outputProductId || !outputProduct) {
      setError(t("production.setups.needOutput"));
      return;
    }
    const normalizedOutput = normalizeMaterialQuantityInput({
      product: outputProduct,
      rawValue: Number(outputQuantityRaw),
      weightUnit: outputWeightUnit,
      productUnitId: outputProductUnitId,
    });
    if (!normalizedOutput.ok) {
      setError(t("production.setups.invalidQuantity"));
      return;
    }
    if (materials.length === 0) {
      setError(t("production.setups.needMaterials"));
      return;
    }
    for (const material of materials) {
      if (material.quantity <= 0) {
        setError(t("production.setups.invalidQuantity"));
        return;
      }
    }

    setSaving(true);
    setError(null);
    try {
      const body = {
        name: trimmedName,
        outputProductId,
        outputQuantity: normalizedOutput.quantity,
        outputProductUnitId: normalizedOutput.productUnitId ?? null,
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
    }
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
        description={t("production.setups.formLede")}
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
          {t("production.setups.outputProduct")}
        </h2>
        {outputProductId && outputProduct ? (
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
        ) : (
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
        )}
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

        {materialCandidates.length === 0 && materialPickerQuery.isSuccess ? (
          <EmptyState
            title={t("production.setups.noEligibleMaterials")}
            detail={
              debouncedMaterial
                ? t("production.setups.noProductsDetail")
                : t("production.setups.noEligibleMaterialsDetail")
            }
          />
        ) : null}

        <ul
          className="m-0 grid list-none grid-cols-1 gap-2 p-0"
          data-testid="production-setup-material-browser"
        >
          {materialCandidates.map((product) => {
            const selected = selectedIds.has(product.productId);
            const available = formatMaterialAvailableCaption(product, t("transfer.available"));
            return (
              <li key={product.productId}>
                <button
                  type="button"
                  disabled={!allowManage || !online || saving}
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
