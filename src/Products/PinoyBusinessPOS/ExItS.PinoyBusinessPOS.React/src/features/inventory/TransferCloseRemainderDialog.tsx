import { useMemo, useState } from "react";
import type { InventoryTransferDto } from "@/api/pos/pos-inventory-transfer-client";
import { INVENTORY_TRANSFER_DISCREPANCY_REASONS } from "@/api/pos/pos-inventory-transfer-client";
import { Button } from "@/components/ui/button";
import { ExitsModal } from "@/components/exits/ExitsModal";
import {
  ExitsTable,
  ExitsTableBody,
  ExitsTableCell,
  ExitsTableContainer,
  ExitsTableHead,
  ExitsTableHeader,
  ExitsTableRow,
} from "@/components/exits/ExitsTable";
import {
  formatTransferQty,
  inventoryTransferDiscrepancyLabelKey,
} from "@/features/inventory/inventory-transfer-labels";
import { lineOutstandingQty as outstandingForLine } from "@/features/inventory/inventory-transfer-receive-helpers";
import { useI18n } from "@/i18n/I18nProvider";

export type TransferCloseRemainderDialogProps = {
  open: boolean;
  transfer: InventoryTransferDto;
  busy: boolean;
  onCancel: () => void;
  onConfirm: (lines: Array<{
    lineId: string;
    productId: string;
    discrepancyReason: string;
    discrepancyNote?: string | null;
  }>) => void;
};

export function TransferCloseRemainderDialog({
  open,
  transfer,
  busy,
  onCancel,
  onConfirm,
}: TransferCloseRemainderDialogProps) {
  const { t } = useI18n();
  const closeLines = useMemo(
    () => transfer.lines.filter((line) => outstandingForLine(line) > 0),
    [transfer.lines],
  );

  const [reasonByLine, setReasonByLine] = useState<Record<string, string>>({});
  const [noteByLine, setNoteByLine] = useState<Record<string, string>>({});

  const allReasonsSet = closeLines.every((line) => reasonByLine[line.lineId]?.trim());

  return (
    <ExitsModal
      open={open}
      onOpenChange={(next) => {
        if (!next && !busy) {
          onCancel();
        }
      }}
      title={t("transfer.closeRemainderTitle")}
      description={t("transfer.closeRemainderBody")}
      testId="transfer-close-remainder-dialog"
      footer={
        <div className="flex flex-wrap justify-end gap-2">
          <Button type="button" intent="neutral" appearance="outline" disabled={busy} onClick={onCancel}>
            {t("transfer.dialogCancel")}
          </Button>
          <Button
            type="button"
            intent="danger"
            disabled={busy || !allReasonsSet}
            onClick={() => {
              onConfirm(
                closeLines.map((line) => ({
                  lineId: line.lineId,
                  productId: line.productId,
                  discrepancyReason: reasonByLine[line.lineId]!.trim(),
                  discrepancyNote: noteByLine[line.lineId]?.trim() || null,
                })),
              );
            }}
            data-testid="transfer-close-remainder-confirm"
          >
            {busy ? t("transfer.closingRemainder") : t("transfer.closeRemainderConfirm")}
          </Button>
        </div>
      }
    >
      <ExitsTableContainer data-testid="transfer-close-remainder-table">
        <ExitsTable>
          <ExitsTableHeader>
            <ExitsTableRow>
              <ExitsTableHead cellAlign="text" colSize="flex">
                {t("transfer.product")}
              </ExitsTableHead>
              <ExitsTableHead cellAlign="center" colSize="numeric">
                {t("transfer.sent")}
              </ExitsTableHead>
              <ExitsTableHead cellAlign="center" colSize="numeric">
                {t("transfer.received")}
              </ExitsTableHead>
              <ExitsTableHead cellAlign="center" colSize="numeric">
                {t("transfer.closedShort")}
              </ExitsTableHead>
              <ExitsTableHead cellAlign="text">{t("transfer.discrepancyReason")}</ExitsTableHead>
            </ExitsTableRow>
          </ExitsTableHeader>
          <ExitsTableBody>
            {closeLines.map((line) => {
              const outstanding = outstandingForLine(line);
              const reasonMissing = !reasonByLine[line.lineId]?.trim();
              return (
                <ExitsTableRow key={line.lineId} data-testid={`transfer-close-line-${line.lineId}`}>
                  <ExitsTableCell cellAlign="text" colSize="flex" className="font-medium">
                    {line.productName}
                  </ExitsTableCell>
                  <ExitsTableCell cellAlign="center" colSize="numeric" className="tabular-nums">
                    {formatTransferQty(line.sentQty)}
                  </ExitsTableCell>
                  <ExitsTableCell cellAlign="center" colSize="numeric" className="tabular-nums">
                    {formatTransferQty(line.receivedQty)}
                  </ExitsTableCell>
                  <ExitsTableCell cellAlign="center" colSize="numeric" className="tabular-nums">
                    {formatTransferQty(outstanding)}
                  </ExitsTableCell>
                  <ExitsTableCell cellAlign="text">
                    <div className="flex min-w-0 flex-col gap-1">
                      <select
                        className={`exits-select${reasonMissing ? " border-destructive" : ""}`}
                        value={reasonByLine[line.lineId] ?? ""}
                        aria-invalid={reasonMissing ? true : undefined}
                        onChange={(e) =>
                          setReasonByLine((prev) => ({ ...prev, [line.lineId]: e.target.value }))
                        }
                        data-testid={`transfer-close-reason-${line.lineId}`}
                      >
                        <option value="">{t("transfer.selectDiscrepancy")}</option>
                        {INVENTORY_TRANSFER_DISCREPANCY_REASONS.map((code) => (
                          <option key={code} value={code}>
                            {t(inventoryTransferDiscrepancyLabelKey(code))}
                          </option>
                        ))}
                      </select>
                      <input
                        className="exits-input"
                        value={noteByLine[line.lineId] ?? ""}
                        placeholder={t("transfer.discrepancyNote")}
                        onChange={(e) =>
                          setNoteByLine((prev) => ({ ...prev, [line.lineId]: e.target.value }))
                        }
                        data-testid={`transfer-close-note-${line.lineId}`}
                      />
                    </div>
                  </ExitsTableCell>
                </ExitsTableRow>
              );
            })}
          </ExitsTableBody>
        </ExitsTable>
      </ExitsTableContainer>
    </ExitsModal>
  );
}
