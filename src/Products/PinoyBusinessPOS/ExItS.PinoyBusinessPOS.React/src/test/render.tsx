import type { ReactElement } from "react";
import { render, type RenderOptions } from "@testing-library/react";
import { RouterProvider, createMemoryRouter } from "react-router-dom";
import { vi } from "vitest";
import { AppProviders } from "@/app/providers";
import { appRoutes } from "@/app/router";
import {
  createOrganizationSellReadyFetch,
  createPersonalPlatformFetch,
  createPersonalSessionSnapshot,
  jsonResponse,
  seedOrganizationSellReadyLocalState,
  type OrganizationTestContextOptions,
  type PersonalTestContextOptions,
} from "@/test/session-context";

export { jsonResponse };

export function stubUnauthenticatedPlatformApi() {
  return vi.fn(async (input: RequestInfo | URL) => {
    const url = String(input);
    if (url.includes("/api/v1/platform/auth/me")) {
      return jsonResponse(401, { errorCode: "application.auth.session_invalid" });
    }
    return jsonResponse(404, { detail: "not mocked" });
  });
}

/** Legacy helper — Personal-shaped session without explicit accountClass (omit = deny on class gates). */
export function stubAuthenticatedPlatformApi() {
  return vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    const method = init?.method ?? "GET";
    if (url.includes("/api/v1/platform/auth/me")) {
      return jsonResponse(200, {
        sessionId: "11111111-1111-1111-1111-111111111111",
        username: "owner",
        displayName: "Owner User",
      });
    }
    if (url.includes("/api/v1/platform/auth/organizations") && method === "GET") {
      return jsonResponse(200, []);
    }
    if (url.includes("/api/v1/platform/antiforgery/token")) {
      return jsonResponse(200, { headerName: "X-XSRF-TOKEN", token: "csrf-token" });
    }
    return jsonResponse(404, { detail: "not mocked" });
  });
}

export function renderAt(path: string, options?: Omit<RenderOptions, "wrapper">) {
  vi.stubGlobal("fetch", stubUnauthenticatedPlatformApi());
  const memoryRouter = createMemoryRouter(appRoutes, { initialEntries: [path] });
  return render(
    <AppProviders>
      <RouterProvider router={memoryRouter} />
    </AppProviders>,
    options,
  );
}

export function renderAuthenticatedAt(path: string, options?: Omit<RenderOptions, "wrapper">) {
  vi.stubGlobal("fetch", stubAuthenticatedPlatformApi());
  const memoryRouter = createMemoryRouter(appRoutes, { initialEntries: [path] });
  return render(
    <AppProviders>
      <RouterProvider router={memoryRouter} />
    </AppProviders>,
    options,
  );
}

/** Renders full app routes under a valid Organization session (optional Sell-ready defaults). */
export function renderOrganizationAt(
  path: string,
  options: OrganizationTestContextOptions & {
    sellReady?: boolean;
    catalogCategories?: unknown;
    catalogProducts?: (url: string) => unknown;
  } = {},
  renderOptions?: Omit<RenderOptions, "wrapper">,
) {
  const { sellReady = true, catalogCategories, catalogProducts, ...ctx } = options;
  seedOrganizationSellReadyLocalState(ctx);
  vi.stubGlobal(
    "fetch",
    sellReady
      ? createOrganizationSellReadyFetch({ ...ctx, catalogCategories, catalogProducts })
      : createOrganizationSellReadyFetch({
          ...ctx,
          openShift: false,
          deviceAuthorized: false,
          catalogCategories,
          catalogProducts,
        }),
  );
  const memoryRouter = createMemoryRouter(appRoutes, { initialEntries: [path] });
  return render(
    <AppProviders>
      <RouterProvider router={memoryRouter} />
    </AppProviders>,
    renderOptions,
  );
}

/** Renders full app routes under an explicit Personal session. */
export function renderPersonalAt(
  path: string,
  options: PersonalTestContextOptions = {},
  renderOptions?: Omit<RenderOptions, "wrapper">,
) {
  vi.stubGlobal("fetch", createPersonalPlatformFetch(options));
  const memoryRouter = createMemoryRouter(appRoutes, { initialEntries: [path] });
  return render(
    <AppProviders>
      <RouterProvider router={memoryRouter} />
    </AppProviders>,
    renderOptions,
  );
}

export function renderApp(ui?: ReactElement, options?: Omit<RenderOptions, "wrapper">) {
  if (ui) {
    vi.stubGlobal("fetch", stubUnauthenticatedPlatformApi());
    const memoryRouter = createMemoryRouter([{ path: "/", element: ui }], {
      initialEntries: ["/"],
    });
    return render(
      <AppProviders>
        <RouterProvider router={memoryRouter} />
      </AppProviders>,
      options,
    );
  }
  return renderAt("/sign-in", options);
}

export { createPersonalSessionSnapshot };
