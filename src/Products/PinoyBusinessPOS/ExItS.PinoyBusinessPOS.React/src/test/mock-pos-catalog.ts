export const MOCK_DRINKS_CATEGORY_ID = "dddddddd-dddd-dddd-dddd-dddddddddddd";
export const MOCK_SNACKS_CATEGORY_ID = "ssssssss-ssss-ssss-ssss-ssssssssssss";
export const MOCK_STAPLES_CATEGORY_ID = "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee";
export const MOCK_COKE_PRODUCT_ID = "cccccccc-cccc-cccc-cccc-cccccccccccc";
export const MOCK_CHIPS_PRODUCT_ID = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";
export const MOCK_RICE_PRODUCT_ID = "rrrrrrrr-rrrr-rrrr-rrrr-rrrrrrrrrrrr";
export const MOCK_MEAT_PRODUCT_ID = "mmmmmmmm-mmmm-mmmm-mmmm-mmmmmmmmmmmm";
export const MOCK_BOTTLE_PRODUCT_ID = "bbbbbbbb-1111-4111-8111-bbbbbbbbbbbb";
export const MOCK_BOTTLE_UNIT_ID = "ubububub-ubub-ubub-ubub-ubububububub";
export const MOCK_OIL_PRODUCT_ID = "oooooooo-oooo-oooo-oooo-oooooooooooo";
export const MOCK_OIL_LITER_UNIT_ID = "ulululul-ulul-ulul-ulul-ulululululul";
export const MOCK_RICE_KG_UNIT_ID = "ukukukuk-ukuk-ukuk-ukuk-ukukukukukuk";
export const MOCK_RICE_SACK_UNIT_ID = "usususus-usus-usus-usus-usususususus";
export const MOCK_OOS_PRODUCT_ID = "aaaaaaaa-0000-4000-8000-aaaaaaaaaaaa";

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
    {
      categoryId: MOCK_STAPLES_CATEGORY_ID,
      organizationId: "11111111-1111-1111-1111-111111111111",
      name: "Staples",
      status: "Active",
      createdAtUtc: "2026-01-01T00:00:00Z",
      updatedAtUtc: "2026-01-01T00:00:00Z",
    },
  ],
  totalCount: 3,
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
  unitOfMeasure: "Bottle",
  sellingMode: "PerItem",
  sellingPrice: 25,
  status: "Active",
  canBeSold: true,
  isTracked: true,
  onHandQuantity: 48,
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
  unitOfMeasure: "Pack",
  sellingMode: "PerItem",
  sellingPrice: 15,
  status: "Active",
  canBeSold: true,
  createdAtUtc: "2026-01-01T00:00:00Z",
  updatedAtUtc: "2026-01-01T00:00:00Z",
};

export const mockRiceProduct = {
  productId: MOCK_RICE_PRODUCT_ID,
  organizationId: "11111111-1111-1111-1111-111111111111",
  name: "Rice",
  sku: "RICE-50",
  barcode: "4800012345678",
  categoryId: MOCK_STAPLES_CATEGORY_ID,
  unitOfMeasure: "Kilogram",
  sellingMode: "PerItem",
  sellingPrice: 55,
  status: "Active",
  canBeSold: true,
  isTracked: true,
  onHandQuantity: 500,
  createdAtUtc: "2026-01-01T00:00:00Z",
  updatedAtUtc: "2026-01-01T00:00:00Z",
  units: [
    {
      unitId: MOCK_RICE_KG_UNIT_ID,
      productId: MOCK_RICE_PRODUCT_ID,
      kind: "Sell",
      displayName: "Kilogram",
      shortLabel: "kg",
      multiplierToBase: 1,
      sellingPrice: 55,
      allowsCustomQuantity: false,
      isActive: true,
      sortOrder: 0,
    },
    {
      unitId: MOCK_RICE_SACK_UNIT_ID,
      productId: MOCK_RICE_PRODUCT_ID,
      kind: "Sell",
      displayName: "Sack 50kg",
      shortLabel: "sack",
      multiplierToBase: 50,
      sellingPrice: 2600,
      allowsCustomQuantity: false,
      isActive: true,
      sortOrder: 1,
    },
  ],
};

