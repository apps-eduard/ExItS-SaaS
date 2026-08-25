import { useRouteError, isRouteErrorResponse } from "react-router-dom";
import { ClientErrorPanel } from "@/diagnostics/ClientErrorPanel";
import { normalizeReactClientError } from "@/diagnostics/normalize-pos-error";

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
      ? window.location.pathname
      : undefined;

  return (
    <div className="flex min-h-dvh min-w-0 items-start justify-center bg-background p-4">
      <ClientErrorPanel
        input={normalizeReactClientError({
          source: "react-error-boundary",
          error,
          pathname: location,
        })}
        onReload={() => window.location.reload()}
      />
    </div>
  );
}
