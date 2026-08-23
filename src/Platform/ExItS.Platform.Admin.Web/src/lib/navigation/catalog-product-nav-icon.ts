/** Distinct nav icons per catalog product code (icon rail + By Product children). */
export function catalogProductNavIcon(productCode: string): string {
  switch (productCode.trim().toLowerCase()) {
    case "pinoy-business-pos":
      return "store";
    case "pinoy-loan-manager":
      return "landmark";
    default:
      return "box";
  }
}
