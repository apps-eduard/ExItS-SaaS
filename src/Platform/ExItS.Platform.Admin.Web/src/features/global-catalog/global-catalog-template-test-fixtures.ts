import { vi } from "vitest";
import {
  jsonResponse,
  mockAuthenticatedFetch,
  type AuthenticatedFetchOptions,
} from "@/test/auth-fixtures";
import type {
  CatalogTemplateSelectionMode,
  GlobalCatalogTemplateStatus,
} from "@/api/global-catalog/global-catalog-types";

export type GlobalCatalogTemplateProductRecord = {
  id: string;
  globalProductId: string;
  sortOrder: number;
  isFeatured: boolean;
  isFirstBatch: boolean;
  productName?: string;
  sku?: string;
  barcode?: string;
  brand?: string;
};

export type GlobalCatalogTemplateRecord = {
  id: string;
  name: string;
  slug: string;
  description?: string | null;
  iconReference?: string | null;
  primaryBusinessType: string;
  primaryBusinessTypeId: string;
  status: GlobalCatalogTemplateStatus;
  defaultBatchSize: number;
  selectionMode: CatalogTemplateSelectionMode;
  publishedAtUtc?: string | null;
  productCount: number;
  firstBatchCount: number;
  createdAtUtc: string;
  updatedAtUtc: string;
  products: GlobalCatalogTemplateProductRecord[];
};

export type GlobalCatalogAvailableProductRecord = {
  id: string;
  name: string;
  sku: string;
  brand: string;
  status: string;
  unit: string;
  sellingMode: string;
};

export type GlobalCatalogTemplateMockOptions = Pick<AuthenticatedFetchOptions, "permissions"> & {
  templates?: GlobalCatalogTemplateRecord[];
  availableProducts?: GlobalCatalogAvailableProductRecord[];
  updateConflict?: boolean;
  lifecycleConflict?: boolean;
};

const CONCURRENCY_CONFLICT_DETAIL = "Catalog template was updated by another operator.";
const BUSINESS_TYPE_ID = "dddddddd-dddd-dddd-dddd-dddddddddddd";

const DEFAULT_AVAILABLE_PRODUCTS: GlobalCatalogAvailableProductRecord[] = [
  {
    id: "11111111-1111-1111-1111-111111111111",
    name: "Canned Tuna",
    sku: "TUNA-001",
    brand: "Blue Sea",
    status: "Active",
    unit: "Can",
    sellingMode: "PerItem",
  },
  {
    id: "22222222-2222-2222-2222-222222222222",
    name: "Instant Noodles",
    sku: "NOOD-001",
    brand: "Quick Meal",
    status: "Active",
    unit: "Pack",
    sellingMode: "PerItem",
  },
  {
    id: "33333333-3333-3333-3333-333333333333",
    name: "Bottled Water",
    sku: "WATER-001",
    brand: "Fresh Spring",
    status: "Active",
    unit: "Bottle",
    sellingMode: "PerItem",
  },
];

