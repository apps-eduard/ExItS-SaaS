import { vi } from "vitest";
import {
  jsonResponse,
  mockAuthenticatedFetch,
  textResponse,
  type AuthenticatedFetchOptions,
} from "@/test/auth-fixtures";
import type { GlobalCatalogImportStatus } from "@/api/global-catalog/global-catalog-types";

export const VALIDATED_IMPORT_ID = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
export const QUEUED_IMPORT_ID = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";
export const COMPLETED_IMPORT_ID = "cccccccc-cccc-cccc-cccc-cccccccccccc";
export const COMPLETED_WARNINGS_IMPORT_ID = "dddddddd-dddd-dddd-dddd-dddddddddd01";
export const FAILED_IMPORT_ID = "eeeeeeee-eeee-eeee-eeee-eeeeeeeeee01";

export type GlobalCatalogImportRecord = {
  id: string;
  fileName: string;
  fileFormat: string;
  contentType?: string | null;
  fileSizeBytes: number;
  fileSha256: string;
  idempotencyKey?: string | null;
  requestedBy: string;
  status: GlobalCatalogImportStatus;
  totalCount: number;
  processedCount: number;
  importedCount: number;
  skippedCount: number;
  failedCount: number;
  pendingCount: number;
  validProductCount: number;
  existingCategoriesReferencedCount: number;
  newCategoriesToCreateCount: number;
  warningCount: number;
  previewSummary?: string | null;
  currentStage?: string | null;
  errorSummary?: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
  startedAtUtc?: string | null;
  completedAtUtc?: string | null;
  lastHeartbeatAtUtc?: string | null;
  previewItems?: Array<Record<string, unknown>>;
  targetTemplateId?: string | null;
  targetTemplateName?: string | null;
};

export type GlobalCatalogImportErrorRecord = {
  id: string;
  rowNumber: number;
  name: string;
  sku?: string | null;
  barcode?: string | null;
  status: string;
  errorCode?: string | null;
  errorMessage?: string | null;
};

export type GlobalCatalogImportMockOptions = Pick<AuthenticatedFetchOptions, "permissions"> & {
  jobs?: GlobalCatalogImportRecord[];
  errors?: GlobalCatalogImportErrorRecord[];
  rejectUpload?: boolean;
  rejectEmptyUpload?: boolean;
  rejectUnsupportedUpload?: boolean;
};

const TEMPLATE_CSV = "name,unit,sku,brand,global_category_id\nSample,Piece,SKU-1,Brand,00000000-0000-0000-0000-000000000001\n";

const DEFAULT_PREVIEW_ITEMS = [
  {
    id: "11111111-1111-1111-1111-111111111101",
    rowNumber: 2,
    name: "Sardines",
    sku: "SAR-1",
    barcode: "1234567890123",
    categoryName: "Pantry",
    unit: "Piece",
    costPrice: 20,
    sellingPrice: 25.5,
    status: "Valid",
    willCreateCategory: false,
  },
  {
    id: "11111111-1111-1111-1111-111111111102",
    rowNumber: 3,
    name: "Noodles",
    sku: "NOOD-1",
    categoryName: "New Category",
    unit: "Pack",
    costPrice: 10,
    sellingPrice: 12,
    status: "ValidWithNewCategory",
    errorMessage: "New category will be created: New Category",
    willCreateCategory: true,
  },
];

