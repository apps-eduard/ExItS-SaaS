import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { ErrorState } from "@/components/exits/ErrorState";
import { I18nProvider } from "@/i18n/I18nProvider";
import { PreferencesProvider } from "@/hooks/usePreferences";
import { normalizePosError } from "@/diagnostics/normalize-pos-error";
import { PlatformApiError } from "@/api/platform/platform-http";

describe("ErrorState diagnostics", () => {
  it("keeps friendly translated copy and exposes copyable diagnostics", async () => {
    const user = userEvent.setup();
    vi.spyOn(navigator.clipboard, "writeText").mockResolvedValue(undefined);

    render(
      <PreferencesProvider>
        <I18nProvider>
          <ErrorState
          title="Security check failed"
          detail="Please sign in again."
          diagnostic={normalizePosError({
            source: "workspace",
            error: new PlatformApiError(403, {
              errorCode: "application.auth.account_scope_denied",
              traceId: "trace-live-001",
            }),
            operation: "antiforgery bootstrap",
            httpMethod: "GET",
            path: "/api/v1/platform/antiforgery/token",
            screen: "Choose workspace",
            friendlyMessage: "Please sign in again.",
          })}
          />
        </I18nProvider>
      </PreferencesProvider>,
    );

    expect(screen.getByText("Security check failed")).toBeInTheDocument();
    expect(screen.getByText("Please sign in again.")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Copy error details" })).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Technical details" }));
    const details = screen.getByTestId("technical-error-details") as HTMLTextAreaElement;
    expect(details.value).toContain("application.auth.account_scope_denied");
    expect(details.value).toContain("trace-live-001");
  });
});
