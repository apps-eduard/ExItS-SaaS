import { afterEach, describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { App } from "@/app/App";
import { clearPlatformAntiforgeryToken } from "@/api/platform-http";
import { mockAuthenticatedFetch, sampleAuthorization } from "@/test/auth-fixtures";
import { UI_PREFERENCES_STORAGE_KEY } from "@/lib/preferences/ui-preferences";
import { installGlobalCatalogBusinessTypeMock } from "@/features/global-catalog/global-catalog-test-fixtures";
import {
  COMPLETED_IMPORT_ID,
  COMPLETED_WARNINGS_IMPORT_ID,
  FAILED_IMPORT_ID,
  installGlobalCatalogImportMock,
  VALIDATED_IMPORT_ID,
  viewGlobalCatalogWithoutImportPermissions,
} from "@/features/global-catalog/global-catalog-import-test-fixtures";

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

function makeCsvFile(name = "products.csv", contents = "name,unit,sku,brand\nA,Piece,SKU-1,Brand") {
  return new File([contents], name, { type: "text/csv" });
}

describe("global catalog imports", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
    clearPlatformAntiforgeryToken();
    window.localStorage.removeItem(UI_PREFERENCES_STORAGE_KEY);
  });

  it("shows Imports navigation for importGlobalProducts permission", async () => {
    stubDesktop();
    installGlobalCatalogImportMock();
    window.history.replaceState({}, "", "/admin");
    render(<App />);
    expect(await screen.findByRole("link", { name: "Imports" })).toBeInTheDocument();
  });

  it("fail-closes imports route without importGlobalProducts permission", async () => {
    stubMobileListViewport();
    installGlobalCatalogImportMock({
      permissions: viewGlobalCatalogWithoutImportPermissions(),
    });
    window.history.replaceState({}, "", "/admin/global-catalog/imports");
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Access denied" })).toBeInTheDocument();
    expect(screen.queryByRole("heading", { name: "Imports" })).not.toBeInTheDocument();
  });

  it("loads the import jobs list query", async () => {
    stubMobileListViewport();
    const { fetchMock } = installGlobalCatalogImportMock();
    window.history.replaceState({}, "", "/admin/global-catalog/imports");
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Imports" })).toBeInTheDocument();
    expect(await screen.findByText("validated-import.csv")).toBeInTheDocument();
    await waitFor(() => {
      const urls = fetchMock.mock.calls.map(([input]) => String(input));
      expect(
        urls.some(
          (url) =>
            url.includes("/products/imports") &&
            url.includes("page=1") &&
            url.includes("pageSize=20"),
        ),
      ).toBe(true);
    });
  });

  it("maps status filter to server parameters", async () => {
    stubMobileListViewport();
    const { fetchMock } = installGlobalCatalogImportMock();
    window.history.replaceState({}, "", "/admin/global-catalog/imports");
    const user = userEvent.setup();
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Imports" })).toBeInTheDocument();
    await user.selectOptions(screen.getByLabelText("Status"), "Completed");
    await waitFor(() => {
      const urls = fetchMock.mock.calls.map(([input]) => String(input));
      expect(urls.some((url) => url.includes("status=Completed"))).toBe(true);
    });
  });

  it("paginates the import jobs list", async () => {
    stubDesktop();
    const manyJobs = Array.from({ length: 25 }, (_, index) => ({
      id: `99999999-9999-9999-9999-${String(index).padStart(12, "0")}`,
      fileName: `bulk-${index}.csv`,
      fileFormat: "Csv",
      fileSizeBytes: 100,
      fileSha256: "a".repeat(64),
      requestedBy: "olivia@example.test",
      status: "Completed" as const,
      totalCount: 1,
      processedCount: 1,
      importedCount: 1,
      skippedCount: 0,
      failedCount: 0,
      pendingCount: 0,
      validProductCount: 1,
      existingCategoriesReferencedCount: 1,
      newCategoriesToCreateCount: 0,
      warningCount: 0,
      createdAtUtc: "2026-08-01T08:00:00Z",
      updatedAtUtc: "2026-08-01T08:00:00Z",
    }));
    const { fetchMock } = installGlobalCatalogImportMock({ jobs: manyJobs });
    window.history.replaceState({}, "", "/admin/global-catalog/imports");
    const user = userEvent.setup();
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Imports" })).toBeInTheDocument();
    expect(await screen.findByText("bulk-0.csv")).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Next" }));
    await waitFor(() => {
      expect(window.location.search).toContain("page=2");
      const urls = fetchMock.mock.calls.map(([input]) => String(input));
      expect(urls.some((url) => url.includes("page=2"))).toBe(true);
    });
  });

  it("downloads the import template from the server endpoint", async () => {
    stubMobileListViewport();
    const { fetchMock } = installGlobalCatalogImportMock();
    window.history.replaceState({}, "", "/admin/global-catalog/imports");
    const user = userEvent.setup();
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Imports" })).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Download CSV template" }));
    await waitFor(() => {
      const urls = fetchMock.mock.calls.map(([input]) => String(input));
      expect(urls.some((url) => url.includes("/imports/template.csv"))).toBe(true);
    });
  });

  it("uploads a valid multipart file without antiforgery headers", async () => {
    stubMobileListViewport();
    const { fetchMock, uploadHeaders } = installGlobalCatalogImportMock();
    window.history.replaceState({}, "", "/admin/global-catalog/imports");
    const user = userEvent.setup();
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Imports" })).toBeInTheDocument();
    const fileInput = screen.getByLabelText("Import file");
    await user.upload(fileInput, makeCsvFile());
    await user.click(screen.getByRole("button", { name: "Upload and validate" }));
    await waitFor(() => {
      expect(window.location.pathname).toMatch(/\/admin\/global-catalog\/imports\//);
    });
    const uploadCall = fetchMock.mock.calls.find(
      ([input, init]) =>
        String(input).includes("/products/imports") && (init?.method ?? "GET") === "POST",
    );
    expect(uploadCall).toBeTruthy();
    expect(uploadHeaders.some((headers) => headers.get("x-xsrf-token"))).toBe(false);
  });

  it("supports optional idempotency key on upload", async () => {
    stubMobileListViewport();
    const { fetchMock } = installGlobalCatalogImportMock();
    window.history.replaceState({}, "", "/admin/global-catalog/imports");
    const user = userEvent.setup();
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Imports" })).toBeInTheDocument();
    await user.type(screen.getByLabelText("Idempotency key (optional)"), "upload-key-1");
    await user.upload(screen.getByLabelText("Import file"), makeCsvFile());
    await user.click(screen.getByRole("button", { name: "Upload and validate" }));
    await waitFor(() => {
      expect(window.location.pathname).toMatch(/\/admin\/global-catalog\/imports\//);
    });
    const uploadCalls = fetchMock.mock.calls.filter(
      ([input, init]) =>
        String(input).includes("/products/imports") && (init?.method ?? "GET") === "POST",
    );
    expect(uploadCalls.length).toBeGreaterThan(0);
  });

  it("shows client error for unsupported file types", async () => {
    stubMobileListViewport();
    installGlobalCatalogImportMock();
    window.history.replaceState({}, "", "/admin/global-catalog/imports");
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Imports" })).toBeInTheDocument();
    const fileInput = screen.getByLabelText("Import file");
    fireEvent.change(fileInput, {
      target: { files: [new File(["bad"], "notes.txt", { type: "text/plain" })] },
    });
    expect(
      await screen.findByText("Only .csv and .xlsx files are supported."),
    ).toBeInTheDocument();
  });

  it("shows client error for empty files", async () => {
    stubMobileListViewport();
    installGlobalCatalogImportMock();
    window.history.replaceState({}, "", "/admin/global-catalog/imports");
    const user = userEvent.setup();
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Imports" })).toBeInTheDocument();
    await user.upload(screen.getByLabelText("Import file"), new File([], "empty.csv", { type: "text/csv" }));
    await user.click(screen.getByRole("button", { name: "Upload and validate" }));
    expect(await screen.findByText("The selected file is empty.")).toBeInTheDocument();
  });

  it("loads import detail preview and summary", async () => {
    stubMobileListViewport();
    installGlobalCatalogImportMock();
    window.history.replaceState({}, "", `/admin/global-catalog/imports/${VALIDATED_IMPORT_ID}`);
    render(<App />);
    expect(await screen.findByRole("heading", { name: "validated-import.csv" })).toBeInTheDocument();
    expect(await screen.findByText("2 valid products, 1 new category will be created.")).toBeInTheDocument();
    expect(await screen.findByText("Sardines")).toBeInTheDocument();
    expect(await screen.findByText("Noodles")).toBeInTheDocument();
  });

  it("confirms a validated import with CSRF transport", async () => {
    stubMobileListViewport();
    const { mutationHeaders } = installGlobalCatalogImportMock();
    window.history.replaceState({}, "", `/admin/global-catalog/imports/${VALIDATED_IMPORT_ID}`);
    const user = userEvent.setup();
    render(<App />);
    expect(await screen.findByRole("heading", { name: "validated-import.csv" })).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Confirm import" }));
    await user.click(within(screen.getByRole("dialog")).getByRole("button", { name: "Confirm import" }));
    await waitFor(() => {
      expect(mutationHeaders.some((headers) => headers.get("x-xsrf-token"))).toBe(true);
    });
  });

  it("loads paged import errors on failed jobs", async () => {
    stubMobileListViewport();
    const { fetchMock } = installGlobalCatalogImportMock();
    window.history.replaceState({}, "", `/admin/global-catalog/imports/${FAILED_IMPORT_ID}`);
    const user = userEvent.setup();
    render(<App />);
    expect(await screen.findByRole("heading", { name: "failed-import.csv" })).toBeInTheDocument();
    await waitFor(() => {
      const urls = fetchMock.mock.calls.map(([input]) => String(input));
      expect(urls.some((url) => url.includes(`/imports/${FAILED_IMPORT_ID}/errors`))).toBe(true);
    });
    expect(await screen.findByText("Failed Product 0")).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Next" }));
    await waitFor(() => {
      const urls = fetchMock.mock.calls.map(([input]) => String(input));
      expect(urls.some((url) => url.includes("/errors") && url.includes("page=2"))).toBe(true);
    });
  });

  it("renders completed import status", async () => {
    stubMobileListViewport();
    installGlobalCatalogImportMock();
    window.history.replaceState({}, "", `/admin/global-catalog/imports/${COMPLETED_IMPORT_ID}`);
    render(<App />);
    expect(await screen.findByRole("heading", { name: "completed-import.csv" })).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Confirm import" })).not.toBeInTheDocument();
  });

  it("renders completed with warnings import status", async () => {
    stubMobileListViewport();
    installGlobalCatalogImportMock();
    window.history.replaceState({}, "", `/admin/global-catalog/imports/${COMPLETED_WARNINGS_IMPORT_ID}`);
    render(<App />);
    expect(await screen.findByRole("heading", { name: "completed-warnings.csv" })).toBeInTheDocument();
    expect(await screen.findByText("Completed with warnings")).toBeInTheDocument();
    expect(await screen.findByText("1 row was skipped.")).toBeInTheDocument();
  });

  it("redirects legacy /admin/catalog/imports route", async () => {
    stubMobileListViewport();
    installGlobalCatalogImportMock();
    window.history.replaceState({}, "", "/admin/catalog/imports");
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Imports" })).toBeInTheDocument();
    expect(window.location.pathname).toBe("/admin/global-catalog/imports");
  });

  it("regresses GCAT-01 global products list", async () => {
    stubMobileListViewport();
    mockAuthenticatedFetch({ permissions: sampleAuthorization.permissions });
    window.history.replaceState({}, "", "/admin/global-catalog/products");
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Global Products" })).toBeInTheDocument();
  });

  it("regresses GCAT-02 business types list", async () => {
    stubMobileListViewport();
    installGlobalCatalogBusinessTypeMock({
      permissions: sampleAuthorization.permissions,
    });
    window.history.replaceState({}, "", "/admin/global-catalog/business-types");
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Business Types" })).toBeInTheDocument();
  });
});
