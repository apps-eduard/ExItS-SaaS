import { vi } from "vitest";
import {
  jsonResponse,
  mockAuthenticatedFetch,
  type AuthenticatedFetchOptions,
} from "@/test/auth-fixtures";
import type { GlobalBusinessTypeStatus } from "@/api/global-catalog/global-catalog-types";

export type GlobalCatalogBusinessTypeRecord = {
  id: string;
  code: string;
  name: string;
  description?: string | null;
  status: GlobalBusinessTypeStatus;
  sortOrder: number;
  iconReference?: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
};

export type GlobalCatalogBusinessTypeMockOptions = Pick<AuthenticatedFetchOptions, "permissions"> & {
  items?: GlobalCatalogBusinessTypeRecord[];
  updateConflict?: boolean;
  statusConflict?: boolean;
};

const CONCURRENCY_CONFLICT_DETAIL = "Business type was updated by another operator.";
const DUPLICATE_CODE_DETAIL = "A business type with this code already exists.";
const DUPLICATE_NAME_DETAIL = "A business type with this name already exists.";

const DEFAULT_BUSINESS_TYPE_ITEMS: GlobalCatalogBusinessTypeRecord[] = [
  {
    id: "dddddddd-dddd-dddd-dddd-dddddddddddd",
    code: "sari-sari",
    name: "Sari-Sari Store",
    description: "Neighborhood store",
    status: "Active",
    sortOrder: 1,
    iconReference: "store",
    createdAtUtc: "2026-01-01T08:00:00Z",
    updatedAtUtc: "2026-08-01T08:00:00Z",
  },
  {
    id: "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee",
    code: "mini-grocery",
    name: "Mini Grocery",
    description: "Small grocery",
    status: "Inactive",
    sortOrder: 2,
    createdAtUtc: "2026-01-02T08:00:00Z",
    updatedAtUtc: "2026-08-02T08:00:00Z",
  },
  {
    id: "ffffffff-ffff-ffff-ffff-ffffffffffff",
    code: "bakery",
    name: "Bakery",
    description: "Fresh bread and pastries",
    status: "Archived",
    sortOrder: 3,
    createdAtUtc: "2026-01-03T08:00:00Z",
    updatedAtUtc: "2026-08-03T08:00:00Z",
  },
];

function pathnameOf(url: string): string {
  try {
    return new URL(url, "http://local.test").pathname;
  } catch {
    return url;
  }
}

function cloneDefaultItems(): GlobalCatalogBusinessTypeRecord[] {
  return DEFAULT_BUSINESS_TYPE_ITEMS.map((item) => ({ ...item }));
}

function parseBody(init?: RequestInit): Record<string, unknown> | null {
  try {
    return init?.body ? (JSON.parse(String(init.body)) as Record<string, unknown>) : null;
  } catch {
    return null;
  }
}

function matchesSearch(item: GlobalCatalogBusinessTypeRecord, search: string): boolean {
  const needle = search.toLowerCase();
  return (
    item.name.toLowerCase().includes(needle) ||
    item.code.toLowerCase().includes(needle) ||
    String(item.description ?? "")
      .toLowerCase()
      .includes(needle)
  );
}

function compareBusinessTypes(
  left: GlobalCatalogBusinessTypeRecord,
  right: GlobalCatalogBusinessTypeRecord,
  sortBy: string,
  sortDesc: boolean,
): number {
  let result: number;
  switch (sortBy) {
    case "Name":
      result = left.name.localeCompare(right.name);
      break;
    case "Code":
      result = left.code.localeCompare(right.code);
      break;
    case "Status":
      result = left.status.localeCompare(right.status);
      break;
    case "UpdatedAtUtc":
      result = left.updatedAtUtc.localeCompare(right.updatedAtUtc);
      break;
    case "CreatedAtUtc":
      result = left.createdAtUtc.localeCompare(right.createdAtUtc);
      break;
    case "SortOrder":
    default:
      result = left.sortOrder - right.sortOrder;
      break;
  }
  return sortDesc ? -result : result;
}

function listBusinessTypes(
  items: GlobalCatalogBusinessTypeRecord[],
  url: URL,
): Response {
  let filtered = [...items];
  const status = url.searchParams.get("status");
  if (status) {
    filtered = filtered.filter((item) => item.status === status);
  }
  const search = url.searchParams.get("search")?.trim();
  if (search) {
    filtered = filtered.filter((item) => matchesSearch(item, search));
  }

  const sortBy = url.searchParams.get("sortBy") ?? "SortOrder";
  const sortDesc = url.searchParams.get("sortDesc") === "true";
  filtered.sort((left, right) => compareBusinessTypes(left, right, sortBy, sortDesc));

  const page = Math.max(1, Number(url.searchParams.get("page") ?? "1") || 1);
  const pageSize = Math.max(1, Number(url.searchParams.get("pageSize") ?? "20") || 20);
  const start = (page - 1) * pageSize;
  const pagedItems = filtered.slice(start, start + pageSize);

  return jsonResponse(200, {
    items: pagedItems,
    totalCount: filtered.length,
    page,
    pageSize,
  });
}

function conflictResponse(): Response {
  return jsonResponse(409, {
    title: "Conflict",
    status: 409,
    detail: CONCURRENCY_CONFLICT_DETAIL,
    errorCode: "application.concurrency_conflict",
  });
}

