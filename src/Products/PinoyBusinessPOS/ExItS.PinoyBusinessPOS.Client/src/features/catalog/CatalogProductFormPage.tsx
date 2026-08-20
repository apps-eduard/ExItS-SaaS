import { useEffect, useMemo, useState, type FormEvent } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  createCatalogCategory,
  createCatalogProduct,
  deactivateCatalogProduct,
  getCatalogProduct,
  listCatalogCategories,
  reactivateCatalogProduct,
  updateCatalogProduct,
  uploadCatalogProductImage,
} from "@/api/pos/pos-catalog-client";
import {
  DEFAULT_CATALOG_SELLING_MODE,
  DEFAULT_CATALOG_SELLING_PRICE,
  DEFAULT_CATALOG_UNIT_OF_MEASURE,
  POS_SELLING_MODE_CODES,
  POS_UNIT_OF_MEASURE_CODES,
  type PosSellingModeCode,
  type PosUnitOfMeasureCode,
} from "@/api/pos/pos-catalog-options";
import { PosApiError } from "@/api/pos/pos-http";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import {
  createEmptyUnitDraft,
  draftsToUnitInputs,
  unitsFromDto,
  validateUnitDrafts,
  type ProductUnitDraft,
} from "@/features/catalog/product-unit-drafts";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

