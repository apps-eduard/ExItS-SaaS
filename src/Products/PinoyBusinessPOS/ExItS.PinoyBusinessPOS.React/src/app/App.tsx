import { RouterProvider } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import { router } from "@/app/router";
import { GlobalErrorBoundary } from "@/diagnostics/GlobalErrorBoundary";
import { GlobalRuntimeErrorHost } from "@/diagnostics/GlobalRuntimeErrorHost";

export function App() {
  return (
    <GlobalErrorBoundary>
      <AppProviders>
        <GlobalRuntimeErrorHost>
          <RouterProvider router={router} />
        </GlobalRuntimeErrorHost>
      </AppProviders>
    </GlobalErrorBoundary>
  );
}
