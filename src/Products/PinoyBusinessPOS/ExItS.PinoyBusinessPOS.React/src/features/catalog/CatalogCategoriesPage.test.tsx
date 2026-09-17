import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import * as catalogClient from "@/api/pos/pos-catalog-client";
import type { PosProductCategoryDto } from "@/api/pos/pos-catalog-types";
import { CatalogCategoriesPage } from "@/features/catalog/CatalogCategoriesPage";

const orgId = "11111111-1111-1111-1111-111111111111";
const branchId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
const categoryId = "cccccccc-cccc-cccc-cccc-cccccccccccc";

const workspaceMock = {
  boundWorkspace: {
    organizationId: orgId,
    organizationDisplayName: "Kizy Store",
    branchId,
    branchName: "Main Branch",
    experience: "operations" as const,
  },
  sessionGrant: {
    productAccessAllowed: true,
    membershipRole: "OrganizationOwner",
    productLocalRoleCode: "Owner",
    mappedPosRoleCode: "Owner",
  } as Record<string, unknown>,
};

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => workspaceMock,
}));

vi.mock("@/connectivity/browser-online", () => ({
  useBrowserOnline: () => true,
  subscribeBrowserOnline: (onChange: (online: boolean) => void) => {
    onChange(true);
    return () => undefined;
  },
}));

function activeCategory(overrides: Partial<PosProductCategoryDto> = {}): PosProductCategoryDto {
  return {
    categoryId,
    organizationId: orgId,
    name: "Snacks",
    status: "Active",
    createdAtUtc: "2026-08-01T00:00:00Z",
    updatedAtUtc: "2026-08-01T00:00:00Z",
    ...overrides,
  };
}

describe("CatalogCategoriesPage Brands-pattern UI", () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    workspaceMock.sessionGrant = {
      productAccessAllowed: true,
      membershipRole: "OrganizationOwner",
      productLocalRoleCode: "Owner",
      mappedPosRoleCode: "Owner",
    };
  });

  it("matches Brands structure: quick add, search, status chips, rename/deactivate", async () => {
    let categories = [activeCategory()];
    vi.spyOn(catalogClient, "listCatalogCategories").mockImplementation(async () => ({
      items: categories,
      totalCount: categories.length,
      page: 1,
      pageSize: 100,
    }));
    const createSpy = vi
      .spyOn(catalogClient, "createCatalogCategory")
      .mockImplementation(async (_w, body) => {
        const created = activeCategory({
          categoryId: "ffffffff-ffff-ffff-ffff-ffffffffffff",
          name: body.name,
        });
        categories = [...categories, created];
        return created;
      });
    const updateSpy = vi
      .spyOn(catalogClient, "updateCatalogCategory")
      .mockImplementation(async (_w, id, body) => {
        categories = categories.map((c) =>
          c.categoryId === id
            ? { ...c, name: body.name, updatedAtUtc: "2026-08-29T11:00:00Z" }
            : c,
        );
        return categories.find((c) => c.categoryId === id)!;
      });
    const deactivateSpy = vi
      .spyOn(catalogClient, "deactivateCatalogCategory")
      .mockImplementation(async (_w, id) => {
        categories = categories.map((c) =>
          c.categoryId === id ? { ...c, status: "Inactive" } : c,
        );
        return categories.find((c) => c.categoryId === id)!;
      });
    const reactivateSpy = vi
      .spyOn(catalogClient, "reactivateCatalogCategory")
      .mockImplementation(async (_w, id) => {
        categories = categories.map((c) =>
          c.categoryId === id ? { ...c, status: "Active" } : c,
        );
        return categories.find((c) => c.categoryId === id)!;
      });

    const user = userEvent.setup();
    render(
      <AppProviders>
        <MemoryRouter initialEntries={["/catalog/categories"]}>
          <Routes>
            <Route path="/catalog/categories" element={<CatalogCategoriesPage />} />
          </Routes>
        </MemoryRouter>
      </AppProviders>,
    );

    await screen.findByTestId("catalog-categories-page");
    await screen.findByTestId(`catalog-category-row-${categoryId}`);

    // Brands-pattern markers
    expect(screen.getByTestId("catalog-category-create")).toBeInTheDocument();
    expect(screen.getByTestId("catalog-category-name")).toBeInTheDocument();
    expect(screen.getByTestId("catalog-category-create-submit")).toBeInTheDocument();
    expect(screen.getByTestId("catalog-categories-search")).toBeInTheDocument();
    expect(screen.getByTestId("catalog-category-status-filters")).toBeInTheDocument();
    expect(screen.getByTestId("catalog-category-status-Active")).toBeInTheDocument();
    expect(screen.getByTestId("catalog-category-status-Inactive")).toBeInTheDocument();
    expect(screen.getByTestId("catalog-category-status-all")).toBeInTheDocument();
    expect(screen.getByTestId("catalog-category-list")).toBeInTheDocument();
    expect(screen.getByTestId("catalog-categories-table")).toBeInTheDocument();
    expect(screen.getAllByTestId(`catalog-category-rename-${categoryId}`)[0]).toHaveTextContent(/rename/i);
    expect(screen.getAllByTestId(`catalog-category-deactivate-${categoryId}`)[0]).toHaveTextContent(
      /deactivate/i,
    );

    // Expense-clone markers must not return
    expect(screen.queryByTestId("catalog-category-status-filter")).not.toBeInTheDocument();
    expect(screen.queryByTestId(`catalog-category-edit-${categoryId}`)).not.toBeInTheDocument();

    await user.type(screen.getByTestId("catalog-category-name"), "Beverages");
    await user.click(screen.getByTestId("catalog-category-create-submit"));
    await waitFor(() => expect(createSpy).toHaveBeenCalled());

    // Prefer mobile card rename control when both list + table are in the DOM
    const renameButtons = screen.getAllByTestId(`catalog-category-rename-${categoryId}`);
    await user.click(renameButtons[0]!);
    const nameInput = screen.getAllByTestId(`catalog-category-rename-input-${categoryId}`)[0]!;
    await user.clear(nameInput);
    await user.type(nameInput, "Snacks & More");
    await user.click(screen.getAllByTestId(`catalog-category-rename-save-${categoryId}`)[0]!);
    await waitFor(() => {
      expect(updateSpy).toHaveBeenCalledWith(
        expect.anything(),
        categoryId,
        expect.objectContaining({
          name: "Snacks & More",
          expectedUpdatedAtUtc: "2026-08-01T00:00:00Z",
        }),
      );
    });

    vi.spyOn(window, "confirm").mockReturnValue(true);
    const deactivateButtons = screen.getAllByTestId(`catalog-category-deactivate-${categoryId}`);
    await user.click(deactivateButtons[0]!);
    await waitFor(() => expect(deactivateSpy).toHaveBeenCalledWith(expect.anything(), categoryId));

    const reactivateButtons = screen.getAllByTestId(`catalog-category-reactivate-${categoryId}`);
    await user.click(reactivateButtons[0]!);
    await waitFor(() => expect(reactivateSpy).toHaveBeenCalledWith(expect.anything(), categoryId));

    await user.click(screen.getByTestId("catalog-category-status-all"));
    await waitFor(() => {
      expect(catalogClient.listCatalogCategories).toHaveBeenCalledWith(
        expect.anything(),
        expect.objectContaining({ status: "" }),
        expect.anything(),
      );
    });

    // Table shell remains present for desktop layout
    expect(within(screen.getByTestId("catalog-categories-table")).getByRole("table")).toBeInTheDocument();
  });
});
