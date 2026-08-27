import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { I18nProvider } from "@/i18n/I18nProvider";
import { PreferencesProvider } from "@/hooks/usePreferences";
import type { PlatformMerchantCatalogTemplate } from "@/api/platform/merchant-catalog-types";
import type { PosCatalogImportJob, PosTemplateImportStatus } from "@/api/pos/pos-catalog-import-types";

const ORG_ID = "99999999-9999-4999-8999-999999999999";
const BRANCH_ID = "88888888-8888-4888-8888-888888888888";
const TEMPLATE_ID = "tmpl-1";
const PRODUCT_A = "gp-a";
const PRODUCT_B = "gp-b";
const JOB_ID = "job-1";

const listPublishedTemplates = vi.fn();
const getPublishedTemplate = vi.fn();
const searchActiveGlobalProducts = vi.fn();
const listActiveGlobalCategories = vi.fn();
const getTemplateImportStatus = vi.fn();
const listImportedGlobalProducts = vi.fn();
const importTemplateBatch = vi.fn();
const importTemplateNextBatch = vi.fn();
const importSelectedGlobalProducts = vi.fn();
const getCatalogImportJob = vi.fn();

let online = true;

vi.mock("@/connectivity/browser-online", () => ({
  useBrowserOnline: () => online,
  subscribeBrowserOnline: (onChange: (online: boolean) => void) => {
    onChange(online);
    return () => undefined;
  },
}));

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => ({
    boundWorkspace: {
      organizationId: ORG_ID,
      organizationDisplayName: "Sari Store",
      branchId: BRANCH_ID,
      branchName: "Main branch",
      experience: "managing",
    },
  }),
}));

vi.mock("@/api/platform/merchant-catalog-client", () => ({
  listPublishedTemplates: (...args: unknown[]) => listPublishedTemplates(...args),
  getPublishedTemplate: (...args: unknown[]) => getPublishedTemplate(...args),
  searchActiveGlobalProducts: (...args: unknown[]) => searchActiveGlobalProducts(...args),
  listActiveGlobalCategories: (...args: unknown[]) => listActiveGlobalCategories(...args),
  globalProductImageUrl: (id: string) => `/platform-api/image/${id}`,
}));

vi.mock("@/api/pos/pos-catalog-import-client", () => ({
  getTemplateImportStatus: (...args: unknown[]) => getTemplateImportStatus(...args),
  listImportedGlobalProducts: (...args: unknown[]) => listImportedGlobalProducts(...args),
  importTemplateBatch: (...args: unknown[]) => importTemplateBatch(...args),
  importTemplateNextBatch: (...args: unknown[]) => importTemplateNextBatch(...args),
  importSelectedGlobalProducts: (...args: unknown[]) => importSelectedGlobalProducts(...args),
  getCatalogImportJob: (...args: unknown[]) => getCatalogImportJob(...args),
}));

const { CatalogTemplateImportPage } = await import("@/features/catalog/CatalogTemplateImportPage");
const { CatalogGlobalBrowsePage } = await import("@/features/catalog/CatalogGlobalBrowsePage");
const { CatalogImportJobPage } = await import("@/features/catalog/CatalogImportJobPage");

function templateStatus(overrides: Partial<PosTemplateImportStatus> = {}): PosTemplateImportStatus {
  return {
    platformTemplateId: TEMPLATE_ID,
    firstBatchTotal: 2,
    firstBatchImportedCount: 0,
    firstBatchComplete: false,
    subsequentTotal: 0,
    subsequentImportedCount: 0,
    subsequentRemainingCount: 0,
    hasSubsequentBatches: false,
    canImportFirstBatch: true,
    canImportNextBatch: false,
    suggestedNextBatchNumber: 1,
    nextBatchSizeEstimate: 2,
    defaultBatchSize: 50,
    ...overrides,
  };
}

function templateDetail(): PlatformMerchantCatalogTemplate {
  return {
    id: TEMPLATE_ID,
    name: "Sari-Sari Starter",
    slug: "sari-sari-starter",
    description: "Everyday goods",
    primaryBusinessType: "SariSari",
    primaryBusinessTypeId: "bt-1",
    status: "Published",
    defaultBatchSize: 50,
    selectionMode: "Batch",
    productCount: 2,
    firstBatchCount: 2,
    createdAtUtc: "2026-01-01T00:00:00Z",
    updatedAtUtc: "2026-01-01T00:00:00Z",
    products: [
      {
        id: "tp-1",
        globalProductId: PRODUCT_A,
        sortOrder: 1,
        isFeatured: true,
        isFirstBatch: true,
        productName: "Rice 1kg",
        categoryName: "Staples",
        unit: "kg",
        brand: "Local",
        sellingPrice: 50,
      },
      {
        id: "tp-2",
        globalProductId: PRODUCT_B,
        sortOrder: 2,
        isFeatured: false,
        isFirstBatch: true,
        productName: "Cooking Oil",
        categoryName: "Staples",
        unit: "bottle",
        brand: "BrandX",
        sellingPrice: 80,
      },
    ],
  };
}

