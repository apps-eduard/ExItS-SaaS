import type { QueryClient } from "@tanstack/react-query";
import {
  isAbortError,
  reportGlobalClientError,
} from "@/diagnostics/global-error-reporter";
import { isAuthenticationLostError } from "@/session/session-expiry";

type QueryMeta = {
  suppressGlobalError?: boolean;
  reportGlobalError?: boolean;
  operation?: string;
};

function readMeta(meta: Record<string, unknown> | undefined): QueryMeta {
  return (meta ?? {}) as QueryMeta;
}

/**
 * Reports selected React Query failures to the global copyable error overlay.
 *
 * Default: do **not** escalate query failures. List/detail screens render ErrorState
 * inline; a full-screen overlay was trapping bottom-nav clicks and felt like a freeze
 * (especially after a few navigations hit an unmocked/404 endpoint).
 *
 * Opt in with meta.reportGlobalError when a screen has no inline error UI.
 * Set meta.suppressGlobalError to force silence even when reportGlobalError is set.
 */
export function attachGlobalQueryErrorHandlers(queryClient: QueryClient): void {
  queryClient.getQueryCache().subscribe((event) => {
    if (event.type !== "updated") {
      return;
    }

    const query = event.query;
    if (query.state.status !== "error" || !query.state.error) {
      return;
    }

    const meta = readMeta(query.meta as Record<string, unknown> | undefined);
    if (meta.suppressGlobalError || !meta.reportGlobalError) {
      return;
    }

    const error = query.state.error;
    if (isAbortError(error) || isAuthenticationLostError(error)) {
      return;
    }

    reportGlobalClientError({
      error,
      source: "network",
      operation: meta.operation ?? `query ${JSON.stringify(query.queryKey)}`,
      friendlyMessage:
        error instanceof Error ? error.message : "A network or server request failed.",
      pathname: typeof window !== "undefined" ? window.location.pathname : undefined,
    });
  });

  queryClient.getMutationCache().subscribe((event) => {
    if (event.type !== "updated") {
      return;
    }

    const mutation = event.mutation;
    if (mutation.state.status !== "error" || !mutation.state.error) {
      return;
    }

    const meta = readMeta(mutation.meta as Record<string, unknown> | undefined);
    if (meta.suppressGlobalError) {
      return;
    }

    const error = mutation.state.error;
    if (isAbortError(error) || isAuthenticationLostError(error)) {
      return;
    }

    if (!meta.reportGlobalError) {
      return;
    }

    reportGlobalClientError({
      error,
      source: "network",
      operation: meta.operation ?? `mutation ${JSON.stringify(mutation.options.mutationKey ?? [])}`,
      friendlyMessage:
        error instanceof Error ? error.message : "An action failed due to a network or server error.",
      pathname: typeof window !== "undefined" ? window.location.pathname : undefined,
    });
  });
}
