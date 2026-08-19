import { render, screen, waitFor } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { getLocalValidationEnabled, listQuickLoginIdentities } from "@/api/auth/auth-client";
import { DevelopmentTestUserTools } from "@/features/auth/DevelopmentTestUserTools";
import { PreferencesProvider } from "@/hooks/use-preferences";
import { areDevelopmentToolsAllowed } from "@/lib/auth/development-tools";

vi.mock("@/api/auth/auth-client", () => ({
  getLocalValidationEnabled: vi.fn(),
  listQuickLoginIdentities: vi.fn(),
}));

vi.mock("@/lib/auth/development-tools", () => ({
  areDevelopmentToolsAllowed: vi.fn(),
}));

function renderTools() {
  return render(
    <PreferencesProvider>
      <DevelopmentTestUserTools onSelectLogin={() => undefined} />
    </PreferencesProvider>,
  );
}

describe("DevelopmentTestUserTools", () => {
  afterEach(() => {
    vi.clearAllMocks();
  });

  it("renders nothing and makes no Local Validation API calls when the frontend mode is disallowed", () => {
    vi.mocked(areDevelopmentToolsAllowed).mockReturnValue(false);
    renderTools();

    expect(screen.queryByText("Development Tools")).not.toBeInTheDocument();
    expect(getLocalValidationEnabled).not.toHaveBeenCalled();
    expect(listQuickLoginIdentities).not.toHaveBeenCalled();
  });

  it("does not query identities when Local Validation is disabled", async () => {
    vi.mocked(areDevelopmentToolsAllowed).mockReturnValue(true);
    vi.mocked(getLocalValidationEnabled).mockResolvedValue(false);
    renderTools();

    await waitFor(() => {
      expect(getLocalValidationEnabled).toHaveBeenCalledOnce();
    });
    expect(listQuickLoginIdentities).not.toHaveBeenCalled();
    expect(screen.queryByText("Development Tools")).not.toBeInTheDocument();
  });

  it("loads the selector when frontend mode and Local Validation both permit it", async () => {
    vi.mocked(areDevelopmentToolsAllowed).mockReturnValue(true);
    vi.mocked(getLocalValidationEnabled).mockResolvedValue(true);
    vi.mocked(listQuickLoginIdentities).mockResolvedValue([
      {
        key: "olivia",
        username: "olivia",
        displayName: "Olivia Mendoza",
        email: "olivia@example.test",
        listLabel: "Olivia Mendoza",
      },
    ]);

    renderTools();

    expect(await screen.findByLabelText("Test user")).toBeInTheDocument();
    expect(getLocalValidationEnabled).toHaveBeenCalledOnce();
    expect(listQuickLoginIdentities).toHaveBeenCalledOnce();
  });
});