function job(overrides: Partial<PosCatalogImportJob> = {}): PosCatalogImportJob {
  return {
    jobId: JOB_ID,
    organizationId: ORG_ID,
    jobKind: "TemplateBatch",
    platformTemplateId: TEMPLATE_ID,
    batchNumber: 1,
    catalogSource: "Template",
    status: "Running",
    totalCount: 2,
    processedCount: 1,
    importedCount: 1,
    skippedCount: 0,
    failedCount: 0,
    createdAtUtc: "2026-08-23T00:00:00Z",
    updatedAtUtc: "2026-08-23T00:01:00Z",
    ...overrides,
  };
}

function renderApp(path: string) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return render(
    <MemoryRouter initialEntries={[path]}>
      <QueryClientProvider client={queryClient}>
        <PreferencesProvider>
          <I18nProvider>
            <Routes>
              <Route path="/catalog/templates" element={<CatalogTemplateImportPage />} />
              <Route path="/catalog/global-catalog" element={<CatalogGlobalBrowsePage />} />
              <Route path="/catalog/import-jobs/:jobId" element={<CatalogImportJobPage />} />
              <Route path="/catalog" element={<div>products-home</div>} />
            </Routes>
          </I18nProvider>
        </PreferencesProvider>
      </QueryClientProvider>
    </MemoryRouter>,
  );
}

describe("CatalogTemplateImportPage", () => {
  beforeEach(() => {
    online = true;
    vi.clearAllMocks();
    listPublishedTemplates.mockResolvedValue({
      items: [
        {
          id: TEMPLATE_ID,
          name: "Sari-Sari Starter",
          slug: "sari-sari-starter",
          description: "Everyday goods",
          primaryBusinessType: "SariSari",
          primaryBusinessTypeId: "bt-1",
          status: "Published",
          defaultBatchSize: 50,
          selectionMode: "Batch",
          productCount: 2,
          firstBatchCount: 2,
          createdAtUtc: "2026-01-01T00:00:00Z",
          updatedAtUtc: "2026-01-01T00:00:00Z",
        },
      ],
      totalCount: 1,
      page: 1,
      pageSize: 40,
    });
    getTemplateImportStatus.mockResolvedValue(templateStatus());
    getPublishedTemplate.mockResolvedValue(templateDetail());
    listImportedGlobalProducts.mockResolvedValue({ importedIds: [PRODUCT_B] });
    importTemplateBatch.mockResolvedValue(job({ status: "Queued", processedCount: 0, importedCount: 0 }));
    getCatalogImportJob.mockResolvedValue(job({ status: "Queued", processedCount: 0, importedCount: 0 }));
  });

  it("lists templates and supports search", async () => {
    const user = userEvent.setup();
    renderApp("/catalog/templates");
    expect(await screen.findByTestId("catalog-template-choose")).toBeInTheDocument();
    expect(await screen.findByText("Sari-Sari Starter")).toBeInTheDocument();
    expect(screen.getByText(/Ready for first batch/i)).toBeInTheDocument();

    await user.type(screen.getByLabelText(/Search templates/i), "sari");
    await waitFor(() => {
      expect(listPublishedTemplates).toHaveBeenCalledWith(
        expect.objectContaining({ search: "sari" }),
        expect.anything(),
      );
    });
  });

  it("walks choose → preview → confirm and requires confirmation before start", async () => {
    const user = userEvent.setup();
    renderApp("/catalog/templates");
    await user.click(await screen.findByTestId(`catalog-template-select-${TEMPLATE_ID}`));
    expect(await screen.findByTestId("catalog-template-preview")).toBeInTheDocument();
    expect(screen.getByText("Rice 1kg")).toBeInTheDocument();
    expect(screen.getByText("Already added")).toBeInTheDocument();

    await user.click(screen.getByTestId("catalog-template-continue-confirm"));
    expect(await screen.findByTestId("catalog-template-confirm")).toBeInTheDocument();

    const start = screen.getByTestId("catalog-template-start-import");
    expect(start).toBeDisabled();
    await user.click(screen.getByTestId("catalog-template-confirm-checkbox"));
    expect(start).toBeEnabled();
    await user.click(start);

    await waitFor(() => {
      expect(importTemplateBatch).toHaveBeenCalledWith(
        expect.objectContaining({ organizationId: ORG_ID, branchId: BRANCH_ID }),
        expect.objectContaining({ platformTemplateId: TEMPLATE_ID, batchNumber: 1 }),
      );
    });
    expect(await screen.findByTestId("catalog-import-job-page")).toBeInTheDocument();
  });

  it("shows online-required when offline", async () => {
    online = false;
    renderApp("/catalog/templates");
    expect(await screen.findByTestId("online-required")).toBeInTheDocument();
    expect(screen.getByText(/need internet/i)).toBeInTheDocument();
  });

  it("shows API error on the choose step", async () => {
    listPublishedTemplates.mockRejectedValue(new Error("templates unavailable"));
    renderApp("/catalog/templates");
    expect(await screen.findByText("templates unavailable")).toBeInTheDocument();
  });
});

