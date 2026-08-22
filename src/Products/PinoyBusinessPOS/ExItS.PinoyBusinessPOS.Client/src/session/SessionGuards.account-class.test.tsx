import { render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { describe, expect, it, vi } from "vitest";
import { RequireOrganizationSession, RequirePersonalSession } from "@/session/SessionGuards";
import type { BrowserSessionSnapshot } from "@/api/platform/browser-session";

const sessionState = vi.hoisted(() => ({
  status: "authenticated" as "loading" | "authenticated" | "unauthenticated" | "expired",
  session: null as BrowserSessionSnapshot | null,
}));

vi.mock("@/session/SessionProvider", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@/session/SessionProvider")>();
  return {
    ...actual,
    useSession: () => ({
      status: sessionState.status,
      session: sessionState.session,
      coldStartGrant: null,
      coldStartDenial: null,
      signIn: vi.fn(),
      signOut: vi.fn(),
      refreshSession: vi.fn(),
    }),
  };
});

vi.mock("@/i18n/I18nProvider", () => ({
  useI18n: () => ({
    t: (key: string) => key,
    locale: "en",
    setLocale: vi.fn(),
  }),
}));

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => ({
    status: "ready",
    boundWorkspace: null,
    routingPlan: null,
    sessionGrant: null,
  }),
}));

function renderWithRoutes(session: BrowserSessionSnapshot) {
  sessionState.status = "authenticated";
  sessionState.session = session;

  return render(
    <MemoryRouter initialEntries={["/probe"]}>
      <Routes>
        <Route
          path="/probe"
          element={
            <RequirePersonalSession>
              <div data-testid="personal-ok">personal-ok</div>
            </RequirePersonalSession>
          }
        />
        <Route
          path="/org"
          element={
            <RequireOrganizationSession>
              <div data-testid="org-ok">org-ok</div>
            </RequireOrganizationSession>
          }
        />
      </Routes>
    </MemoryRouter>,
  );
}

describe("SessionGuards AccountClass", () => {
  it("allows Personal session on Personal-only surface", () => {
    renderWithRoutes({ accountClass: "Personal", email: "paul@gmail.com" });
    expect(screen.getByTestId("personal-ok")).toBeInTheDocument();
  });

  it("denies Organization session on Personal-only surface", () => {
    renderWithRoutes({
      accountClass: "Organization",
      email: "paul@ORG907757",
      organizationContextLocked: true,
    });
    expect(screen.getByTestId("account-class-denied")).toBeInTheDocument();
    expect(screen.queryByTestId("personal-ok")).not.toBeInTheDocument();
  });

  it("denies Platform session on Organization surface", () => {
    sessionState.status = "authenticated";
    sessionState.session = { accountClass: "Platform", email: "admin@exits" };
    render(
      <MemoryRouter initialEntries={["/org"]}>
        <Routes>
          <Route
            path="/org"
            element={
              <RequireOrganizationSession>
                <div data-testid="org-ok">org-ok</div>
              </RequireOrganizationSession>
            }
          />
        </Routes>
      </MemoryRouter>,
    );
    expect(screen.getByTestId("account-class-denied")).toBeInTheDocument();
    expect(screen.queryByTestId("org-ok")).not.toBeInTheDocument();
  });

  it("never infers AccountClass from email when server omits class", () => {
    renderWithRoutes({ email: "paul@gmail.com" });
    expect(screen.getByTestId("account-class-denied")).toBeInTheDocument();
  });
});
