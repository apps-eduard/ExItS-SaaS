import { QuantityStepper } from "@/components/exits/MoneyQuantity";
import { Button } from "@/components/ui/button";
import {
  allDamagedDraft,
  allSellableDraft,
  balanceFromConfirmed,
  balanceFromRecovered,
  formatInspectQty,
  parseInspectDisposition,
  type InspectDispositionDraft,
} from "@/features/inventory/inventory-inspect-disposition";
import { formatTransferQty } from "@/features/inventory/inventory-transfer-labels";
import { useI18n } from "@/i18n/I18nProvider";

type InventoryInspectDispositionPanelProps = {
  totalQty: number;
  draft: InspectDispositionDraft;
  onChange: (next: InspectDispositionDraft) => void;
  recoveredLabel: string;
  confirmedLabel: string;
  helpText: string;
  allSellableLabel: string;
  allDamagedLabel: string;
  mustEqualLabel: string;
  disabled?: boolean;
  testIdPrefix: string;
};

function draftQty(text: string): number {
  if (text.trim() === "") {
    return 0;
  }
  const n = Number(text);
  return Number.isFinite(n) && n >= 0 ? n : 0;
}

export function InventoryInspectDispositionPanel({
  totalQty,
  draft,
  onChange,
  recoveredLabel,
  confirmedLabel,
  helpText,
  allSellableLabel,
  allDamagedLabel,
  mustEqualLabel,
  disabled = false,
  testIdPrefix,
}: InventoryInspectDispositionPanelProps) {
  const { t } = useI18n();
  const parsed = parseInspectDisposition(draft, totalQty);
  const balanced = parsed.ok;
  const decreaseLabel = t("transfer.decreaseQuantity");
  const increaseLabel = t("transfer.increaseQuantity");

  return (
    <div
      className="flex w-full min-w-0 flex-col gap-2"
      data-testid={`${testIdPrefix}-disposition`}
    >
      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
        {helpText.replace("{qty}", formatTransferQty(totalQty))}
      </p>
      <div className="flex flex-wrap gap-2">
        <Button
          type="button"
          intent="primary"
          appearance="outline"
          size="sm"
          disabled={disabled}
          onClick={() => onChange(allSellableDraft(totalQty))}
          data-testid={`${testIdPrefix}-all-sellable`}
        >
          {allSellableLabel}
        </Button>
        <Button
          type="button"
          intent="primary"
          appearance="outline"
          size="sm"
          disabled={disabled}
          onClick={() => onChange(allDamagedDraft(totalQty))}
          data-testid={`${testIdPrefix}-all-damaged`}
        >
          {allDamagedLabel}
        </Button>
      </div>
      <div className="flex min-w-0 flex-col gap-3">
        <label className="flex min-w-0 flex-col gap-1.5 text-[length:var(--exits-text-sm)]">
          {recoveredLabel}
          <QuantityStepper
            compact
            variant="outline"
            min={0}
            max={totalQty}
            step={1}
            precision={3}
            value={draftQty(draft.recovered)}
            disabled={disabled}
            onChange={(next) =>
              onChange(balanceFromRecovered(totalQty, formatInspectQty(next)))
            }
            decreaseLabel={decreaseLabel}
            increaseLabel={increaseLabel}
            ariaLabel={recoveredLabel}
            valueTestId={`${testIdPrefix}-sellable`}
            className="max-w-full justify-start"
          />
        </label>
        <label className="flex min-w-0 flex-col gap-1.5 text-[length:var(--exits-text-sm)]">
          {confirmedLabel}
          <QuantityStepper
            compact
            variant="outline"
            min={0}
            max={totalQty}
            step={1}
            precision={3}
            value={draftQty(draft.confirmed)}
            disabled={disabled}
            onChange={(next) =>
              onChange(balanceFromConfirmed(totalQty, formatInspectQty(next)))
            }
            decreaseLabel={decreaseLabel}
            increaseLabel={increaseLabel}
            ariaLabel={confirmedLabel}
            valueTestId={`${testIdPrefix}-damaged`}
            className="max-w-full justify-start"
          />
        </label>
      </div>
      {!balanced ? (
        <p
          className="m-0 text-[length:var(--exits-text-xs)] text-muted"
          data-testid={`${testIdPrefix}-must-equal`}
        >
          {mustEqualLabel.replace("{qty}", formatTransferQty(totalQty))}
        </p>
      ) : null}
    </div>
  );
}

export function isInspectDispositionReady(
  draft: InspectDispositionDraft,
  totalQty: number,
): boolean {
  return parseInspectDisposition(draft, totalQty).ok;
}
