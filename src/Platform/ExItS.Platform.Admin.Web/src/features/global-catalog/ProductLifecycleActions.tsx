import { useState } from "react";
import type { GlobalProductDetail, GlobalProductStatus } from "@/api/global-catalog/global-catalog-types";
import { classifyGlobalCatalogMutationFailure } from "@/api/global-catalog/global-catalog-errors";
import { ConfirmActionDialog } from "@/components/exits/ConfirmActionDialog";
import { Button } from "@/components/ui/button";
import {
  globalCatalogMutationDetail,
  globalCatalogMutationMessageKey,
} from "@/features/global-catalog/global-catalog-mutation-feedback";
import { useGlobalCatalogMutations } from "@/features/global-catalog/use-global-catalog-mutations";
import { usePreferences } from "@/hooks/use-preferences";

type LifecycleAction = {
  status: GlobalProductStatus;
  labelKey:
    | "globalCatalog.lifecycle.activate"
    | "globalCatalog.lifecycle.draft"
    | "globalCatalog.lifecycle.archive";
  destructive?: boolean;
};

function availableProductActions(status: GlobalProductStatus): LifecycleAction[] {
  if (status === "Draft") {
    return [
      { status: "Active", labelKey: "globalCatalog.lifecycle.activate" },
      { status: "Archived", labelKey: "globalCatalog.lifecycle.archive", destructive: true },
    ];
  }
  if (status === "Active") {
    return [
      { status: "Draft", labelKey: "globalCatalog.lifecycle.draft" },
      { status: "Archived", labelKey: "globalCatalog.lifecycle.archive", destructive: true },
    ];
  }
  return [{ status: "Active", labelKey: "globalCatalog.lifecycle.activate" }];
}

export function ProductLifecycleActions({
  product,
  canManage,
}: {
  product: GlobalProductDetail;
  canManage: boolean;
}) {
  const { t } = usePreferences();
  const { changeProductStatus } = useGlobalCatalogMutations();
  const [pendingAction, setPendingAction] = useState<LifecycleAction | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  if (!canManage) {
    return null;
  }

  const actions = availableProductActions(product.status);

  async function runAction() {
    if (!pendingAction || !product.updatedAtUtc) {
      return;
    }
    setErrorMessage(null);
    try {
      await changeProductStatus.mutateAsync({
        productId: product.id,
        status: pendingAction.status,
        expectedUpdatedAtUtc: product.updatedAtUtc!,
      });
      setPendingAction(null);
    } catch (error) {
      const failure = classifyGlobalCatalogMutationFailure(error);
      setErrorMessage(globalCatalogMutationDetail(failure) ?? t(globalCatalogMutationMessageKey(failure)));
    }
  }

  return (
    <>
      <div className="flex flex-wrap gap-2">
        {actions.map((action) => (
          <Button
            key={action.status}
            type="button"
            size="sm"
            variant={action.destructive ? "destructive" : "outline"}
            onClick={() => {
              setErrorMessage(null);
              setPendingAction(action);
            }}
          >
            {t(action.labelKey)}
          </Button>
        ))}
      </div>

      <ConfirmActionDialog
        open={pendingAction != null}
        title={pendingAction ? t("globalCatalog.lifecycle.confirmTitle") : ""}
        description={pendingAction ? t("globalCatalog.lifecycle.confirmBody") : ""}
        confirmLabel={pendingAction ? t(pendingAction.labelKey) : t("globalCatalog.save")}
        cancelLabel={t("globalCatalog.cancel")}
        pendingLabel={t("globalCatalog.saving")}
        destructive={pendingAction?.destructive}
        pending={changeProductStatus.isPending}
        error={
          errorMessage ? (
            <p className="text-[length:var(--exits-text-sm)] text-danger" role="alert">
              {errorMessage}
            </p>
          ) : undefined
        }
        onCancel={() => {
          if (!changeProductStatus.isPending) {
            setPendingAction(null);
            setErrorMessage(null);
          }
        }}
        onConfirm={() => void runAction()}
      />
    </>
  );
}
