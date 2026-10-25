import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import { AppTopBar } from "@/components/exits/AppTopBar";
import { UiStandardsSidenavProvider } from "@/features/ui-standards/UiStandardsSidenav";

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => ({
    boundWorkspace: null,
    workspaces: [],
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
        accountClass: "Personal",
        organizationContextLocked: false,
        displayName: "Tester",
      },
      status: "authenticated",
    }),
  };
});

vi.mock("@/session/account-class", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@/session/account-class")>();
  return {
    ...actual,
    isOrganizationContextLocked: () => false,
    sessionAccountClass: () => "Personal",
  };
});

function renderUiStandardsTopBar() {
  return render(
    <AppProviders>
      <MemoryRouter initialEntries={["/ui-standards"]}>
        <UiStandardsSidenavProvider>
          <Routes>
            <Route path="/ui-standards" element={<AppTopBar />} />
          </Routes>
        </UiStandardsSidenavProvider>
      </MemoryRouter>
    </AppProviders>,
  );
}

describe("AppTopBar on UI Standards", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("uses the ui-standards row: sidenav toggle, page title, no workspace chrome", () => {
    renderUiStandardsTopBar();
    const topBar = screen.getByTestId("app-top-bar");
    expect(topBar).toHaveAttribute("data-ui-standards", "true");
    expect(topBar.className).toMatch(/app-top-bar--ui-standards/);
    expect(topBar.querySelector(".app-top-bar__row--ui-standards")).toBeInTheDocument();
    expect(screen.getByTestId("app-top-bar-leading")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standards-sidenav-toggle")).toBeInTheDocument();
    expect(screen.getByText("ExItS UI Standard")).toBeInTheDocument();
    const home = screen.getByTestId("org-shell-home");
    expect(home).toHaveAttribute("href", "/personal");
    expect(screen.queryByTestId("workspace-context")).not.toBeInTheDocument();
    expect(screen.queryByTestId("org-needs-attention")).not.toBeInTheDocument();
    expect(screen.queryByTestId("org-notification-bell")).not.toBeInTheDocument();
  });
});
