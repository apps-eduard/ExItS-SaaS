export type ExpirationLotDraft = {
  id: string;
  quantity: string;
  expiryDate: string;
  lotNumber: string;
};

export type ParsedExpirationLotRow = {
  quantity: number;
  expiryDate: string;
  lotNumber: string;
};

const QUANTITY_EPSILON = 1e-9;

let draftIdCounter = 0;

export function createExpirationLotDraft(
  quantity = "",
  id = `exp-lot-${Date.now()}-${++draftIdCounter}`,
): ExpirationLotDraft {
  return {
    id,
    quantity,
    expiryDate: "",
    lotNumber: "",
  };
}

export function parseLotDraftQuantity(raw: string): number | null {
  const trimmed = raw.trim();
  if (!trimmed) {
    return null;
  }
  const value = Number(trimmed);
  if (Number.isNaN(value)) {
    return null;
  }
  return value;
}

export function sumAllocatedQuantity(rows: Array<{ quantity: number }>): number {
  return rows.reduce((sum, row) => sum + (Number.isFinite(row.quantity) ? row.quantity : 0), 0);
}

export function remainingToAllocate(onHand: number, allocated: number): number {
  return onHand - allocated;
}

export function isOverAllocated(allocated: number, onHand: number): boolean {
  return allocated - onHand > QUANTITY_EPSILON;
}

export function overAllocatedAmount(allocated: number, onHand: number): number {
  return Math.max(0, allocated - onHand);
}

export function maxQuantityForRow(
  onHand: number,
  drafts: ExpirationLotDraft[],
  rowId: string,
): number {
  const rows = parseExpirationLotRows(drafts);
  let otherAllocated = 0;
  for (let index = 0; index < drafts.length; index += 1) {
    if (drafts[index].id === rowId) {
      continue;
    }
    const quantity = rows[index]?.quantity;
    if (Number.isFinite(quantity) && quantity > 0) {
      otherAllocated += quantity;
    }
  }
  return Math.max(0, onHand - otherAllocated);
}

export function clampLotDraftQuantity(
  onHand: number,
  drafts: ExpirationLotDraft[],
  rowId: string,
  raw: string,
): string {
  const parsed = parseLotDraftQuantity(raw);
  if (parsed === null) {
    return raw;
  }
  const max = maxQuantityForRow(onHand, drafts, rowId);
  if (parsed <= max + QUANTITY_EPSILON) {
    return raw;
  }
  if (max <= 0) {
    return "";
  }
  return String(max);
}

export function quantitiesMatchOnHand(allocated: number, onHand: number): boolean {
  return Math.abs(allocated - onHand) < QUANTITY_EPSILON;
}

export function parseExpirationLotRows(drafts: ExpirationLotDraft[]): ParsedExpirationLotRow[] {
  return drafts.map((draft) => ({
    quantity: parseLotDraftQuantity(draft.quantity) ?? Number.NaN,
    expiryDate: draft.expiryDate.trim(),
    lotNumber: draft.lotNumber.trim(),
  }));
}

export function canSubmitExpirationAllocation(
  onHand: number,
  drafts: ExpirationLotDraft[],
): boolean {
  if (drafts.length === 0) {
    return onHand === 0;
  }

  const rows = parseExpirationLotRows(drafts);
  if (rows.some((row) => !(row.quantity > 0) || !row.expiryDate)) {
    return false;
  }

  return quantitiesMatchOnHand(sumAllocatedQuantity(rows), onHand);
}

export function isExpiryDateInPast(expiryDate: string, today: string): boolean {
  const trimmed = expiryDate.trim();
  if (!trimmed) {
    return false;
  }
  return trimmed < today;
}

export function toExistingStockLotInputs(drafts: ExpirationLotDraft[]) {
  return parseExpirationLotRows(drafts).map((row) => ({
    quantity: row.quantity,
    expiryDate: row.expiryDate,
    lotNumber: row.lotNumber ? row.lotNumber : null,
  }));
}