const DEFAULT_TEMPLATES: GlobalCatalogTemplateRecord[] = [
  {
    id: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
    name: "Sari-Sari Starter",
    slug: "sari-sari-starter",
    description: "Starter catalog for sari-sari stores",
    iconReference: "store",
    primaryBusinessType: "sari-sari",
    primaryBusinessTypeId: BUSINESS_TYPE_ID,
    status: "Draft",
    defaultBatchSize: 20,
    selectionMode: "Curated",
    productCount: 1,
    firstBatchCount: 1,
    createdAtUtc: "2026-01-01T08:00:00Z",
    updatedAtUtc: "2026-08-01T08:00:00Z",
    products: [
      {
        id: "99999999-9999-9999-9999-999999999991",
        globalProductId: "11111111-1111-1111-1111-111111111111",
        sortOrder: 1,
        isFeatured: true,
        isFirstBatch: true,
        productName: "Canned Tuna",
        sku: "TUNA-001",
        brand: "Blue Sea",
      },
    ],
  },
  {
    id: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
    name: "Mini Grocery Essentials",
    slug: "mini-grocery-essentials",
    description: "Published essentials template",
    primaryBusinessType: "sari-sari",
    primaryBusinessTypeId: BUSINESS_TYPE_ID,
    status: "Published",
    defaultBatchSize: 25,
    selectionMode: "Hybrid",
    publishedAtUtc: "2026-07-01T08:00:00Z",
    productCount: 2,
    firstBatchCount: 1,
    createdAtUtc: "2026-02-01T08:00:00Z",
    updatedAtUtc: "2026-08-02T08:00:00Z",
    products: [
      {
        id: "99999999-9999-9999-9999-999999999992",
        globalProductId: "11111111-1111-1111-1111-111111111111",
        sortOrder: 1,
        isFeatured: false,
        isFirstBatch: true,
        productName: "Canned Tuna",
        sku: "TUNA-001",
      },
      {
        id: "99999999-9999-9999-9999-999999999993",
        globalProductId: "22222222-2222-2222-2222-222222222222",
        sortOrder: 2,
        isFeatured: true,
        isFirstBatch: false,
        productName: "Instant Noodles",
        sku: "NOOD-001",
      },
    ],
  },
  {
    id: "cccccccc-cccc-cccc-cccc-cccccccccccc",
    name: "Legacy Template",
    slug: "legacy-template",
    description: "Archived template",
    primaryBusinessType: "sari-sari",
    primaryBusinessTypeId: BUSINESS_TYPE_ID,
    status: "Archived",
    defaultBatchSize: 10,
    selectionMode: "Auto",
    productCount: 0,
    firstBatchCount: 0,
    createdAtUtc: "2026-03-01T08:00:00Z",
    updatedAtUtc: "2026-08-03T08:00:00Z",
    products: [],
  },
];

function pathnameOf(url: string): string {
  try {
    return new URL(url, "http://local.test").pathname;
  } catch {
    return url;
  }
}

function cloneTemplates(items: GlobalCatalogTemplateRecord[]): GlobalCatalogTemplateRecord[] {
  return items.map((item) => ({
    ...item,
    products: item.products.map((product) => ({ ...product })),
  }));
}

function parseBody(init?: RequestInit): Record<string, unknown> | null {
  try {
    return init?.body ? (JSON.parse(String(init.body)) as Record<string, unknown>) : null;
  } catch {
    return null;
  }
}

function summarize(template: GlobalCatalogTemplateRecord) {
  const { products, ...summary } = template;
  return {
    ...summary,
    productCount: products.length,
    firstBatchCount: products.filter((product) => product.isFirstBatch).length,
  };
}

function compareTemplates(
  left: GlobalCatalogTemplateRecord,
  right: GlobalCatalogTemplateRecord,
  sortBy: string,
  sortDesc: boolean,
): number {
  let result: number;
  switch (sortBy) {
    case "Slug":
      result = left.slug.localeCompare(right.slug);
      break;
    case "Status":
      result = left.status.localeCompare(right.status);
      break;
    case "PrimaryBusinessType":
      result = left.primaryBusinessType.localeCompare(right.primaryBusinessType);
      break;
    case "UpdatedAtUtc":
      result = left.updatedAtUtc.localeCompare(right.updatedAtUtc);
      break;
    case "CreatedAtUtc":
      result = left.createdAtUtc.localeCompare(right.createdAtUtc);
      break;
    case "ProductCount":
      result = left.products.length - right.products.length;
      break;
    case "Name":
    default:
      result = left.name.localeCompare(right.name);
      break;
  }
  return sortDesc ? -result : result;
}

