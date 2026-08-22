import { useRef, useState } from "react";
import type { GlobalProductDetail } from "@/api/global-catalog/global-catalog-types";
import { classifyGlobalCatalogMutationFailure } from "@/api/global-catalog/global-catalog-errors";
import { globalProductImageUrl } from "@/api/global-catalog/global-catalog-http";
import { ConfirmActionDialog } from "@/components/exits/ConfirmActionDialog";
import { Button } from "@/components/ui/button";
import {
  globalCatalogMutationDetail,
  globalCatalogMutationMessageKey,
} from "@/features/global-catalog/global-catalog-mutation-feedback";
import { useGlobalCatalogMutations } from "@/features/global-catalog/use-global-catalog-mutations";
import { usePreferences } from "@/hooks/use-preferences";
import { env } from "@/lib/env";

export function ProductImagePanel({
  product,
  canManage,
}: {
  product: GlobalProductDetail;
  canManage: boolean;
}) {
  const { t } = usePreferences();
  const inputRef = useRef<HTMLInputElement>(null);
  const { uploadProductImage, removeProductImage } = useGlobalCatalogMutations();
  const [confirmRemove, setConfirmRemove] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const previewUrl =
    product.hasImage && product.imageVersion != null
      ? globalProductImageUrl(env.platformApiBaseUrl, product.id, "medium", product.imageVersion)
      : null;

  async function onUpload(file: File) {
    setErrorMessage(null);
    try {
      await uploadProductImage.mutateAsync({ productId: product.id, file });
    } catch (error) {
      const failure = classifyGlobalCatalogMutationFailure(error);
      setErrorMessage(globalCatalogMutationDetail(failure) ?? t(globalCatalogMutationMessageKey(failure)));
    }
  }

  async function onRemove() {
    setErrorMessage(null);
    try {
      await removeProductImage.mutateAsync(product.id);
      setConfirmRemove(false);
    } catch (error) {
      const failure = classifyGlobalCatalogMutationFailure(error);
      setErrorMessage(globalCatalogMutationDetail(failure) ?? t(globalCatalogMutationMessageKey(failure)));
    }
  }

  return (
    <section className="grid gap-3 rounded-[var(--exits-density-radius)] border border-border bg-surface p-4">
      <div className="flex flex-wrap items-start justify-between gap-2">
        <div>
          <h2 className="text-[length:var(--exits-text-base)] font-semibold">
            {t("globalCatalog.image.title")}
          </h2>
          <p className="mt-0.5 text-[length:var(--exits-text-sm)] text-muted">
            {t("globalCatalog.image.description")}
          </p>
        </div>
        {canManage ? (
          <div className="flex flex-wrap gap-2">
            <Button
              type="button"
              size="sm"
              disabled={uploadProductImage.isPending}
              onClick={() => inputRef.current?.click()}
            >
              {product.hasImage ? t("globalCatalog.image.replace") : t("globalCatalog.image.upload")}
            </Button>
            {product.hasImage ? (
              <Button
                type="button"
                size="sm"
                variant="destructive"
                disabled={removeProductImage.isPending}
                onClick={() => setConfirmRemove(true)}
              >
                {t("globalCatalog.image.remove")}
              </Button>
            ) : null}
          </div>
        ) : null}
      </div>

      <input
        ref={inputRef}
        type="file"
        accept="image/jpeg,image/png,image/webp"
        className="sr-only"
        onChange={(event) => {
          const file = event.target.files?.[0];
          event.target.value = "";
          if (file) {
            void onUpload(file);
          }
        }}
      />

      {previewUrl ? (
        <img
          src={previewUrl}
          alt={product.name}
          className="max-h-48 w-auto rounded-[var(--exits-density-radius)] border border-border object-contain"
        />
      ) : (
        <p className="text-[length:var(--exits-text-sm)] text-muted">{t("globalCatalog.image.empty")}</p>
      )}

      {errorMessage ? (
        <p className="text-[length:var(--exits-text-sm)] text-danger" role="alert">
          {errorMessage}
        </p>
      ) : null}

      <ConfirmActionDialog
        open={confirmRemove}
        title={t("globalCatalog.image.removeConfirmTitle")}
        description={t("globalCatalog.image.removeConfirmBody")}
        confirmLabel={t("globalCatalog.image.remove")}
        cancelLabel={t("globalCatalog.cancel")}
        pendingLabel={t("globalCatalog.saving")}
        destructive
        pending={removeProductImage.isPending}
        onCancel={() => {
          if (!removeProductImage.isPending) {
            setConfirmRemove(false);
          }
        }}
        onConfirm={() => void onRemove()}
      />
    </section>
  );
}
