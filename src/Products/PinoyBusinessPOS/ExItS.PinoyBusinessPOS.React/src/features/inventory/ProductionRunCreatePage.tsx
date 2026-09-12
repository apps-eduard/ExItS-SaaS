import { Package } from "lucide-react";
import { useEffect, useMemo, useRef, useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { canManageInventory } from "@/access/pos-capabilities";
import { getCatalogProduct, listCatalogProducts } from "@/api/pos/pos-catalog-client";
import type { PosCatalogProductDto } from "@/api/pos/pos-catalog-types";
import { enableInventoryTracking, getInventoryProduct } from "@/api/pos/pos-inventory-client";
import { PosApiError } from "@/api/pos/pos-http";
import {
  createProductionRun,
  getProductionDefinition,
  listProductionDefinitions,
  type ProductionDefinitionDto,
} from "@/api/pos/pos-production-client";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { StickyActionBar } from "@/components/exits/FoundationStates";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { SearchField } from "@/components/exits/SearchField";
import { useToast } from "@/components/exits/ToastProvider";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { isLikelyNetworkFailure } from "@/connectivity/network-failure";
import {
  maxProducibleFromStock,
  productionScaleFactor,
  scaleProductionQuantity,
} from "@/features/inventory/production-labels";
import {
  findProduceShortages,
  hasProduceShortage,
} from "@/features/inventory/production-run-stock";
import { materialBaseUomLabel } from "@/features/inventory/production-material-uom";
import { ProductionMaterialQuantitySheet } from "@/features/inventory/ProductionMaterialQuantitySheet";
import type { ProductionMaterialDraft } from "@/features/inventory/production-material-uom";
import { useI18n } from "@/i18n/I18nProvider";
import { createSecureMutationId } from "@/lib/secure-mutation-id";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

type MaterialPreview = {
  materialProductId: string;
  name: string;
  uom: string;
  expected: number;
  actual: number;
  available: number | null;
  isExtra: boolean;
  productUnitId?: string | null;
};

export function ProductionRunCreatePage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { showToast } = useToast();
  const [searchParams] = useSearchParams();
  const preselectDefinitionId = searchParams.get("definitionId")?.trim() || null;
  const online = useBrowserOnline();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const allowManage = canManageInventory(sessionGrant);

  const [definitionId, setDefinitionId] = useState<string | null>(preselectDefinitionId);
  const [outputQuantity, setOutputQuantity] = useState("");
  const [actualByProduct, setActualByProduct] = useState<Record<string, string>>({});
  const [extraMaterials, setExtraMaterials] = useState<ProductionMaterialDraft[]>([]);
  const [extraAvailable, setExtraAvailable] = useState<Record<string, number | null>>({});
  const [addIngredientOpen, setAddIngredientOpen] = useState(false);
  const [ingredientSearch, setIngredientSearch] = useState("");
  const [debouncedIngredientSearch, setDebouncedIngredientSearch] = useState("");
  const [qtySheetProduct, setQtySheetProduct] = useState<PosCatalogProductDto | null>(null);
  const [notes, setNotes] = useState("");
  const [referenceNumber, setReferenceNumber] = useState("");
  const [expirationDate, setExpirationDate] = useState("");
  const [lotNumber, setLotNumber] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [statusLocked, setStatusLocked] = useState(false);
  const runIdRef = useRef<string | null>(null);

  const workspace = useMemo(
    () =>
      boundWorkspace?.branchId
        ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
        : null,
    [boundWorkspace],
  );

  useEffect(() => {
    const handle = window.setTimeout(
      () => setDebouncedIngredientSearch(ingredientSearch.trim()),
      250,
    );
    return () => window.clearTimeout(handle);
  }, [ingredientSearch]);

  const definitionsQuery = useQuery({
    queryKey: ["production-definitions", "active", workspace?.organizationId],
    enabled: Boolean(workspace) && online && allowManage,
    queryFn: ({ signal }) =>
      listProductionDefinitions(
        workspace!,
        { page: 1, pageSize: 50, isActive: true },
        signal,
      ),
  });

  const definitionQuery = useQuery({
    queryKey: ["production-definition", workspace?.organizationId, definitionId],
    enabled: Boolean(workspace) && Boolean(definitionId) && online,
    queryFn: ({ signal }) => getProductionDefinition(workspace!, definitionId!, signal),
  });

  const definition = definitionQuery.data ?? null;

  useEffect(() => {
    if (!definition) {
      return;
    }
    setOutputQuantity(String(definition.outputQuantityEntered));
    const next: Record<string, string> = {};
    for (const component of definition.components) {
      next[component.materialProductId] = String(component.quantityEntered);
    }
    setActualByProduct(next);
    setExtraMaterials([]);
    setExtraAvailable({});
    setAddIngredientOpen(false);
  }, [definition?.productionDefinitionId]);

  const outputProductQuery = useQuery({
    queryKey: ["catalog-product", workspace?.organizationId, definition?.outputProductId],
    enabled: Boolean(workspace) && Boolean(definition?.outputProductId) && online,
    queryFn: ({ signal }) => getCatalogProduct(workspace!, definition!.outputProductId, signal),
  });

  const materialPreviewQuery = useQuery({
    queryKey: [
      "production-run-preview",
      workspace?.organizationId,
      workspace?.branchId,
      definition?.productionDefinitionId,
      outputQuantity,
    ],
    enabled: Boolean(workspace) && Boolean(definition) && online,
    queryFn: async ({ signal }) => {
      const def = definition!;
      const outQty = Number(outputQuantity);
      const scale = productionScaleFactor(def.outputQuantityEntered, outQty);
      if (scale == null) {
        return [] as Array<{
          materialProductId: string;
          name: string;
          uom: string;
          expected: number;
          available: number | null;
          productUnitId: string | null;
        }>;
      }
      const rows: Array<{
        materialProductId: string;
        name: string;
        uom: string;
        expected: number;
        available: number | null;
        productUnitId: string | null;
      }> = [];
      for (const component of def.components) {
        let name = component.materialProductId;
        let uom = "";
        let available: number | null = null;
        try {
          const [product, inventory] = await Promise.all([
            getCatalogProduct(workspace!, component.materialProductId, signal),
            getInventoryProduct(workspace!, component.materialProductId, signal).catch(() => null),
          ]);
          name = product.name;
          const unit = (product.units ?? []).find((u) => u.unitId === component.productUnitId);
          uom = unit?.shortLabel || unit?.displayName || materialBaseUomLabel(product);
          available =
            inventory?.sellableQuantity ??
            inventory?.onHandQuantity ??
            null;
        } catch {
          // keep id fallback
        }
        rows.push({
          materialProductId: component.materialProductId,
          name,
          uom,
          expected: scaleProductionQuantity(component.quantityEntered, scale),
          available,
          productUnitId: component.productUnitId ?? null,
        });
      }
      return rows;
    },
  });

  const recipeProductIds = useMemo(
    () => new Set((definition?.components ?? []).map((c) => c.materialProductId)),
    [definition?.components],
  );

  const extraPickerQuery = useQuery({
    queryKey: [
      "catalog-products",
      "production-run-extra-picker",
      workspace?.organizationId,
      workspace?.branchId,
      debouncedIngredientSearch,
    ],
    enabled: Boolean(workspace) && online && allowManage && addIngredientOpen,
    queryFn: ({ signal }) =>
      listCatalogProducts(
        workspace!,
        {
          search: debouncedIngredientSearch || undefined,
          status: "Active",
          canBeUsedAsIngredient: true,
          pageSize: 40,
        },
        signal,
      ),
  });

  const materials: MaterialPreview[] = useMemo(() => {
    const recipeRows = (materialPreviewQuery.data ?? []).map((row) => {
      const actualRaw = actualByProduct[row.materialProductId];
      const actualParsed = actualRaw != null ? Number(actualRaw) : row.expected;
      return {
        ...row,
        actual: Number.isFinite(actualParsed) ? actualParsed : row.expected,
        isExtra: false as const,
      };
    });
    const extras: MaterialPreview[] = extraMaterials.map((extra) => ({
      materialProductId: extra.materialProductId,
      name: extra.name,
      uom: extra.displayUom,
      expected: 0,
      actual: extra.quantity,
      available: extraAvailable[extra.materialProductId] ?? null,
      isExtra: true,
      productUnitId: extra.productUnitId ?? null,
    }));
    return [...recipeRows, ...extras];
  }, [materialPreviewQuery.data, actualByProduct, extraMaterials, extraAvailable]);

  const shortageRows = useMemo(
    () =>
      materials.map((row) => ({
        materialProductId: row.materialProductId,
        name: row.name,
        uom: row.uom,
        required: row.actual,
        available: row.available,
        isExtra: row.isExtra,
      })),
    [materials],
  );
  const shortages = useMemo(() => findProduceShortages(shortageRows), [shortageRows]);
  const produceBlocked = hasProduceShortage(shortageRows);

  const maxProducible = useMemo(() => {
    if (!definition) {
      return null;
    }
    return maxProducibleFromStock({
      definitionOutputQuantity: definition.outputQuantityEntered,
      components: (materialPreviewQuery.data ?? []).map((row) => ({
        quantityEntered:
          definition.components.find((c) => c.materialProductId === row.materialProductId)
            ?.quantityEntered ?? row.expected,
        available: row.available,
      })),
    });
  }, [definition, materialPreviewQuery.data]);

  const tracksExpiration = outputProductQuery.data?.tracksExpiration === true;
  const outputUom =
    outputProductQuery.data != null
      ? materialBaseUomLabel(outputProductQuery.data)
      : "";

  const extraCandidates = useMemo(() => {
    const selectedExtras = new Set(extraMaterials.map((m) => m.materialProductId));
    return (extraPickerQuery.data?.items ?? []).filter((product) => {
      if (product.canBeUsedAsIngredient !== true) {
        return false;
      }
      if (product.productId === definition?.outputProductId) {
        return false;
      }
      if (recipeProductIds.has(product.productId) || selectedExtras.has(product.productId)) {
        return false;
      }
      return true;
    });
  }, [
    extraPickerQuery.data?.items,
    extraMaterials,
    definition?.outputProductId,
    recipeProductIds,
  ]);

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }

  function onSelectDefinition(id: string) {
    setDefinitionId(id || null);
    setError(null);
    setStatusLocked(false);
    runIdRef.current = null;
  }

  function syncActualsToExpected(def: ProductionDefinitionDto, outQty: number) {
    const scale = productionScaleFactor(def.outputQuantityEntered, outQty);
    if (scale == null) {
      return;
    }
    const next: Record<string, string> = {};
    for (const component of def.components) {
      next[component.materialProductId] = String(
        scaleProductionQuantity(component.quantityEntered, scale),
      );
    }
    setActualByProduct(next);
  }

  async function openExtraSheet(product: PosCatalogProductDto) {
    let available: number | null = null;
    try {
      const inventory = await getInventoryProduct(workspace!, product.productId);
      available = inventory.sellableQuantity ?? inventory.onHandQuantity ?? null;
    } catch {
      available = null;
    }
    setExtraAvailable((prev) => ({ ...prev, [product.productId]: available }));
    setQtySheetProduct(product);
  }

  function confirmExtra(draft: ProductionMaterialDraft) {
    const available = extraAvailable[draft.materialProductId];
    if (available != null && draft.quantity > available + 1e-9) {
      setError(
        t("production.produce.extraExceedsAvailable")
          .replace("{name}", draft.name)
          .replace("{available}", `${available} ${draft.displayUom}`),
      );
      return;
    }
    setExtraMaterials((prev) => [
      ...prev.filter((m) => m.materialProductId !== draft.materialProductId),
      draft,
    ]);
    setQtySheetProduct(null);
    setAddIngredientOpen(false);
    setIngredientSearch("");
    setError(null);
  }

  async function submit() {
    if (!workspace || !allowManage || !online || saving || statusLocked || !definitionId || !definition) {
      return;
    }
    if (produceBlocked) {
      setError(t("production.produce.cannotProduceShortage"));
      return;
    }
    const outQty = Number(outputQuantity);
    if (!Number.isFinite(outQty) || outQty <= 0) {
      setError(t("production.produce.invalidQuantity"));
      return;
    }
    if (tracksExpiration && !expirationDate.trim()) {
      setError(t("production.produce.expirationRequired"));
      return;
    }
    for (const row of materials) {
      if (!Number.isFinite(row.actual) || row.actual <= 0) {
        setError(t("production.produce.invalidQuantity"));
        return;
      }
    }

    if (!runIdRef.current) {
      const generated = createSecureMutationId();
      if (!generated.ok) {
        setError(t("production.produce.saveFailed"));
        return;
      }
      runIdRef.current = generated.id;
    }
    const productionRunId = runIdRef.current;
    setSaving(true);
    setError(null);

    const overrides = materials
      .filter((row) => !row.isExtra)
      .filter((row) => Math.abs(row.actual - row.expected) > 1e-9)
      .map((row) => ({
        materialProductId: row.materialProductId,
        actualQuantity: row.actual,
        productUnitId: row.productUnitId ?? null,
      }));

    const extrasPayload = materials
      .filter((row) => row.isExtra)
      .map((row) => ({
        materialProductId: row.materialProductId,
        actualQuantity: row.actual,
        productUnitId: row.productUnitId ?? null,
      }));

    const body = {
      productionDefinitionId: definitionId,
      outputQuantity: outQty,
      branchId: workspace.branchId,
      notes: notes.trim() || null,
      referenceNumber: referenceNumber.trim() || null,
      outputExpirationDate: expirationDate.trim() || null,
      outputLotNumber: lotNumber.trim() || null,
      materialOverrides: overrides.length > 0 ? overrides : null,
      extraMaterials: extrasPayload.length > 0 ? extrasPayload : null,
      productionRunId,
    };

    try {
      // Recipe create should have enabled tracking; recover if an older output is still untracked.
      try {
        const outputInv = await getInventoryProduct(workspace, definition.outputProductId);
        if (outputInv.isTracked !== true) {
          await enableInventoryTracking(workspace, definition.outputProductId, {
            openingQuantity: 0,
          });
        }
      } catch {
        try {
          await enableInventoryTracking(workspace, definition.outputProductId, {
            openingQuantity: 0,
          });
        } catch (enableErr) {
          setError(
            enableErr instanceof PosApiError
              ? (enableErr.problem.detail ?? t("production.recipes.enableTrackingFailed"))
              : t("production.recipes.enableTrackingFailed"),
          );
          return;
        }
      }

      const created = await createProductionRun(workspace, body);
      runIdRef.current = null;
      await queryClient.invalidateQueries({ queryKey: ["inventory"] });
      await queryClient.invalidateQueries({ queryKey: ["production"] });
      await queryClient.invalidateQueries({ queryKey: ["pos-catalog-browse"] });
      await queryClient.invalidateQueries({ queryKey: ["catalog"] });
      await queryClient.invalidateQueries({ queryKey: ["pos-catalog"] });
      showToast(
        t("production.produce.successToast")
          .replace("{qty}", String(outQty))
          .replace("{name}", created.outputNameSnapshot || definition.name),
        "success",
      );
      navigate(`/inventory/production/runs/${created.productionRunId}`, { replace: true });
    } catch (err) {
      if (isLikelyNetworkFailure(err)) {
        setError(t("checkout.confirmingTransaction"));
        try {
          const created = await createProductionRun(workspace, body);
          runIdRef.current = null;
          navigate(`/inventory/production/runs/${created.productionRunId}`, { replace: true });
          return;
        } catch (retryErr) {
          if (isLikelyNetworkFailure(retryErr)) {
            setStatusLocked(true);
            setError(t("checkout.transactionStatusUnknown"));
            return;
          }
          setError(
            retryErr instanceof PosApiError
              ? (retryErr.problem.detail ?? t("production.produce.saveFailed"))
              : t("production.produce.saveFailed"),
          );
          return;
        }
      }
      setError(
        err instanceof PosApiError
          ? (err.problem.detail ?? t("production.produce.saveFailed"))
          : t("production.produce.saveFailed"),
      );
    } finally {
      setSaving(false);
    }
  }

  const activeDefinitions = definitionsQuery.data?.items ?? [];

  return (
    <div
      className="production-run-create-page exits-page flex min-w-0 flex-col gap-3 pb-4"
      data-testid="production-run-create-page"
    >
      <PageHeader
        title={t("production.produce.title")}
        description={t("production.produce.lede")}
        backTo="/inventory/production"
        backLabel={t("production.backHome")}
        backTestId="page-header-back-production"
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
        {t("production.produce.selectSetup")}
        <select
          className="exits-select"
          value={definitionId ?? ""}
          onChange={(e) => onSelectDefinition(e.target.value)}
          disabled={!allowManage || statusLocked}
          data-testid="production-run-definition"
        >
          <option value="">{t("production.produce.chooseSetup")}</option>
          {activeDefinitions.map((item) => (
            <option key={item.productionDefinitionId} value={item.productionDefinitionId}>
              {item.name} — {item.outputQuantityEntered} · {item.componentCount}{" "}
              {t("production.produce.ingredientsCountLabel")}
            </option>
          ))}
        </select>
      </label>

      {definitionsQuery.isSuccess && activeDefinitions.length === 0 ? (
        <EmptyState
              variant="setup"
              align="center"
              icon={<Package className="size-5" strokeWidth={1.75} />}
          title={t("production.setups.empty")}
          detail={t("production.produce.noActiveSetups")}
        />
      ) : null}

      {definition ? (
        <>
          <Card className="flex flex-col gap-1 p-3" data-testid="production-run-recipe-summary">
            <div className="font-medium">{definition.name}</div>
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
              {t("production.setups.revision").replace("{revision}", String(definition.revision))}
              {" · "}
              {t("production.produce.standardOutput")}: {definition.outputQuantityEntered}
              {outputUom ? ` ${outputUom}` : ""}
            </p>
          </Card>

          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            {t("production.produce.outputQuantity")}
            <div className="flex flex-wrap items-center gap-2">
              <input
                type="number"
                min={0}
                step="any"
                className="min-w-0 flex-1 rounded-md border border-border bg-background px-3"
                value={outputQuantity}
                onChange={(e) => {
                  const raw = e.target.value;
                  setOutputQuantity(raw);
                  const qty = Number(raw);
                  if (Number.isFinite(qty) && qty > 0) {
                    syncActualsToExpected(definition, qty);
                  }
                }}
                disabled={!allowManage || statusLocked}
                data-testid="production-run-output-qty"
              />
              {outputUom ? (
                <span className="text-[length:var(--exits-text-sm)] text-muted">{outputUom}</span>
              ) : null}
            </div>
          </label>
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("production.produce.scaleHint")}
          </p>
          {maxProducible != null ? (
            <p
              className="m-0 text-[length:var(--exits-text-sm)] text-muted"
              data-testid="production-run-max-producible"
            >
              {t("production.produce.maxProducible").replace(
                "{qty}",
                `${maxProducible}${outputUom ? ` ${outputUom}` : ""}`,
              )}
            </p>
          ) : null}

          <section className="flex flex-col gap-2" data-testid="production-run-materials">
            <h2 className="m-0 text-[length:var(--exits-text-md)] font-medium">
              {t("production.produce.materials")}
            </h2>
            {materialPreviewQuery.isLoading ? (
              <LoadingState label={t("production.loading")} />
            ) : null}
            <ul className="m-0 flex list-none flex-col gap-2 p-0">
              {materials.map((row) => {
                const short =
                  row.available != null && row.actual > row.available + 1e-9;
                return (
                  <li key={`${row.isExtra ? "extra" : "recipe"}-${row.materialProductId}`}>
                    <Card
                      className="flex flex-col gap-2 p-3"
                      data-testid={`production-run-material-${row.materialProductId}`}
                    >
                      <div className="flex flex-wrap items-baseline justify-between gap-2">
                        <div className="font-medium">{row.name}</div>
                        {row.isExtra ? (
                          <span className="text-[length:var(--exits-text-xs)] text-muted">
                            {t("production.produce.extraBadge")}
                          </span>
                        ) : null}
                      </div>
                      <p className="m-0 text-[length:var(--exits-text-sm)]">
                        {t("production.produce.required")}: {row.actual} {row.uom}
                      </p>
                      {row.available != null ? (
                        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                          {t("production.produce.available")}: {row.available} {row.uom}
                        </p>
                      ) : null}
                      {short ? (
                        <p
                          className="m-0 text-[length:var(--exits-text-sm)] text-destructive"
                          data-testid={`production-availability-short-${row.materialProductId}`}
                        >
                          {t("production.produce.shortBy").replace(
                            "{quantity}",
                            `${Math.round((row.actual - (row.available ?? 0)) * 1000) / 1000} ${row.uom}`,
                          )}
                        </p>
                      ) : row.available != null ? (
                        <p
                          className="m-0 text-[length:var(--exits-text-sm)] text-muted"
                          data-testid={`production-availability-ok-${row.materialProductId}`}
                        >
                          {t("production.produce.availabilityOk")}
                        </p>
                      ) : null}
                      {!row.isExtra ? (
                        <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
                          {t("production.produce.actual")}
                          <input
                            type="number"
                            min={0}
                            step="any"
                            className="rounded-md border border-border bg-background px-3"
                            value={
                              actualByProduct[row.materialProductId] ?? String(row.actual)
                            }
                            onChange={(e) =>
                              setActualByProduct((prev) => ({
                                ...prev,
                                [row.materialProductId]: e.target.value,
                              }))
                            }
                            disabled={!allowManage || statusLocked}
                            data-testid={`production-run-actual-${row.materialProductId}`}
                          />
                        </label>
                      ) : (
                        <Button
                          type="button"
                          variant="ghost"
                          className="w-fit"
                          disabled={!allowManage || statusLocked}
                          onClick={() =>
                            setExtraMaterials((prev) =>
                              prev.filter((m) => m.materialProductId !== row.materialProductId),
                            )
                          }
                        >
                          {t("production.setups.removeMaterial")}
                        </Button>
                      )}
                    </Card>
                  </li>
                );
              })}
            </ul>

            {shortages.length > 0 ? (
              <Card
                className="flex flex-col gap-2 border-destructive/40 p-3"
                data-testid="production-run-shortage-summary"
              >
                <p className="m-0 font-medium text-destructive">
                  {t("production.produce.cannotProduce").replace(
                    "{qty}",
                    `${outputQuantity}${outputUom ? ` ${outputUom}` : ""}`,
                  )}
                </p>
                <ul className="m-0 list-disc pl-5 text-[length:var(--exits-text-sm)]">
                  {shortages.map((item) => (
                    <li key={item.materialProductId}>
                      {item.name}: {t("production.produce.required")} {item.required} {item.uom},{" "}
                      {t("production.produce.available")} {item.available} {item.uom},{" "}
                      {t("production.produce.shortBy").replace(
                        "{quantity}",
                        `${item.shortBy} ${item.uom}`,
                      )}
                    </li>
                  ))}
                </ul>
              </Card>
            ) : null}

            <Button
              type="button"
              variant="outline"
              className="w-fit"
              disabled={!allowManage || statusLocked}
              data-testid="production-run-add-ingredient"
              onClick={() => setAddIngredientOpen((open) => !open)}
            >
              {t("production.produce.addIngredient")}
            </Button>

            {addIngredientOpen ? (
              <Card className="flex flex-col gap-2 p-3" data-testid="production-run-extra-picker">
                <SearchField
                  label={t("production.setups.searchMaterial")}
                  value={ingredientSearch}
                  onChange={(e) => setIngredientSearch(e.target.value)}
                  onClear={() => setIngredientSearch("")}
                  placeholder={t("production.setups.searchMaterialPlaceholder")}
                  data-testid="production-run-extra-search"
                />
                <ul className="m-0 flex list-none flex-col gap-2 p-0">
                  {extraCandidates.map((product) => (
                    <li key={product.productId}>
                      <button
                        type="button"
                        className="flex w-full flex-col gap-0.5 rounded-md border border-border p-3 text-left"
                        data-testid={`production-run-extra-candidate-${product.productId}`}
                        onClick={() => void openExtraSheet(product)}
                      >
                        <span className="font-medium">{product.name}</span>
                        <span className="text-[length:var(--exits-text-sm)] text-muted">
                          {materialBaseUomLabel(product)}
                        </span>
                      </button>
                    </li>
                  ))}
                </ul>
              </Card>
            ) : null}
          </section>

          {tracksExpiration ? (
            <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
              {t("production.produce.expiration")}
              <input
                type="date"
                className="rounded-md border border-border bg-background px-3"
                value={expirationDate}
                onChange={(e) => setExpirationDate(e.target.value)}
                disabled={!allowManage || statusLocked}
                data-testid="production-run-expiration"
              />
            </label>
          ) : null}

          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            {t("production.produce.lotNumber")}
            <input
              className="rounded-md border border-border bg-background px-3"
              value={lotNumber}
              onChange={(e) => setLotNumber(e.target.value)}
              disabled={!allowManage || statusLocked}
              placeholder={t("production.produce.optional")}
            />
          </label>

          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            {t("production.produce.reference")}
            <input
              className="rounded-md border border-border bg-background px-3"
              value={referenceNumber}
              onChange={(e) => setReferenceNumber(e.target.value)}
              disabled={!allowManage || statusLocked}
              placeholder={t("production.produce.optional")}
            />
          </label>

          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            {t("production.produce.notes")}
            <textarea
              className="min-h-20 rounded-md border border-border bg-background px-3 py-2"
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              disabled={!allowManage || statusLocked}
              placeholder={t("production.produce.optional")}
            />
          </label>
        </>
      ) : null}

      <ProductionMaterialQuantitySheet
        open={Boolean(qtySheetProduct)}
        product={qtySheetProduct}
        onCancel={() => setQtySheetProduct(null)}
        onConfirm={confirmExtra}
      />

      <StickyActionBar>
        <Button
          type="button"
          className="w-full"
          disabled={
            !allowManage ||
            !online ||
            saving ||
            statusLocked ||
            !definitionId ||
            materials.length === 0 ||
            produceBlocked
          }
          onClick={() => void submit()}
          data-testid="production-run-submit"
        >
          {saving
            ? t("production.produce.submitting")
            : t("production.produce.submitWithQty").replace(
                "{qty}",
                `${outputQuantity || "—"}${outputUom ? ` ${outputUom}` : ""}`,
              )}
        </Button>
      </StickyActionBar>
    </div>
  );
}
