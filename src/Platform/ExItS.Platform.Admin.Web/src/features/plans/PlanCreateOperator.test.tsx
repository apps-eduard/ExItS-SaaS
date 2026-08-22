import { afterEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { App } from "@/app/App";
import { PLATFORM_PERMISSIONS } from "@/api/authorization/authorization-types";
import { jsonResponse, sampleAuthorization, textResponse } from "@/test/auth-fixtures";

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

describe("PlanCreateOperator", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
  });

  it("opens create dialog and posts a new plan", async () => {
    stubDesktop();
    const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      const method = init?.method ?? "GET";
      if (url.includes("/auth/me")) {
        return jsonResponse(200, {
          sessionId: "11111111-1111-1111-1111-111111111111",
          userId: "22222222-2222-2222-2222-222222222222",
          username: "olivia",
          displayName: "Olivia Mendoza",
          email: "olivia@example.test",
          expiresAtUtc: "2026-08-19T12:00:00Z",
          absoluteExpiresAtUtc: "2026-08-20T12:00:00Z",
          selectedOrganizationId: null,
          selectedOrganizationDisplayName: null,
          organizationSelectionState: "None",
          activeOrganizationCount: 0,
          accountClass: "Platform",
        });
      }
      if (url.includes("/authorization/me")) {
        return jsonResponse(200, {
          ...sampleAuthorization,
          permissions: [
            ...sampleAuthorization.permissions,
            PLATFORM_PERMISSIONS.manageCatalog,
          ],
        });
      }
      if (url.includes("/health")) {
        return textResponse(200, "Healthy");
      }
      if (url.includes("/catalog/products") && method === "GET") {
        return jsonResponse(200, {
          items: [
            {
              id: "cccccccc-cccc-cccc-cccc-cccccccccccc",
              code: "pinoy-business-pos",
              displayName: "Pinoy Business POS",
              status: "Active",
            },
          ],
          totalCount: 1,
          page: 1,
          pageSize: 20,
        });
      }
      if (url.includes("/catalog/plans?")) {
        return jsonResponse(200, { items: [], totalCount: 0, page: 1, pageSize: 20 });
      }
      if (url.includes("/catalog/products/pinoy-business-pos/plans") && method === "POST") {
        return jsonResponse(200, {
          id: "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee",
          productCode: "pinoy-business-pos",
          code: "starter-plus",
          displayName: "Starter Plus",
          status: "Inactive",
          currencyCode: "PHP",
        });
      }
      if (url.includes("/catalog/plans/eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee")) {
        return jsonResponse(200, {
          id: "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee",
          productCode: "pinoy-business-pos",
          code: "starter-plus",
          displayName: "Starter Plus",
          status: "Inactive",
          currencyCode: "PHP",
        });
      }
      if (url.includes("/antiforgery/token")) {
        return jsonResponse(200, { headerName: "X-XSRF-TOKEN", token: "csrf" });
      }
      return jsonResponse(200, { items: [], totalCount: 0, page: 1, pageSize: 1 });
    });
    vi.stubGlobal("fetch", fetchMock);
    window.history.replaceState({}, "", "/admin/plans");
    const user = userEvent.setup();
    render(<App />);
    await user.click(await screen.findByRole("button", { name: "Create plan" }));
    await user.type(screen.getByLabelText("Plan code"), "starter-plus");
    await user.type(screen.getByLabelText("Display name"), "Starter Plus");
    const createButtons = screen.getAllByRole("button", { name: "Create plan" });
    await user.click(createButtons[createButtons.length - 1]!);
    await waitFor(() => {
      expect(fetchMock.mock.calls.some(([input, init]) => {
        const url = String(input);
        return (
          url.includes("/catalog/products/pinoy-business-pos/plans") &&
          (init?.method ?? "GET") === "POST"
        );
      })).toBe(true);
    });
  });
});
