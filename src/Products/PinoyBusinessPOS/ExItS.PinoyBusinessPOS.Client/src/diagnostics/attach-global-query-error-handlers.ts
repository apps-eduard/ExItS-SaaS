import type { QueryClient } from "@tanstack/react-query";
import {
  isAbortError,
  reportGlobalClientError,
} from "@/diagnostics/global-error-reporter";

type QueryMeta = {
  suppressGlobalError?: boolean;
  reportGlobalError?: boolean;
  operation?: string;
};

function readMeta(meta: Record<string, unknown> | undefined): QueryMeta {
  return (meta ?? {}) as QueryMeta;
}

/**
 * Reports uncaught React Query failures to the global copyable error overlay.
 * Set query/mutation meta.suppressGlobalError when the screen handles the error inline.
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
    if (meta.suppressGlobalError) {
      return;
    }

    const error = query.state.error;
    if (isAbortError(error)) {
      return;
    }

    const isInitialFailure = query.state.data === undefined && query.state.errorUpdateCount <= 1;
    if (!meta.reportGlobalError && !isInitialFailure) {
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
    if (isAbortError(error)) {
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
