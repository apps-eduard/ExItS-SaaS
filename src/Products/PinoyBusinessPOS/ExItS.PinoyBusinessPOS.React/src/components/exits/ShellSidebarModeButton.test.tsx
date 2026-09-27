import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type { ReactNode } from "react";
import { MemoryRouter } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import { AppTopBar } from "@/components/exits/AppTopBar";
import {
  defaultUiPreferences,
  readUiPreferences,
  UI_PREFERENCES_STORAGE_KEY,
  writeUiPreferences,
} from "@/lib/preferences/ui-preferences";

vi.mock("@/session/SessionProvider", () => ({
  useSession: () => ({
    signOut: vi.fn(),
    session: {
      accountClass: "Organization",
      organizationContextLocked: false,
      displayName: "User",
    },
    status: "authenticated",
  }),
}));

vi.mock("@/session/account-class", () => ({
  isOrganizationContextLocked: () => false,
  sessionAccountClass: () => "Organization",
}));

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => ({
    boundWorkspace: {
      organizationId: "11111111-1111-1111-1111-111111111111",
      organizationDisplayName: "Kizy Store",
      branchId: "22222222-2222-2222-2222-222222222222",
      branchName: "Main",
      branchType: "Retail",
    },
    workspaces: [],
    clearBoundWorkspace: vi.fn(),
  }),
}));

function renderTopBar(ui: ReactNode = <AppTopBar hideDesktopBrand />) {
  return render(
    <AppProviders>
      <MemoryRouter>{ui}</MemoryRouter>
    </AppProviders>,
  );
}

describe("topbar sidebar mode cycle", () => {
  beforeEach(() => {
    window.localStorage.removeItem(UI_PREFERENCES_STORAGE_KEY);
    writeUiPreferences({ ...defaultUiPreferences, navigationMode: "standard" });
    document.documentElement.dataset.navigationMode = "standard";
  });

  it("shows the cycle control only on shell desktop topbars", () => {
    const { unmount } = renderTopBar(<AppTopBar hideDesktopBrand />);
    expect(screen.getByTestId("shell-sidebar-mode-button")).toBeInTheDocument();
    expect(screen.getByTestId("app-top-bar-leading")).toBeInTheDocument();
    unmount();

    renderTopBar(<AppTopBar />);
    expect(screen.queryByTestId("shell-sidebar-mode-button")).not.toBeInTheDocument();
  });

  it("cycles standard → compact → hidden → standard and persists", async () => {
    const user = userEvent.setup();
    renderTopBar();

    const button = screen.getByTestId("shell-sidebar-mode-button");
    expect(button).toHaveAttribute("data-navigation-mode", "standard");
    expect(button).toHaveAttribute("aria-label", "Show sidebar icons only");

    await user.click(button);
    expect(button).toHaveAttribute("data-navigation-mode", "compact");
    expect(document.documentElement.dataset.navigationMode).toBe("compact");
    expect(readUiPreferences().navigationMode).toBe("compact");
    expect(button).toHaveAttribute("aria-label", "Hide sidebar");

    await user.click(button);
    expect(button).toHaveAttribute("data-navigation-mode", "hidden");
    expect(document.documentElement.dataset.navigationMode).toBe("hidden");
    expect(readUiPreferences().navigationMode).toBe("hidden");
    expect(button).toHaveAttribute("aria-label", "Show full sidebar");

    await user.click(button);
    expect(button).toHaveAttribute("data-navigation-mode", "standard");
    expect(document.documentElement.dataset.navigationMode).toBe("standard");
    expect(readUiPreferences().navigationMode).toBe("standard");
  });
});