export function installGlobalCatalogBusinessTypeMock(
  options: GlobalCatalogBusinessTypeMockOptions = {},
) {
  const mutationHeaders: Headers[] = [];
  let items = options.items ? options.items.map((item) => ({ ...item })) : cloneDefaultItems();

  const innerMock = mockAuthenticatedFetch({
    permissions: options.permissions,
  });

  const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    const path = pathnameOf(url);
    const method = init?.method ?? "GET";

    if (method !== "GET") {
      mutationHeaders.push(new Headers(init?.headers));
    }

    if (!url.includes("/api/v1/platform/global-catalog/business-types")) {
      return innerMock(input, init);
    }

    const parsedUrl = new URL(url, "http://local.test");
    const statusMatch = path.match(
      /\/api\/v1\/platform\/global-catalog\/business-types\/([0-9a-fA-F-]{36})\/status$/,
    );
    const detailMatch = path.match(
      /\/api\/v1\/platform\/global-catalog\/business-types\/([0-9a-fA-F-]{36})$/,
    );

    if (statusMatch && method === "POST") {
      if (options.statusConflict) {
        return conflictResponse();
      }
      const body = parseBody(init);
      const businessTypeId = statusMatch[1]!;
      const existing = items.find((item) => item.id === businessTypeId);
      if (!existing) {
        return jsonResponse(404, { title: "Not Found", status: 404 });
      }
      const expectedUpdatedAtUtc = body?.expectedUpdatedAtUtc;
      if (
        typeof expectedUpdatedAtUtc === "string" &&
        existing.updatedAtUtc &&
        expectedUpdatedAtUtc !== existing.updatedAtUtc
      ) {
        return conflictResponse();
      }
      const nextStatus = body?.status;
      if (typeof nextStatus !== "string") {
        return jsonResponse(400, { title: "Bad Request", status: 400 });
      }
      const updated: GlobalCatalogBusinessTypeRecord = {
        ...existing,
        status: nextStatus as GlobalBusinessTypeStatus,
        updatedAtUtc: "2026-08-22T08:06:00Z",
      };
      items = items.map((item) => (item.id === businessTypeId ? updated : item));
      return jsonResponse(200, updated);
    }

    if (detailMatch && method === "GET") {
      const match = items.find((item) => item.id === detailMatch[1]);
      if (!match) {
        return jsonResponse(404, { title: "Not Found", status: 404 });
      }
      return jsonResponse(200, match);
    }

    if (detailMatch && method === "PUT") {
      if (options.updateConflict) {
        return conflictResponse();
      }
      const body = parseBody(init);
      const businessTypeId = detailMatch[1]!;
      const existing = items.find((item) => item.id === businessTypeId);
      if (!existing) {
        return jsonResponse(404, { title: "Not Found", status: 404 });
      }
      const expectedUpdatedAtUtc = body?.expectedUpdatedAtUtc;
      if (
        typeof expectedUpdatedAtUtc === "string" &&
        existing.updatedAtUtc &&
        expectedUpdatedAtUtc !== existing.updatedAtUtc
      ) {
        return conflictResponse();
      }
      const nextName = typeof body?.name === "string" ? body.name : existing.name;
      if (items.some((item) => item.id !== businessTypeId && item.name === nextName)) {
        return jsonResponse(409, {
          title: "Conflict",
          status: 409,
          detail: DUPLICATE_NAME_DETAIL,
          errorCode: "application.duplicate_business_type_name",
        });
      }
      const updated: GlobalCatalogBusinessTypeRecord = {
        ...existing,
        name: nextName,
        description:
          body?.description === null || typeof body?.description === "string"
            ? (body.description as string | null)
            : existing.description ?? null,
        sortOrder:
          typeof body?.sortOrder === "number" && Number.isFinite(body.sortOrder)
            ? body.sortOrder
            : existing.sortOrder,
        iconReference:
          body?.iconReference === null || typeof body?.iconReference === "string"
            ? (body.iconReference as string | null)
            : existing.iconReference ?? null,
        updatedAtUtc: "2026-08-22T08:05:00Z",
      };
      items = items.map((item) => (item.id === businessTypeId ? updated : item));
      return jsonResponse(200, updated);
    }

    if (path.endsWith("/api/v1/platform/global-catalog/business-types") && method === "GET") {
      return listBusinessTypes(items, parsedUrl);
    }

    if (path.endsWith("/api/v1/platform/global-catalog/business-types") && method === "POST") {
      const body = parseBody(init);
      const code = typeof body?.code === "string" ? body.code.trim() : "";
      const name = typeof body?.name === "string" ? body.name.trim() : "";
      if (!code || !name) {
        return jsonResponse(400, { title: "Bad Request", status: 400 });
      }
      if (items.some((item) => item.code.toLowerCase() === code.toLowerCase())) {
        return jsonResponse(409, {
          title: "Conflict",
          status: 409,
          detail: DUPLICATE_CODE_DETAIL,
          errorCode: "application.duplicate_business_type_code",
        });
      }
      if (items.some((item) => item.name === name)) {
        return jsonResponse(409, {
          title: "Conflict",
          status: 409,
          detail: DUPLICATE_NAME_DETAIL,
          errorCode: "application.duplicate_business_type_name",
        });
      }
      const created: GlobalCatalogBusinessTypeRecord = {
        id: crypto.randomUUID(),
        code,
        name,
        description:
          typeof body?.description === "string"
            ? body.description
            : body?.description === null
              ? null
              : null,
        status: "Active",
        sortOrder:
          typeof body?.sortOrder === "number" && Number.isFinite(body.sortOrder)
            ? body.sortOrder
            : 0,
        iconReference:
          typeof body?.iconReference === "string"
            ? body.iconReference
            : body?.iconReference === null
              ? null
              : null,
        createdAtUtc: "2026-08-22T08:00:00Z",
        updatedAtUtc: "2026-08-22T08:00:00Z",
      };
      items = [created, ...items];
      return jsonResponse(201, created);
    }

    return jsonResponse(404, { title: "Not Found", status: 404 });
  });

  vi.stubGlobal("fetch", fetchMock);

  return {
    fetchMock,
    getItems: () => items,
    mutationHeaders,
  };
}