function cloneDefaultJobs(): GlobalCatalogImportRecord[] {
  return [
    {
      id: VALIDATED_IMPORT_ID,
      fileName: "validated-import.csv",
      fileFormat: "Csv",
      contentType: "text/csv",
      fileSizeBytes: 1024,
      fileSha256: "a".repeat(64),
      requestedBy: "olivia@example.test",
      status: "Validated",
      totalCount: 2,
      processedCount: 0,
      importedCount: 0,
      skippedCount: 0,
      failedCount: 0,
      pendingCount: 2,
      validProductCount: 2,
      existingCategoriesReferencedCount: 1,
      newCategoriesToCreateCount: 1,
      warningCount: 1,
      previewSummary: "2 valid products, 1 new category will be created.",
      createdAtUtc: "2026-08-20T08:00:00Z",
      updatedAtUtc: "2026-08-20T08:01:00Z",
      previewItems: DEFAULT_PREVIEW_ITEMS,
    },
    {
      id: QUEUED_IMPORT_ID,
      fileName: "queued-import.csv",
      fileFormat: "Csv",
      fileSizeBytes: 512,
      fileSha256: "b".repeat(64),
      requestedBy: "olivia@example.test",
      status: "Queued",
      totalCount: 1,
      processedCount: 0,
      importedCount: 0,
      skippedCount: 0,
      failedCount: 0,
      pendingCount: 1,
      validProductCount: 1,
      existingCategoriesReferencedCount: 1,
      newCategoriesToCreateCount: 0,
      warningCount: 0,
      currentStage: "Queued",
      createdAtUtc: "2026-08-19T08:00:00Z",
      updatedAtUtc: "2026-08-19T08:02:00Z",
      previewItems: [],
    },
    {
      id: COMPLETED_IMPORT_ID,
      fileName: "completed-import.csv",
      fileFormat: "Csv",
      fileSizeBytes: 768,
      fileSha256: "c".repeat(64),
      requestedBy: "olivia@example.test",
      status: "Completed",
      totalCount: 2,
      processedCount: 2,
      importedCount: 2,
      skippedCount: 0,
      failedCount: 0,
      pendingCount: 0,
      validProductCount: 2,
      existingCategoriesReferencedCount: 2,
      newCategoriesToCreateCount: 0,
      warningCount: 0,
      createdAtUtc: "2026-08-18T08:00:00Z",
      updatedAtUtc: "2026-08-18T08:10:00Z",
      completedAtUtc: "2026-08-18T08:10:00Z",
      previewItems: [],
    },
    {
      id: COMPLETED_WARNINGS_IMPORT_ID,
      fileName: "completed-warnings.csv",
      fileFormat: "Csv",
      fileSizeBytes: 900,
      fileSha256: "d".repeat(64),
      requestedBy: "olivia@example.test",
      status: "CompletedWithWarnings",
      totalCount: 3,
      processedCount: 3,
      importedCount: 2,
      skippedCount: 1,
      failedCount: 0,
      pendingCount: 0,
      validProductCount: 2,
      existingCategoriesReferencedCount: 2,
      newCategoriesToCreateCount: 0,
      warningCount: 1,
      errorSummary: "1 row was skipped.",
      createdAtUtc: "2026-08-17T08:00:00Z",
      updatedAtUtc: "2026-08-17T08:12:00Z",
      completedAtUtc: "2026-08-17T08:12:00Z",
      previewItems: [],
    },
    {
      id: FAILED_IMPORT_ID,
      fileName: "failed-import.csv",
      fileFormat: "Csv",
      fileSizeBytes: 640,
      fileSha256: "e".repeat(64),
      requestedBy: "olivia@example.test",
      status: "Failed",
      totalCount: 2,
      processedCount: 2,
      importedCount: 0,
      skippedCount: 0,
      failedCount: 2,
      pendingCount: 0,
      validProductCount: 0,
      existingCategoriesReferencedCount: 0,
      newCategoriesToCreateCount: 0,
      warningCount: 0,
      errorSummary: "All rows failed validation.",
      createdAtUtc: "2026-08-16T08:00:00Z",
      updatedAtUtc: "2026-08-16T08:05:00Z",
      completedAtUtc: "2026-08-16T08:05:00Z",
      previewItems: [],
    },
  ];
}

const DEFAULT_ERRORS: GlobalCatalogImportErrorRecord[] = Array.from({ length: 25 }, (_, index) => ({
  id: `22222222-2222-2222-2222-${String(index).padStart(12, "0")}`,
  rowNumber: index + 2,
  name: `Failed Product ${index}`,
  sku: `FAIL-${index}`,
  status: "Failed",
  errorCode: "application.catalog_import.validation",
  errorMessage: `Row ${index + 2} failed validation.`,
}));

function pathnameOf(url: string): string {
  try {
    return new URL(url, "http://local.test").pathname;
  } catch {
    return url;
  }
}

