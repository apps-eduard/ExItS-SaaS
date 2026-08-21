import { useRouteError, isRouteErrorResponse } from "react-router-dom";
import { ClientErrorPanel } from "@/diagnostics/ClientErrorPanel";
import { safeDiagnosticLocation } from "@/diagnostics/diagnostic-redaction";

export function RouteErrorPage() {
  const routeError = useRouteError();
  const error =
    routeError instanceof Error
      ? routeError
      : isRouteErrorResponse(routeError)
        ? new Error(`${routeError.status} ${routeError.statusText}`)
        : new Error("Route error (details omitted for privacy)");

  const location =
    typeof window !== "undefined"
      ? safeDiagnosticLocation(window.location.href, window.location.pathname)
      : { url: undefined, pathname: undefined };

  return (
    <div className="flex min-h-dvh min-w-0 items-start justify-center bg-background p-4">
      <ClientErrorPanel
        input={{
          source: "react-error-boundary",
          error,
          occurredAt: new Date().toISOString(),
          url: location.url,
          pathname: location.pathname,
          mode: import.meta.env.MODE,
        }}
        onReload={() => window.location.reload()}
      />
    </div>
  );
}
