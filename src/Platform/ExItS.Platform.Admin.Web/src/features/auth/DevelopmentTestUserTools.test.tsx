import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it, vi } from "vitest";
import { getLocalValidationEnabled, listQuickLoginIdentities } from "@/api/auth/auth-client";
import { DevelopmentTestUserTools } from "@/features/auth/DevelopmentTestUserTools";
import { PreferencesProvider } from "@/hooks/use-preferences";
import { areTestUserToolsPermitted } from "@/lib/auth/development-tools";

vi.mock("@/api/auth/auth-client", () => ({
  getLocalValidationEnabled: vi.fn(),
  listQuickLoginIdentities: vi.fn(),
}));

vi.mock("@/lib/auth/development-tools", () => ({
  areTestUserToolsPermitted: vi.fn(),
  areDevelopmentToolsAllowed: vi.fn(),
}));

const olivia = {
  key: "olivia",
  username: "olivia",
  displayName: "Olivia Mendoza",
  email: "olivia.mendoza@exits.local",
  listLabel: "Olivia Mendoza",
};

function renderTools(onSelectLogin: (loginId: string) => void = () => undefined) {
  return render(
    <PreferencesProvider>
      <DevelopmentTestUserTools onSelectLogin={onSelectLogin} />
    </PreferencesProvider>,
  );
}

describe("DevelopmentTestUserTools", () => {
  afterEach(() => {
    vi.clearAllMocks();
    delete window.__EXITS_PLATFORM_ADMIN_WEB__;
  });

  it("renders nothing and makes no Local Validation API calls when the frontend gate is closed", () => {
    vi.mocked(areTestUserToolsPermitted).mockReturnValue(false);
    renderTools();

    expect(screen.queryByText("Development Tools")).not.toBeInTheDocument();
    expect(screen.queryByText("Local Validation")).not.toBeInTheDocument();
    expect(getLocalValidationEnabled).not.toHaveBeenCalled();
    expect(listQuickLoginIdentities).not.toHaveBeenCalled();
  });

  it("hides the selector when the backend Local Validation flag is false", async () => {
    vi.mocked(areTestUserToolsPermitted).mockReturnValue(true);
    vi.mocked(getLocalValidationEnabled).mockResolvedValue(false);
    renderTools();

    await waitFor(() => {
      expect(getLocalValidationEnabled).toHaveBeenCalledOnce();
    });
    expect(listQuickLoginIdentities).not.toHaveBeenCalled();
    expect(screen.queryByLabelText(/test user/i)).not.toBeInTheDocument();
  });

  it("hides the selector when the backend Local Validation request fails", async () => {
    vi.mocked(areTestUserToolsPermitted).mockReturnValue(true);
    vi.mocked(getLocalValidationEnabled).mockRejectedValue(new Error("unreachable"));
    renderTools();

    await waitFor(() => {
      expect(getLocalValidationEnabled).toHaveBeenCalledOnce();
    });
    expect(listQuickLoginIdentities).not.toHaveBeenCalled();
    expect(screen.queryByLabelText(/test user/i)).not.toBeInTheDocument();
  });

  it("shows the selector when frontend tools and backend Local Validation are both enabled", async () => {
    vi.mocked(areTestUserToolsPermitted).mockReturnValue(true);
    vi.mocked(getLocalValidationEnabled).mockResolvedValue(true);
    vi.mocked(listQuickLoginIdentities).mockResolvedValue([olivia]);

    renderTools();

    expect(await screen.findByLabelText("Test user")).toBeInTheDocument();
    expect(getLocalValidationEnabled).toHaveBeenCalledOnce();
    expect(listQuickLoginIdentities).toHaveBeenCalledOnce();
  });

  it("labels the area as Local Validation when the runtime flag is true", async () => {
    window.__EXITS_PLATFORM_ADMIN_WEB__ = { localValidationToolsEnabled: true };
    vi.mocked(areTestUserToolsPermitted).mockReturnValue(true);
    vi.mocked(getLocalValidationEnabled).mockResolvedValue(true);
    vi.mocked(listQuickLoginIdentities).mockResolvedValue([olivia]);

    renderTools();

    expect(await screen.findByText("Local Validation")).toBeInTheDocument();
    expect(screen.getByLabelText("Test User — Local Validation")).toBeInTheDocument();
    expect(screen.queryByText("Development Tools")).not.toBeInTheDocument();
  });

  it("selecting an identity reports the login id and does not include a password", async () => {
    vi.mocked(areTestUserToolsPermitted).mockReturnValue(true);
    vi.mocked(getLocalValidationEnabled).mockResolvedValue(true);
    vi.mocked(listQuickLoginIdentities).mockResolvedValue([olivia]);
    const onSelectLogin = vi.fn();
    const user = userEvent.setup();

    renderTools(onSelectLogin);
    await screen.findByLabelText("Test user");
    await user.selectOptions(screen.getByLabelText("Test user"), "olivia");

    expect(onSelectLogin).toHaveBeenCalledExactlyOnceWith("olivia.mendoza@exits.local");
    expect(JSON.stringify(onSelectLogin.mock.calls)).not.toMatch(/password/i);
  });
});
