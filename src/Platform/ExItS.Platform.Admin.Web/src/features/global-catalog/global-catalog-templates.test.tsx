import { afterEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { App } from "@/app/App";
import { clearPlatformAntiforgeryToken } from "@/api/platform-http";
import { UI_PREFERENCES_STORAGE_KEY } from "@/lib/preferences/ui-preferences";
import {
  GLOBAL_CATALOG_TEMPLATE_FIXTURE_IDS,
  installGlobalCatalogTemplateMock,
} from "@/features/global-catalog/global-catalog-template-test-fixtures";

const { draftId, publishedId, archivedId } = GLOBAL_CATALOG_TEMPLATE_FIXTURE_IDS;

function stubDesktop() {
  vi.spyOn(window, "matchMedia").mockImplementation((query: string) => {
    return {
      matches: query.includes("min-width: 1024px") || query.includes("min-width: 768px"),
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

function stubMobileListViewport() {
  vi.spyOn(window, "matchMedia").mockImplementation((query: string) => {
    return {
      matches: false,
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

async function confirmLifecycle(user: ReturnType<typeof userEvent.setup>, actionLabel: string) {
  await user.click(screen.getByRole("button", { name: actionLabel }));
  await user.click(screen.getByRole("button", { name: actionLabel, hidden: false }));
}

describe("global catalog templates", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
    clearPlatformAntiforgeryToken();
    window.localStorage.removeItem(UI_PREFERENCES_STORAGE_KEY);
  });

  it("loads the templates list query", async () => {
    stubMobileListViewport();
    const { fetchMock } = installGlobalCatalogTemplateMock();
    window.history.replaceState({}, "", "/admin/global-catalog/templates");
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Templates" })).toBeInTheDocument();
    expect(await screen.findByText("Sari-Sari Starter")).toBeInTheDocument();
    await waitFor(() => {
      const urls = fetchMock.mock.calls.map(([input]) => String(input));
      expect(
        urls.some(
          (url) =>
            url.includes("/global-catalog/templates") &&
            url.includes("page=1") &&
            url.includes("pageSize=20"),
        ),
      ).toBe(true);
    });
  });

  it("maps search and status filters to server parameters", async () => {
    stubMobileListViewport();
    const { fetchMock } = installGlobalCatalogTemplateMock();
    window.history.replaceState({}, "", "/admin/global-catalog/templates");
    const user = userEvent.setup();
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Templates" })).toBeInTheDocument();
    await user.type(screen.getByLabelText("Search"), "legacy");
    await user.click(screen.getByRole("button", { name: "Search" }));
    await user.selectOptions(screen.getByLabelText("Status"), "Archived");
    await waitFor(() => {
      const urls = fetchMock.mock.calls.map(([input]) => String(input));
      expect(urls.some((url) => url.includes("search=legacy"))).toBe(true);
      expect(urls.some((url) => url.includes("status=Archived"))).toBe(true);
    });
  });

  it("paginates the templates list", async () => {
    stubMobileListViewport();
    const items = Array.from({ length: 25 }, (_, index) => ({
      id: `11111111-1111-1111-1111-${String(index).padStart(12, "0")}`,
      name: `Template ${index}`,
      slug: `template-${index}`,
      primaryBusinessType: "sari-sari",
      primaryBusinessTypeId: GLOBAL_CATALOG_TEMPLATE_FIXTURE_IDS.businessTypeId,
      status: "Draft" as const,
      defaultBatchSize: 20,
      selectionMode: "Curated" as const,
      productCount: 0,
      firstBatchCount: 0,
      createdAtUtc: "2026-01-01T08:00:00Z",
      updatedAtUtc: "2026-08-01T08:00:00Z",
      products: [],
    }));
    installGlobalCatalogTemplateMock({ templates: items });
    window.history.replaceState({}, "", "/admin/global-catalog/templates");
    const user = userEvent.setup();
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Templates" })).toBeInTheDocument();
    expect(await screen.findByText("Template 0")).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Next" }));
    await waitFor(() => {
      expect(window.location.search).toContain("page=2");
    });
  });

  it("creates a catalog template", async () => {
    stubDesktop();
    installGlobalCatalogTemplateMock();
    window.history.replaceState({}, "", "/admin/global-catalog/templates/new");
    const user = userEvent.setup();
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Create template" })).toBeInTheDocument();
    await user.type(screen.getByLabelText("Name"), "Neighborhood Essentials");
    await user.selectOptions(screen.getByLabelText("Primary business type"), GLOBAL_CATALOG_TEMPLATE_FIXTURE_IDS.businessTypeId);
    await user.click(screen.getByRole("button", { name: "Save" }));
    expect(await screen.findByRole("heading", { name: "Neighborhood Essentials" })).toBeInTheDocument();
  });

  it("updates a catalog template on edit", async () => {
    stubDesktop();
    installGlobalCatalogTemplateMock();
    window.history.replaceState({}, "", `/admin/global-catalog/templates/${draftId}/edit`);
    const user = userEvent.setup();
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Edit template" })).toBeInTheDocument();
    const nameInput = screen.getByLabelText("Name");
    await user.clear(nameInput);
    await user.type(nameInput, "Updated Starter");
    await user.click(screen.getByRole("button", { name: "Save" }));
    expect(await screen.findByRole("heading", { name: "Updated Starter" })).toBeInTheDocument();
  });

  it("shows concurrency conflict detail on edit", async () => {
    stubDesktop();
    installGlobalCatalogTemplateMock({ updateConflict: true });
    window.history.replaceState({}, "", `/admin/global-catalog/templates/${draftId}/edit`);
    const user = userEvent.setup();
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Edit template" })).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Save" }));
    expect(
      await screen.findByText("Catalog template was updated by another operator."),
    ).toBeInTheDocument();
  });

  it("hides mutation controls for view-only users", async () => {
    stubDesktop();
    installGlobalCatalogTemplateMock({
      permissions: ["platform.permission.view_portfolio", "platform.permission.view_global_catalog"],
    });
    window.history.replaceState({}, "", `/admin/global-catalog/templates/${draftId}`);
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Sari-Sari Starter" })).toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Edit" })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Publish" })).not.toBeInTheDocument();
    expect(screen.queryByText("Available products")).not.toBeInTheDocument();
  });

  it("publishes a draft template with products", async () => {
    stubDesktop();
    installGlobalCatalogTemplateMock();
    window.history.replaceState({}, "", `/admin/global-catalog/templates/${draftId}`);
    const user = userEvent.setup();
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Sari-Sari Starter" })).toBeInTheDocument();
    await confirmLifecycle(user, "Publish");
    await waitFor(() => {
      expect(screen.queryByRole("button", { name: "Publish" })).not.toBeInTheDocument();
    });
    expect(screen.getByRole("button", { name: "Unpublish" })).toBeInTheDocument();
  });

  it("unpublishes a published template", async () => {
    stubDesktop();
    installGlobalCatalogTemplateMock();
    window.history.replaceState({}, "", `/admin/global-catalog/templates/${publishedId}`);
    const user = userEvent.setup();
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Mini Grocery Essentials" })).toBeInTheDocument();
    await confirmLifecycle(user, "Unpublish");
    expect(await screen.findByText("Draft")).toBeInTheDocument();
  });

  it("archives a template", async () => {
    stubDesktop();
    installGlobalCatalogTemplateMock();
    window.history.replaceState({}, "", `/admin/global-catalog/templates/${publishedId}`);
    const user = userEvent.setup();
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Mini Grocery Essentials" })).toBeInTheDocument();
    await confirmLifecycle(user, "Archive");
    expect(await screen.findByText("Archived")).toBeInTheDocument();
  });

  it("keeps archived templates read-only", async () => {
    stubDesktop();
    installGlobalCatalogTemplateMock();
    window.history.replaceState({}, "", `/admin/global-catalog/templates/${archivedId}`);
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Legacy Template" })).toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Edit" })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Publish" })).not.toBeInTheDocument();
    expect(screen.queryByText("Available products")).not.toBeInTheDocument();
  });

  it("assigns and removes products in composition", async () => {
    stubDesktop();
    installGlobalCatalogTemplateMock({
      templates: [
        {
          id: draftId,
          name: "Empty Draft",
          slug: "empty-draft",
          primaryBusinessType: "sari-sari",
          primaryBusinessTypeId: GLOBAL_CATALOG_TEMPLATE_FIXTURE_IDS.businessTypeId,
          status: "Draft",
          defaultBatchSize: 20,
          selectionMode: "Curated",
          productCount: 0,
          firstBatchCount: 0,
          createdAtUtc: "2026-01-01T08:00:00Z",
          updatedAtUtc: "2026-08-01T08:00:00Z",
          products: [],
        },
      ],
    });
    window.history.replaceState({}, "", `/admin/global-catalog/templates/${draftId}`);
    const user = userEvent.setup();
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Empty Draft" })).toBeInTheDocument();
    expect(await screen.findByText("Available products")).toBeInTheDocument();
    const assignButtons = await screen.findAllByRole("button", { name: "Assign" });
    await user.click(assignButtons[0]!);
    expect(await screen.findByText("Canned Tuna")).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Remove product" }));
    expect(await screen.findByText("No products assigned to this template yet.")).toBeInTheDocument();
  });
});
