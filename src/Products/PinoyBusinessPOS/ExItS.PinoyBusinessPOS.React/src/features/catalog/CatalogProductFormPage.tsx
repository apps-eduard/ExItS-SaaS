import { useEffect, useState, type ReactNode } from "react";
import { Ban, Loader2, Plus, RotateCcw, Save } from "lucide-react";
import { useNavigate, useParams } from "react-router-dom";

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

import { Input } from "@/components/ui/input";

import { ErrorState } from "@/components/exits/ErrorState";

import { LoadingState } from "@/components/exits/LoadingState";

import { PageHeader } from "@/components/exits/PageHeader";

import { StatusChip } from "@/components/exits/StatusChip";

import { pageBackNav } from "@/navigation/page-back-nav";

import {
  createEmptyUnitDraft,
  draftsToUnitInputs,
  unitsFromDto,
  validateUnitDrafts,
  type ProductUnitDraft,
} from "@/features/catalog/product-unit-drafts";

import { useI18n } from "@/i18n/I18nProvider";

import { usePosWorkspaceScope } from "@/workspace/use-pos-workspace-scope";

function FormSelect({
  label,

  name,

  value,

  disabled,

  testId,

  onChange,

  children,
}: {
  label: string;

  name: string;

  value: string;

  disabled?: boolean;

  testId?: string;

  onChange: (value: string) => void;

  children: ReactNode;
}) {
  const fieldId = name;

  return (
    <label className="catalog-form-field flex min-w-0 flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold">
      {label}

      <select
        id={fieldId}

        name={name}

        data-testid={testId}

        className="catalog-form-select"

        value={value}

        disabled={disabled}

        onChange={(event) => onChange(event.target.value)}
      >
        {children}
      </select>
    </label>
  );
}

function FormCheck({
  label,

  checked,

  testId,

  onChange,
}: {
  label: string;

  checked: boolean;

  testId?: string;

  onChange: (checked: boolean) => void;
}) {
  return (
    <label className="catalog-form-check catalog-form-field--full">
      <input
        type="checkbox"

        data-testid={testId}

        checked={checked}

        onChange={(event) => onChange(event.target.checked)}
      />

      {label}
    </label>
  );
}