function listTemplates(templates: GlobalCatalogTemplateRecord[], url: URL): Response {
  let filtered = [...templates];
  const status = url.searchParams.get("status");
  if (status) {
    filtered = filtered.filter((item) => item.status === status);
  }
  const primaryBusinessTypeId = url.searchParams.get("primaryBusinessTypeId");
  if (primaryBusinessTypeId) {
    filtered = filtered.filter((item) => item.primaryBusinessTypeId === primaryBusinessTypeId);
  }
  const search = url.searchParams.get("search")?.trim().toLowerCase();
  if (search) {
    filtered = filtered.filter(
      (item) =>
        item.name.toLowerCase().includes(search) ||
        item.slug.toLowerCase().includes(search) ||
        String(item.description ?? "")
          .toLowerCase()
          .includes(search),
    );
  }
  const sortBy = url.searchParams.get("sortBy") ?? "Name";
  const sortDesc = url.searchParams.get("sortDesc") === "true";
  filtered.sort((left, right) => compareTemplates(left, right, sortBy, sortDesc));
  const page = Math.max(1, Number(url.searchParams.get("page") ?? "1") || 1);
  const pageSize = Math.max(1, Number(url.searchParams.get("pageSize") ?? "20") || 20);
  const start = (page - 1) * pageSize;
  return jsonResponse(200, {
    items: filtered.slice(start, start + pageSize).map(summarize),
    totalCount: filtered.length,
    page,
    pageSize,
  });
}

