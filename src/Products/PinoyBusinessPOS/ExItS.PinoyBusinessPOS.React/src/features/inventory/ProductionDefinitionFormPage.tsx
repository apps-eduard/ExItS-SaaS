import { useEffect, useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
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
import {
  resolveBusinessUsage,
  type ProductBusinessUsage,
} from "@/features/catalog/product-business-usage";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

type DraftMaterial = {
  materialProductId: string;
  name: string;
  quantity: number;
  uom: string;
};

function matchesUsage(
  product: PosCatalogProductDto,
  allowed: readonly ProductBusinessUsage[],
): boolean {
  return allowed.includes(resolveBusinessUsage(product));
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
  const [outputName, setOutputName] = useState("");
  const [outputUom, setOutputUom] = useState("");
  const [outputQuantity, setOutputQuantity] = useState("1");
  const [materials, setMaterials] = useState<DraftMaterial[]>([]);
  const [materialQty, setMaterialQty] = useState<Record<string, string>>({});
  const [outputSearch, setOutputSearch] = useState("");
  const [materialSearch, setMaterialSearch] = useState("");
  const [debouncedOutput, setDebouncedOutput] = useState("");
  const [debouncedMaterial, setDebouncedMaterial] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [hydrated, setHydrated] = useState(!isEdit);

  useEffect(() => {
    const handle = window.setTimeout(() => setDebouncedOutput(outputSearch.trim()), 250);
    return () => window.clearTimeout(handle);
  }, [outputSearch]);

  useEffect(() => {
    const handle = window.setTimeout(() => setDebouncedMaterial(materialSearch.trim()), 250);
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
      setOutputQuantity(String(def.outputQuantityEntered));
      try {
        const output = await getCatalogProduct(workspace!, def.outputProductId);
        if (!cancelled) {
          setOutputName(output.name);
          setOutputUom(output.unitOfMeasure);
        }
      } catch {
        if (!cancelled) {
          setOutputName(def.outputProductId);
        }
      }
      const loaded: DraftMaterial[] = [];
      const qtyMap: Record<string, string> = {};
      for (const component of def.components) {
        let productName = component.materialProductId;
        let uom = "";
        try {
          const product = await getCatalogProduct(workspace!, component.materialProductId);
          productName = product.name;
          uom = product.unitOfMeasure;
        } catch {
          // keep id fallback
        }
        loaded.push({
          materialProductId: component.materialProductId,
          name: productName,
          quantity: component.quantityEntered,
          uom,
        });
        qtyMap[component.materialProductId] = String(component.quantityEntered);
      }
      if (!cancelled) {
        setMaterials(loaded);
        setMaterialQty(qtyMap);
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
      debouncedMaterial,
    ],
    enabled: Boolean(workspace) && online && allowManage,
    queryFn: ({ signal }) =>
      listCatalogProducts(
        workspace!,
        { search: debouncedMaterial || undefined, status: "Active", pageSize: 40 },
        signal,
      ),
  });

  const outputCandidates = useMemo(() => {
    const items = outputPickerQuery.data?.items ?? [];
    return items.filter(
      (p) =>
        matchesUsage(p, ["ProducedItem", "Resale"]) ||
        p.isProduced === true ||
        p.canBeSold !== false,
    );
  }, [outputPickerQuery.data?.items]);

  const materialCandidates = useMemo(() => {
    const items = materialPickerQuery.data?.items ?? [];
    const selected = new Set(materials.map((m) => m.materialProductId));
    return items.filter((p) => {
      if (selected.has(p.productId) || p.productId === outputProductId) {
        return false;
      }
      return (
        p.canBeUsedAsIngredient === true ||
        matchesUsage(p, ["Ingredient"]) ||
        resolveBusinessUsage(p) === "Ingredient"
      );
    });
  }, [materialPickerQuery.data?.items, materials, outputProductId]);

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
    setOutputName(product.name);
    setOutputUom(product.unitOfMeasure);
    setOutputSearch("");
    setError(null);
  }

  function addMaterial(product: PosCatalogProductDto) {
    const raw = materialQty[product.productId] ?? "1";
    const qty = Number(raw);
    if (!Number.isFinite(qty) || qty <= 0) {
      setError(t("production.setups.invalidQuantity"));
      return;
    }
    setMaterials((prev) => [
      ...prev.filter((m) => m.materialProductId !== product.productId),
      {
        materialProductId: product.productId,
        name: product.name,
        quantity: qty,
        uom: product.unitOfMeasure,
      },
    ]);
    setMaterialQty((prev) => ({ ...prev, [product.productId]: "1" }));
    setError(null);
  }

  function updateMaterialQty(productId: string, raw: string) {
    setMaterialQty((prev) => ({ ...prev, [productId]: raw }));
    const qty = Number(raw);
    if (!Number.isFinite(qty) || qty <= 0) {
      return;
    }
    setMaterials((prev) =>
      prev.map((m) => (m.materialProductId === productId ? { ...m, quantity: qty } : m)),
    );
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
    if (!outputProductId) {
      setError(t("production.setups.needOutput"));
      return;
    }
    const outQty = Number(outputQuantity);
    if (!Number.isFinite(outQty) || outQty <= 0) {
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
        outputQuantity: outQty,
        components: materials.map((m, index) => ({
          materialProductId: m.materialProductId,
          quantity: m.quantity,
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
          className="min-h-11 rounded-md border border-border bg-background px-3"
          value={name}
          onChange={(e) => setName(e.target.value)}
          disabled={!allowManage}
          data-testid="production-setup-name"
        />
      </label>

      <section className="flex flex-col gap-2">
        <h2 className="m-0 text-[length:var(--exits-text-md)] font-medium">
          {t("production.setups.outputProduct")}
        </h2>
        {outputProductId ? (
          <Card className="flex flex-col gap-2 p-3">
            <div className="font-medium">{outputName}</div>
            <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
              {t("production.setups.outputQuantity")}
              <input
                type="number"
                min={0}
                step="any"
                className="min-h-11 rounded-md border border-border bg-background px-3"
                value={outputQuantity}
                onChange={(e) => setOutputQuantity(e.target.value)}
                disabled={!allowManage}
                data-testid="production-setup-output-qty"
              />
            </label>
            {outputUom ? (
              <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{outputUom}</p>
            ) : null}
            <Button
              type="button"
              variant="ghost"
              className="min-h-11 w-fit"
              disabled={!allowManage}
              onClick={() => {
                setOutputProductId(null);
                setOutputName("");
                setOutputUom("");
              }}
            >
              {t("production.setups.changeProduct")}
            </Button>
          </Card>
        ) : (
          <>
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
            <ul className="m-0 flex list-none flex-col gap-2 p-0">
              {outputCandidates.map((product) => (
                <li key={product.productId}>
                  <Card className="flex flex-wrap items-center justify-between gap-2 p-3">
                    <div className="min-w-0">
                      <div className="font-medium">{product.name}</div>
                      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                        {product.unitOfMeasure}
                      </p>
                    </div>
                    <Button
                      type="button"
                      className="min-h-11"
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

      <section className="flex flex-col gap-2">
        <h2 className="m-0 text-[length:var(--exits-text-md)] font-medium">
          {t("production.setups.materials")}
        </h2>
        {materials.length === 0 ? (
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("production.setups.draftEmpty")}
          </p>
        ) : (
          <ul className="m-0 flex list-none flex-col gap-2 p-0">
            {materials.map((material) => (
              <li key={material.materialProductId}>
                <Card className="flex flex-col gap-2 p-3">
                  <div className="font-medium">{material.name}</div>
                  <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
                    {t("production.setups.materialQuantity")}
                    <input
                      type="number"
                      min={0}
                      step="any"
                      className="min-h-11 rounded-md border border-border bg-background px-3"
                      value={
                        materialQty[material.materialProductId] ?? String(material.quantity)
                      }
                      onChange={(e) =>
                        updateMaterialQty(material.materialProductId, e.target.value)
                      }
                      disabled={!allowManage}
                    />
                  </label>
                  <Button
                    type="button"
                    variant="ghost"
                    className="min-h-11 w-fit"
                    disabled={!allowManage}
                    onClick={() =>
                      setMaterials((prev) =>
                        prev.filter((m) => m.materialProductId !== material.materialProductId),
                      )
                    }
                  >
                    {t("production.setups.removeMaterial")}
                  </Button>
                </Card>
              </li>
            ))}
          </ul>
        )}

        <SearchField
          label={t("production.setups.searchMaterial")}
          value={materialSearch}
          onChange={(e) => setMaterialSearch(e.target.value)}
          onClear={() => setMaterialSearch("")}
          placeholder={t("production.setups.searchMaterial")}
          data-testid="production-setup-material-search"
        />
        <ul className="m-0 flex list-none flex-col gap-2 p-0">
          {materialCandidates.map((product) => (
            <li key={product.productId}>
              <Card className="flex flex-col gap-2 p-3">
                <div className="font-medium">{product.name}</div>
                <div className="flex flex-wrap items-end gap-2">
                  <label className="flex min-w-[5.5rem] flex-1 flex-col gap-1 text-[length:var(--exits-text-sm)]">
                    {t("production.setups.materialQuantity")}
                    <input
                      type="number"
                      min={0}
                      step="any"
                      className="min-h-11 rounded-md border border-border bg-background px-3"
                      value={materialQty[product.productId] ?? "1"}
                      onChange={(e) =>
                        setMaterialQty((prev) => ({
                          ...prev,
                          [product.productId]: e.target.value,
                        }))
                      }
                      disabled={!allowManage}
                    />
                  </label>
                  <Button
                    type="button"
                    className="min-h-11"
                    disabled={!allowManage || !online}
                    onClick={() => addMaterial(product)}
                  >
                    {t("production.setups.addMaterial")}
                  </Button>
                </div>
              </Card>
            </li>
          ))}
        </ul>
      </section>

      <StickyActionBar>
        <Button
          type="button"
          className="min-h-11 w-full"
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
