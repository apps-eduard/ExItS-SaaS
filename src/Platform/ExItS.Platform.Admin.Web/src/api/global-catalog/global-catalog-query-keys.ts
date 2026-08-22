export const globalCatalogQueryKeys = {
  businessTypes: {
    all: ["global-catalog", "business-types"] as const,
    lookup: ["global-catalog", "business-types", "lookup"] as const,
    list: (query: unknown) => ["global-catalog", "business-types", "list", query] as const,
    detail: (businessTypeId: string) =>
      ["global-catalog", "business-types", "detail", businessTypeId] as const,
  },
  categories: {
    all: ["global-catalog", "categories"] as const,
    list: (query: unknown) => ["global-catalog", "categories", "list", query] as const,
    detail: (categoryId: string) => ["global-catalog", "categories", "detail", categoryId] as const,
    lookup: ["global-catalog", "categories", "lookup"] as const,
  },
  products: {
    all: ["global-catalog", "products"] as const,
    list: (query: unknown) => ["global-catalog", "products", "list", query] as const,
    detail: (productId: string) => ["global-catalog", "products", "detail", productId] as const,
  },
} as const;