export function CatalogProductFormPage({ mode }: { mode: "create" | "edit" }) {
  const { t } = useI18n();
  const navigate = useNavigate();
  const { productId } = useParams();
  const queryClient = useQueryClient();
  const { boundWorkspace } = useWorkspace();
  const workspace = useMemo(
    () =>
      boundWorkspace
        ? {
            organizationId: boundWorkspace.organizationId,
            branchId: boundWorkspace.branchId,
          }
        : null,
    [boundWorkspace],
  );

  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [sku, setSku] = useState("");
  const [barcode, setBarcode] = useState("");
  const [categoryId, setCategoryId] = useState("");
  const [canBeSold, setCanBeSold] = useState(true);
  const [unitOfMeasure, setUnitOfMeasure] = useState<PosUnitOfMeasureCode>(
    DEFAULT_CATALOG_UNIT_OF_MEASURE,
  );
  const [sellingMode, setSellingMode] = useState<PosSellingModeCode>(DEFAULT_CATALOG_SELLING_MODE);
  const [sellingPrice, setSellingPrice] = useState(String(DEFAULT_CATALOG_SELLING_PRICE));
  const [configurePackages, setConfigurePackages] = useState(false);
  const [unitDrafts, setUnitDrafts] = useState<ProductUnitDraft[]>([]);
  const [expectedUpdatedAtUtc, setExpectedUpdatedAtUtc] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [newCategoryName, setNewCategoryName] = useState("");

  const categoriesQuery = useQuery({
    queryKey: ["catalog", "categories", workspace?.organizationId, workspace?.branchId],
    enabled: Boolean(workspace),
    queryFn: ({ signal }) => listCatalogCategories(workspace!, { status: "Active" }, signal),
  });

  const productQuery = useQuery({
    queryKey: ["catalog", "product", workspace?.organizationId, productId],
    enabled: Boolean(workspace) && mode === "edit" && Boolean(productId),
    queryFn: ({ signal }) => getCatalogProduct(workspace!, productId!, signal),
  });

  useEffect(() => {
    if (!productQuery.data) {
      return;
    }
    const product = productQuery.data;
    setName(product.name);
    setDescription(product.description ?? "");
    setSku(product.sku ?? "");
    setBarcode(product.barcode ?? "");
    setCategoryId(product.categoryId ?? "");
    setCanBeSold(product.canBeSold !== false);
    setUnitOfMeasure(
      (POS_UNIT_OF_MEASURE_CODES.includes(product.unitOfMeasure as PosUnitOfMeasureCode)
        ? product.unitOfMeasure
        : DEFAULT_CATALOG_UNIT_OF_MEASURE) as PosUnitOfMeasureCode,
    );
    setSellingMode(
      (POS_SELLING_MODE_CODES.includes(product.sellingMode as PosSellingModeCode)
        ? product.sellingMode
        : DEFAULT_CATALOG_SELLING_MODE) as PosSellingModeCode,
    );
    setSellingPrice(String(product.sellingPrice ?? DEFAULT_CATALOG_SELLING_PRICE));
    const drafts = unitsFromDto(product.units);
    setUnitDrafts(drafts);
    setConfigurePackages(drafts.length > 0);
    setExpectedUpdatedAtUtc(product.updatedAtUtc);
  }, [productQuery.data]);

  useEffect(() => {
    if (sellingMode === "ByWeight") {
      setUnitOfMeasure("Kilogram");
    }
  }, [sellingMode]);

  const saveMutation = useMutation({
    mutationFn: async () => {
      if (!workspace) {
        throw new Error("Workspace required");
      }
      const price = Number(sellingPrice);
      if (Number.isNaN(price) || price < 0) {
        throw new Error(t("catalog.invalidPrice"));
      }
      if (sellingMode === "ByWeight" && unitOfMeasure !== "Kilogram") {
        throw new Error(t("catalog.byWeightRequiresKg"));
      }

      let unitsPayload = undefined as ReturnType<typeof draftsToUnitInputs> | undefined;
      if (configurePackages) {
        const validation = validateUnitDrafts(unitDrafts);
        if (validation) {
          throw new Error(validation);
        }
        unitsPayload = draftsToUnitInputs(unitDrafts);
      }

      const body = {
        name: name.trim(),
        description: description.trim() || null,
        sku: sku.trim() || null,
        barcode: barcode.trim() || null,
        categoryId: categoryId || null,
        unitOfMeasure,
        sellingPrice: price,
        sellingMode,
        canBeSold,
        units: unitsPayload,
      };
      if (mode === "create") {
        return createCatalogProduct(workspace, body);
      }
      return updateCatalogProduct(workspace, productId!, {
        ...body,
        expectedUpdatedAtUtc,
        // Omit units when not configuring — preserve server units.
        units: configurePackages ? unitsPayload : undefined,
      });
    },
    onSuccess: async (product) => {
      await queryClient.invalidateQueries({ queryKey: ["catalog"] });
      setExpectedUpdatedAtUtc(product.updatedAtUtc);
      setError(null);
      if (mode === "create") {
        navigate(`/catalog/products/${product.productId}/edit`, { replace: true });
      }
    },
    onError: (err) => {
      if (err instanceof PosApiError) {
        if (err.status === 409) {
          setError(err.problem.detail ?? t("catalog.conflict"));
          return;
        }
        setError(err.problem.detail ?? err.message);
        return;
      }
      setError((err as Error).message);
    },
  });

  async function handleCreateCategory(event: FormEvent) {
    event.preventDefault();
    if (!workspace || !newCategoryName.trim()) {
      return;
    }
    try {
      const created = await createCatalogCategory(workspace, { name: newCategoryName.trim() });
      await queryClient.invalidateQueries({ queryKey: ["catalog", "categories"] });
      setCategoryId(created.categoryId);
      setNewCategoryName("");
    } catch (err) {
      setError(
        err instanceof PosApiError ? (err.problem.detail ?? err.message) : (err as Error).message,
      );
    }
  }

  function updateDraft(key: string, patch: Partial<ProductUnitDraft>) {
    setUnitDrafts((current) =>
      current.map((draft) => (draft.key === key ? { ...draft, ...patch } : draft)),
    );
  }

  if (!workspace || (mode === "edit" && productQuery.isLoading)) {
    return <LoadingState label={t("loading.label")} />;
  }

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="catalog-product-form">
      <PageHeader
        title={mode === "create" ? t("catalog.newProduct") : t("catalog.editProduct")}
        description={t("catalog.productFormLede")}
      />
      {error ? <ErrorState title={t("error.title")} detail={error} /> : null}
      <Card>
        <form
          className="flex flex-col gap-3"
          onSubmit={(event) => {
            event.preventDefault();
            saveMutation.mutate();
          }}
        >
          <Input
            label={t("catalog.name")}
            name="productName"
            required
            value={name}
            onChange={(e) => setName(e.target.value)}
          />
          <Input
            label={t("catalog.description")}
            name="productDescription"
            value={description}
            onChange={(e) => setDescription(e.target.value)}
          />
          <Input
            label={t("catalog.sku")}
            name="productSku"
            value={sku}
            onChange={(e) => setSku(e.target.value)}
          />
          <Input
            label={t("catalog.barcode")}
            name="productBarcode"
            value={barcode}
            onChange={(e) => setBarcode(e.target.value)}
          />
          <label className="flex flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold">
            {t("catalog.category")}
            <select
              className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-background px-3 font-normal"
              value={categoryId}
              onChange={(e) => setCategoryId(e.target.value)}
            >
              <option value="">{t("catalog.noCategory")}</option>
              {categoriesQuery.data?.items.map((category) => (
                <option key={category.categoryId} value={category.categoryId}>
                  {category.name}
                </option>
              ))}
            </select>
          </label>

          <label className="flex flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold">
            {t("catalog.baseUnit")}
            <select
              data-testid="catalog-base-uom"
              className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-background px-3 font-normal"
              value={unitOfMeasure}
              disabled={sellingMode === "ByWeight"}
              onChange={(e) => setUnitOfMeasure(e.target.value as PosUnitOfMeasureCode)}
            >
              {POS_UNIT_OF_MEASURE_CODES.map((code) => (
                <option key={code} value={code}>
                  {code}
                </option>
              ))}
            </select>
          </label>

          <label className="flex flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold">
            {t("catalog.sellingMode")}
            <select
              data-testid="catalog-selling-mode"
              className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-background px-3 font-normal"
              value={sellingMode}
              onChange={(e) => setSellingMode(e.target.value as PosSellingModeCode)}
            >
              {POS_SELLING_MODE_CODES.map((code) => (
                <option key={code} value={code}>
                  {code === "PerItem"
                    ? t("catalog.sellingModePerItem")
                    : t("catalog.sellingModeByWeight")}
                </option>
              ))}
            </select>
          </label>

          <Input
            label={t("catalog.baseSellingPrice")}
            name="sellingPrice"
            inputMode="decimal"
            value={sellingPrice}
            onChange={(e) => setSellingPrice(e.target.value)}
          />

          <label className="flex min-h-11 items-center gap-2 text-[length:var(--exits-text-sm)] font-semibold">
            <input
              type="checkbox"
              checked={canBeSold}
              onChange={(e) => setCanBeSold(e.target.checked)}
            />
            {t("catalog.canBeSold")}
          </label>

          <label className="flex min-h-11 items-center gap-2 text-[length:var(--exits-text-sm)] font-semibold">
            <input
              type="checkbox"
              data-testid="catalog-configure-packages"
              checked={configurePackages}
              onChange={(e) => {
                const next = e.target.checked;
                setConfigurePackages(next);
                if (next && unitDrafts.length === 0) {
                  setUnitDrafts([createEmptyUnitDraft("Purchase"), createEmptyUnitDraft("Sell")]);
                }
              }}
            />
            {t("catalog.configurePackages")}
          </label>

          {configurePackages ? (
            <div className="flex flex-col gap-3" data-testid="catalog-unit-editor">
              <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                {t("catalog.packagesLede")}
              </p>
              {unitDrafts.map((draft) => (
                <Card key={draft.key} className="flex flex-col gap-2 p-3">
                  <div className="flex flex-wrap items-center justify-between gap-2">
                    <span className="font-semibold">{draft.kind}</span>
                    <Button
                      type="button"
                      variant="ghost"
                      className="min-h-11"
                      onClick={() =>
                        setUnitDrafts((current) => current.filter((row) => row.key !== draft.key))
                      }
                    >
                      {t("catalog.removeUnit")}
                    </Button>
                  </div>
                  <Input
                    label={t("catalog.unitDisplayName")}
                    name={`${draft.key}-name`}
                    value={draft.displayName}
                    onChange={(e) => updateDraft(draft.key, { displayName: e.target.value })}
                  />
                  <Input
                    label={t("catalog.unitShortLabel")}
                    name={`${draft.key}-short`}
                    value={draft.shortLabel}
                    onChange={(e) => updateDraft(draft.key, { shortLabel: e.target.value })}
                  />
                  <Input
                    label={t("catalog.multiplierToBase")}
                    name={`${draft.key}-mult`}
                    inputMode="decimal"
                    value={draft.multiplierToBase}
                    onChange={(e) => updateDraft(draft.key, { multiplierToBase: e.target.value })}
                  />
                  {draft.kind === "Sell" ? (
                    <Input
                      label={t("catalog.unitSellingPrice")}
                      name={`${draft.key}-price`}
                      inputMode="decimal"
                      value={draft.sellingPrice}
                      onChange={(e) => updateDraft(draft.key, { sellingPrice: e.target.value })}
                    />
                  ) : null}
                  {draft.kind === "Sell" ? (
                    <label className="flex min-h-11 items-center gap-2 text-[length:var(--exits-text-sm)] font-semibold">
                      <input
                        type="checkbox"
                        checked={draft.allowsCustomQuantity}
                        onChange={(e) =>
                          updateDraft(draft.key, { allowsCustomQuantity: e.target.checked })
                        }
                      />
                      {t("catalog.allowsCustomQuantity")}
                    </label>
                  ) : null}
                </Card>
              ))}
              <div className="flex flex-wrap gap-2">
                <Button
                  type="button"
                  variant="ghost"
                  className="min-h-11"
                  onClick={() =>
                    setUnitDrafts((current) => [...current, createEmptyUnitDraft("Purchase")])
                  }
                >
                  {t("catalog.addPurchaseUnit")}
                </Button>
                <Button
                  type="button"
                  variant="ghost"
                  className="min-h-11"
                  onClick={() =>
                    setUnitDrafts((current) => [...current, createEmptyUnitDraft("Sell")])
                  }
                >
                  {t("catalog.addSellUnit")}
                </Button>
              </div>
            </div>
          ) : null}

          {mode === "edit" && productId ? (
            <Input
              label={t("catalog.image")}
              name="productImage"
              type="file"
              accept="image/jpeg,image/png,image/webp"
              onChange={(event) => {
                const file = event.target.files?.[0];
                if (!file || !workspace) {
                  return;
                }
                void uploadCatalogProductImage(workspace, productId, file)
                  .then(() => queryClient.invalidateQueries({ queryKey: ["catalog"] }))
                  .catch((err) =>
                    setError(
                      err instanceof PosApiError
                        ? (err.problem.detail ?? err.message)
                        : (err as Error).message,
                    ),
                  );
              }}
            />
          ) : null}
          <div className="flex flex-wrap gap-2">
            <Button type="submit" className="min-h-11" disabled={saveMutation.isPending}>
              {saveMutation.isPending ? t("catalog.saving") : t("catalog.save")}
            </Button>
            <Button asChild variant="ghost" className="min-h-11">
              <Link to="/catalog">{t("catalog.back")}</Link>
            </Button>
            {mode === "edit" && productQuery.data?.status === "Active" ? (
              <Button
                type="button"
                variant="ghost"
                className="min-h-11"
                onClick={() => {
                  if (!workspace || !productId) {
                    return;
                  }
                  void deactivateCatalogProduct(workspace, productId)
                    .then(() => queryClient.invalidateQueries({ queryKey: ["catalog"] }))
                    .then(() => navigate("/catalog"));
                }}
              >
                {t("catalog.deactivate")}
              </Button>
            ) : null}
            {mode === "edit" && productQuery.data?.status !== "Active" ? (
              <Button
                type="button"
                variant="ghost"
                className="min-h-11"
                onClick={() => {
                  if (!workspace || !productId) {
                    return;
                  }
                  void reactivateCatalogProduct(workspace, productId).then(() =>
                    queryClient.invalidateQueries({ queryKey: ["catalog"] }),
                  );
                }}
              >
                {t("catalog.reactivate")}
              </Button>
            ) : null}
          </div>
        </form>
      </Card>
      <Card>
        <form
          className="flex flex-col gap-2 sm:flex-row sm:items-end"
          onSubmit={(event) => void handleCreateCategory(event)}
        >
          <div className="min-w-0 flex-1">
            <Input
              label={t("catalog.newCategoryPlaceholder")}
              name="inlineCategoryName"
              value={newCategoryName}
              onChange={(e) => setNewCategoryName(e.target.value)}
              placeholder={t("catalog.newCategoryPlaceholder")}
            />
          </div>
          <Button type="submit" variant="ghost" className="min-h-11">
            {t("catalog.addCategory")}
          </Button>
        </form>
      </Card>
    </div>
  );
}

export function CatalogProductCreatePage() {
  return <CatalogProductFormPage mode="create" />;
}

export function CatalogProductEditPage() {
  return <CatalogProductFormPage mode="edit" />;
}
