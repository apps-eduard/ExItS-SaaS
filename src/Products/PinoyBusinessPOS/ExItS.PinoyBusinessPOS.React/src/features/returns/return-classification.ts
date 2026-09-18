const QUANTITY_EPSILON = 0.0005;

export function roundReturnQuantity(value: number): number {
  if (!Number.isFinite(value)) {
    return 0;
  }
  return Math.round(value * 1000) / 1000;
}

export function parseReturnQuantityInput(raw: string): number {
  const parsed = Number(raw);
  if (!Number.isFinite(parsed)) {
    return 0;
  }
  return Math.max(0, roundReturnQuantity(parsed));
}

export function isValidClassificationTotal(
  returnedQuantity: number,
  sellableQuantity: number,
  damagedQuantity: number,
): boolean {
  const expected = roundReturnQuantity(returnedQuantity);
  const actual = roundReturnQuantity(sellableQuantity) + roundReturnQuantity(damagedQuantity);
  return Math.abs(expected - actual) <= QUANTITY_EPSILON;
}
