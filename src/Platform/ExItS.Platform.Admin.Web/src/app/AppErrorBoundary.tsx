import { Component, type ErrorInfo, type ReactNode } from "react";

type AppErrorBoundaryProps = {
  children: ReactNode;
};

type AppErrorBoundaryState = {
  hasError: boolean;
};

export class AppErrorBoundary extends Component<AppErrorBoundaryProps, AppErrorBoundaryState> {
  public override state: AppErrorBoundaryState = { hasError: false };

  public static getDerivedStateFromError(): AppErrorBoundaryState {
    return { hasError: true };
  }

  public override componentDidCatch(error: Error, info: ErrorInfo): void {
    console.error("Platform Admin Web failed to render.", error.name, info.componentStack);
  }

  public override render(): ReactNode {
    if (this.state.hasError) {
      return (
        <main>
          <h1>Something went wrong</h1>
          <p>The application could not continue. Refresh and try again.</p>
        </main>
      );
    }

    return this.props.children;
  }
}
