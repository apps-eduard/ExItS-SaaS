import { useNavigate } from "react-router-dom";
import { ConfirmActionDialog } from "@/components/exits/ConfirmActionDialog";
import { OFFER_DELIVERY_SETTINGS_PATH } from "@/features/branches/offer-delivery-queries";
import type { MessageKey } from "@/i18n/messages";

export type BranchDeliveryOrgOfferGuardDialogProps = {
  open: boolean;
  onCancel: () => void;
  t: (key: MessageKey) => string;
};

/**
 * Intercepts branch Delivery OFF→ON while organization Offer Delivery is OFF.
 * Does not enable the branch; routes the user to Connected Commerce fulfillment.
 */
export function BranchDeliveryOrgOfferGuardDialog({
  open,
  onCancel,
  t,
}: BranchDeliveryOrgOfferGuardDialogProps) {
  const navigate = useNavigate();

  return (
    <ConfirmActionDialog
      open={open}
      variant="info"
      title={t("branches.deliveryOrgDisabledTitle")}
      description={t("branches.deliveryOrgDisabledBody")}
      confirmLabel={t("branches.deliveryOrgDisabledOpenSettings")}
      cancelLabel={t("branches.cancel")}
      testId="branch-delivery-org-disabled-dialog"
      onCancel={onCancel}
      onConfirm={() => {
        onCancel();
        navigate(OFFER_DELIVERY_SETTINGS_PATH);
      }}
    />
  );
}

export const ORGANIZATION_DELIVERY_NOT_OFFERED_ERROR =
  "pos.connected_supplier.organization_delivery_not_offered";

export function isOrganizationDeliveryNotOfferedError(err: unknown): boolean {
  if (!err || typeof err !== "object") {
    return false;
  }
  const code =
    "errorCode" in err && typeof (err as { errorCode?: unknown }).errorCode === "string"
      ? (err as { errorCode: string }).errorCode
      : "problem" in err &&
          typeof (err as { problem?: { errorCode?: unknown } }).problem?.errorCode === "string"
        ? (err as { problem: { errorCode: string } }).problem.errorCode
        : null;
  return code === ORGANIZATION_DELIVERY_NOT_OFFERED_ERROR;
}
