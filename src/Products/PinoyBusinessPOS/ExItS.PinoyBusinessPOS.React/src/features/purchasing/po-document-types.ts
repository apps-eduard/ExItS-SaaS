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
  quantityLabel: string;
  unitCost: number;
  lineTotal: number;
  /** Buyer receive progress — omitted on seller snapshot. */
  receivedLabel?: string;
  outstandingLabel?: string;
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
