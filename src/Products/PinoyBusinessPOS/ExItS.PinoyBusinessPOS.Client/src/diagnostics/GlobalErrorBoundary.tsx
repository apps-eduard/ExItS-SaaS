import { Component, type ErrorInfo, type ReactNode } from "react";
import { ClientErrorPanel } from "@/diagnostics/ClientErrorPanel";
import type { ClientErrorReportInput } from "@/diagnostics/client-error-report";
import { safeDiagnosticLocation } from "@/diagnostics/diagnostic-redaction";

type Props = { children: ReactNode };
type State = {
  report: ClientErrorReportInput | null;
};

function captureLocation(): Pick<ClientErrorReportInput, "url" | "pathname"> {
  if (typeof window === "undefined") {
    return {};
  }
  const location = safeDiagnosticLocation(window.location.href, window.location.pathname);
  return { url: location.url, pathname: location.pathname };
}

export class GlobalErrorBoundary extends Component<Props, State> {
  state: State = { report: null };

  static getDerivedStateFromError(error: Error): State {
    const location =
      typeof window !== "undefined"
        ? safeDiagnosticLocation(window.location.href, window.location.pathname)
        : { url: undefined, pathname: undefined };
    return {
      report: {
        source: "react-error-boundary",
        error,
        occurredAt: new Date().toISOString(),
        url: location.url,
        pathname: location.pathname,
        mode: import.meta.env.MODE,
      },
    };
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    this.setState((prev) => ({
      report: {
        ...(prev.report ?? {
          source: "react-error-boundary",
          error,
          occurredAt: new Date().toISOString(),
        }),
        error,
        componentStack: info.componentStack,
        ...captureLocation(),
        mode: import.meta.env.MODE,
      },
    }));
    console.error("[ExItS] React error boundary", error, info.componentStack);
  }

  render() {
    if (this.state.report) {
      return (
        <div className="flex min-h-dvh min-w-0 items-start justify-center bg-background p-4">
          <ClientErrorPanel input={this.state.report} onReload={() => window.location.reload()} />
        </div>
      );
    }
    return this.props.children;
  }
}
