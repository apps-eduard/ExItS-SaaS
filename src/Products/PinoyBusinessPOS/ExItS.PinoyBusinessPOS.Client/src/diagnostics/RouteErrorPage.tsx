import { useRouteError, isRouteErrorResponse } from "react-router-dom";
import { ClientErrorPanel } from "@/diagnostics/ClientErrorPanel";

export function RouteErrorPage() {
  const routeError = useRouteError();
  const error =
    routeError instanceof Error
      ? routeError
      : isRouteErrorResponse(routeError)
        ? new Error(`${routeError.status} ${routeError.statusText}: ${routeError.data ?? ""}`)
        : new Error(String(routeError));

  return (
    <div className="flex min-h-dvh min-w-0 items-start justify-center bg-background p-4">
      <ClientErrorPanel
        input={{
          source: "react-error-boundary",
          error,
          occurredAt: new Date().toISOString(),
          url: typeof window !== "undefined" ? window.location.href : undefined,
          pathname: typeof window !== "undefined" ? window.location.pathname : undefined,
          mode: import.meta.env.MODE,
        }}
        onReload={() => window.location.reload()}
      />
    </div>
  );
}
