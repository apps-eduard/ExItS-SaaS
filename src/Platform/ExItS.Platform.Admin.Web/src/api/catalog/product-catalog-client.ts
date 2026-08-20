import { parsePagedResult, type PagedResult } from "@/api/platform/paged-result";
import { platformRequest } from "@/api/platform-http";
import { withQuery } from "@/lib/http/query-string";

export type CatalogProduct = {
  id: string;
  code: string;
  displayName: string;
  status: string;
};

function asRecord(value: unknown): Record<string, unknown> | null {
  return typeof value === "object" && value !== null ? (value as Record<string, unknown>) : null;
}

function readString(record: Record<string, unknown>, ...keys: string[]): string | undefined {
  for (const key of keys) {
    const value = record[key];
    if (typeof value === "string" && value.trim().length > 0) {
      return value;
    }
  }
  return undefined;
}

export function mapCatalogProduct(payload: unknown): CatalogProduct | null {
  const record = asRecord(payload);
  if (!record) {
    return null;
  }
  const id = readString(record, "id", "Id");
  const code = readString(record, "code", "Code");
  const displayName = readString(record, "displayName", "DisplayName") ?? code;
  const status = readString(record, "status", "Status") ?? "Active";
  if (!id || !code) {
    return null;
  }
  return { id, code, displayName: displayName ?? code, status };
}

export function listCatalogProducts(
  baseUrl: string,
  options?: { pageSize?: number; status?: string; signal?: AbortSignal },
): Promise<PagedResult<CatalogProduct>> {
  const path = withQuery("/api/v1/platform/catalog/products", {
    page: 1,
    pageSize: options?.pageSize ?? 100,
    status: options?.status ?? "Active",
    sortBy: "DisplayName",
    sortDesc: false,
  });
  return platformRequest<unknown>(baseUrl, { path, signal: options?.signal }).then((payload) => {
    const page = parsePagedResult<unknown>(payload);
    return {
      ...page,
      items: page.items.flatMap((item) => {
        const mapped = mapCatalogProduct(item);
        return mapped ? [mapped] : [];
      }),
    };
  });
}
