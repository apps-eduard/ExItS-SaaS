import { describe, expect, it, vi, beforeEach } from "vitest";
import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import { AppTopBar } from "@/components/exits/AppTopBar";
import type { BoundWorkspace } from "@/workspace/types";
import { TEST_ORG_A_ID } from "@/test/session-context";

const navState = {
  bound: {
    organizationId: TEST_ORG_A_ID,
    organizationDisplayName: "Kizy Store",
    branchId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
    branchName: "Main",
    branchType: "Retail",
  } as BoundWorkspace,
  workspaces: [
    {
      organizationId: TEST_ORG_A_ID,
      displayName: "Kizy Store",
      branches: [
        {
          branchId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
          name: "Main",
          secondaryLine: "Retail",
          isPrimary: true,
          isActive: true,
          branchType: "Retail" as const,
        },
      ],
    },
  ],
  locked: false,
};

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => ({
    boundWorkspace: navState.bound,
    workspaces: navState.workspaces,
    clearBoundWorkspace: vi.fn(),
  }),
}));

vi.mock("@/session/SessionProvider", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@/session/SessionProvider")>();
  return {
    ...actual,
    useSession: () => ({
      signOut: vi.fn(async () => ({ ok: true, nextRoute: "/sign-in" })),
      session: {
        accountClass: "Organization",
        organizationContextLocked: navState.locked,
        displayName: "Owner",
      },
      status: "authenticated",
    }),
  };
});

vi.mock("@/session/account-class", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@/session/account-class")>();
  return {
    ...actual,
    isOrganizationContextLocked: () => navState.locked,
    sessionAccountClass: () => "Organization",
  };
});

function renderTopBar(hideDesktopBrand: boolean) {
  return render(
    <AppProviders>
      <MemoryRouter>
        <AppTopBar hideDesktopBrand={hideDesktopBrand} />
      </MemoryRouter>
    </AppProviders>,
  );
}

describe("AppTopBar desktop brand ownership", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("uses shell-mobile brand composition when hideDesktopBrand is set (no desktop product duplicate)", () => {
    renderTopBar(true);
    const topBar = screen.getByTestId("app-top-bar");
    expect(topBar).toHaveAttribute("data-hide-desktop-brand", "true");
    expect(topBar.className).toMatch(/app-top-bar--shell-desktop/);
    expect(topBar.querySelector(".app-top-bar__brand--shell-mobile")).toBeInTheDocument();
    // No separate md:block desktop product-name node.
    expect(topBar.querySelector(".app-top-bar__brand-copy.hidden.md\\:block")).not.toBeInTheDocument();
  });

  it("keeps full brand composition when not in a sidebar shell", () => {
    renderTopBar(false);
    const topBar = screen.getByTestId("app-top-bar");
    expect(topBar).toHaveAttribute("data-hide-desktop-brand", "false");
    expect(topBar.querySelector(".app-top-bar__brand--shell-mobile")).not.toBeInTheDocument();
    expect(topBar.querySelector(".app-top-bar__mark")).toBeInTheDocument();
  });
});
