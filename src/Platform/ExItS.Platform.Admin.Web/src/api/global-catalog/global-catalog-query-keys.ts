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
  imports: {
    all: ["global-catalog", "imports"] as const,
    list: (query: unknown) => ["global-catalog", "imports", "list", query] as const,
    detail: (jobId: string) => ["global-catalog", "imports", "detail", jobId] as const,
    errors: (jobId: string, query: unknown) =>
      ["global-catalog", "imports", "errors", jobId, query] as const,
  },
  templates: {
    all: ["global-catalog", "templates"] as const,
    list: (query: unknown) => ["global-catalog", "templates", "list", query] as const,
    detail: (templateId: string) => ["global-catalog", "templates", "detail", templateId] as const,
    availableProducts: (templateId: string, query: unknown) =>
      ["global-catalog", "templates", "available-products", templateId, query] as const,
  },
} as const;
