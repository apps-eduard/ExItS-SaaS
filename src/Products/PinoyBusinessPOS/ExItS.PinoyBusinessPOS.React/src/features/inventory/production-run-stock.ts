export type ProduceMaterialStockRow = {
  materialProductId: string;
  name: string;
  uom: string;
  required: number;
  available: number | null;
  isExtra?: boolean;
};

export type ProduceShortage = {
  materialProductId: string;
  name: string;
  uom: string;
  required: number;
  available: number;
  shortBy: number;
};

export function findProduceShortages(rows: ProduceMaterialStockRow[]): ProduceShortage[] {
  const shortages: ProduceShortage[] = [];
  for (const row of rows) {
    if (row.available == null || !Number.isFinite(row.available)) {
      continue;
    }
    if (row.required > row.available + 1e-9) {
      shortages.push({
        materialProductId: row.materialProductId,
        name: row.name,
        uom: row.uom,
        required: row.required,
        available: row.available,
        shortBy: roundQty(row.required - row.available),
      });
    }
  }
  return shortages;
}

export function hasProduceShortage(rows: ProduceMaterialStockRow[]): boolean {
  return findProduceShortages(rows).length > 0;
}

function roundQty(value: number): number {
  return Math.round(value * 1000) / 1000;
}
