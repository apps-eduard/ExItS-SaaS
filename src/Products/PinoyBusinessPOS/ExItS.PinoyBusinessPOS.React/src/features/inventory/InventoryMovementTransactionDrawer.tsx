import { useQuery } from "@tanstack/react-query";
import type { PosStockMovementDto } from "@/api/pos/pos-inventory-client";
import { getInventoryTransfer } from "@/api/pos/pos-inventory-transfer-client";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import { SideDrawer } from "@/components/exits/SideDrawer";
import { LoadingState } from "@/components/exits/LoadingState";
import { ErrorState } from "@/components/exits/ErrorState";
import { Button } from "@/components/ui/button";
import { ActorAttribution } from "@/features/actors/ActorAttribution";
import type { OrganizationActorDisplayName } from "@/api/platform/actor-directory-client";
import {
  describeMovementBucketEffects,
  formatSignedBucketQty,
  movementNeedsBucketBreakdown,
} from "@/features/inventory/inventory-movement-bucket-effects";
import { resolveDamageHoldDecisionDisplay } from "@/features/inventory/inventory-movement-damage-hold-display";
import {
  extractTransferReferenceNumber,
  inventoryTransferDetailPath,
  isInventoryTransferMovement,
  resolveInventoryTransferTransactionId,
} from "@/features/inventory/inventory-movement-transfer-ref";
import {
  formatTransferQty,
  inventoryTransferStatusLabelKey,
} from "@/features/inventory/inventory-transfer-labels";
import { inventoryMovementTypeLabelKey } from "@/features/purchasing/purchase-cost-display";
import { AppLinkWithReturn } from "@/navigation/AppLinkWithReturn";
import { useI18n } from "@/i18n/I18nProvider";

function formatWhen(iso: string): string {
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) {
    return iso;
  }
  return date.toLocaleString();
}

function branchLabel(name: string | null | undefined, id: string): string {
  const trimmed = name?.trim();
  return trimmed && trimmed.length > 0 ? trimmed : id.slice(0, 8);
}

