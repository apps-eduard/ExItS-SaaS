import type { ReactNode } from "react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { ErrorState } from "@/components/exits/ErrorState";
import { AppErrorBoundary } from "@/app/AppErrorBoundary";
import { DiagnosticsProvider, useDiagnostics } from "@/hooks/use-diagnostics";
import { PreferencesProvider } from "@/hooks/use-preferences";
import type { DiagnosticRecord } from "@/lib/diagnostics/diagnostic-types";
import { copyDiagnosticReport } from "@/lib/diagnostics/copy-diagnostic-text";
import { formatDiagnosticForClipboard } from "@/lib/diagnostics/build-diagnostic-report";
import { UI_PREFERENCES_STORAGE_KEY } from "@/lib/preferences/ui-preferences";

vi.mock("@/lib/diagnostics/copy-diagnostic-text", async () => {
  const actual = await vi.importActual<typeof import("@/lib/diagnostics/copy-diagnostic-text")>(
    "@/lib/diagnostics/copy-diagnostic-text",
  );
  return {
    ...actual,
    copyDiagnosticReport: vi.fn(),
  };
});

const diagnostic: DiagnosticRecord = {
  application: "Platform Admin React",
  errorReference: "ERR-8F32A1",
  timestampUtc: "2026-08-22T08:30:00.000Z",
  buildSha: "19119089",
  environment: "Development",
  pagePath: "/admin",
  operation: "Load authorization",
  category: "SERVER_ERROR",
  userMessage: "Unable to complete this request.",
  httpMethod: "GET",
  apiPath: "/api/v1/platform/authorization/me",
  httpStatus: 500,
  httpStatusLabel: "500",
  errorCode: "platform.unhandled_error",
  correlationId: "7f9c2f2e-aaaa-bbbb-cccc-ddddeeeeffff",
  traceId: "00-server-trace",
  retryable: true,
  errorType: "PlatformApiError",
};

function renderWithPreferences(ui: ReactNode) {
  return render(<PreferencesProvider>{ui}</PreferencesProvider>);
}

describe("ErrorState", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
    window.localStorage.removeItem(UI_PREFERENCES_STORAGE_KEY);
  });

  it("shows a friendly message, reference, copy action, and retry", async () => {
    vi.mocked(copyDiagnosticReport).mockResolvedValue(true);
    const user = userEvent.setup();
    const onRetry = vi.fn();
    renderWithPreferences(<ErrorState diagnostic={diagnostic} onRetry={onRetry} />);

    expect(screen.getByRole("alert")).toBeInTheDocument();
    expect(screen.getByText("Something went wrong")).toBeInTheDocument();
    expect(screen.getByText(/ERR-8F32A1/)).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Copy error details" }));
    await screen.findByText("Error details copied.");
    expect(copyDiagnosticReport).toHaveBeenCalledWith(diagnostic);
    await user.click(screen.getByRole("button", { name: "Retry" }));
    expect(onRetry).toHaveBeenCalledOnce();
  });

  it("shows clipboard fallback when copy fails", async () => {
    vi.mocked(copyDiagnosticReport).mockResolvedValue(false);
    const user = userEvent.setup();
    renderWithPreferences(<ErrorState diagnostic={diagnostic} />);
    await user.click(screen.getByRole("button", { name: "Copy error details" }));
    expect(await screen.findByText("Unable to copy error details.")).toBeInTheDocument();
    const fallback = screen.getByRole("textbox", { name: "Select the report below and copy manually." });
    expect(fallback).toHaveValue(formatDiagnosticForClipboard(diagnostic));
  });
});

function Boom({ secret }: { secret: string }) {
  throw new Error(secret);
  return null;
}

describe("AppErrorBoundary", () => {
  it("catches render failures without leaking secrets", async () => {
    vi.mocked(copyDiagnosticReport).mockReset();
    vi.mocked(copyDiagnosticReport).mockResolvedValue(true);
    const user = userEvent.setup();
    vi.spyOn(console, "error").mockImplementation(() => undefined);

    renderWithPreferences(
      <AppErrorBoundary>
        <Boom secret="SUPER_SECRET_PASSWORD_123" />
      </AppErrorBoundary>,
    );

    expect(screen.getByRole("heading", { name: "Something went wrong" })).toBeInTheDocument();
    expect(screen.getByText(/ERR-/)).toBeInTheDocument();
    expect(document.body.textContent).not.toContain("SUPER_SECRET_PASSWORD_123");
    await user.click(screen.getByRole("button", { name: "Copy error details" }));
    await screen.findByText("Error details copied.");
    const record = vi.mocked(copyDiagnosticReport).mock.calls.at(-1)?.[0];
    expect(record?.category).toBe("REACT_RENDER_ERROR");
  });
});

function ReportTwice() {
  const { report } = useDiagnostics();
  return (
    <button
      type="button"
      onClick={() => {
        report(new Error("first"), { operation: "One" });
        report(new Error("second"), { operation: "Two" });
      }}
    >
      Trigger
    </button>
  );
}

describe("DiagnosticsProvider", () => {
  it("keeps a single global notice", async () => {
    const user = userEvent.setup();
    renderWithPreferences(
      <DiagnosticsProvider>
        <ReportTwice />
      </DiagnosticsProvider>,
    );

    await user.click(screen.getByRole("button", { name: "Trigger" }));
    expect(screen.getAllByRole("alert")).toHaveLength(1);
    await user.click(screen.getByRole("button", { name: "Close" }));
    await waitFor(() => {
      expect(screen.queryByRole("alert")).not.toBeInTheDocument();
    });
  });
});
