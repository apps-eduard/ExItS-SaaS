import { afterEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { App } from "@/app/App";
import { clearPlatformAntiforgeryToken } from "@/api/platform-http";
import { sampleAuthorization } from "@/test/auth-fixtures";
import { UI_PREFERENCES_STORAGE_KEY } from "@/lib/preferences/ui-preferences";
import { installGlobalCatalogBusinessTypeMock } from "@/features/global-catalog/global-catalog-test-fixtures";

const ACTIVE_ID = "dddddddd-dddd-dddd-dddd-dddddddddddd";
const INACTIVE_ID = "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee";
const ARCHIVED_ID = "ffffffff-ffff-ffff-ffff-ffffffffffff";

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

function setFilipinoLanguage() {
  window.localStorage.setItem(
    UI_PREFERENCES_STORAGE_KEY,
    JSON.stringify({
      theme: "system",
      density: "balanced",
      language: "fil-PH",
      sidebarCollapsed: false,
    }),
  );
}

function buildPagedBusinessTypeItems() {
  const items = [
    {
      id: ACTIVE_ID,
      code: "sari-sari",
      name: "Sari-Sari Store",
      description: "Neighborhood store",
      status: "Active" as const,
      sortOrder: 1,
      iconReference: "store",
      createdAtUtc: "2026-01-01T08:00:00Z",
      updatedAtUtc: "2026-08-01T08:00:00Z",
    },
    {
      id: INACTIVE_ID,
      code: "mini-grocery",
      name: "Mini Grocery",
      description: "Small grocery",
      status: "Inactive" as const,
      sortOrder: 2,
      createdAtUtc: "2026-01-02T08:00:00Z",
      updatedAtUtc: "2026-08-02T08:00:00Z",
    },
    {
      id: ARCHIVED_ID,
      code: "bakery",
      name: "Bakery",
      description: "Fresh bread and pastries",
      status: "Archived" as const,
      sortOrder: 3,
      createdAtUtc: "2026-01-03T08:00:00Z",
      updatedAtUtc: "2026-08-03T08:00:00Z",
    },
  ];
  for (let index = 0; index < 18; index += 1) {
    items.push({
      id: `11111111-1111-1111-1111-${String(index).padStart(12, "0")}`,
      code: `extra-${index}`,
      name: `Extra Type ${index}`,
      description: "",
      status: "Active" as const,
      sortOrder: 100 + index,
      iconReference: "",
      createdAtUtc: "2026-02-01T08:00:00Z",
      updatedAtUtc: "2026-02-01T08:00:00Z",
    });
  }
  return items;
}

async function confirmLifecycle(user: ReturnType<typeof userEvent.setup>, actionLabel: string) {
  await user.click(screen.getByRole("button", { name: actionLabel }));
  await user.click(screen.getByRole("button", { name: actionLabel, hidden: false }));
}

describe("global catalog business types", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
    clearPlatformAntiforgeryToken();
    window.localStorage.removeItem(UI_PREFERENCES_STORAGE_KEY);
  });

  it("loads the business types list query", async () => {
    stubMobileListViewport();
    const { fetchMock } = installGlobalCatalogBusinessTypeMock();
    window.history.replaceState({}, "", "/admin/global-catalog/business-types");
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Business Types" })).toBeInTheDocument();
    expect(await screen.findByText("Sari-Sari Store")).toBeInTheDocument();
    await waitFor(() => {
      const urls = fetchMock.mock.calls.map(([input]) => String(input));
      expect(
        urls.some(
          (url) =>
            url.includes("/global-catalog/business-types") &&
            url.includes("page=1") &&
            url.includes("pageSize=20"),
        ),
      ).toBe(true);
    });
  });

  it("maps search to server parameters", async () => {
    stubMobileListViewport();
    const { fetchMock } = installGlobalCatalogBusinessTypeMock();
    window.history.replaceState({}, "", "/admin/global-catalog/business-types");
    const user = userEvent.setup();
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Business Types" })).toBeInTheDocument();
    await user.type(screen.getByLabelText("Search"), "bakery");
    await user.click(screen.getByRole("button", { name: "Search" }));
    await waitFor(() => {
      const urls = fetchMock.mock.calls.map(([input]) => String(input));
      expect(urls.some((url) => url.includes("search=bakery"))).toBe(true);
    });
  });

  it("maps status filter to server parameters", async () => {
    stubMobileListViewport();
    const { fetchMock } = installGlobalCatalogBusinessTypeMock();
    window.history.replaceState({}, "", "/admin/global-catalog/business-types");
    const user = userEvent.setup();
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Business Types" })).toBeInTheDocument();
    await user.selectOptions(screen.getByLabelText("Status"), "Inactive");
    await waitFor(() => {
      const urls = fetchMock.mock.calls.map(([input]) => String(input));
      expect(urls.some((url) => url.includes("status=Inactive"))).toBe(true);
    });
  });

  it("maps sorting to server parameters", async () => {
    stubMobileListViewport();
    const { fetchMock } = installGlobalCatalogBusinessTypeMock();
    window.history.replaceState({}, "", "/admin/global-catalog/business-types");
    const user = userEvent.setup();
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Business Types" })).toBeInTheDocument();
    await user.selectOptions(screen.getByLabelText("Sort"), "Name");
    await user.selectOptions(screen.getByLabelText("Order"), "desc");
    await waitFor(() => {
      const urls = fetchMock.mock.calls.map(([input]) => String(input));
      expect(urls.some((url) => url.includes("sortBy=Name") && url.includes("sortDesc=true"))).toBe(
        true,
      );
    });
  });

  it("paginates the business types list", async () => {
    stubMobileListViewport();
    const { fetchMock } = installGlobalCatalogBusinessTypeMock({
      items: buildPagedBusinessTypeItems(),
    });
    window.history.replaceState({}, "", "/admin/global-catalog/business-types?sortBy=CreatedAtUtc");
    const user = userEvent.setup();
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Business Types" })).toBeInTheDocument();
    expect(await screen.findByText("Extra Type 0")).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Next" }));
    await waitFor(() => {
      expect(window.location.search).toContain("page=2");
      const urls = fetchMock.mock.calls.map(([input]) => String(input));
      expect(urls.some((url) => url.includes("page=2"))).toBe(true);
    });
  });

  it("renders business type detail", async () => {
    stubDesktop();
    installGlobalCatalogBusinessTypeMock();
    window.history.replaceState({}, "", `/admin/global-catalog/business-types/${ACTIVE_ID}`);
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Sari-Sari Store" })).toBeInTheDocument();
    expect(screen.getByText("sari-sari")).toBeInTheDocument();
    expect(screen.getByText("Neighborhood store")).toBeInTheDocument();
  });

  it("creates a business type", async () => {
    stubDesktop();
    installGlobalCatalogBusinessTypeMock();
    window.history.replaceState({}, "", "/admin/global-catalog/business-types/new");
    const user = userEvent.setup();
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Create business type" })).toBeInTheDocument();
    await user.type(screen.getByLabelText("Code"), "cafe");
    await user.type(screen.getByLabelText("Name"), "Cafe");
    await user.click(screen.getByRole("button", { name: "Save" }));
    expect(await screen.findByRole("heading", { name: "Cafe" })).toBeInTheDocument();
  });

  it("shows duplicate code detail on create", async () => {
    stubDesktop();
    installGlobalCatalogBusinessTypeMock();
    window.history.replaceState({}, "", "/admin/global-catalog/business-types/new");
    const user = userEvent.setup();
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Create business type" })).toBeInTheDocument();
    await user.type(screen.getByLabelText("Code"), "sari-sari");
    await user.type(screen.getByLabelText("Name"), "Duplicate Code Test");
    await user.click(screen.getByRole("button", { name: "Save" }));
    expect(
      await screen.findByText("A business type with this code already exists."),
    ).toBeInTheDocument();
  });

  it("shows duplicate name detail on create", async () => {
    stubDesktop();
    installGlobalCatalogBusinessTypeMock();
    window.history.replaceState({}, "", "/admin/global-catalog/business-types/new");
    const user = userEvent.setup();
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Create business type" })).toBeInTheDocument();
    await user.type(screen.getByLabelText("Code"), "duplicate-name");
    await user.type(screen.getByLabelText("Name"), "Sari-Sari Store");
    await user.click(screen.getByRole("button", { name: "Save" }));
    expect(
      await screen.findByText("A business type with this name already exists."),
    ).toBeInTheDocument();
  });

  it("updates business type name on edit", async () => {
    stubDesktop();
    installGlobalCatalogBusinessTypeMock();
    window.history.replaceState({}, "", `/admin/global-catalog/business-types/${ACTIVE_ID}/edit`);
    const user = userEvent.setup();
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Edit business type" })).toBeInTheDocument();
    const nameInput = screen.getByLabelText("Name");
    await user.clear(nameInput);
    await user.type(nameInput, "Sari-Sari Updated");
    await user.click(screen.getByRole("button", { name: "Save" }));
    expect(await screen.findByRole("heading", { name: "Sari-Sari Updated" })).toBeInTheDocument();
  });

  it("updates business type description on edit", async () => {
    stubDesktop();
    installGlobalCatalogBusinessTypeMock();
    window.history.replaceState({}, "", `/admin/global-catalog/business-types/${ACTIVE_ID}/edit`);
    const user = userEvent.setup();
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Edit business type" })).toBeInTheDocument();
    const descriptionInput = screen.getByLabelText("Description");
    await user.clear(descriptionInput);
    await user.type(descriptionInput, "Updated neighborhood store");
    await user.click(screen.getByRole("button", { name: "Save" }));
    expect(await screen.findByText("Updated neighborhood store")).toBeInTheDocument();
  });

  it("updates business type sort order on edit", async () => {
    stubDesktop();
    installGlobalCatalogBusinessTypeMock();
    window.history.replaceState({}, "", `/admin/global-catalog/business-types/${ACTIVE_ID}/edit`);
    const user = userEvent.setup();
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Edit business type" })).toBeInTheDocument();
    const sortOrderInput = screen.getByLabelText("Sort order");
    await user.clear(sortOrderInput);
    await user.type(sortOrderInput, "99");
    await user.click(screen.getByRole("button", { name: "Save" }));
    expect(await screen.findByText("99")).toBeInTheDocument();
  });

  it("keeps code immutable after create", async () => {
    stubDesktop();
    const { fetchMock } = installGlobalCatalogBusinessTypeMock();
    window.history.replaceState({}, "", `/admin/global-catalog/business-types/${ACTIVE_ID}/edit`);
    const user = userEvent.setup();
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Edit business type" })).toBeInTheDocument();
    expect(screen.getByLabelText("Code")).toBeDisabled();
    expect(screen.getByLabelText("Code")).toHaveValue("sari-sari");
    const nameInput = screen.getByLabelText("Name");
    await user.clear(nameInput);
    await user.type(nameInput, "Immutable Code Check");
    await user.click(screen.getByRole("button", { name: "Save" }));
    await waitFor(() => {
      const putCall = fetchMock.mock.calls.find(
        ([input, init]) =>
          String(input).includes(`/business-types/${ACTIVE_ID}`) && init?.method === "PUT",
      );
      expect(putCall).toBeDefined();
      const body = JSON.parse(String(putCall?.[1]?.body));
      expect(body).not.toHaveProperty("code");
    });
  });

  it("transitions Active to Inactive", async () => {
    stubDesktop();
    installGlobalCatalogBusinessTypeMock();
    window.history.replaceState({}, "", `/admin/global-catalog/business-types/${ACTIVE_ID}`);
    const user = userEvent.setup();
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Sari-Sari Store" })).toBeInTheDocument();
    await confirmLifecycle(user, "Deactivate");
    expect(await screen.findByText("Inactive")).toBeInTheDocument();
  });

  it("transitions Active to Archived", async () => {
    stubDesktop();
    installGlobalCatalogBusinessTypeMock();
    window.history.replaceState({}, "", `/admin/global-catalog/business-types/${ACTIVE_ID}`);
    const user = userEvent.setup();
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Sari-Sari Store" })).toBeInTheDocument();
    await confirmLifecycle(user, "Archive");
    expect(await screen.findByText("Archived")).toBeInTheDocument();
  });

  it("transitions Inactive to Active", async () => {
    stubDesktop();
    installGlobalCatalogBusinessTypeMock();
    window.history.replaceState({}, "", `/admin/global-catalog/business-types/${INACTIVE_ID}`);
    const user = userEvent.setup();
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Mini Grocery" })).toBeInTheDocument();
    await confirmLifecycle(user, "Activate");
    expect(await screen.findByText("Active")).toBeInTheDocument();
  });

  it("transitions Inactive to Archived", async () => {
    stubDesktop();
    installGlobalCatalogBusinessTypeMock();
    window.history.replaceState({}, "", `/admin/global-catalog/business-types/${INACTIVE_ID}`);
    const user = userEvent.setup();
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Mini Grocery" })).toBeInTheDocument();
    await confirmLifecycle(user, "Archive");
    expect(await screen.findByText("Archived")).toBeInTheDocument();
  });

  it("transitions Archived to Active", async () => {
    stubDesktop();
    installGlobalCatalogBusinessTypeMock();
    window.history.replaceState({}, "", `/admin/global-catalog/business-types/${ARCHIVED_ID}`);
    const user = userEvent.setup();
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Bakery" })).toBeInTheDocument();
    await confirmLifecycle(user, "Activate");
    expect(await screen.findByText("Active")).toBeInTheDocument();
  });

  it("transitions Archived to Inactive", async () => {
    stubDesktop();
    installGlobalCatalogBusinessTypeMock();
    window.history.replaceState({}, "", `/admin/global-catalog/business-types/${ARCHIVED_ID}`);
    const user = userEvent.setup();
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Bakery" })).toBeInTheDocument();
    await confirmLifecycle(user, "Deactivate");
    expect(await screen.findByText("Inactive")).toBeInTheDocument();
  });

  it("shows conflict detail on stale business type edit", async () => {
    stubDesktop();
    installGlobalCatalogBusinessTypeMock({ updateConflict: true });
    window.history.replaceState({}, "", `/admin/global-catalog/business-types/${ACTIVE_ID}/edit`);
    const user = userEvent.setup();
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Edit business type" })).toBeInTheDocument();
    const nameInput = screen.getByLabelText("Name");
    await user.clear(nameInput);
    await user.type(nameInput, "Stale Edit");
    await user.click(screen.getByRole("button", { name: "Save" }));
    expect(
      await screen.findByText("Business type was updated by another operator."),
    ).toBeInTheDocument();
  });

  it("shows conflict detail on stale business type status change", async () => {
    stubDesktop();
    installGlobalCatalogBusinessTypeMock({ statusConflict: true });
    window.history.replaceState({}, "", `/admin/global-catalog/business-types/${ACTIVE_ID}`);
    const user = userEvent.setup();
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Sari-Sari Store" })).toBeInTheDocument();
    await confirmLifecycle(user, "Deactivate");
    expect(
      await screen.findByText("Business type was updated by another operator."),
    ).toBeInTheDocument();
  });

  it("fail-closes pages without viewGlobalCatalog", async () => {
    stubDesktop();
    installGlobalCatalogBusinessTypeMock({
      permissions: sampleAuthorization.permissions.filter(
        (item) => item !== "platform.permission.view_global_catalog",
      ),
    });
    window.history.replaceState({}, "", "/admin/global-catalog/business-types");
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Access denied" })).toBeInTheDocument();
  });

  it("hides mutation controls without manage permissions", async () => {
    stubMobileListViewport();
    installGlobalCatalogBusinessTypeMock({
      permissions: ["platform.permission.view_portfolio", "platform.permission.view_global_catalog"],
    });
    window.history.replaceState({}, "", "/admin/global-catalog/business-types");
    const { unmount } = render(<App />);
    expect(await screen.findByRole("heading", { name: "Business Types" })).toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Create business type" })).not.toBeInTheDocument();
    unmount();
    window.history.replaceState({}, "", `/admin/global-catalog/business-types/${ACTIVE_ID}`);
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Sari-Sari Store" })).toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Edit" })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Deactivate" })).not.toBeInTheDocument();
  });

  it("sends CSRF header on business type mutation", async () => {
    stubDesktop();
    const { mutationHeaders } = installGlobalCatalogBusinessTypeMock();
    window.history.replaceState({}, "", "/admin/global-catalog/business-types/new");
    const user = userEvent.setup();
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Create business type" })).toBeInTheDocument();
    await user.type(screen.getByLabelText("Code"), "cafe");
    await user.type(screen.getByLabelText("Name"), "Cafe");
    await user.click(screen.getByRole("button", { name: "Save" }));
    await waitFor(() => {
      expect(
        mutationHeaders.some((headers) => headers.get("X-XSRF-TOKEN") === "test-antiforgery-token"),
      ).toBe(true);
    });
  });

  it("shows only active business types in category and product pickers", async () => {
    stubDesktop();
    installGlobalCatalogBusinessTypeMock();
    window.history.replaceState({}, "", "/admin/global-catalog/categories/new");
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Create category" })).toBeInTheDocument();
    expect(await screen.findByText("Sari-Sari Store")).toBeInTheDocument();
    expect(screen.queryByText("Mini Grocery")).not.toBeInTheDocument();
    expect(screen.queryByText("Bakery")).not.toBeInTheDocument();

    window.history.replaceState({}, "", "/admin/global-catalog/products/new");
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Create product" })).toBeInTheDocument();
    expect(await screen.findByText("Sari-Sari Store")).toBeInTheDocument();
    expect(screen.queryByText("Mini Grocery")).not.toBeInTheDocument();
    expect(screen.queryByText("Bakery")).not.toBeInTheDocument();
  });

  it("renders English labels", async () => {
    stubMobileListViewport();
    installGlobalCatalogBusinessTypeMock();
    window.history.replaceState({}, "", "/admin/global-catalog/business-types");
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Business Types" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Create business type" })).toBeInTheDocument();
  });

  it("renders Filipino labels when language preference is fil-PH", async () => {
    stubMobileListViewport();
    setFilipinoLanguage();
    installGlobalCatalogBusinessTypeMock();
    window.history.replaceState({}, "", "/admin/global-catalog/business-types");
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Mga Uri ng Negosyo" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Gumawa ng uri ng negosyo" })).toBeInTheDocument();
  });
});