function listAvailableProducts(
  template: GlobalCatalogTemplateRecord,
  pool: GlobalCatalogAvailableProductRecord[],
  url: URL,
): Response {
  const assignedIds = new Set(template.products.map((product) => product.globalProductId));
  let filtered = pool.filter((product) => !assignedIds.has(product.id));
  const search = url.searchParams.get("search")?.trim().toLowerCase();
  if (search) {
    filtered = filtered.filter(
      (product) =>
        product.name.toLowerCase().includes(search) ||
        product.sku.toLowerCase().includes(search) ||
        product.brand.toLowerCase().includes(search),
    );
  }
  const page = Math.max(1, Number(url.searchParams.get("page") ?? "1") || 1);
  const pageSize = Math.max(1, Number(url.searchParams.get("pageSize") ?? "10") || 10);
  const start = (page - 1) * pageSize;
  return jsonResponse(200, {
    items: filtered.slice(start, start + pageSize),
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

function businessTypeListResponse(): Response {
  return jsonResponse(200, {
    items: [
      {
        id: BUSINESS_TYPE_ID,
        code: "sari-sari",
        name: "Sari-Sari Store",
        description: "Neighborhood store",
        status: "Active",
        sortOrder: 1,
        iconReference: "store",
        createdAtUtc: "2026-01-01T08:00:00Z",
        updatedAtUtc: "2026-08-01T08:00:00Z",
      },
    ],
    totalCount: 1,
    page: 1,
    pageSize: 100,
  });
}

function touchTemplate(template: GlobalCatalogTemplateRecord) {
  template.updatedAtUtc = new Date().toISOString();
  template.productCount = template.products.length;
  template.firstBatchCount = template.products.filter((product) => product.isFirstBatch).length;
}

export function installGlobalCatalogTemplateMock(options: GlobalCatalogTemplateMockOptions = {}) {
  const mutationHeaders: Headers[] = [];
  const templates = options.templates
    ? cloneTemplates(options.templates)
    : cloneTemplates(DEFAULT_TEMPLATES);
  const availableProducts = options.availableProducts
    ? options.availableProducts.map((item) => ({ ...item }))
    : DEFAULT_AVAILABLE_PRODUCTS.map((item) => ({ ...item }));

  const innerMock = mockAuthenticatedFetch({
    permissions: options.permissions ?? [
      "platform.permission.view_portfolio",
      "platform.permission.view_global_catalog",
      "platform.permission.manage_catalog_templates",
      "platform.permission.publish_catalog_templates",
    ],
  });

  const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    const path = pathnameOf(url);
    const method = init?.method ?? "GET";

    if (method !== "GET") {
      mutationHeaders.push(new Headers(init?.headers));
    }

    if (url.includes("/api/v1/platform/global-catalog/business-types") && method === "GET") {
      return businessTypeListResponse();
    }

    if (!url.includes("/api/v1/platform/global-catalog/templates")) {
      return innerMock(input, init);
    }

    const parsedUrl = new URL(url, "http://local.test");
    const availableMatch = path.match(
      /\/api\/v1\/platform\/global-catalog\/templates\/([0-9a-fA-F-]{36})\/available-products$/,
    );
    const lifecycleMatch = path.match(
      /\/api\/v1\/platform\/global-catalog\/templates\/([0-9a-fA-F-]{36})\/(publish|unpublish|archive)$/,
    );
    const productMatch = path.match(
      /\/api\/v1\/platform\/global-catalog\/templates\/([0-9a-fA-F-]{36})\/products(?:\/([0-9a-fA-F-]{36})|\/order|\/bulk(?:-remove)?)?$/,
    );
    const detailMatch = path.match(/\/api\/v1\/platform\/global-catalog\/templates\/([0-9a-fA-F-]{36})$/);

    if (path.endsWith("/templates") && method === "GET") {
      return listTemplates(templates, parsedUrl);
    }

    if (path.endsWith("/templates") && method === "POST") {
      const body = parseBody(init);
      const id = crypto.randomUUID();
      const created: GlobalCatalogTemplateRecord = {
        id,
        name: String(body?.name ?? "New Template"),
        slug: String(body?.slug ?? "new-template"),
        description: typeof body?.description === "string" ? body.description : "",
        iconReference: typeof body?.iconReference === "string" ? body.iconReference : "",
        primaryBusinessType: "sari-sari",
        primaryBusinessTypeId: String(body?.primaryBusinessTypeId ?? BUSINESS_TYPE_ID),
        status: "Draft",
        defaultBatchSize: Number(body?.defaultBatchSize ?? 20),
        selectionMode: (body?.selectionMode as CatalogTemplateSelectionMode) ?? "Curated",
        productCount: 0,
        firstBatchCount: 0,
        createdAtUtc: new Date().toISOString(),
        updatedAtUtc: new Date().toISOString(),
        products: [],
      };
      templates.push(created);
      return jsonResponse(201, { ...created, products: [] });
    }

    if (availableMatch && method === "GET") {
      const template = templates.find((item) => item.id === availableMatch[1]);
      if (!template) {
        return jsonResponse(404, { title: "Not Found", status: 404 });
      }
      return listAvailableProducts(template, availableProducts, parsedUrl);
    }

    if (detailMatch && method === "GET") {
      const template = templates.find((item) => item.id === detailMatch[1]);
      if (!template) {
        return jsonResponse(404, { title: "Not Found", status: 404 });
      }
      return jsonResponse(200, template);
    }

    if (detailMatch && method === "PUT") {
      if (options.updateConflict) {
        return conflictResponse();
      }
      const template = templates.find((item) => item.id === detailMatch[1]);
      if (!template) {
        return jsonResponse(404, { title: "Not Found", status: 404 });
      }
      const body = parseBody(init);
      template.name = String(body?.name ?? template.name);
      template.slug = String(body?.slug ?? template.slug);
      template.description = typeof body?.description === "string" ? body.description : template.description;
      template.defaultBatchSize = Number(body?.defaultBatchSize ?? template.defaultBatchSize);
      template.selectionMode =
        (body?.selectionMode as CatalogTemplateSelectionMode) ?? template.selectionMode;
      touchTemplate(template);
      return jsonResponse(200, template);
    }

    if (lifecycleMatch && method === "POST") {
      if (options.lifecycleConflict) {
        return conflictResponse();
      }
      const template = templates.find((item) => item.id === lifecycleMatch[1]);
      if (!template) {
        return jsonResponse(404, { title: "Not Found", status: 404 });
      }
      const action = lifecycleMatch[2];
      if (action === "publish") {
        if (template.products.length === 0) {
          return jsonResponse(422, {
            title: "Validation failed",
            status: 422,
            detail: "A template must include at least one product before publish.",
          });
        }
        template.status = "Published";
        template.publishedAtUtc = new Date().toISOString();
      } else if (action === "unpublish") {
        template.status = "Draft";
      } else {
        template.status = "Archived";
      }
      touchTemplate(template);
      return jsonResponse(200, template);
    }

    if (productMatch) {
      const template = templates.find((item) => item.id === productMatch[1]);
      if (!template) {
        return jsonResponse(404, { title: "Not Found", status: 404 });
      }

      if (path.endsWith("/products/bulk") && method === "POST") {
        const body = parseBody(init);
        const ids = Array.isArray(body?.globalProductIds)
          ? body.globalProductIds.filter((item): item is string => typeof item === "string")
          : [];
        for (const globalProductId of ids) {
          if (template.products.some((product) => product.globalProductId === globalProductId)) {
            continue;
          }
          const source = availableProducts.find((product) => product.id === globalProductId);
          template.products.push({
            id: crypto.randomUUID(),
            globalProductId,
            sortOrder: template.products.length + 1,
            isFeatured: Boolean(body?.isFeatured),
            isFirstBatch: Boolean(body?.isFirstBatch),
            productName: source?.name,
            sku: source?.sku,
            brand: source?.brand,
          });
        }
        touchTemplate(template);
        return jsonResponse(200, template);
      }

      if (path.endsWith("/products/bulk-remove") && method === "POST") {
        const body = parseBody(init);
        const ids = Array.isArray(body?.globalProductIds)
          ? body.globalProductIds.filter((item): item is string => typeof item === "string")
          : [];
        template.products = template.products.filter(
          (product) => !ids.includes(product.globalProductId),
        );
        template.products.forEach((product, index) => {
          product.sortOrder = index + 1;
        });
        touchTemplate(template);
        return jsonResponse(200, template);
      }

      if (path.endsWith("/products/order") && method === "PUT") {
        const body = parseBody(init);
        const ordered = Array.isArray(body?.orderedGlobalProductIds)
          ? body.orderedGlobalProductIds.filter((item): item is string => typeof item === "string")
          : [];
        template.products.sort(
          (left, right) =>
            ordered.indexOf(left.globalProductId) - ordered.indexOf(right.globalProductId),
        );
        template.products.forEach((product, index) => {
          product.sortOrder = index + 1;
        });
        touchTemplate(template);
        return jsonResponse(200, template);
      }

      if (productMatch[2] && method === "PATCH") {
        const product = template.products.find((item) => item.globalProductId === productMatch[2]);
        if (!product) {
          return jsonResponse(404, { title: "Not Found", status: 404 });
        }
        const body = parseBody(init);
        if (typeof body?.isFeatured === "boolean") {
          product.isFeatured = body.isFeatured;
        }
        if (typeof body?.isFirstBatch === "boolean") {
          product.isFirstBatch = body.isFirstBatch;
        }
        touchTemplate(template);
        return jsonResponse(200, template);
      }

      if (productMatch[2] && method === "DELETE") {
        template.products = template.products.filter(
          (product) => product.globalProductId !== productMatch[2],
        );
        template.products.forEach((product, index) => {
          product.sortOrder = index + 1;
        });
        touchTemplate(template);
        return jsonResponse(200, template);
      }

      if (path.endsWith("/products") && method === "POST") {
        const body = parseBody(init);
        const globalProductId = String(body?.globalProductId ?? "");
        if (template.products.some((product) => product.globalProductId === globalProductId)) {
          return jsonResponse(409, {
            title: "Conflict",
            status: 409,
            detail: "Product is already assigned to this template.",
          });
        }
        const source = availableProducts.find((product) => product.id === globalProductId);
        template.products.push({
          id: crypto.randomUUID(),
          globalProductId,
          sortOrder: template.products.length + 1,
          isFeatured: Boolean(body?.isFeatured),
          isFirstBatch: Boolean(body?.isFirstBatch),
          productName: source?.name,
          sku: source?.sku,
          brand: source?.brand,
        });
        touchTemplate(template);
        return jsonResponse(200, template);
      }
    }

    return innerMock(input, init);
  });

  vi.stubGlobal("fetch", fetchMock);

  return {
    fetchMock,
    mutationHeaders,
    getTemplates: () => templates,
  };
}

export const GLOBAL_CATALOG_TEMPLATE_FIXTURE_IDS = {
  draftId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  publishedId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
  archivedId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
  businessTypeId: BUSINESS_TYPE_ID,
};
