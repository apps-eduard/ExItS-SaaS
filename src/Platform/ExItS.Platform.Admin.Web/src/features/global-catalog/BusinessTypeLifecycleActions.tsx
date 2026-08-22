import { useState } from "react";
import type {
  GlobalBusinessTypeDetail,
  GlobalBusinessTypeStatus,
} from "@/api/global-catalog/global-catalog-types";
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
  status: GlobalBusinessTypeStatus;
  labelKey:
    | "globalCatalog.lifecycle.activate"
    | "globalCatalog.lifecycle.deactivate"
    | "globalCatalog.lifecycle.archive";
  destructive?: boolean;
};

function availableBusinessTypeActions(status: GlobalBusinessTypeStatus): LifecycleAction[] {
  if (status === "Active") {
    return [
      { status: "Inactive", labelKey: "globalCatalog.lifecycle.deactivate" },
      { status: "Archived", labelKey: "globalCatalog.lifecycle.archive", destructive: true },
    ];
  }
  if (status === "Inactive") {
    return [
      { status: "Active", labelKey: "globalCatalog.lifecycle.activate" },
      { status: "Archived", labelKey: "globalCatalog.lifecycle.archive", destructive: true },
    ];
  }
  return [
    { status: "Active", labelKey: "globalCatalog.lifecycle.activate" },
    { status: "Inactive", labelKey: "globalCatalog.lifecycle.deactivate" },
  ];
}

export function BusinessTypeLifecycleActions({
  businessType,
  canManage,
  onStatusChanged,
}: {
  businessType: GlobalBusinessTypeDetail;
  canManage: boolean;
  onStatusChanged?: () => void;
}) {
  const { t } = usePreferences();
  const { changeBusinessTypeStatus } = useGlobalCatalogMutations();
  const [pendingAction, setPendingAction] = useState<LifecycleAction | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  if (!canManage) {
    return null;
  }

  const actions = availableBusinessTypeActions(businessType.status);

  async function runAction() {
    if (!pendingAction || !businessType.updatedAtUtc) {
      return;
    }
    setErrorMessage(null);
    try {
      await changeBusinessTypeStatus.mutateAsync({
        businessTypeId: businessType.id,
        status: pendingAction.status,
        expectedUpdatedAtUtc: businessType.updatedAtUtc,
      });
      setPendingAction(null);
      onStatusChanged?.();
    } catch (error) {
      const failure = classifyGlobalCatalogMutationFailure(error);
      if (failure.kind === "conflict") {
        onStatusChanged?.();
      }
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
        pending={changeBusinessTypeStatus.isPending}
        error={
          errorMessage ? (
            <p className="text-[length:var(--exits-text-sm)] text-danger" role="alert">
              {errorMessage}
            </p>
          ) : undefined
        }
        onCancel={() => {
          if (!changeBusinessTypeStatus.isPending) {
            setPendingAction(null);
            setErrorMessage(null);
          }
        }}
        onConfirm={() => void runAction()}
      />
    </>
  );
}
