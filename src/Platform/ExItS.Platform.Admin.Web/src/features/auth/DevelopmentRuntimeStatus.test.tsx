import { render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { DevelopmentRuntimeStatus } from "@/features/auth/DevelopmentRuntimeStatus";
import { PreferencesProvider } from "@/hooks/use-preferences";
import { areTestUserToolsPermitted } from "@/lib/auth/development-tools";

vi.mock("@/lib/auth/development-tools", () => ({
  areTestUserToolsPermitted: vi.fn(),
  areDevelopmentToolsAllowed: vi.fn(),
}));

describe("DevelopmentRuntimeStatus", () => {
  afterEach(() => {
    vi.clearAllMocks();
    delete window.__EXITS_PLATFORM_ADMIN_WEB__;
  });

  it("hides runtime diagnostics in production when tools are not permitted", () => {
    vi.mocked(areTestUserToolsPermitted).mockReturnValue(false);
    render(
      <PreferencesProvider>
        <DevelopmentRuntimeStatus />
      </PreferencesProvider>,
    );
    expect(screen.queryByTestId("dev-runtime-status")).not.toBeInTheDocument();
  });

  it("shows app, mode, API, and Local Validation status without secrets", () => {
    window.__EXITS_PLATFORM_ADMIN_WEB__ = {
      localValidationToolsEnabled: true,
      platformApiSameOrigin: true,
      buildSha: "deadbeef",
    };
    vi.mocked(areTestUserToolsPermitted).mockReturnValue(true);
    render(
      <PreferencesProvider>
        <DevelopmentRuntimeStatus />
      </PreferencesProvider>,
    );
    const panel = screen.getByTestId("dev-runtime-status");
    expect(panel).toHaveTextContent("Platform Admin React");
    expect(panel).toHaveTextContent("Frontend mode: test");
    expect(panel).toHaveTextContent("Build: deadbeef");
    expect(panel).toHaveTextContent("API base URL: (same-origin)");
    expect(panel).toHaveTextContent("Local Validation: Enabled");
    expect(panel.textContent).not.toMatch(/password|secret|token/i);
  });
});