function listJobs(jobs: GlobalCatalogImportRecord[], url: URL): Response {
  let filtered = [...jobs];
  const status = url.searchParams.get("status");
  if (status) {
    filtered = filtered.filter((job) => job.status === status);
  }
  const page = Math.max(1, Number(url.searchParams.get("page") ?? "1") || 1);
  const pageSize = Math.max(1, Number(url.searchParams.get("pageSize") ?? "20") || 20);
  const start = (page - 1) * pageSize;
  const pagedItems = filtered.slice(start, start + pageSize).map((job) => ({
    ...job,
    previewItems: undefined,
  }));
  return jsonResponse(200, {
    items: pagedItems,
    totalCount: filtered.length,
    page,
    pageSize,
  });
}

function listErrors(errors: GlobalCatalogImportErrorRecord[], url: URL): Response {
  const page = Math.max(1, Number(url.searchParams.get("page") ?? "1") || 1);
  const pageSize = Math.max(1, Number(url.searchParams.get("pageSize") ?? "20") || 20);
  const start = (page - 1) * pageSize;
  const pagedItems = errors.slice(start, start + pageSize);
  return jsonResponse(200, {
    items: pagedItems,
    totalCount: errors.length,
    page,
    pageSize,
  });
}

async function readUploadMeta(init?: RequestInit): Promise<{
  fileName?: string;
  fileSize?: number;
  idempotencyKey?: string;
}> {
  const body = init?.body;
  if (!(body instanceof FormData)) {
    return {};
  }
  const file = body.get("file");
  const idempotencyKey = body.get("idempotencyKey");
  if (!(file instanceof File)) {
    return {
      idempotencyKey: typeof idempotencyKey === "string" ? idempotencyKey : undefined,
    };
  }
  return {
    fileName: file.name,
    fileSize: file.size,
    idempotencyKey: typeof idempotencyKey === "string" ? idempotencyKey : undefined,
  };
}

