import { PlatformApiError, platformRequest, type PlatformRequestOptions } from "@/api/platform-http";

const inFlightMutations = new Map<string, Promise<unknown>>();

function mutationDedupeKey(method: string, path: string, body: unknown): string {
  return `${method}:${path}:${body === undefined ? "" : JSON.stringify(body)}`;
}

/**
 * Commercial mutations reuse PWEB-20 `platformRequest` (cookie credentials + antiforgery).
 * Concurrent identical method/path/body calls share one in-flight promise so a double-submit
 * does not issue a second HTTP mutation. After settle, a later retry is a new request.
 */
export async function commercialMutationRequest<T>(
  baseUrl: string,
  options: Omit<PlatformRequestOptions, "method"> & {
    method: "POST" | "PUT" | "PATCH" | "DELETE";
  },
): Promise<T> {
  const key = mutationDedupeKey(options.method, options.path, options.body);
  const existing = inFlightMutations.get(key);
  if (existing) {
    return existing as Promise<T>;
  }

  const pending = platformRequest<T>(baseUrl, options).finally(() => {
    inFlightMutations.delete(key);
  });
  inFlightMutations.set(key, pending);
  return pending;
}

export function resetCommercialMutationInFlight(): void {
  inFlightMutations.clear();
}

export function asPlatformApiError(error: unknown): PlatformApiError | null {
  return error instanceof PlatformApiError ? error : null;
}
