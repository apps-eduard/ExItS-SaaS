import { Component, type ErrorInfo, type ReactNode } from "react";
import { ClientErrorPanel } from "@/diagnostics/ClientErrorPanel";
import type { ClientErrorReportInput } from "@/diagnostics/client-error-report";

type Props = { children: ReactNode };
type State = {
  report: ClientErrorReportInput | null;
};

export class GlobalErrorBoundary extends Component<Props, State> {
  state: State = { report: null };

  static getDerivedStateFromError(error: Error): State {
    return {
      report: {
        source: "react-error-boundary",
        error,
        occurredAt: new Date().toISOString(),
        url: typeof window !== "undefined" ? window.location.href : undefined,
        pathname: typeof window !== "undefined" ? window.location.pathname : undefined,
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
        url: typeof window !== "undefined" ? window.location.href : undefined,
        pathname: typeof window !== "undefined" ? window.location.pathname : undefined,
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