export function installGlobalCatalogImportMock(options: GlobalCatalogImportMockOptions = {}) {
  const mutationHeaders: Headers[] = [];
  const uploadHeaders: Headers[] = [];
  let jobs = options.jobs ? options.jobs.map((job) => ({ ...job })) : cloneDefaultJobs();
  const errors = options.errors ? options.errors.map((error) => ({ ...error })) : [...DEFAULT_ERRORS];
  const idempotencyIndex = new Map<string, string>();

  const importPermissions = options.permissions ?? [
    "platform.permission.view_portfolio",
    "platform.permission.view_global_catalog",
    "platform.permission.import_global_products",
  ];

  const innerMock = mockAuthenticatedFetch({
    permissions: importPermissions,
  });

  const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    const path = pathnameOf(url);
    const method = init?.method ?? "GET";

    if (path.endsWith("/imports/template.csv") && method === "GET") {
      return textResponse(200, TEMPLATE_CSV);
    }

    if (!url.includes("/api/v1/platform/global-catalog/products/imports")) {
      return innerMock(input, init);
    }

    const parsedUrl = new URL(url, "http://local.test");
    const confirmMatch = path.match(
      /\/api\/v1\/platform\/global-catalog\/products\/imports\/([0-9a-fA-F-]{36})\/confirm$/,
    );
    const errorsMatch = path.match(
      /\/api\/v1\/platform\/global-catalog\/products\/imports\/([0-9a-fA-F-]{36})\/errors$/,
    );
    const detailMatch = path.match(
      /\/api\/v1\/platform\/global-catalog\/products\/imports\/([0-9a-fA-F-]{36})$/,
    );

    if (method === "POST" && path.endsWith("/products/imports")) {
      uploadHeaders.push(new Headers(init?.headers));
      const uploadMeta = await readUploadMeta(init);
      if (options.rejectEmptyUpload && (!uploadMeta.fileSize || uploadMeta.fileSize <= 0)) {
        return jsonResponse(400, {
          title: "Bad Request",
          status: 400,
          detail: "Uploaded file is empty.",
        });
      }
      if (
        options.rejectUnsupportedUpload &&
        uploadMeta.fileName &&
        !uploadMeta.fileName.toLowerCase().endsWith(".csv") &&
        !uploadMeta.fileName.toLowerCase().endsWith(".xlsx")
      ) {
        return jsonResponse(400, {
          title: "Bad Request",
          status: 400,
          detail: "Only .csv and .xlsx files are supported.",
        });
      }
      if (options.rejectUpload) {
        return jsonResponse(400, {
          title: "Bad Request",
          status: 400,
          detail: "Upload rejected.",
        });
      }
      if (uploadMeta.idempotencyKey && idempotencyIndex.has(uploadMeta.idempotencyKey)) {
        const existingId = idempotencyIndex.get(uploadMeta.idempotencyKey)!;
        const existing = jobs.find((job) => job.id === existingId);
        if (existing) {
          return jsonResponse(201, existing);
        }
      }
      const created: GlobalCatalogImportRecord = {
        id: crypto.randomUUID(),
        fileName: uploadMeta.fileName ?? "upload.csv",
        fileFormat: uploadMeta.fileName?.toLowerCase().endsWith(".xlsx") ? "Xlsx" : "Csv",
        contentType: "text/csv",
        fileSizeBytes: uploadMeta.fileSize ?? 128,
        fileSha256: "f".repeat(64),
        idempotencyKey: uploadMeta.idempotencyKey ?? null,
        requestedBy: "olivia@example.test",
        status: "Validated",
        totalCount: 1,
        processedCount: 0,
        importedCount: 0,
        skippedCount: 0,
        failedCount: 0,
        pendingCount: 1,
        validProductCount: 1,
        existingCategoriesReferencedCount: 1,
        newCategoriesToCreateCount: 0,
        warningCount: 0,
        previewSummary: "1 valid product.",
        createdAtUtc: "2026-08-22T08:00:00Z",
        updatedAtUtc: "2026-08-22T08:00:00Z",
        previewItems: DEFAULT_PREVIEW_ITEMS.slice(0, 1),
      };
      jobs = [created, ...jobs];
      if (uploadMeta.idempotencyKey) {
        idempotencyIndex.set(uploadMeta.idempotencyKey, created.id);
      }
      return jsonResponse(201, created);
    }

    if (confirmMatch && method === "POST") {
      mutationHeaders.push(new Headers(init?.headers));
      const jobId = confirmMatch[1]!;
      const existing = jobs.find((job) => job.id === jobId);
      if (!existing) {
        return jsonResponse(404, { title: "Not Found", status: 404 });
      }
      if (existing.status !== "Validated") {
        return jsonResponse(409, {
          title: "Conflict",
          status: 409,
          detail: "Import can only be confirmed when status is Validated.",
        });
      }
      const updated: GlobalCatalogImportRecord = {
        ...existing,
        status: "Queued",
        currentStage: "Queued",
        updatedAtUtc: "2026-08-22T08:03:00Z",
      };
      jobs = jobs.map((job) => (job.id === jobId ? updated : job));
      return jsonResponse(200, updated);
    }

    if (errorsMatch && method === "GET") {
      return listErrors(errors, parsedUrl);
    }

    if (detailMatch && method === "GET") {
      const match = jobs.find((job) => job.id === detailMatch[1]);
      if (!match) {
        return jsonResponse(404, { title: "Not Found", status: 404 });
      }
      return jsonResponse(200, match);
    }

    if (path.endsWith("/products/imports") && method === "GET") {
      return listJobs(jobs, parsedUrl);
    }

    return jsonResponse(404, { title: "Not Found", status: 404 });
  });

  vi.stubGlobal("fetch", fetchMock);

  return {
    fetchMock,
    getJobs: () => jobs,
    mutationHeaders,
    uploadHeaders,
  };
}

export function importPermissionsOnly(): string[] {
  return [
    "platform.permission.view_portfolio",
    "platform.permission.import_global_products",
  ];
}

export function viewGlobalCatalogWithoutImportPermissions(): string[] {
  return [
    "platform.permission.view_portfolio",
    "platform.permission.view_global_catalog",
  ];
}
