import { z } from "zod";
import { PlatformApiError, platformRequest } from "@/api/platform/platform-http";
import {
  EMPTY_CUSTOMER_LIST_CONNECTION_OVERLAY,
  normalizeCustomerLinkId,
  type CustomerListConnectionOverlay,
} from "@/features/customers/customer-list-connection";

const guidSchema = z
  .string()
  .regex(/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/);

const PAGE_SIZE = 100;
const MAX_PAGES = 10;

function pick(raw: Record<string, unknown>, camel: string, pascal: string): unknown {
  return raw[camel] ?? raw[pascal];
}

const pagedEnvelopeSchema = z.object({
  items: z.array(z.unknown()),
  totalCount: z.number(),
  page: z.number(),
  pageSize: z.number(),
});

const linkedUserSchema = z.object({
  businessCustomerId: guidSchema,
  status: z.string(),
});

const linkRequestSchema = z.object({
  businessCustomerId: guidSchema,
  status: z.string(),
});

function normalizePaged(raw: unknown): unknown {
  if (!raw || typeof raw !== "object") {
    return raw;
  }
  const r = raw as Record<string, unknown>;
  return {
    items: r.items ?? r.Items ?? [],
    totalCount: r.totalCount ?? r.TotalCount ?? 0,
    page: r.page ?? r.Page ?? 1,
    pageSize: r.pageSize ?? r.PageSize ?? PAGE_SIZE,
  };
}

async function listAllPages(
  pathForPage: (page: number) => string,
  signal?: AbortSignal,
): Promise<unknown[]> {
  const items: unknown[] = [];
  let page = 1;
  let totalCount = Number.POSITIVE_INFINITY;

  while (page <= MAX_PAGES && items.length < totalCount) {
    const raw = await platformRequest<unknown>({
      path: pathForPage(page),
      signal,
    });
    const parsed = pagedEnvelopeSchema.parse(normalizePaged(raw));
    totalCount = parsed.totalCount;
    items.push(...parsed.items);
    if (parsed.items.length === 0) {
      break;
    }
    page += 1;
  }

  return items;
}

function isAuthDenied(error: unknown): boolean {
  return error instanceof PlatformApiError && (error.status === 401 || error.status === 403);
}

export function overlayFromLinkPages(input: {
  linkedUsers: unknown[];
  pendingRequests: unknown[];
}): CustomerListConnectionOverlay {
  const connected = new Set<string>();
  for (const raw of input.linkedUsers) {
    if (!raw || typeof raw !== "object") {
      continue;
    }
    const r = raw as Record<string, unknown>;
    const parsed = linkedUserSchema.safeParse({
      businessCustomerId: pick(r, "businessCustomerId", "BusinessCustomerId"),
      status: pick(r, "status", "Status"),
    });
    if (!parsed.success) {
      continue;
    }
    const status = parsed.data.status.trim().toLowerCase();
    if (status !== "active" && status !== "linked") {
      continue;
    }
    const id = normalizeCustomerLinkId(parsed.data.businessCustomerId);
    if (id) {
      connected.add(id);
    }
  }

  const pending = new Set<string>();
  for (const raw of input.pendingRequests) {
    if (!raw || typeof raw !== "object") {
      continue;
    }
    const r = raw as Record<string, unknown>;
    const parsed = linkRequestSchema.safeParse({
      businessCustomerId: pick(r, "businessCustomerId", "BusinessCustomerId"),
      status: pick(r, "status", "Status"),
    });
    if (!parsed.success) {
      continue;
    }
    if (parsed.data.status.trim().toLowerCase() !== "pending") {
      continue;
    }
    const id = normalizeCustomerLinkId(parsed.data.businessCustomerId);
    if (id && !connected.has(id)) {
      pending.add(id);
    }
  }

  return {
    connectedBusinessCustomerIds: connected,
    pendingBusinessCustomerIds: pending,
    loaded: true,
  };
}

/**
 * Two org-wide Platform reads (not N+1). 401/403 and transport failures return an
 * unloaded/empty overlay so the customer list still renders ExItS ID vs local.
 */
export async function loadOrganizationCustomerLinkOverlay(
  organizationId: string,
  signal?: AbortSignal,
): Promise<CustomerListConnectionOverlay> {
  const org = organizationId.trim();
  if (!org) {
    return EMPTY_CUSTOMER_LIST_CONNECTION_OVERLAY;
  }

  try {
    const [linkedUsers, pendingRequests] = await Promise.all([
      listAllPages(
        (page) =>
          `/api/v1/organizations/${org}/linked-customer-app-users?page=${page}&pageSize=${PAGE_SIZE}`,
        signal,
      ),
      listAllPages(
        (page) =>
          `/api/v1/organizations/${org}/customer-link-requests?status=Pending&page=${page}&pageSize=${PAGE_SIZE}`,
        signal,
      ),
    ]);
    return overlayFromLinkPages({ linkedUsers, pendingRequests });
  } catch (error) {
    if (isAuthDenied(error)) {
      return {
        connectedBusinessCustomerIds: new Set(),
        pendingBusinessCustomerIds: new Set(),
        loaded: true,
      };
    }
    return EMPTY_CUSTOMER_LIST_CONNECTION_OVERLAY;
  }
}
