import { afterEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { App } from "@/app/App";
import { AUTH_ERROR_CODES } from "@/api/auth/auth-types";
import { jsonResponse, sampleAuthorization, sampleSession, textResponse } from "@/test/auth-fixtures";

const subscription = {
  id: "11111111-1111-1111-1111-111111111111",
  organizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  organizationDisplayName: "Northwind Market",
  productCode: "pinoy-business-pos",
  planId: "22222222-2222-2222-2222-222222222222",
  status: "Active",
  productDisplayName: "Pinoy Business POS",
  planDisplayName: "Starter",
};

function stubDesktop() {
  vi.spyOn(window, "matchMedia").mockImplementation((query: string) => {
    return {
      matches: query.includes("min-width: 768px"),
      media: query,
      onchange: null,
      addEventListener: () => undefined,
      removeEventListener: () => undefined,
      addListener: () => undefined,
      removeListener: () => undefined,
      dispatchEvent: () => true,
    } as MediaQueryList;
  });
}

describe("session expiry UX on subscriptions", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
  });

  it("redirects to login with return path when a protected request returns 401", async () => {
    stubDesktop();
    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: RequestInfo | URL) => {
        const url = String(input);
        if (url.includes("/auth/me")) {
          return jsonResponse(200, sampleSession);
        }
        if (url.includes("/authorization/me")) {
          return jsonResponse(200, sampleAuthorization);
        }
        if (url.includes("/health")) {
          return textResponse(200, "Healthy");
        }
        if (url.includes("/api/v1/platform/subscriptions")) {
          return jsonResponse(401, {
            status: 401,
            errorCode: AUTH_ERROR_CODES.sessionExpired,
            detail: "Session expired.",
          });
        }
        if (url.includes("/catalog/products")) {
          return jsonResponse(200, { items: [], totalCount: 0, page: 1, pageSize: 20 });
        }
        return jsonResponse(200, { items: [], totalCount: 0, page: 1, pageSize: 1 });
      }),
    );
    window.history.replaceState({}, "", "/admin/subscriptions");
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Sign In" })).toBeInTheDocument();
    expect(window.location.pathname).toBe("/admin/login");
    expect(window.location.search).toContain("return=%2Fadmin%2Fsubscriptions");
    expect(window.location.search).toContain("notice=session-expired");
    expect(
      await screen.findByText("Your session has expired. Please sign in again."),
    ).toBeInTheDocument();
  });

  it("keeps the subscriptions page on 403 Forbidden (does not redirect to login)", async () => {
    stubDesktop();
    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: RequestInfo | URL) => {
        const url = String(input);
        if (url.includes("/auth/me")) {
          return jsonResponse(200, sampleSession);
        }
        if (url.includes("/authorization/me")) {
          return jsonResponse(200, sampleAuthorization);
        }
        if (url.includes("/health")) {
          return textResponse(200, "Healthy");
        }
        if (url.includes("/api/v1/platform/subscriptions")) {
          return jsonResponse(403, { status: 403, title: "Forbidden" });
        }
        if (url.includes("/catalog/products")) {
          return jsonResponse(200, { items: [], totalCount: 0, page: 1, pageSize: 20 });
        }
        return jsonResponse(200, { items: [], totalCount: 0, page: 1, pageSize: 1 });
      }),
    );
    window.history.replaceState({}, "", "/admin/subscriptions");
    render(<App />);
    expect(
      await screen.findByRole("heading", { name: "Unable to load subscriptions.", level: 2 }),
    ).toBeInTheDocument();
    expect(window.location.pathname).toBe("/admin/subscriptions");
    expect(screen.queryByRole("heading", { name: "Sign In" })).not.toBeInTheDocument();
  });

  it("restores /admin/subscriptions after re-login from session expiry", async () => {
    stubDesktop();
    let authenticated = false;
    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: RequestInfo | URL) => {
        const url = String(input);
        if (url.includes("/auth/me")) {
          if (!authenticated) {
            return jsonResponse(401, {
              status: 401,
              errorCode: AUTH_ERROR_CODES.sessionInvalid,
            });
          }
          return jsonResponse(200, sampleSession);
        }
        if (url.includes("/auth/login")) {
          authenticated = true;
          return jsonResponse(200, { ...sampleSession, sessionToken: "opaque" });
        }
        if (url.includes("/authorization/me")) {
          return jsonResponse(200, sampleAuthorization);
        }
        if (url.includes("/health")) {
          return textResponse(200, "Healthy");
        }
        if (url.includes("/api/v1/platform/subscriptions")) {
          return jsonResponse(200, {
            items: [subscription],
            totalCount: 1,
            page: 1,
            pageSize: 20,
          });
        }
        if (url.includes("/catalog/products")) {
          return jsonResponse(200, { items: [], totalCount: 0, page: 1, pageSize: 20 });
        }
        return jsonResponse(200, { items: [], totalCount: 0, page: 1, pageSize: 1 });
      }),
    );
    window.history.replaceState(
      {},
      "",
      "/admin/login?return=%2Fadmin%2Fsubscriptions&notice=session-expired",
    );
    const user = userEvent.setup();
    render(<App />);
    await screen.findByRole("heading", { name: "Sign In" });
    await user.type(screen.getByLabelText("Email"), "olivia@example.test");
    await user.type(screen.getByLabelText("Password"), "secret-password");
    await user.click(screen.getByRole("button", { name: "Sign In" }));
    await waitFor(() => {
      expect(window.location.pathname).toBe("/admin/subscriptions");
    });
    expect(await screen.findByText("Northwind Market")).toBeInTheDocument();
  });
});
