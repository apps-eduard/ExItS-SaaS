import type { ReactNode } from "react";
import type { StatusChipTone } from "@/components/exits/StatusChip";

/** Perspective of the viewer relative to the PO transaction. */
export type PoDocumentPerspective = "buyer" | "seller";

export type PoDocumentMetaField = {
  key: string;
  label: string;
  value: ReactNode;
};

export type PoDocumentLine = {
  id: string;
  productName: string;
  sku?: string | null;
  /** Numeric qty only; unit renders below in xs when `unitLabel` is set. */
  quantityLabel: string;
  /** Optional UOM under quantity (xs muted). */
  unitLabel?: string | null;
  unitCost: number;
  lineTotal: number;
};

export type PoDocumentEditableLine = PoDocumentLine & {
  quantity: number;
  unitOfMeasure?: string;
  unitCostEditable?: boolean;
  quantityEditable?: boolean;
};

export type PoDocumentStatus = {
  label: string;
  tone: StatusChipTone;
};

export type PoDocumentTotalRow = {
  key: string;
  label: string;
  amount: number;
  emphasis?: "normal" | "strong";
  testId?: string;
};