describe("CatalogImportJobPage", () => {
  beforeEach(() => {
    online = true;
    vi.clearAllMocks();
    getCatalogImportJob.mockResolvedValue(job());
  });

  it("shows progress counts and keeps non-terminal status", async () => {
    renderApp(`/catalog/import-jobs/${JOB_ID}`);
    const card = await screen.findByTestId("catalog-import-job-card");
    expect(within(card).getByText("Running")).toBeInTheDocument();
    expect(within(card).getByText("1/2")).toBeInTheDocument();
    expect(screen.getByText(/may continue in the background/i)).toBeInTheDocument();
  });
});

describe("CatalogGlobalBrowsePage", () => {
  beforeEach(() => {
    online = true;
    vi.clearAllMocks();
    listActiveGlobalCategories.mockResolvedValue({
      items: [{ id: "cat-1", name: "Staples", sortOrder: 1, status: "Active", createdAtUtc: "", updatedAtUtc: "" }],
      totalCount: 1,
      page: 1,
      pageSize: 100,
    });
    searchActiveGlobalProducts.mockResolvedValue({
      items: [
        {
          id: PRODUCT_A,
          name: "Rice 1kg",
          sku: "RICE-1",
          barcode: "111",
          unit: "kg",
          sellingMode: "PerItem",
          globalCategoryId: "cat-1",
          hasImage: false,
        },
        {
          id: PRODUCT_B,
          name: "Cooking Oil",
          sku: "OIL-1",
          barcode: "222",
          unit: "bottle",
          sellingMode: "PerItem",
          globalCategoryId: "cat-1",
          hasImage: false,
        },
      ],
      totalCount: 2,
      page: 1,
      pageSize: 40,
    });
    listImportedGlobalProducts.mockResolvedValue({ importedIds: [PRODUCT_B] });
    importSelectedGlobalProducts.mockResolvedValue(job({ jobKind: "SelectedProducts", status: "Queued" }));
    getCatalogImportJob.mockResolvedValue(job({ jobKind: "SelectedProducts", status: "Queued" }));
  });

  it("searches, multi-selects new products, and blocks already-added duplicates", async () => {
    const user = userEvent.setup();
    renderApp("/catalog/global-catalog");
    expect(await screen.findByText("Rice 1kg")).toBeInTheDocument();
    expect(await screen.findByText("Already added")).toBeInTheDocument();
    expect(screen.queryByTestId(`catalog-global-select-${PRODUCT_B}`)).not.toBeInTheDocument();

    await user.type(screen.getByLabelText(/Search global products/i), "rice");
    await waitFor(() => {
      expect(searchActiveGlobalProducts).toHaveBeenCalledWith(
        expect.objectContaining({ search: "rice" }),
        expect.anything(),
      );
    });

    await user.click(screen.getByTestId(`catalog-global-select-${PRODUCT_A}`));
    const importButton = screen.getByTestId("catalog-global-import");
    expect(importButton).toBeEnabled();
    await user.click(importButton);

    await waitFor(() => {
      expect(importSelectedGlobalProducts).toHaveBeenCalledWith(
        expect.objectContaining({ organizationId: ORG_ID }),
        expect.objectContaining({ platformGlobalProductIds: [PRODUCT_A] }),
      );
    });
    expect(await screen.findByTestId("catalog-import-job-page")).toBeInTheDocument();
  });

  it("shows online-required when offline", async () => {
    online = false;
    renderApp("/catalog/global-catalog");
    expect(await screen.findByTestId("online-required")).toBeInTheDocument();
  });

  it("shows API error from search", async () => {
    searchActiveGlobalProducts.mockRejectedValue(new Error("search failed"));
    renderApp("/catalog/global-catalog");
    expect(await screen.findByText("search failed")).toBeInTheDocument();
  });
});

describe("catalog import responsive basics", () => {
  it("template page uses card list without table markup", async () => {
    online = true;
    listPublishedTemplates.mockResolvedValue({
      items: [
        {
          id: TEMPLATE_ID,
          name: "Sari-Sari Starter",
          slug: "sari",
          primaryBusinessType: "SariSari",
          primaryBusinessTypeId: "bt-1",
          status: "Published",
          defaultBatchSize: 50,
          selectionMode: "Batch",
          productCount: 2,
          firstBatchCount: 2,
          createdAtUtc: "2026-01-01T00:00:00Z",
          updatedAtUtc: "2026-01-01T00:00:00Z",
        },
      ],
      totalCount: 1,
      page: 1,
      pageSize: 40,
    });
    getTemplateImportStatus.mockResolvedValue(templateStatus());
    renderApp("/catalog/templates");
    expect(await screen.findByTestId("catalog-templates-page")).toBeInTheDocument();
    expect(document.querySelector("table")).toBeNull();
  });
});
