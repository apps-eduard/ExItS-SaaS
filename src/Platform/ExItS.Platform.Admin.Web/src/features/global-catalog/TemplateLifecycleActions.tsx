import { useState } from "react";
import type { GlobalCatalogTemplateDetail } from "@/api/global-catalog/global-catalog-types";
import { classifyGlobalCatalogMutationFailure } from "@/api/global-catalog/global-catalog-errors";
import { ConfirmActionDialog } from "@/components/exits/ConfirmActionDialog";
import { Button } from "@/components/ui/button";
import {
  globalCatalogMutationDetail,
  globalCatalogMutationMessageKey,
} from "@/features/global-catalog/global-catalog-mutation-feedback";
import { useGlobalCatalogMutations } from "@/features/global-catalog/use-global-catalog-mutations";
import { usePreferences } from "@/hooks/use-preferences";
import type { MessageKey } from "@/lib/i18n/messages";

type LifecycleAction = {
  kind: "publish" | "unpublish" | "archive";
  labelKey: MessageKey;
  destructive?: boolean;
};

function availableTemplateActions(status: GlobalCatalogTemplateDetail["status"]): LifecycleAction[] {
  if (status === "Draft") {
    return [
      { kind: "publish", labelKey: "globalCatalog.templates.lifecycle.publish" },
      { kind: "archive", labelKey: "globalCatalog.lifecycle.archive", destructive: true },
    ];
  }
  if (status === "Published") {
    return [
      { kind: "unpublish", labelKey: "globalCatalog.templates.lifecycle.unpublish" },
      { kind: "archive", labelKey: "globalCatalog.lifecycle.archive", destructive: true },
    ];
  }
  return [];
}

export function TemplateLifecycleActions({
  template,
  canPublish,
  onChanged,
}: {
  template: GlobalCatalogTemplateDetail;
  canPublish: boolean;
  onChanged?: () => void;
}) {
  const { t } = usePreferences();
  const { publishTemplate, unpublishTemplate, archiveTemplate } = useGlobalCatalogMutations();
  const [pendingAction, setPendingAction] = useState<LifecycleAction | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  if (!canPublish || template.status === "Archived") {
    return null;
  }

  const actions = availableTemplateActions(template.status);
  const pending =
    publishTemplate.isPending || unpublishTemplate.isPending || archiveTemplate.isPending;

  async function runAction() {
    if (!pendingAction || !template.updatedAtUtc) {
      return;
    }
    setErrorMessage(null);
    try {
      const args = {
        templateId: template.id,
        expectedUpdatedAtUtc: template.updatedAtUtc,
      };
      if (pendingAction.kind === "publish") {
        await publishTemplate.mutateAsync(args);
      } else if (pendingAction.kind === "unpublish") {
        await unpublishTemplate.mutateAsync(args);
      } else {
        await archiveTemplate.mutateAsync(args);
      }
      setPendingAction(null);
      onChanged?.();
    } catch (error) {
      const failure = classifyGlobalCatalogMutationFailure(error);
      if (failure.kind === "conflict") {
        onChanged?.();
      }
      setErrorMessage(globalCatalogMutationDetail(failure) ?? t(globalCatalogMutationMessageKey(failure)));
    }
  }

  return (
    <>
      <div className="flex flex-wrap gap-2">
        {actions.map((action) => (
          <Button
            key={action.kind}
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
        title={
          pendingAction
            ? t(
                pendingAction.destructive
                  ? "globalCatalog.lifecycle.confirmArchiveTitle"
                  : "globalCatalog.lifecycle.confirmTitle",
              )
            : ""
        }
        description={
          pendingAction
            ? t(
                pendingAction.destructive
                  ? "globalCatalog.lifecycle.confirmArchiveBody"
                  : "globalCatalog.lifecycle.confirmBody",
              )
            : ""
        }
        confirmLabel={pendingAction ? t(pendingAction.labelKey) : t("globalCatalog.save")}
        cancelLabel={t("globalCatalog.cancel")}
        pendingLabel={t("globalCatalog.saving")}
        destructive={pendingAction?.destructive}
        pending={pending}
        error={
          errorMessage ? (
            <p className="text-[length:var(--exits-text-sm)] text-danger" role="alert">
              {errorMessage}
            </p>
          ) : undefined
        }
        onCancel={() => {
          if (!pending) {
            setPendingAction(null);
            setErrorMessage(null);
          }
        }}
        onConfirm={() => void runAction()}
      />
    </>
  );
}
