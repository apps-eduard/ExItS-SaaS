/** Document presentation audience — filters seller-private fields without mutating transaction data. */
export type DocumentAudience = "Seller" | "Customer";

export function isCustomerAudience(audience: DocumentAudience | undefined): boolean {
  return audience === "Customer";
}
