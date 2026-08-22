export type CatalogFeatureDefinition = {
  productCode: string;
  featureCode: string;
  displayName: string;
  valueType: string;
  status: string;
  createdAtUtc?: string;
  updatedAtUtc?: string;
};

export function featureSupportsNumericLimit(valueType: string): boolean {
  return valueType === "NumericLimit" || valueType === "QuantityLimit";
}