export function CatalogProductFormPage({ mode }: { mode: "create" | "edit" }) {
  const { t } = useI18n();

  const navigate = useNavigate();

  const { productId } = useParams();

  const queryClient = useQueryClient();

  const workspace = usePosWorkspaceScope();

  const [name, setName] = useState("");

  const [description, setDescription] = useState("");

  const [sku, setSku] = useState("");

  const [barcode, setBarcode] = useState("");

  const [categoryId, setCategoryId] = useState("");

  const [canBeSold, setCanBeSold] = useState(true);

  const [tracksExpiration, setTracksExpiration] = useState(false);

  const [expirationWarningDays, setExpirationWarningDays] = useState("7");

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

    setTracksExpiration(product.tracksExpiration === true);

    setExpirationWarningDays(String(product.expirationWarningDays ?? 7));

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

      const warningDays = Number(expirationWarningDays);

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

        tracksExpiration,

        expirationWarningDays:
          tracksExpiration && !Number.isNaN(warningDays) && warningDays > 0 ? warningDays : null,
      };

      if (mode === "create") {
        return createCatalogProduct(workspace, body);
      }

      return updateCatalogProduct(workspace, productId!, {
        ...body,

        expectedUpdatedAtUtc,

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

  const statusMutation = useMutation({
    mutationFn: async (action: "deactivate" | "reactivate") => {
      if (!workspace || !productId) {
        throw new Error("Workspace required");
      }
      if (action === "deactivate") {
        await deactivateCatalogProduct(workspace, productId);
        return action;
      }
      await reactivateCatalogProduct(workspace, productId);
      return action;
    },
    onSuccess: async (action) => {
      await queryClient.invalidateQueries({ queryKey: ["catalog"] });
      setError(null);
      if (action === "deactivate") {
        navigate("/catalog");
      }
    },
    onError: (err) => {
      setError(
        err instanceof PosApiError ? (err.problem.detail ?? err.message) : (err as Error).message,
      );
    },
  });

  const createCategoryMutation = useMutation({
    mutationFn: async () => {
      if (!workspace || !newCategoryName.trim()) {
        throw new Error("Category name required");
      }
      return createCatalogCategory(workspace, { name: newCategoryName.trim() });
    },
    onSuccess: async (created) => {
      await queryClient.invalidateQueries({ queryKey: ["catalog", "categories"] });
      setCategoryId(created.categoryId);
      setNewCategoryName("");
      setError(null);
    },
    onError: (err) => {
      setError(
        err instanceof PosApiError ? (err.problem.detail ?? err.message) : (err as Error).message,
      );
    },
  });

  function updateDraft(key: string, patch: Partial<ProductUnitDraft>) {
    setUnitDrafts((current) =>
      current.map((draft) => (draft.key === key ? { ...draft, ...patch } : draft)),
    );
  }

  if (!workspace || (mode === "edit" && productQuery.isLoading)) {
    return <LoadingState label={t("loading.label")} />;
  }

  const productStatus = productQuery.data?.status;

  const isActive = productStatus?.toLowerCase() === "active";

  return (
    <div
      className="catalog-form-page exits-page flex min-w-0 flex-col gap-3"
      data-testid="catalog-product-form"
    >
      <PageHeader
        title={mode === "create" ? t("catalog.newProduct") : t("catalog.editProduct")}

        subtitle={mode === "edit" && name.trim() ? name.trim() : undefined}

        description={t("catalog.productFormLede")}

        backTo={pageBackNav.catalog.to}

        backLabel={t(pageBackNav.catalog.labelKey)}

        backTestId="page-header-back-catalog"

        trailing={
          mode === "edit" && productStatus ? (
            <StatusChip tone={isActive ? "success" : "warning"}>{productStatus}</StatusChip>
          ) : undefined
        }
      />

      {error ? <ErrorState title={t("error.title")} detail={error} /> : null}

      <form
        className="flex flex-col gap-3"

        onSubmit={(event) => {
          event.preventDefault();

          saveMutation.mutate();
        }}
      >
        <section className="catalog-form-section exits-animate-panel">
          <h2 className="catalog-form-section__title">{t("catalog.sectionBasics")}</h2>

          <div className="catalog-form-section__grid">
            <div className="catalog-form-field--full">
              <Input
                label={t("catalog.name")}

                name="productName"

                required

                value={name}

                onChange={(e) => setName(e.target.value)}
              />
            </div>

            <div className="catalog-form-field--full">
              <Input
                label={t("catalog.description")}

                name="productDescription"

                value={description}

                onChange={(e) => setDescription(e.target.value)}
              />
            </div>

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

            <FormSelect
              label={t("catalog.category")}

              name="productCategory"

              value={categoryId}

              onChange={setCategoryId}
            >
              <option value="">{t("catalog.noCategory")}</option>

              {categoriesQuery.data?.items.map((category) => (
                <option key={category.categoryId} value={category.categoryId}>
                  {category.name}
                </option>
              ))}
            </FormSelect>
          </div>

          <div className="catalog-form-quick-add">
            <p className="catalog-form-quick-add__label">{t("catalog.sectionCategoryQuickAdd")}</p>
            <div className="catalog-form-quick-add__row">
              <div className="catalog-form-quick-add__field">
                <Input
                  label={t("catalog.newCategoryPlaceholder")}
                  name="inlineCategoryName"
                  value={newCategoryName}
                  onChange={(e) => setNewCategoryName(e.target.value)}
                  placeholder={t("catalog.newCategoryPlaceholder")}
                  onKeyDown={(event) => {
                    if (event.key !== "Enter") {
                      return;
                    }
                    event.preventDefault();
                    if (newCategoryName.trim() && !createCategoryMutation.isPending) {
                      createCategoryMutation.mutate();
                    }
                  }}
                />
              </div>
              <Button
                type="button"
                variant="outline"
                className="catalog-form-quick-add__button"
                data-testid="catalog-add-category"
                disabled={!newCategoryName.trim() || createCategoryMutation.isPending}
                onClick={() => createCategoryMutation.mutate()}
              >
                {createCategoryMutation.isPending ? (
                  <Loader2 className="size-4 shrink-0 animate-spin" aria-hidden />
                ) : (
                  <Plus className="size-4 shrink-0" aria-hidden />
                )}
                {createCategoryMutation.isPending
                  ? t("catalog.addingCategory")
                  : t("catalog.addCategory")}
              </Button>
            </div>
          </div>
        </section>

        <section className="catalog-form-section exits-animate-panel">
          <h2 className="catalog-form-section__title">{t("catalog.sectionPricing")}</h2>

          <div className="catalog-form-section__grid">
            <FormSelect
              label={t("catalog.baseUnit")}

              name="productBaseUom"

              testId="catalog-base-uom"

              value={unitOfMeasure}

              disabled={sellingMode === "ByWeight"}

              onChange={(value) => setUnitOfMeasure(value as PosUnitOfMeasureCode)}
            >
              {POS_UNIT_OF_MEASURE_CODES.map((code) => (
                <option key={code} value={code}>
                  {code}
                </option>
              ))}
            </FormSelect>

            <FormSelect
              label={t("catalog.sellingMode")}

              name="productSellingMode"

              testId="catalog-selling-mode"

              value={sellingMode}

              onChange={(value) => setSellingMode(value as PosSellingModeCode)}
            >
              {POS_SELLING_MODE_CODES.map((code) => (
                <option key={code} value={code}>
                  {code === "PerItem"
                    ? t("catalog.sellingModePerItem")
                    : t("catalog.sellingModeByWeight")}
                </option>
              ))}
            </FormSelect>

            <Input
              label={t("catalog.baseSellingPrice")}

              name="sellingPrice"

              inputMode="decimal"

              value={sellingPrice}

              onChange={(e) => setSellingPrice(e.target.value)}
            />

            <FormCheck
              label={t("catalog.canBeSold")}

              checked={canBeSold}

              onChange={setCanBeSold}
            />
          </div>
        </section>

        <section className="catalog-form-section exits-animate-panel">
          <h2 className="catalog-form-section__title">{t("catalog.sectionExpiration")}</h2>

          <div className="catalog-form-section__grid">
            <FormCheck
              label={t("catalog.tracksExpiration")}

              checked={tracksExpiration}

              testId="catalog-tracks-expiration"

              onChange={setTracksExpiration}
            />

            {tracksExpiration ? (
              <Input
                label={t("catalog.expirationWarningDays")}

                name="expirationWarningDays"

                inputMode="numeric"

                value={expirationWarningDays}

                onChange={(e) => setExpirationWarningDays(e.target.value)}

                data-testid="catalog-expiration-warning-days"
              />
            ) : null}

            {tracksExpiration ? (
              <p className="catalog-form-field--full m-0 text-[length:var(--exits-text-sm)] text-muted">
                {t("catalog.expirationWarningHint")}
              </p>
            ) : null}
          </div>
        </section>

        <section className="catalog-form-section exits-animate-panel">
          <h2 className="catalog-form-section__title">{t("catalog.sectionPackages")}</h2>

          <FormCheck
            label={t("catalog.configurePackages")}

            checked={configurePackages}

            testId="catalog-configure-packages"

            onChange={(next) => {
              setConfigurePackages(next);

              if (next && unitDrafts.length === 0) {
                setUnitDrafts([createEmptyUnitDraft("Purchase"), createEmptyUnitDraft("Sell")]);
              }
            }}
          />

          {configurePackages ? (
            <div className="flex flex-col gap-3" data-testid="catalog-unit-editor">
              <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                {t("catalog.packagesLede")}
              </p>

              {unitDrafts.map((draft) => (
                <div key={draft.key} className="catalog-form-unit-card">
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
                    <FormCheck
                      label={t("catalog.allowsCustomQuantity")}

                      checked={draft.allowsCustomQuantity}

                      onChange={(checked) =>
                        updateDraft(draft.key, { allowsCustomQuantity: checked })
                      }
                    />
                  ) : null}
                </div>
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
        </section>

        {mode === "edit" && productId ? (
          <section className="catalog-form-section exits-animate-panel">
            <h2 className="catalog-form-section__title">{t("catalog.sectionImage")}</h2>

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
          </section>
        ) : null}

        <div className="catalog-form-actions" data-testid="catalog-form-actions">
          <div className="catalog-form-actions__primary">
            <Button
              type="submit"
              className="catalog-form-actions__save"
              data-testid="catalog-save"
              disabled={saveMutation.isPending || statusMutation.isPending}
            >
              {saveMutation.isPending ? (
                <>
                  <Loader2 className="size-4 shrink-0 animate-spin" aria-hidden />
                  {t("catalog.saving")}
                </>
              ) : (
                <>
                  <Save className="size-4 shrink-0" aria-hidden />
                  {t("catalog.save")}
                </>
              )}
            </Button>
          </div>

          {mode === "edit" && productQuery.data?.status === "Active" ? (
            <div className="catalog-form-actions__secondary">
              <Button
                type="button"
                variant="destructive"
                className="catalog-form-actions__danger"
                data-testid="catalog-deactivate"
                disabled={saveMutation.isPending || statusMutation.isPending}
                onClick={() => statusMutation.mutate("deactivate")}
              >
                {statusMutation.isPending ? (
                  <Loader2 className="size-4 shrink-0 animate-spin" aria-hidden />
                ) : (
                  <Ban className="size-4 shrink-0" aria-hidden />
                )}
                {t("catalog.deactivate")}
              </Button>
            </div>
          ) : null}

          {mode === "edit" && productQuery.data?.status !== "Active" ? (
            <div className="catalog-form-actions__secondary">
              <Button
                type="button"
                variant="outline"
                className="catalog-form-actions__restore"
                data-testid="catalog-reactivate"
                disabled={saveMutation.isPending || statusMutation.isPending}
                onClick={() => statusMutation.mutate("reactivate")}
              >
                {statusMutation.isPending ? (
                  <Loader2 className="size-4 shrink-0 animate-spin" aria-hidden />
                ) : (
                  <RotateCcw className="size-4 shrink-0" aria-hidden />
                )}
                {t("catalog.reactivate")}
              </Button>
            </div>
          ) : null}
        </div>
      </form>
    </div>
  );
}

export function CatalogProductCreatePage() {
  return <CatalogProductFormPage mode="create" />;
}

export function CatalogProductEditPage() {
  return <CatalogProductFormPage mode="edit" />;
}
