import { useQuery } from "@tanstack/react-query";
import type { PosStockMovementDto } from "@/api/pos/pos-inventory-client";
import {
  getInventoryTransfer,
  listInventoryTransfers,
} from "@/api/pos/pos-inventory-transfer-client";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import { SideDrawer } from "@/components/exits/SideDrawer";
import { LoadingState } from "@/components/exits/LoadingState";
import { ErrorState } from "@/components/exits/ErrorState";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { ActorAttribution } from "@/features/actors/ActorAttribution";
import type { OrganizationActorDisplayName } from "@/api/platform/actor-directory-client";
import {
  describeMovementBucketEffects,
  formatSignedBucketQty,
  movementNeedsBucketBreakdown,
} from "@/features/inventory/inventory-movement-bucket-effects";
import {
  buildExceptionCustodyReceivingDecisionView,
  exceptionMovementTypeLabelKey,
  matchExceptionCustodyForMovement,
  resolveExceptionMovementRoute,
} from "@/features/inventory/inventory-movement-exception-display";
import { resolveDamageHoldDecisionDisplay } from "@/features/inventory/inventory-movement-damage-hold-display";
import {
  extractTransferReferenceNumber,
  inventoryTransferDetailPath,
  isInventoryTransferMovement,
  isTransferExceptionMovement,
  resolveInventoryTransferTransactionId,
} from "@/features/inventory/inventory-movement-transfer-ref";
import {
  formatTransferQty,
  inventoryTransferStatusLabelKey,
} from "@/features/inventory/inventory-transfer-labels";
import {
  buildOverallFulfillmentView,
  buildReceivingDecisionView,
  computeThisShipmentTotals,
  computeThisTransferWaivedQty,
  formatExceptionCustodyReturnStatusLabel,
  formatReturnToSourceCustodyLabel,
  otherReasonLabelKey,
  resolveExceptionCustodyExpectedItemLabel,
  resolveExceptionCustodyItemLabel,
  transferCustodyDecisionLabelKey,
  transferCustodyStatusLabelKey,
  transferDiscrepancyFollowUpLabelKey,
  transferMissingDispositionLabelKey,
} from "@/features/inventory/inventory-transfer-summary-presentation";
import { inventoryMovementTypeLabelKey } from "@/features/purchasing/purchase-cost-display";
import { requiresActualProduct } from "@/features/inventory/transfer-exception-custody-policy";
import { AppLinkWithReturn } from "@/navigation/AppLinkWithReturn";
import { useI18n } from "@/i18n/I18nProvider";

function branchLabel(name: string | null | undefined, id: string): string {
  const trimmed = name?.trim();
  return trimmed && trimmed.length > 0 ? trimmed : id.slice(0, 8);
}

