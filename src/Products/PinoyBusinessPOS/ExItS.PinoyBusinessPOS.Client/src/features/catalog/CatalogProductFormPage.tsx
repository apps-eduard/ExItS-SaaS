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
  DEFAULT_CATALOG_SELLING_PRICE,
  DEFAULT_CATALOG_UNIT_OF_MEASURE,
} from "@/api/pos/pos-catalog-types";
import { PosApiError } from "@/api/pos/pos-http";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
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
    setName(productQuery.data.name);
    setDescription(productQuery.data.description ?? "");
    setSku(productQuery.data.sku ?? "");
    setBarcode(productQuery.data.barcode ?? "");
    setCategoryId(productQuery.data.categoryId ?? "");
    setCanBeSold(productQuery.data.canBeSold !== false);
    setExpectedUpdatedAtUtc(productQuery.data.updatedAtUtc);
  }, [productQuery.data]);

  const saveMutation = useMutation({
    mutationFn: async () => {
      if (!workspace) {
        throw new Error("Workspace required");
      }
      const body = {
        name: name.trim(),
        description: description.trim() || null,
        sku: sku.trim() || null,
        barcode: barcode.trim() || null,
        categoryId: categoryId || null,
        unitOfMeasure: productQuery.data?.unitOfMeasure ?? DEFAULT_CATALOG_UNIT_OF_MEASURE,
        sellingPrice: productQuery.data?.sellingPrice ?? DEFAULT_CATALOG_SELLING_PRICE,
        sellingMode: productQuery.data?.sellingMode ?? "ByUnit",
        canBeSold,
      };
      if (mode === "create") {
        return createCatalogProduct(workspace, body);
      }
      return updateCatalogProduct(workspace, productId!, {
        ...body,
        expectedUpdatedAtUtc,
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
          <label className="flex min-h-11 items-center gap-2 text-[length:var(--exits-text-sm)] font-semibold">
            <input
              type="checkbox"
              checked={canBeSold}
              onChange={(e) => setCanBeSold(e.target.checked)}
            />
            {t("catalog.canBeSold")}
          </label>
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
