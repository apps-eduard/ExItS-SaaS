import { useEffect, useState } from "react";
import { zodResolver } from "@hookform/resolvers/zod";
import { Archive, Loader2, PlayCircle, PowerOff, Save } from "lucide-react";
import { useForm } from "react-hook-form";
import { PLATFORM_PERMISSIONS } from "@/api/authorization/authorization-types";
import type { CatalogProduct } from "@/api/catalog/product-catalog-client";
import { classifyCommercialMutationFailure } from "@/api/commercial/commercial-errors";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { ConfirmActionDialog } from "@/components/exits/ConfirmActionDialog";
import { DashboardSection } from "@/components/exits/dashboard/DashboardSection";
import { StatusIndicator } from "@/components/exits/StatusIndicator";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  useActivateProductMutation,
  useDeactivateProductMutation,
  useRenameProductMutation,
  useRetireProductMutation,
} from "@/features/commercial/use-commercial-mutations";
import {
  productLifecycleActions,
  productRenameValues,
} from "@/features/products/product-lifecycle-mapping";
import { productMutationFailureCopy } from "@/features/products/product-mutation-feedback";
import {
  productRenameSchema,
  type ProductRenameValues,
} from "@/features/products/product-lifecycle-schema";
import { useAuthorization } from "@/hooks/use-authorization";
import { usePreferences } from "@/hooks/use-preferences";
import type { MessageKey } from "@/lib/i18n/messages";

type Feedback = { tone: "info" | "danger"; title: string; detail: string };
type LifecycleConfirm = "activate" | "deactivate" | "retire";

