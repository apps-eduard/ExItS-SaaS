import { afterEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { createMemoryRouter, RouterProvider } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import { SessionGate } from "@/auth/SessionGate";
import { SignInPage } from "@/auth/SignInPage";
import { collectWebStorageAuthHits } from "@/auth/web-storage-guard";
import { FoundationHomePage } from "@/features/foundation/FoundationHomePage";
import { AppShell } from "@/layouts/AppShell";

const loginUrl = "/platform-api/api/v1/platform/auth/login";

const sessionBody = {
  sessionId: "11111111-1111-4111-8111-111111111111",
  userId: "22222222-2222-4222-8222-222222222222",
  username: "maria.santos",
  displayName: "Maria Santos",
  email: "maria.santos@exits.local",
  expiresAtUtc: "2026-12-31T00:00:00.000Z",
  absoluteExpiresAtUtc: "2026-12-31T00:00:00.000Z",
  lastActivityAtUtc: "2026-08-19T00:00:00.000Z",
  organizationSelectionState: "None",
  activeOrganizationCount: 0,
};

function jsonResponse(status: number, body: unknown) {
  return {
    ok: status >= 200 && status < 300,
    status,
    json: async () => body,
  };
}

function renderAuthApp() {
  const router = createMemoryRouter(
    [
      {
        element: <SessionGate />,
        children: [
          { path: "/sign-in", element: <SignInPage /> },
          {
            path: "/",
            element: <AppShell />,
            children: [{ index: true, element: <FoundationHomePage /> }],
          },
        ],
      },
    ],
    { initialEntries: ["/"] },
  );

  return render(
    <AppProviders>
      <RouterProvider router={router} />
    </AppProviders>,
  );
}

describe("browser session gate", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    window.localStorage.clear();
    window.sessionStorage.clear();
  });

  it("sends signed-out users to Sign in", async () => {
    vi.stubGlobal(
      "fetch",
      vi
        .fn()
        .mockResolvedValue(jsonResponse(401, { status: 401, errorCode: "auth.session_invalid" })),
    );
    renderAuthApp();
    expect(await screen.findByRole("heading", { name: "Sign in" })).toBeInTheDocument();
    expect(screen.queryByRole("heading", { name: "Client foundation" })).not.toBeInTheDocument();
  });

  it("signs in with password login and never persists sessionToken", async () => {
    let authenticated = false;
    const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      if (url.includes("/auth/login")) {
        authenticated = true;
        expect(init?.credentials).toBe("include");
        expect(new Headers(init?.headers).get("Authorization")).toBeNull();
        return jsonResponse(200, { ...sessionBody, sessionToken: "reusable-secret" });
      }
      if (url.includes("/auth/me")) {
        return authenticated
          ? jsonResponse(200, sessionBody)
          : jsonResponse(401, { status: 401, errorCode: "auth.session_invalid" });
      }
      return jsonResponse(404, {});
    });
    vi.stubGlobal("fetch", fetchMock);

    const user = userEvent.setup();
    renderAuthApp();
    expect(await screen.findByRole("heading", { name: "Sign in" })).toBeInTheDocument();
    await user.type(screen.getByLabelText("Email or username"), "maria.santos");
    await user.type(screen.getByLabelText("Password"), "not-a-real-secret");
    await user.click(screen.getByRole("button", { name: "Sign in" }));

    expect(await screen.findByRole("heading", { name: "Client foundation" })).toBeInTheDocument();
    expect(screen.getByText("Maria Santos")).toBeInTheDocument();
    expect(collectWebStorageAuthHits()).toEqual([]);
    expect(JSON.stringify(window.localStorage)).not.toContain("reusable-secret");
    expect(JSON.stringify(window.sessionStorage)).not.toContain("reusable-secret");
    expect(fetchMock.mock.calls.some(([requestUrl]) => String(requestUrl) === loginUrl)).toBe(true);
  });

  it("keeps invalid credentials on Sign in with a generic error", async () => {
    const fetchMock = vi.fn(async (input: RequestInfo | URL) => {
      const url = String(input);
      if (url.includes("/auth/login")) {
        return jsonResponse(401, {
          status: 401,
          detail: "Do not show this server detail",
          errorCode: "auth.invalid_credentials",
        });
      }
      return jsonResponse(401, { status: 401, errorCode: "auth.session_invalid" });
    });
    vi.stubGlobal("fetch", fetchMock);
    const user = userEvent.setup();
    renderAuthApp();
    expect(await screen.findByRole("heading", { name: "Sign in" })).toBeInTheDocument();
    await user.type(screen.getByLabelText("Email or username"), "maria.santos");
    await user.type(screen.getByLabelText("Password"), "wrong-password");
    await user.click(screen.getByRole("button", { name: "Sign in" }));
    expect(await screen.findByRole("alert")).toHaveTextContent("Email or password is incorrect.");
    expect(screen.queryByText("Do not show this server detail")).not.toBeInTheDocument();
    await waitFor(() =>
      expect(screen.getByRole("heading", { name: "Sign in" })).toBeInTheDocument(),
    );
  });
});
