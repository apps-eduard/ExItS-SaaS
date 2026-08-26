export const accessKeys = {
  organizations: ["plm", "organizations"] as const,
  effective: (productCode: string, organizationId: string | null) =>
    ["plm", "product-access", productCode, organizationId] as const,
};
