import { Component, type ErrorInfo, type ReactNode } from "react";
import { ErrorState } from "@/components/exits/ErrorState";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";
import type { DiagnosticRecord } from "@/lib/diagnostics/diagnostic-types";

type AppErrorBoundaryProps = {
  children: ReactNode;
};

type AppErrorBoundaryState = {
  diagnostic: DiagnosticRecord | null;
};

export class AppErrorBoundary extends Component<AppErrorBoundaryProps, AppErrorBoundaryState> {
  public override state: AppErrorBoundaryState = { diagnostic: null };

  public static getDerivedStateFromError(error: Error): AppErrorBoundaryState {
    return {
      diagnostic: normalizeDiagnosticError({
        error,
        category: "REACT_RENDER_ERROR",
        operation: "Render application",
      }),
    };
  }

  public override componentDidCatch(error: Error, info: ErrorInfo): void {
    this.setState((current) => {
      if (!current.diagnostic) {
        return current;
      }
      return {
        diagnostic: normalizeDiagnosticError({
          error,
          category: "REACT_RENDER_ERROR",
          operation: "Render application",
          componentStack: info.componentStack ?? undefined,
          environment: {
            createReference: () => current.diagnostic?.errorReference ?? "ERR-000000",
            now: () => current.diagnostic?.timestampUtc ?? new Date().toISOString(),
            pathname: current.diagnostic.pagePath,
          },
        }),
      };
    });
  }

  public override render(): ReactNode {
    if (this.state.diagnostic) {
      return (
        <main className="flex min-h-dvh items-center justify-center bg-background p-[var(--exits-page-padding)]">
          <ErrorState
            diagnostic={this.state.diagnostic}
            headingLevel="h1"
            onReload={() => window.location.reload()}
            onRetry={() => this.setState({ diagnostic: null })}
          />
        </main>
      );
    }

    return this.props.children;
  }
}