export function InventoryMovementTransactionDrawer({
  open,
  onOpenChange,
  movement,
  transferContext = null,
  unitOfMeasure,
  workspace,
  resolveActor,
  actorsLoading,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  movement: PosStockMovementDto | null;
  /** Open transfer transaction details without a specific stock movement (e.g. family card). */
  transferContext?: { transferId: string; transferNumber?: string | null } | null;
  unitOfMeasure: string;
  workspace: PosWorkspaceScope | null;
  resolveActor: (actorId: string) => OrganizationActorDisplayName | null | undefined;
  actorsLoading: boolean;
}) {
  const { t } = useI18n();
  const transferIdFromMovement =
    movement && isInventoryTransferMovement(movement)
      ? resolveInventoryTransferTransactionId(movement)
      : (transferContext?.transferId?.trim() || null);
  const transferNumber = movement
    ? extractTransferReferenceNumber(movement)
    : (transferContext?.transferNumber?.trim() || null);

  /** When server omitted transactionId, resolve by TR# so the header link still works. */
  const transferIdByNumberQuery = useQuery({
    queryKey: [
      "inventory-transfer-by-number",
      workspace?.organizationId,
      transferNumber,
    ],
    enabled:
      open &&
      Boolean(workspace) &&
      !transferIdFromMovement &&
      Boolean(transferNumber),
    queryFn: async ({ signal }) => {
      const page = await listInventoryTransfers(
        workspace!,
        { transferNumber: transferNumber!, page: 1, pageSize: 5 },
        signal,
      );
      const exact =
        page.items.find(
          (item) =>
            item.transferNumber?.trim().toLowerCase() ===
            transferNumber!.toLowerCase(),
        ) ?? page.items[0];
      return exact?.transferId?.trim() || null;
    },
  });

  const transferId =
    transferIdFromMovement || transferIdByNumberQuery.data || null;

  const transferQuery = useQuery({
    queryKey: ["inventory-transfer", workspace?.organizationId, transferId],
    enabled: open && Boolean(workspace) && Boolean(transferId),
    queryFn: ({ signal }) => getInventoryTransfer(workspace!, transferId!, signal),
  });

  const effects =
    movement && movementNeedsBucketBreakdown(movement.movementType)
      ? describeMovementBucketEffects(movement.movementType, movement.quantityEffect)
      : null;

  const transfer = transferQuery.data;
  const familyMembers = transfer?.familyMembers ?? [];
  const thisShipment = transfer ? computeThisShipmentTotals(transfer) : null;
  const thisTransferWaived = transfer ? computeThisTransferWaivedQty(transfer) : 0;
  const overallFulfillment = transfer ? buildOverallFulfillmentView(transfer) : null;
  const isDamageReturnIn = movement?.movementType === "TransferDamageReturnIn";
  const isDamageReturnOut = movement?.movementType === "TransferDamageReturnOut";
  const isDamageReturnMovement = isDamageReturnIn || isDamageReturnOut;
  const isExceptionMovement = movement
    ? isTransferExceptionMovement(movement.movementType)
    : false;
  const matchedCustody =
    movement?.movementType === "TransferDamageHold"
      ? (transfer?.damageCustodies ?? []).find(
          (c) =>
            !movement.sourceId ||
            c.receiptLineId.toLowerCase() === movement.sourceId.toLowerCase(),
        ) ?? (transfer?.damageCustodies ?? [])[0]
      : null;
  const matchedExceptionCustody =
    movement && transfer && isExceptionMovement
      ? matchExceptionCustodyForMovement(transfer, movement)
      : null;
  const receivingDecision =
    transfer && matchedExceptionCustody
      ? buildExceptionCustodyReceivingDecisionView(transfer, matchedExceptionCustody)
      : transfer
        ? buildReceivingDecisionView(transfer)
        : null;
  const exceptionRoute =
    movement && transfer && isExceptionMovement
      ? resolveExceptionMovementRoute(movement.movementType, transfer)
      : null;
  const typeLabel = movement
    ? t(
        isExceptionMovement
          ? exceptionMovementTypeLabelKey(
              movement.movementType,
              matchedExceptionCustody?.reasonCode ?? receivingDecision?.otherReasonCode,
            )
          : inventoryMovementTypeLabelKey(movement.movementType),
      )
    : "";
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
  const hasContent = movement != null || transferId != null || transferNumber != null;
  const attributionActorId = movement?.recordedBy ?? transfer?.createdBy ?? null;
  const attributionAtUtc = movement?.recordedAtUtc ?? transfer?.createdAtUtc ?? null;
  const headerTransferNumber =
    transfer?.transferNumber?.trim() || transferNumber || null;
  const transferLookupPending =
    !transferIdFromMovement &&
    Boolean(transferNumber) &&
    (transferIdByNumberQuery.isLoading || transferIdByNumberQuery.isFetching);

  const expectedItemLabel =
    matchedExceptionCustody && transfer
      ? resolveExceptionCustodyExpectedItemLabel(transfer, matchedExceptionCustody)
      : null;
  const actualItemLabel =
    matchedExceptionCustody && transfer
      ? resolveExceptionCustodyItemLabel(transfer, matchedExceptionCustody)
      : null;
  const exceptionNote = receivingDecision?.otherReasonNote?.trim() || null;
  const requiresActualForMovement = requiresActualProduct(
    matchedExceptionCustody?.reasonCode ?? "",
  );

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
          {!hasContent ? null : (
            <>
              {headerTransferNumber || transferId ? (
                <div data-testid="inventory-movement-transaction-transfer-header">
                  {transferId ? (
                    <AppLinkWithReturn
                      to={inventoryTransferDetailPath(transferId)}
                      className="m-0 inline-block text-[length:var(--exits-text-lg)] font-semibold text-primary underline underline-offset-2"
                      data-testid="inventory-movement-transaction-transfer-number"
                    >
                      {headerTransferNumber ?? transferId}
                    </AppLinkWithReturn>
                  ) : (
                    <p
                      className="m-0 text-[length:var(--exits-text-lg)] font-semibold"
                      data-testid="inventory-movement-transaction-transfer-number-pending"
                    >
                      {headerTransferNumber}
                    </p>
                  )}
                  {transferLookupPending || transferQuery.isLoading ? (
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

              {movement ? (
              <section data-testid="inventory-movement-transaction-this-movement">
                <h3 className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
                  {t("inventory.transactionThisMovement")}
                </h3>
                <p className="mt-1 mb-0" data-testid="inventory-movement-type-label">
                  {typeLabel}
                </p>
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

                {matchedExceptionCustody && transfer ? (
                  <dl
                    className="mt-2 mb-0 grid grid-cols-1 gap-2 text-[length:var(--exits-text-sm)]"
                    data-testid="inventory-movement-exception-this-movement"
                  >
                    {movement.movementType === "TransferExceptionExpectedRestore" &&
                    expectedItemLabel &&
                    expectedItemLabel !== "—" ? (
                      <>
                        <div>
                          <dt className="text-muted">{t("transfer.expectedItem")}</dt>
                          <dd
                            className="m-0 font-semibold"
                            data-testid="inventory-movement-exception-expected-item"
                          >
                            {expectedItemLabel}
                          </dd>
                        </div>
                        <div>
                          <dt className="text-muted">{t("inventory.exceptionWhy")}</dt>
                          <dd className="m-0 text-muted">
                            {t("inventory.exceptionExpectedRestoreWhy")
                              .replace("{qty}", formatTransferQty(Math.abs(movement.quantityEffect)))
                              .replace("{product}", expectedItemLabel)}
                          </dd>
                        </div>
                      </>
                    ) : null}
                    {movement.movementType !== "TransferExceptionExpectedRestore" &&
                    actualItemLabel &&
                    actualItemLabel !== "—" ? (
                      <div>
                        <dt className="text-muted">{t("transfer.actualItem")}</dt>
                        <dd
                          className="m-0 font-semibold"
                          data-testid="inventory-movement-exception-actual-item"
                        >
                          {actualItemLabel}
                        </dd>
                      </div>
                    ) : null}
                    {matchedExceptionCustody.reasonCode ? (
                      <div>
                        <dt className="text-muted">{t("transfer.reason")}</dt>
                        <dd
                          className="m-0 font-medium"
                          data-testid="inventory-movement-exception-reason"
                        >
                          {t(otherReasonLabelKey(matchedExceptionCustody.reasonCode))}
                        </dd>
                      </div>
                    ) : null}
                    {movement.movementType !== "TransferExceptionExpectedRestore" &&
                    expectedItemLabel &&
                    expectedItemLabel !== "—" &&
                    requiresActualForMovement ? (
                      <div>
                        <dt className="text-muted">{t("transfer.expectedItem")}</dt>
                        <dd
                          className="m-0 font-medium"
                          data-testid="inventory-movement-exception-expected-item"
                        >
                          {expectedItemLabel}
                        </dd>
                      </div>
                    ) : null}
                    {movement.movementType === "TransferExceptionExpectedRestore" &&
                    actualItemLabel &&
                    actualItemLabel !== "—" ? (
                      <div>
                        <dt className="text-muted">{t("transfer.actualItem")}</dt>
                        <dd
                          className="m-0 font-medium"
                          data-testid="inventory-movement-exception-actual-item"
                        >
                          {actualItemLabel}
                        </dd>
                      </div>
                    ) : null}
                    {exceptionNote ? (
                      <div>
                        <dt className="text-muted">{t("transfer.note")}</dt>
                        <dd
                          className="m-0 font-medium"
                          data-testid="inventory-movement-exception-note"
                        >
                          {exceptionNote}
                        </dd>
                      </div>
                    ) : null}
                    {movement.movementType === "TransferExceptionReturnRestock" ||
                    movement.movementType === "TransferExceptionReturnIn" ? (
                      <>
                        <div>
                          <dt className="text-muted">{t("inventory.exceptionReturnedFrom")}</dt>
                          <dd className="m-0 font-medium">{returnFromBranch}</dd>
                        </div>
                        <div>
                          <dt className="text-muted">{t("inventory.exceptionReceivedBy")}</dt>
                          <dd className="m-0 font-medium">{returnToBranch}</dd>
                        </div>
                      </>
                    ) : null}
                  </dl>
                ) : null}

                {exceptionRoute &&
                transfer &&
                movement.movementType !== "TransferExceptionReturnRestock" &&
                movement.movementType !== "TransferExceptionReturnIn" ? (
                  <dl
                    className="mt-2 mb-0 grid grid-cols-1 gap-2 text-[length:var(--exits-text-sm)] sm:grid-cols-2"
                    data-testid="inventory-movement-exception-route"
                  >
                    <div>
                      <dt className="text-muted">{t("inventory.transactionFrom")}</dt>
                      <dd className="m-0 font-semibold">
                        {exceptionRoute.fromBranchName ??
                          branchLabel(
                            exceptionRoute.isReturnRoute
                              ? transfer.destinationBranchName
                              : transfer.sourceBranchName,
                            exceptionRoute.isReturnRoute
                              ? transfer.destinationBranchId
                              : transfer.sourceBranchId,
                          )}
                      </dd>
                    </div>
                    <div>
                      <dt className="text-muted">{t("inventory.transactionTo")}</dt>
                      <dd className="m-0 font-semibold">
                        {exceptionRoute.toBranchName ??
                          branchLabel(
                            exceptionRoute.isReturnRoute
                              ? transfer.sourceBranchName
                              : transfer.destinationBranchName,
                            exceptionRoute.isReturnRoute
                              ? transfer.sourceBranchId
                              : transfer.destinationBranchId,
                          )}
                      </dd>
                    </div>
                  </dl>
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

                {movement.sellableBefore != null &&
                movement.sellableDelta != null &&
                movement.sellableAfter != null ? (
                  <dl
                    className="mt-3 mb-0 grid grid-cols-1 gap-1 text-[length:var(--exits-text-sm)]"
                    data-testid="inventory-movement-sellable-balance"
                  >
                    <div className="flex flex-wrap items-baseline justify-between gap-2">
                      <dt className="text-muted">{t("inventory.sellableBefore")}</dt>
                      <dd className="m-0 font-semibold tabular-nums">
                        {movement.sellableBefore} {unitOfMeasure}
                      </dd>
                    </div>
                    <div className="flex flex-wrap items-baseline justify-between gap-2">
                      <dt className="text-muted">{t("inventory.transactionThisMovement")}</dt>
                      <dd className="m-0 font-semibold tabular-nums">
                        {formatSignedBucketQty(movement.sellableDelta)} {unitOfMeasure}
                      </dd>
                    </div>
                    <div className="flex flex-wrap items-baseline justify-between gap-2">
                      <dt className="text-muted">{t("inventory.sellableAfter")}</dt>
                      <dd className="m-0 font-semibold tabular-nums">
                        {movement.sellableAfter} {unitOfMeasure}
                      </dd>
                    </div>
                  </dl>
                ) : null}

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
              ) : null}

              {transfer && thisShipment ? (
                <Card
                  className="flex min-w-0 flex-col gap-2 p-3"
                  treatment="bordered"
                  padding="compact"
                  data-testid="inventory-movement-transaction-this-transfer"
                >
                  <h3 className="m-0 text-[length:var(--exits-text-sm)] font-semibold text-foreground">
                    {t("inventory.transactionThisTransfer")}
                  </h3>
                  <dl className="m-0 grid grid-cols-2 gap-2 text-[length:var(--exits-text-sm)] sm:grid-cols-3">
                    <div>
                      <dt className="text-muted">{t("transfer.sent")}</dt>
                      <dd className="m-0 font-semibold tabular-nums">
                        {formatTransferQty(thisShipment.sent)}
                      </dd>
                    </div>
                    <div>
                      <dt className="text-muted">{t("transfer.goodReceived")}</dt>
                      <dd className="m-0 font-semibold tabular-nums">
                        {formatTransferQty(thisShipment.goodReceived)}
                      </dd>
                    </div>
                    <div>
                      <dt className="text-muted">{t("transfer.damaged")}</dt>
                      <dd className="m-0 font-semibold tabular-nums">
                        {formatTransferQty(thisShipment.damaged)}
                      </dd>
                    </div>
                    <div>
                      <dt className="text-muted">{t("transfer.missing")}</dt>
                      <dd className="m-0 font-semibold tabular-nums">
                        {formatTransferQty(thisShipment.missing)}
                      </dd>
                    </div>
                    <div>
                      <dt className="text-muted">{t("transfer.other")}</dt>
                      <dd className="m-0 font-semibold tabular-nums">
                        {formatTransferQty(thisShipment.other)}
                      </dd>
                    </div>
                    <div>
                      <dt className="text-muted">{t("transfer.acceptedWaived")}</dt>
                      <dd className="m-0 font-semibold tabular-nums">
                        {formatTransferQty(thisTransferWaived)}
                      </dd>
                    </div>
                  </dl>
                </Card>
              ) : null}

              {transfer && receivingDecision?.hasDiscrepancy ? (
                <Card
                  className="flex min-w-0 flex-col gap-2 p-3"
                  treatment="bordered"
                  padding="compact"
                  data-testid="inventory-movement-transaction-receiving-decision"
                >
                  <h3 className="m-0 text-[length:var(--exits-text-sm)] font-semibold text-foreground">
                    {t("inventory.transactionReceivingDecision")}
                  </h3>
                  <dl className="m-0 grid grid-cols-[1fr_auto] gap-x-4 gap-y-1.5 text-[length:var(--exits-text-sm)]">
                    {receivingDecision.damagedQty > 1e-9 ? (
                      <>
                        <dt className="m-0 text-muted">{t("transfer.damaged")}</dt>
                        <dd className="m-0 text-end font-semibold tabular-nums">
                          {formatTransferQty(receivingDecision.damagedQty)}
                        </dd>
                        {receivingDecision.damagedFollowUp ? (
                          <>
                            <dt className="m-0 text-muted">{t("transfer.replacement")}</dt>
                            <dd className="m-0 text-end font-medium">
                              {receivingDecision.damagedFollowUp === "RequestReplacement"
                                ? t("transfer.replacementRequestedShort")
                                : receivingDecision.damagedFollowUp === "AcceptShortage"
                                  ? t("transfer.noReplacement")
                                  : (transferDiscrepancyFollowUpLabelKey(
                                      receivingDecision.damagedFollowUp,
                                    )
                                      ? t(
                                          transferDiscrepancyFollowUpLabelKey(
                                            receivingDecision.damagedFollowUp,
                                          )!,
                                        )
                                      : receivingDecision.damagedFollowUp)}
                            </dd>
                          </>
                        ) : null}
                        {receivingDecision.custodyDecision ? (
                          <>
                            <dt className="m-0 text-muted">{t("transfer.custodyLabel")}</dt>
                            <dd className="m-0 text-end font-medium">
                              {transferCustodyDecisionLabelKey(receivingDecision.custodyDecision)
                                ? t(
                                    transferCustodyDecisionLabelKey(
                                      receivingDecision.custodyDecision,
                                    )!,
                                  )
                                : receivingDecision.custodyDecision}
                            </dd>
                          </>
                        ) : null}
                        {receivingDecision.custodyStatus ? (
                          <>
                            <dt className="m-0 text-muted">{t("transfer.returnStatus")}</dt>
                            <dd className="m-0 text-end font-medium">
                              {transferCustodyStatusLabelKey(receivingDecision.custodyStatus)
                                ? t(
                                    transferCustodyStatusLabelKey(
                                      receivingDecision.custodyStatus,
                                    )!,
                                  )
                                : receivingDecision.custodyStatus}
                            </dd>
                          </>
                        ) : null}
                      </>
                    ) : null}

                    {receivingDecision.missingQty > 1e-9 ? (
                      <>
                        <dt className="m-0 text-muted">{t("transfer.missing")}</dt>
                        <dd className="m-0 text-end font-semibold tabular-nums">
                          {formatTransferQty(receivingDecision.missingQty)}
                        </dd>
                        {receivingDecision.missingDisposition ? (
                          <>
                            <dt className="m-0 text-muted">{t("transfer.followUp.decisionCol")}</dt>
                            <dd className="m-0 text-end font-medium">
                              {transferMissingDispositionLabelKey(
                                receivingDecision.missingDisposition,
                              )
                                ? t(
                                    transferMissingDispositionLabelKey(
                                      receivingDecision.missingDisposition,
                                    )!,
                                  )
                                : receivingDecision.missingDisposition}
                            </dd>
                          </>
                        ) : null}
                      </>
                    ) : null}

                    {receivingDecision.otherQty > 1e-9 ? (
                      <>
                        <dt className="m-0 text-muted">{t("transfer.otherDiscrepancy")}</dt>
                        <dd className="m-0 text-end font-semibold tabular-nums">
                          {formatTransferQty(receivingDecision.otherQty)}
                        </dd>
                        <dt className="m-0 text-muted">{t("transfer.reason")}</dt>
                        <dd className="m-0 text-end font-medium">
                          {t(otherReasonLabelKey(receivingDecision.otherReasonCode))}
                        </dd>
                        {receivingDecision.showExpectedAndActualItems &&
                        receivingDecision.expectedProductId ? (
                          <>
                            <dt className="m-0 text-muted">{t("transfer.expectedItem")}</dt>
                            <dd
                              className="m-0 text-end font-medium"
                              data-testid="inventory-movement-receiving-expected-item"
                            >
                              {receivingDecision.expectedProductName ??
                                resolveExceptionCustodyExpectedItemLabel(transfer, {
                                  expectedProductId: receivingDecision.expectedProductId,
                                  expectedProductName: receivingDecision.expectedProductName,
                                })}
                            </dd>
                            <dt className="m-0 text-muted">{t("transfer.actualItem")}</dt>
                            <dd
                              className="m-0 text-end font-medium"
                              data-testid="inventory-movement-receiving-actual-item"
                            >
                              {receivingDecision.actualReceivedProductName ??
                                (receivingDecision.actualReceivedProductId
                                  ? resolveExceptionCustodyItemLabel(transfer, {
                                      actualProductId:
                                        receivingDecision.actualReceivedProductId,
                                      expectedProductId: receivingDecision.expectedProductId,
                                      actualProductName:
                                        receivingDecision.actualReceivedProductName,
                                      expectedProductName:
                                        receivingDecision.expectedProductName,
                                    })
                                  : "—")}
                            </dd>
                          </>
                        ) : null}
                        {receivingDecision.otherReasonNote ? (
                          <>
                            <dt className="m-0 text-muted">{t("transfer.note")}</dt>
                            <dd
                              className="m-0 text-end font-medium"
                              data-testid="inventory-movement-receiving-note"
                            >
                              {receivingDecision.otherReasonNote}
                            </dd>
                          </>
                        ) : null}
                        {receivingDecision.otherFollowUp ? (
                          <>
                            <dt className="m-0 text-muted">{t("transfer.followUp.decisionCol")}</dt>
                            <dd className="m-0 text-end font-medium">
                              {transferDiscrepancyFollowUpLabelKey(receivingDecision.otherFollowUp)
                                ? t(
                                    transferDiscrepancyFollowUpLabelKey(
                                      receivingDecision.otherFollowUp,
                                    )!,
                                  )
                                : receivingDecision.otherFollowUp}
                            </dd>
                          </>
                        ) : null}
                        {receivingDecision.otherCustodyDecision ? (
                          <>
                            <dt className="m-0 text-muted">{t("transfer.exceptionCustodyLabel")}</dt>
                            <dd
                              className="m-0 text-end font-medium"
                              data-testid="inventory-movement-receiving-exception-custody"
                            >
                              {receivingDecision.otherCustodyDecision === "ReturnToSource"
                                ? formatReturnToSourceCustodyLabel(
                                    transfer.sourceBranchName,
                                    t("transfer.custody.returnToBranch"),
                                    t("transfer.custody.returnToSource"),
                                  )
                                : transferCustodyDecisionLabelKey(
                                      receivingDecision.otherCustodyDecision,
                                    )
                                  ? t(
                                      transferCustodyDecisionLabelKey(
                                        receivingDecision.otherCustodyDecision,
                                      )!,
                                    )
                                  : receivingDecision.otherCustodyDecision}
                            </dd>
                          </>
                        ) : null}
                        {receivingDecision.otherCustodyStatus ? (
                          <>
                            <dt className="m-0 text-muted">{t("transfer.returnStatus")}</dt>
                            <dd
                              className="m-0 text-end font-medium"
                              data-testid="inventory-movement-receiving-return-status"
                            >
                              {formatExceptionCustodyReturnStatusLabel(
                                receivingDecision.otherCustodyStatus,
                                transfer.sourceBranchName,
                                {
                                  awaitingReturn: t("transfer.custody.awaitingReturn"),
                                  returningToBranch: t("transfer.custody.returningToBranch"),
                                  returnInTransitFallback: t(
                                    "transfer.custody.returnInTransit",
                                  ),
                                  returnedToBranch: t("transfer.custody.returnedToBranch"),
                                  receivedAtSourceFallback: t(
                                    "transfer.custody.receivedAtSource",
                                  ),
                                  heldAtDestination: t("transfer.custody.heldAtDestination"),
                                  awaitingInspection: t(
                                    "transfer.custody.awaitingInspection",
                                  ),
                                },
                              )}
                            </dd>
                          </>
                        ) : null}
                      </>
                    ) : null}
                  </dl>
                </Card>
              ) : null}

              {transfer && overallFulfillment ? (
                <Card
                  className="flex min-w-0 flex-col gap-2 p-3"
                  treatment="bordered"
                  padding="compact"
                  data-testid="inventory-movement-transaction-overall-fulfillment"
                >
                  <h3 className="m-0 text-[length:var(--exits-text-sm)] font-semibold text-foreground">
                    {t("inventory.transactionOverallFulfillment")}
                  </h3>
                  <dl className="m-0 grid grid-cols-2 gap-2 text-[length:var(--exits-text-sm)] sm:grid-cols-3">
                    <div>
                      <dt className="text-muted">{t("transfer.targetRequested")}</dt>
                      <dd className="m-0 font-semibold tabular-nums">
                        {formatTransferQty(overallFulfillment.target)}
                      </dd>
                    </div>
                    <div>
                      <dt className="text-muted">{t("transfer.goodReceived")}</dt>
                      <dd className="m-0 font-semibold tabular-nums">
                        {formatTransferQty(overallFulfillment.goodReceived)}
                      </dd>
                    </div>
                    <div>
                      <dt className="text-muted">{t("transfer.stillInTransit")}</dt>
                      <dd className="m-0 font-semibold tabular-nums">
                        {formatTransferQty(overallFulfillment.openInTransit)}
                      </dd>
                    </div>
                    <div>
                      <dt className="text-muted">{t("transfer.acceptedWaived")}</dt>
                      <dd className="m-0 font-semibold tabular-nums">
                        {formatTransferQty(overallFulfillment.waived)}
                      </dd>
                    </div>
                    <div>
                      <dt className="text-muted">{t("transfer.needsFulfillment")}</dt>
                      <dd className="m-0 font-semibold tabular-nums">
                        {formatTransferQty(overallFulfillment.remainingToDispatch)}
                      </dd>
                    </div>
                  </dl>
                  {familyMembers.length > 1 ? (
                    <ul
                      className="m-0 list-none p-0 text-[length:var(--exits-text-sm)]"
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
                          {` · ${t("transfer.sent")} ${formatTransferQty(member.totalSentQty)} · ${t("transfer.goodReceived")} ${formatTransferQty(member.totalReceivedQty)}`}
                        </li>
                      ))}
                    </ul>
                  ) : null}
                </Card>
              ) : null}

              {attributionActorId && attributionAtUtc ? (
                <section data-testid="inventory-movement-transaction-attribution">
                  <ActorAttribution
                    labelKey="common.recordedBy"
                    actorId={attributionActorId}
                    occurredAtUtc={attributionAtUtc}
                    resolved={resolveActor(attributionActorId)}
                    isLoading={actorsLoading}
                    testId="inventory-movement-transaction-actor"
                  />
                </section>
              ) : null}

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
