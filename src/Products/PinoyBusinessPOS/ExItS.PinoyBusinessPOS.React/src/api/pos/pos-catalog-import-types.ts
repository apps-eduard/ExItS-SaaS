/** POS catalog-import job DTOs (camelCase wire). */

export type PosTemplateImportStatus = {
  platformTemplateId: string;
  firstBatchTotal: number;
  firstBatchImportedCount: number;
  firstBatchComplete: boolean;
  subsequentTotal: number;
  subsequentImportedCount: number;
  subsequentRemainingCount: number;
  hasSubsequentBatches: boolean;
  canImportFirstBatch: boolean;
  canImportNextBatch: boolean;
  suggestedNextBatchNumber: number;
  nextBatchSizeEstimate: number;
  defaultBatchSize: number;
};

export type ImportedGlobalProducts = {
  importedIds: string[];
};

export type PosCatalogImportJob = {
  jobId: string;
  organizationId: string;
  jobKind: string;
  platformTemplateId?: string | null;
  batchNumber?: number | null;
  catalogSource: string;
  status: string;
  totalCount: number;
  processedCount: number;
  importedCount: number;
  skippedCount: number;
  failedCount: number;
  currentStage?: string | null;
  errorSummary?: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
  startedAtUtc?: string | null;
  completedAtUtc?: string | null;
};

export type PosCatalogImportItem = {
  itemId: string;
  platformGlobalProductId: string;
  sortOrder: number;
  name: string;
  sku?: string | null;
  barcode?: string | null;
  unitOfMeasure: string;
  sellingMode: string;
  suggestedPrice: number;
  status: string;
  localProductId?: string | null;
  errorCode?: string | null;
  errorMessage?: string | null;
  processedAtUtc?: string | null;
};

export type PosCatalogImportItemPaged = {
  items: PosCatalogImportItem[];
  totalCount: number;
  page: number;
  pageSize: number;
};

export type ImportTemplateBatchRequest = {
  platformTemplateId: string;
  batchNumber?: number;
  idempotencyKey?: string | null;
};

export type ImportSelectedProductsRequest = {
  platformGlobalProductIds: string[];
  idempotencyKey?: string | null;
};

export function isImportJobTerminal(status: string): boolean {
  const normalized = status.trim().toLowerCase();
  return (
    normalized === "completed" ||
    normalized === "failed" ||
    normalized === "cancelled" ||
    normalized === "canceled"
  );
}

export function isImportJobActive(status: string): boolean {
  const normalized = status.trim().toLowerCase();
  return (
    normalized === "queued" ||
    normalized === "pending" ||
    normalized === "running" ||
    normalized === "processing" ||
    normalized === "inprogress" ||
    normalized === "in_progress"
  );
}