export function InventoryMovementTransactionDrawer({
  open,
  onOpenChange,
  movement,
  unitOfMeasure,
  workspace,
  resolveActor,
  actorsLoading,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  movement: PosStockMovementDto | null;
  unitOfMeasure: string;
  workspace: PosWorkspaceScope | null;
  resolveActor: (actorId: string) => OrganizationActorDisplayName | null | undefined;
  actorsLoading: boolean;
}) {
  const { t } = useI18n();
  const transferId =
    movement && isInventoryTransferMovement(movement)
      ? resolveInventoryTransferTransactionId(movement)
      : null;
  const transferNumber = movement ? extractTransferReferenceNumber(movement) : null;

  const transferQuery = useQuery({
    queryKey: ["inventory-transfer", workspace?.organizationId, transferId],
    enabled: open && Boolean(workspace) && Boolean(transferId),
    queryFn: ({ signal }) => getInventoryTransfer(workspace!, transferId!, signal),
  });

  const typeLabel = movement
    ? t(inventoryMovementTypeLabelKey(movement.movementType))
    : "";
  const effects =
    movement && movementNeedsBucketBreakdown(movement.movementType)
      ? describeMovementBucketEffects(movement.movementType, movement.quantityEffect)
      : null;

  const transfer = transferQuery.data;
  const familyMembers = transfer?.familyMembers ?? [];
  const isDamageReturnIn = movement?.movementType === "TransferDamageReturnIn";
  const isDamageReturnOut = movement?.movementType === "TransferDamageReturnOut";
  const isDamageReturnMovement = isDamageReturnIn || isDamageReturnOut;
  const matchedCustody =
    movement?.movementType === "TransferDamageHold"
      ? (transfer?.damageCustodies ?? []).find(
          (c) =>
            !movement.sourceId ||
            c.receiptLineId.toLowerCase() === movement.sourceId.toLowerCase(),
        ) ?? (transfer?.damageCustodies ?? [])[0]
      : null;
  const damageHoldDecision = movement
    ? resolveDamageHoldDecisionDisplay(movement, matchedCustody)
    : null;
  // Damage return: physical flow is destination → source (opposite of original ship).
  const returnFromBranch = transfer
    ? branchLabel(transfer.destinationBranchName, transfer.destinationBranchId)
    : null;
  const returnToBranch = transfer
    ? branchLabel(transfer.sourceBranchName, transfer.sourceBranchId)
    : null;

  return (
    <SideDrawer
      open={open}
      onClose={() => onOpenChange(false)}
      title={t("inventory.transactionDetailsTitle")}
      testId="inventory-movement-transaction-drawer"
      closeLabel={t("inventory.transactionDetailsClose")}
      closeTestId="inventory-movement-transaction-drawer-close"
      panelClassName="exits-form-drawer__panel exits-form-drawer__panel--md"
    >
      <div className="exits-form-drawer" data-testid="inventory-movement-transaction-drawer-content">
        <div className="exits-form-drawer__body flex flex-col gap-4">
          {!movement ? null : (
            <>
              {transferNumber || transferId ? (
                <div data-testid="inventory-movement-transaction-transfer-header">
                  <p className="m-0 text-[length:var(--exits-text-lg)] font-semibold">
                    {transferNumber ?? transferId}
                  </p>
                  {transferQuery.isLoading ? (
                    <LoadingState label={t("transfer.loading")} />
                  ) : transferQuery.isError ? (
                    <ErrorState
                      title={t("transfer.errorTitle")}
                      detail={t("transfer.notFound")}
                    />
                  ) : transfer ? (
                    <div className="mt-2 flex flex-col gap-2">
                      <p className="m-0 text-[length:var(--exits-text-sm)]">
                        {branchLabel(transfer.sourceBranchName, transfer.sourceBranchId)}
                        {" → "}
                        {branchLabel(transfer.destinationBranchName, transfer.destinationBranchId)}
                      </p>
                      <p className="m-0 text-[length:var(--exits-text-sm)]">
                        {t(inventoryTransferStatusLabelKey(transfer.status))}
                      </p>
                    </div>
                  ) : null}
                </div>
              ) : null}

              <section data-testid="inventory-movement-transaction-this-movement">
                <h3 className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
                  {t("inventory.transactionThisMovement")}
                </h3>
                <p className="mt-1 mb-0">{typeLabel}</p>
                {damageHoldDecision ? (
                  <p
                    className="mt-1 mb-0 text-[length:var(--exits-text-sm)] text-muted"
                    data-testid="inventory-movement-damage-hold-decision"
                  >
                    {t(damageHoldDecision.followUpLabelKey)}
                    {" · "}
                    {t(damageHoldDecision.custodyLabelKey)}
                  </p>
                ) : null}

                {isDamageReturnMovement && transfer ? (
                  <dl
                    className="mt-2 mb-0 grid grid-cols-1 gap-2 text-[length:var(--exits-text-sm)] sm:grid-cols-2"
                    data-testid="inventory-movement-damage-return-route"
                  >
                    <div>
                      <dt className="text-muted">{t("inventory.transactionFrom")}</dt>
                      <dd className="m-0 font-semibold">{returnFromBranch}</dd>
                    </div>
                    <div>
                      <dt className="text-muted">{t("inventory.transactionTo")}</dt>
                      <dd className="m-0 font-semibold">{returnToBranch}</dd>
                    </div>
                  </dl>
                ) : null}

                {effects ? (
                  <>
                    <p className="mt-2 mb-0 font-semibold tabular-nums">
                      {t("inventory.quantity")}:{" "}
                      {Math.abs(movement.quantityEffect)} {unitOfMeasure}
                    </p>
                    <div
                      className="mt-2"
                      data-testid="inventory-movement-transaction-inventory-effect"
                    >
                      <p className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
                        {t("inventory.transactionInventoryEffect")}
                      </p>
                      <p className="mt-1 mb-0 text-[length:var(--exits-text-sm)] text-muted">
                        {t("inventory.bucketPhysical")}:{" "}
                        {formatSignedBucketQty(effects.physicalDelta)}
                        {" · "}
                        {t("inventory.bucketSellable")}:{" "}
                        {formatSignedBucketQty(effects.sellableDelta)}
                        {effects.damagedDelta !== 0 || !isDamageReturnIn
                          ? ` · ${t("inventory.bucketDamaged")}: ${formatSignedBucketQty(effects.damagedDelta)}`
                          : null}
                        {effects.inspectionHoldDelta !== 0
                          ? ` · ${t("inventory.bucketInspectionHold")}: ${formatSignedBucketQty(effects.inspectionHoldDelta)}`
                          : null}
                      </p>
                    </div>
                  </>
                ) : (
                  <p className="mt-1 mb-0 font-semibold tabular-nums">
                    {movement.quantityEffect > 0 ? "+" : ""}
                    {movement.quantityEffect} {unitOfMeasure}
                  </p>
                )}

                {isDamageReturnMovement ? (
                  <div
                    className="mt-2"
                    data-testid="inventory-movement-damage-disposition"
                  >
                    <p className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
                      {t("inventory.transactionDamageDisposition")}
                    </p>
                    <p className="mt-1 mb-0 text-[length:var(--exits-text-sm)] text-muted">
                      {t("inventory.damageDisposition.returnedToSource")}
                    </p>
                  </div>
                ) : null}
              </section>

              {transfer ? (
                <section data-testid="inventory-movement-transaction-transfer-summary">
                  <h3 className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
                    {t("inventory.transactionTransfer")}
                  </h3>
                  <dl className="mt-2 mb-0 grid grid-cols-2 gap-2 text-[length:var(--exits-text-sm)] sm:grid-cols-3">
                    <div>
                      <dt className="text-muted">{t("transfer.sent")}</dt>
                      <dd className="m-0 font-semibold tabular-nums">
                        {formatTransferQty(transfer.totalSentQty)}
                      </dd>
                    </div>
                    <div>
                      <dt className="text-muted">{t("transfer.goodReceived")}</dt>
                      <dd className="m-0 font-semibold tabular-nums">
                        {formatTransferQty(transfer.satisfiedAtDestinationQty ?? transfer.totalReceivedQty)}
                      </dd>
                    </div>
                    <div>
                      <dt className="text-muted">{t("transfer.damaged")}</dt>
                      <dd className="m-0 font-semibold tabular-nums">
                        {formatTransferQty(
                          (transfer.damageCustodies ?? []).reduce((sum, c) => sum + c.quantity, 0),
                        )}
                      </dd>
                    </div>
                    <div>
                      <dt className="text-muted">{t("transfer.stillInTransit")}</dt>
                      <dd className="m-0 font-semibold tabular-nums">
                        {formatTransferQty(transfer.openInTransitQty ?? transfer.totalOutstandingQty)}
                      </dd>
                    </div>
                    <div>
                      <dt className="text-muted">{t("transfer.needsFulfillment")}</dt>
                      <dd className="m-0 font-semibold tabular-nums">
                        {formatTransferQty(transfer.remainingToDispatchQty ?? 0)}
                      </dd>
                    </div>
                    <div>
                      <dt className="text-muted">{t("transfer.acceptedWaived")}</dt>
                      <dd className="m-0 font-semibold tabular-nums">
                        {formatTransferQty(transfer.waivedQty ?? 0)}
                      </dd>
                    </div>
                  </dl>
                  {familyMembers.length > 1 ? (
                    <ul
                      className="mt-2 mb-0 list-none p-0 text-[length:var(--exits-text-sm)]"
                      data-testid="inventory-movement-transaction-family"
                    >
                      <li className="mb-1 font-medium text-muted">
                        {t("inventory.transactionFamily")}
                      </li>
                      {familyMembers.map((member) => (
                        <li key={member.transferId}>
                          {member.isRoot
                            ? member.transferNumber
                              ? `Original ${member.transferNumber}`
                              : t("transfer.family.originalDraft")
                            : member.transferNumber
                              ? `Replacement ${member.transferNumber}`
                              : t("transfer.family.replacementDraft").replace(
                                  "{n}",
                                  String(member.replacementSequence ?? ""),
                                )}
                        </li>
                      ))}
                    </ul>
                  ) : null}
                </section>
              ) : null}

              <section data-testid="inventory-movement-transaction-attribution">
                <ActorAttribution
                  labelKey="common.recordedBy"
                  actorId={movement.recordedBy}
                  occurredAtUtc={movement.recordedAtUtc}
                  resolved={resolveActor(movement.recordedBy)}
                  isLoading={actorsLoading}
                  testId="inventory-movement-transaction-actor"
                />
                <p className="mt-1 mb-0 text-[length:var(--exits-text-sm)] text-muted">
                  {t("inventory.transactionRecordedAt")}: {formatWhen(movement.recordedAtUtc)}
                </p>
              </section>

              {transferId ? (
                <Button asChild data-testid="inventory-movement-view-full-transfer">
                  <AppLinkWithReturn to={inventoryTransferDetailPath(transferId)}>
                    {t("inventory.viewFullTransfer")}
                  </AppLinkWithReturn>
                </Button>
              ) : null}
            </>
          )}
        </div>
      </div>
    </SideDrawer>
  );
}
