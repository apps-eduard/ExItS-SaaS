import { useState } from "react";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import type { EnableExpirationTrackingResponse } from "@/api/pos/pos-inventory-client";
import { Button } from "@/components/ui/button";
import { ExitsModal } from "@/components/exits/ExitsModal";
import { LoadingState } from "@/components/exits/LoadingState";
import { AssignExpirationLotsForm } from "@/features/inventory/AssignExpirationLotsForm";
import { useI18n } from "@/i18n/I18nProvider";

export type ReceiveStockExpirySetupDialogProps = {
  open: boolean;
  workspace: PosWorkspaceScope;
  productId: string;
  productName: string;
  onHandQuantity: number;
  unitOfMeasure: string;
  expirationWarningDays?: number | null;
  loadingOnHand?: boolean;
  onCancel: () => void;
  onSuccess: (result: EnableExpirationTrackingResponse) => void;
  onReloadOnHand: () => void;
};

/**
 * In-place enable-expiration dialog for Manual Receive Stock.
 * Allocates EXISTING on-hand only — never the unsaved receipt quantity.
 */
export function ReceiveStockExpirySetupDialog({
  open,
  workspace,
  productId,
  productName,
  onHandQuantity,
  unitOfMeasure,
  expirationWarningDays,
  loadingOnHand = false,
  onCancel,
  onSuccess,
  onReloadOnHand,
}: ReceiveStockExpirySetupDialogProps) {
  const { t } = useI18n();
  const [submitting, setSubmitting] = useState(false);

  const description = t("purchasing.expirySetupDescription")
    .replace("{product}", productName)
    .replace("{qty}", String(onHandQuantity))
    .replace("{uom}", unitOfMeasure);

  // Only block the whole body on the initial open fetch — never unmount the
  // allocation form mid-edit (that wiped rows and looked like a failed save).
  const showInitialLoading = loadingOnHand && onHandQuantity <= 0;

  return (
    <ExitsModal
      open={open}
      onOpenChange={(next) => {
        if (!next && !submitting) {
          onCancel();
        }
      }}
      title={t("purchasing.expirySetupTitle")}
      description={showInitialLoading ? undefined : description}
      size="lg"
      fullHeightOnCompact
      busy={submitting}
      closeOnOutsideClick={false}
      closeOnEscape={false}
      testId="receive-stock-expiry-setup-dialog"
      closeLabel={t("inventory.enableExpirationCancel")}
      className="lg:max-w-2xl"
    >
      {showInitialLoading ? (
        <LoadingState label={t("loading.label")} />
      ) : (
        <div className="flex flex-col gap-3">
          {loadingOnHand ? (
            <p
              className="m-0 text-[length:var(--exits-text-sm)] text-muted"
              data-testid="receive-stock-expiry-setup-refreshing"
            >
              {t("loading.label")}
            </p>
          ) : null}
          <AssignExpirationLotsForm
            key={productId}
            workspace={workspace}
            productId={productId}
            productName={productName}
            onHandQuantity={onHandQuantity}
            unitOfMeasure={unitOfMeasure}
            expirationWarningDays={expirationWarningDays}
            intent="enable"
            hideIntro
            addRowLabel={t("purchasing.expirySetupAddGroup")}
            onSuccess={onSuccess}
            onAllocationStockChanged={onReloadOnHand}
            onSubmittingChange={setSubmitting}
            actionsExtra={
              <Button
                type="button"
                variant="outline"
                disabled={submitting}
                onClick={onCancel}
                data-testid="receive-stock-expiry-setup-cancel"
              >
                {t("inventory.enableExpirationCancel")}
              </Button>
            }
          />
        </div>
      )}
    </ExitsModal>
  );
}
