export const MOCK_DRINKS_CATEGORY_ID = "dddddddd-dddd-dddd-dddd-dddddddddddd";
export const MOCK_SNACKS_CATEGORY_ID = "ssssssss-ssss-ssss-ssss-ssssssssssss";
export const MOCK_COKE_PRODUCT_ID = "cccccccc-cccc-cccc-cccc-cccccccccccc";
export const MOCK_CHIPS_PRODUCT_ID = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";

export const mockCatalogCategories = {
  items: [
    {
      categoryId: MOCK_DRINKS_CATEGORY_ID,
      organizationId: "11111111-1111-1111-1111-111111111111",
      name: "Drinks",
      status: "Active",
      createdAtUtc: "2026-01-01T00:00:00Z",
      updatedAtUtc: "2026-01-01T00:00:00Z",
    },
    {
      categoryId: MOCK_SNACKS_CATEGORY_ID,
      organizationId: "11111111-1111-1111-1111-111111111111",
      name: "Snacks",
      status: "Active",
      createdAtUtc: "2026-01-01T00:00:00Z",
      updatedAtUtc: "2026-01-01T00:00:00Z",
    },
  ],
  totalCount: 2,
  page: 1,
  pageSize: 50,
};

export const mockCokeProduct = {
  productId: MOCK_COKE_PRODUCT_ID,
  organizationId: "11111111-1111-1111-1111-111111111111",
  name: "Coke 330ml",
  sku: "COKE-330",
  barcode: "4006381333931",
  categoryId: MOCK_DRINKS_CATEGORY_ID,
  unitOfMeasure: "bottle",
  sellingMode: "Unit",
  sellingPrice: 25,
  status: "Active",
  canBeSold: true,
  createdAtUtc: "2026-01-01T00:00:00Z",
  updatedAtUtc: "2026-01-01T00:00:00Z",
};

export const mockChipsProduct = {
  productId: MOCK_CHIPS_PRODUCT_ID,
  organizationId: "11111111-1111-1111-1111-111111111111",
  name: "Potato Chips",
  sku: "CHIPS-50",
  barcode: "1234567890123",
  categoryId: MOCK_SNACKS_CATEGORY_ID,
  unitOfMeasure: "pack",
  sellingMode: "Unit",
  sellingPrice: 15,
  status: "Active",
  canBeSold: true,
  createdAtUtc: "2026-01-01T00:00:00Z",
  updatedAtUtc: "2026-01-01T00:00:00Z",
};

export const mockCatalogProducts = [mockCokeProduct, mockChipsProduct];

export function filterMockProducts(url: string) {
  const parsed = new URL(url, "http://127.0.0.1");
  const categoryId = parsed.searchParams.get("categoryId");
  const search = parsed.searchParams.get("search")?.toLowerCase();

  let items = [...mockCatalogProducts];
  if (categoryId) {
    items = items.filter((product) => product.categoryId === categoryId);
  }
  if (search) {
    items = items.filter(
      (product) =>
        product.name.toLowerCase().includes(search) || product.sku?.toLowerCase().includes(search),
    );
  }

  return {
    items,
    totalCount: items.length,
    page: 1,
    pageSize: 24,
  };
}
