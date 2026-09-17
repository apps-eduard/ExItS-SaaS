import { describe, expect, it, vi, afterEach } from "vitest";
import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { OrgBottomNav } from "@/features/shell/OrgBottomNav";
import { SHELL_DESKTOP_MIN_PX } from "@/features/shell/shell-breakpoints";

vi.mock("@/i18n/I18nProvider", () => ({
  useI18n: () => ({ t: (key: string) => key }),
}));

vi.mock("@/session/SessionProvider", () => ({
  isAuthenticatedOrColdStartOffline: () => true,
  useSession: () => ({ status: "authenticated" }),
}));

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => ({
    sessionGrant: {
      productAccessAllowed: true,
      mappedPosRoleCode: "Owner",
      productLocalRoleCode: "Owner",
      membershipRole: "OrganizationOwner",
      organizationManagementAuthority: true,
      featureCodes: [],
      grantedFeatureCodes: [],
    },
    boundWorkspace: {
      experience: "operations",
      branchType: "Retail",
      organizationDisplayName: "Demo",
    },
  }),
}));

function stubViewport(minWidth: number) {
  Object.defineProperty(window, "matchMedia", {
    writable: true,
    configurable: true,
    value: (query: string) => {
      const match = /min-width:\s*(\d+)px/.exec(query);
      const px = match ? Number(match[1]) : 0;
      return {
        matches: minWidth >= px,
        media: query,
        onchange: null,
        addEventListener: vi.fn(),
        removeEventListener: vi.fn(),
        addListener: vi.fn(),
        removeListener: vi.fn(),
        dispatchEvent: vi.fn(),
      };
    },
  });
}

describe("OrgBottomNav desktop gating", () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it(`unmounts at desktop (>=${SHELL_DESKTOP_MIN_PX})`, () => {
    stubViewport(SHELL_DESKTOP_MIN_PX);
    render(
      <MemoryRouter initialEntries={["/"]}>
        <OrgBottomNav />
      </MemoryRouter>,
    );
    expect(screen.queryByTestId("org-bottom-nav")).not.toBeInTheDocument();
  });

  it("renders below desktop with lg:hidden safety class", () => {
    stubViewport(1023);
    render(
      <MemoryRouter initialEntries={["/"]}>
        <OrgBottomNav />
      </MemoryRouter>,
    );
    const nav = screen.getByTestId("org-bottom-nav");
    expect(nav.className).toMatch(/\blg:hidden\b/);
  });
});