export const mockMeatProduct = {
  productId: MOCK_MEAT_PRODUCT_ID,
  organizationId: "11111111-1111-1111-1111-111111111111",
  name: "Ground Pork",
  sku: "PORK-GW",
  barcode: "4800098765432",
  categoryId: MOCK_STAPLES_CATEGORY_ID,
  unitOfMeasure: "Kilogram",
  sellingMode: "ByWeight",
  sellingPrice: 60,
  status: "Active",
  canBeSold: true,
  isTracked: true,
  onHandQuantity: 12.5,
  tracksExpiration: true,
  createdAtUtc: "2026-01-01T00:00:00Z",
  updatedAtUtc: "2026-01-01T00:00:00Z",
};

export const mockBottleProduct = {
  productId: MOCK_BOTTLE_PRODUCT_ID,
  organizationId: "11111111-1111-1111-1111-111111111111",
  name: "Premium Bottle",
  sku: "BTL-95",
  barcode: "4800099999999",
  categoryId: MOCK_DRINKS_CATEGORY_ID,
  unitOfMeasure: "Piece",
  sellingMode: "PerItem",
  sellingPrice: 100,
  status: "Active",
  canBeSold: true,
  createdAtUtc: "2026-01-01T00:00:00Z",
  updatedAtUtc: "2026-01-01T00:00:00Z",
  units: [
    {
      unitId: MOCK_BOTTLE_UNIT_ID,
      productId: MOCK_BOTTLE_PRODUCT_ID,
      kind: "Sell",
      displayName: "Bottle",
      shortLabel: "btl",
      multiplierToBase: 1,
      sellingPrice: 95,
      allowsCustomQuantity: false,
      isActive: true,
      sortOrder: 0,
    },
  ],
};

export const mockOilProduct = {
  productId: MOCK_OIL_PRODUCT_ID,
  organizationId: "11111111-1111-1111-1111-111111111111",
  name: "Cooking Oil",
  sku: "OIL-1L",
  barcode: "4800088888888",
  categoryId: MOCK_STAPLES_CATEGORY_ID,
  unitOfMeasure: "Liter",
  sellingMode: "PerItem",
  sellingPrice: 80,
  status: "Active",
  canBeSold: true,
  createdAtUtc: "2026-01-01T00:00:00Z",
  updatedAtUtc: "2026-01-01T00:00:00Z",
  units: [
    {
      unitId: MOCK_OIL_LITER_UNIT_ID,
      productId: MOCK_OIL_PRODUCT_ID,
      kind: "Sell",
      displayName: "Liter",
      shortLabel: "L",
      multiplierToBase: 1,
      sellingPrice: 80,
      allowsCustomQuantity: true,
      isActive: true,
      sortOrder: 0,
    },
  ],
};

export const mockOutOfStockProduct = {
  productId: MOCK_OOS_PRODUCT_ID,
  organizationId: "11111111-1111-1111-1111-111111111111",
  name: "Sold Out Juice",
  sku: "JUICE-OOS",
  barcode: "4800077777777",
  categoryId: MOCK_DRINKS_CATEGORY_ID,
  unitOfMeasure: "Bottle",
  sellingMode: "PerItem",
  sellingPrice: 20,
  status: "Active",
  canBeSold: true,
  isTracked: true,
  onHandQuantity: 0,
  stockStatus: "OutOfStock",
  createdAtUtc: "2026-01-01T00:00:00Z",
  updatedAtUtc: "2026-01-01T00:00:00Z",
};

export const mockCatalogProducts = [
  mockCokeProduct,
  mockChipsProduct,
  mockRiceProduct,
  mockMeatProduct,
  mockBottleProduct,
  mockOilProduct,
  mockOutOfStockProduct,
];

export function filterMockProducts(url: string) {
  const parsed = new URL(url, "http://127.0.0.1");
  const categoryId = parsed.searchParams.get("categoryId");
  const brandId = parsed.searchParams.get("brandId");
  const search = parsed.searchParams.get("search")?.toLowerCase();

  let items = [...mockCatalogProducts];
  if (categoryId) {
    items = items.filter((product) => product.categoryId === categoryId);
  }
  if (brandId) {
    items = items.filter((product) => (product as { brandId?: string }).brandId === brandId);
  }
  if (search) {
    items = items.filter(
      (product) =>
        product.name.toLowerCase().includes(search) ||
        product.sku?.toLowerCase().includes(search) ||
        (product as { brandName?: string }).brandName?.toLowerCase().includes(search),
    );
  }

  return {
    items,
    totalCount: items.length,
    page: 1,
    pageSize: 24,
  };
}
