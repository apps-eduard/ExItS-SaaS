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
import { buildDiagnosticReport } from "@/lib/diagnostics/build-diagnostic-report";
import { UI_PREFERENCES_STORAGE_KEY } from "@/lib/preferences/ui-preferences";

vi.mock("@/lib/diagnostics/copy-diagnostic-text", () => ({
  copyDiagnosticReport: vi.fn(),
  copyDiagnosticText: vi.fn(),
}));

const diagnostic: DiagnosticRecord = {
  application: "ExItS Platform Admin Web",
  errorReference: "ERR-A7F3",
  timestamp: "2026-08-19T12:00:00.000Z",
  category: "API",
  message: "Unable to complete this operation.",
  route: "/admin",
  operation: "Load authorization",
  errorType: "PlatformApiError",
  httpStatus: 500,
  requestCorrelationId: "7f9c2f2e-aaaa-bbbb-cccc-ddddeeeeffff",
  locale: "en",
  theme: "system",
  density: "balanced",
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

  it("shows a compact error, copies diagnostics, and invokes retry/close", async () => {
    vi.mocked(copyDiagnosticReport).mockResolvedValue(true);
    const user = userEvent.setup();
    const onRetry = vi.fn();
    const onClose = vi.fn();
    renderWithPreferences(
      <ErrorState diagnostic={diagnostic} onRetry={onRetry} onClose={onClose} />,
    );

    expect(screen.getByRole("alert")).toBeInTheDocument();
    expect(screen.getByText("Something went wrong")).toBeInTheDocument();
    expect(screen.getByText(/ERR-A7F3/)).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Copy diagnostics" }));
    await screen.findByText("Copied");
    expect(copyDiagnosticReport).toHaveBeenCalledWith(diagnostic);
    await user.click(screen.getByRole("button", { name: "Retry" }));
    await user.click(screen.getByRole("button", { name: "Close" }));
    expect(onRetry).toHaveBeenCalledOnce();
    expect(onClose).toHaveBeenCalledOnce();
  });

  it("shows copy failure when clipboard is unavailable", async () => {
    vi.mocked(copyDiagnosticReport).mockResolvedValue(false);
    const user = userEvent.setup();
    renderWithPreferences(<ErrorState diagnostic={diagnostic} />);
    await user.click(screen.getByRole("button", { name: "Copy diagnostics" }));
    expect(await screen.findByText("Unable to copy diagnostics.")).toBeInTheDocument();
  });

  it("is keyboard accessible and localizes to Filipino", async () => {
    const user = userEvent.setup();
    window.localStorage.setItem(
      UI_PREFERENCES_STORAGE_KEY,
      JSON.stringify({
        theme: "dark",
        language: "fil-PH",
        density: "compact",
        sidebarCollapsed: false,
      }),
    );
    renderWithPreferences(
      <ErrorState diagnostic={diagnostic} onRetry={() => undefined} onClose={() => undefined} />,
    );
    expect(document.documentElement.dataset.theme).toBe("dark");
    expect(document.documentElement.dataset.density).toBe("compact");
    expect(screen.getByText("May naganap na problema")).toBeInTheDocument();
    screen.getByRole("button", { name: "Kopyahin ang diagnostics" }).focus();
    expect(screen.getByRole("button", { name: "Kopyahin ang diagnostics" })).toHaveFocus();
    await user.tab();
    expect(screen.getByRole("button", { name: "Subukan ulit" })).toHaveFocus();
  });
});

function Boom({ secret }: { secret: string }) {
  throw new Error(secret);
  return null;
}

describe("AppErrorBoundary", () => {
  it("catches render failures without leaking secrets or going blank", async () => {
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
    await user.click(screen.getByRole("button", { name: "Copy diagnostics" }));
    await screen.findByText("Copied");
    expect(copyDiagnosticReport).toHaveBeenCalled();
    const record = vi.mocked(copyDiagnosticReport).mock.calls.at(-1)?.[0];
    expect(record).toBeDefined();
    const copied = buildDiagnosticReport(record!);
    expect(copied).toContain("RENDER");
    expect(copied).not.toContain("SUPER_SECRET_PASSWORD_123");
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

function ReportAbort() {
  const { report } = useDiagnostics();
  return (
    <button type="button" onClick={() => report(new DOMException("Aborted", "AbortError"))}>
      Abort
    </button>
  );
}

describe("DiagnosticsProvider", () => {
  it("keeps a single global notice and ignores abort errors", async () => {
    const user = userEvent.setup();
    renderWithPreferences(
      <DiagnosticsProvider>
        <ReportTwice />
        <ReportAbort />
      </DiagnosticsProvider>,
    );

    await user.click(screen.getByRole("button", { name: "Trigger" }));
    expect(screen.getAllByRole("alert")).toHaveLength(1);
    expect(screen.getByText("Something went wrong")).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Abort" }));
    expect(screen.getAllByRole("alert")).toHaveLength(1);

    await user.click(screen.getByRole("button", { name: "Close" }));
    await waitFor(() => {
      expect(screen.queryByRole("alert")).not.toBeInTheDocument();
    });
  });
});
