import { Component, type ErrorInfo, type ReactNode } from "react";
import { ClientErrorPanel } from "@/diagnostics/ClientErrorPanel";
import { normalizeReactClientError } from "@/diagnostics/normalize-pos-error";
import type { PosErrorReportInput } from "@/diagnostics/pos-error-report";

type Props = { children: ReactNode };
type State = {
  report: PosErrorReportInput | null;
};

export class GlobalErrorBoundary extends Component<Props, State> {
  state: State = { report: null };

  private recoverAfterHotUpdate = () => {
    if (this.state.report) {
      this.setState({ report: null });
    }
  };

  componentDidMount() {
    const hot = import.meta.hot;
    if (hot && typeof hot.on === "function") {
      hot.on("vite:afterUpdate", this.recoverAfterHotUpdate);
    }
  }

  componentWillUnmount() {
    const hot = import.meta.hot;
    if (hot && typeof hot.off === "function") {
      hot.off("vite:afterUpdate", this.recoverAfterHotUpdate);
    }
  }

  static getDerivedStateFromError(error: Error): State {
    return {
      report: normalizeReactClientError({
        source: "react-error-boundary",
        error,
      }),
    };
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    this.setState({
      report: normalizeReactClientError({
        source: "react-error-boundary",
        error,
        componentStack: info.componentStack,
      }),
    });
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