export function ProductLifecycleOperator({ product }: { product: CatalogProduct }) {
  const { t } = usePreferences();
  const authorization = useAuthorization();
  const canManage = authorization.hasPermission(PLATFORM_PERMISSIONS.manageCatalog);
  const [feedback, setFeedback] = useState<Feedback | null>(null);
  const [lifecycleConfirm, setLifecycleConfirm] = useState<LifecycleConfirm | null>(null);

  const renameMutation = useRenameProductMutation();
  const activateMutation = useActivateProductMutation();
  const deactivateMutation = useDeactivateProductMutation();
  const retireMutation = useRetireProductMutation();

  const renameForm = useForm<ProductRenameValues>({
    resolver: async (values, context, options) =>
      zodResolver(productRenameSchema)(values, context, options),
    defaultValues: productRenameValues(product),
  });

  useEffect(() => {
    renameForm.reset(productRenameValues(product));
  }, [product, renameForm]);

  const lifecycle = productLifecycleActions(product.status);
  const mutationBusy =
    renameMutation.isPending ||
    activateMutation.isPending ||
    deactivateMutation.isPending ||
    retireMutation.isPending;

  function showSuccess(messageKey: MessageKey) {
    setFeedback({
      tone: "info",
      title: t("products.mutation.success.title"),
      detail: t(messageKey),
    });
  }

  function handleMutationFailure(error: unknown) {
    const copy = productMutationFailureCopy(error, t);
    setFeedback({ tone: "danger", title: copy.title, detail: copy.detail });
    if (classifyCommercialMutationFailure(error).kind === "conflict") {
      renameForm.reset(productRenameValues(product));
    }
  }

  async function saveRename(values: ProductRenameValues) {
    if (!canManage) return;
    setFeedback(null);
    try {
      await renameMutation.mutateAsync({
        productId: product.id,
        body: {
          displayName: values.displayName,
          expectedUpdatedAtUtc: product.updatedAtUtc ?? null,
        },
      });
      showSuccess("products.mutation.success.rename");
    } catch (error) {
      handleMutationFailure(error);
    }
  }

  async function runLifecycle(action: LifecycleConfirm) {
    if (!canManage) return;
    setFeedback(null);
    try {
      const input = { productId: product.id };
      if (action === "activate") {
        await activateMutation.mutateAsync(input);
        showSuccess("products.mutation.success.activate");
      } else if (action === "deactivate") {
        await deactivateMutation.mutateAsync(input);
        showSuccess("products.mutation.success.deactivate");
      } else {
        await retireMutation.mutateAsync(input);
        showSuccess("products.mutation.success.retire");
      }
    } catch (error) {
      handleMutationFailure(error);
    } finally {
      setLifecycleConfirm(null);
    }
  }

  return (
    <div className="grid gap-4">
      {feedback ? (
        <Alert title={feedback.title} tone={feedback.tone === "danger" ? "danger" : "info"}>
          {feedback.detail}
        </Alert>
      ) : null}

      {!canManage ? (
        <Alert title={t("products.editor.readOnly.title")}>{t("products.editor.readOnly.body")}</Alert>
      ) : null}

      <DashboardSection
        title={t("products.detail.identity")}
        description={t("products.editor.identity.hint")}
      >
        <form className="grid gap-3" onSubmit={renameForm.handleSubmit(saveRename)}>
          <dl className="grid gap-2 text-[length:var(--exits-text-sm)] sm:grid-cols-2">
            <div className="min-w-0">
              <dt className="text-[length:var(--exits-text-xs)] text-muted">
                {t("products.column.code")}
              </dt>
              <dd className="break-all font-mono text-[length:var(--exits-text-xs)]">{product.code}</dd>
            </div>
            <div className="min-w-0">
              <dt className="text-[length:var(--exits-text-xs)] text-muted">
                {t("products.column.status")}
              </dt>
              <dd>
                <StatusIndicator
                  tone={
                    product.status === "Active"
                      ? "success"
                      : product.status === "Retired"
                        ? "danger"
                        : "warning"
                  }
                  label={product.status}
                />
              </dd>
            </div>
            <div className="min-w-0 sm:col-span-2">
              <dt className="text-[length:var(--exits-text-xs)] text-muted">
                {t("products.detail.field.id")}
              </dt>
              <dd className="break-all font-mono text-[length:var(--exits-text-xs)]">{product.id}</dd>
            </div>
          </dl>
          <div className="grid gap-1">
            <Label htmlFor="product-display-name">{t("products.column.displayName")}</Label>
            <Input
              id="product-display-name"
              disabled={!canManage || mutationBusy || product.status === "Retired"}
              {...renameForm.register("displayName")}
            />
            {renameForm.formState.errors.displayName ? (
              <p className="text-[length:var(--exits-text-xs)] text-danger">
                {renameForm.formState.errors.displayName.message}
              </p>
            ) : null}
          </div>
          {canManage && product.status !== "Retired" ? (
            <div>
              <Button type="submit" disabled={mutationBusy || !renameForm.formState.isDirty}>
                {renameMutation.isPending ? (
                  <Loader2 aria-hidden className="mr-2 size-4 animate-spin" />
                ) : (
                  <Save aria-hidden className="mr-2 size-4" />
                )}
                {t("products.editor.rename.save")}
              </Button>
            </div>
          ) : null}
        </form>
      </DashboardSection>

      <DashboardSection title={t("products.editor.lifecycle.title")}>
        {product.status === "Retired" ? (
          <p className="text-[length:var(--exits-text-sm)] text-muted">
            {t("products.editor.lifecycle.retired")}
          </p>
        ) : (
          <div className="flex flex-wrap gap-2">
            {lifecycle.canActivate ? (
              <Button
                type="button"
                variant="secondary"
                disabled={!canManage || mutationBusy}
                onClick={() => setLifecycleConfirm("activate")}
              >
                <PlayCircle aria-hidden className="mr-2 size-4" />
                {t("products.editor.lifecycle.activate")}
              </Button>
            ) : null}
            {lifecycle.canDeactivate ? (
              <Button
                type="button"
                variant="secondary"
                disabled={!canManage || mutationBusy}
                onClick={() => setLifecycleConfirm("deactivate")}
              >
                <PowerOff aria-hidden className="mr-2 size-4" />
                {t("products.editor.lifecycle.deactivate")}
              </Button>
            ) : null}
            {lifecycle.canRetire ? (
              <Button
                type="button"
                variant="destructive"
                disabled={!canManage || mutationBusy}
                onClick={() => setLifecycleConfirm("retire")}
              >
                <Archive aria-hidden className="mr-2 size-4" />
                {t("products.editor.lifecycle.retire")}
              </Button>
            ) : null}
          </div>
        )}
      </DashboardSection>

      <ConfirmActionDialog
        open={lifecycleConfirm != null}
        title={
          lifecycleConfirm === "activate"
            ? t("products.editor.lifecycle.confirmActivate.title")
            : lifecycleConfirm === "deactivate"
              ? t("products.editor.lifecycle.confirmDeactivate.title")
              : t("products.editor.lifecycle.confirmRetire.title")
        }
        description={
          lifecycleConfirm === "activate"
            ? t("products.editor.lifecycle.confirmActivate.body")
            : lifecycleConfirm === "deactivate"
              ? t("products.editor.lifecycle.confirmDeactivate.body")
              : t("products.editor.lifecycle.confirmRetire.body")
        }
        confirmLabel={t("products.editor.lifecycle.confirmAction")}
        cancelLabel={t("products.editor.dialog.cancel")}
        pendingLabel={t("products.editor.dialog.pending")}
        pending={mutationBusy}
        destructive={lifecycleConfirm === "retire"}
        onConfirm={() => lifecycleConfirm && void runLifecycle(lifecycleConfirm)}
        onCancel={() => setLifecycleConfirm(null)}
      />
    </div>
  );
}
