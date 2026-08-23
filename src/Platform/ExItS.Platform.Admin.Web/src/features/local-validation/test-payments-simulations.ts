/** UI labels (Blazor parity) mapped to LocalValidationPaymentProvider accepted tokens. */
export const TEST_PAYMENT_SIMULATION_OPTIONS = [
  { label: "Succeeded", apiValue: "succeed" },
  { label: "Declined", apiValue: "declined" },
  { label: "Pending", apiValue: "pending" },
  { label: "Failed", apiValue: "failed" },
  { label: "RenewalSucceeded", apiValue: "renewal-succeeded" },
  { label: "RenewalFailed", apiValue: "renewal-failed" },
  { label: "Refunded", apiValue: "refunded" },
] as const;

export type TestPaymentSimulationLabel =
  (typeof TEST_PAYMENT_SIMULATION_OPTIONS)[number]["label"];

export const TEST_PAYMENT_BILLING_CYCLES = ["Monthly", "Annual"] as const;
export type TestPaymentBillingCycle = (typeof TEST_PAYMENT_BILLING_CYCLES)[number];

export function apiValueForSimulationLabel(label: TestPaymentSimulationLabel): string {
  const match = TEST_PAYMENT_SIMULATION_OPTIONS.find((item) => item.label === label);
  return match?.apiValue ?? "succeed";
}

const GUID_PATTERN =
  /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/;

export function isGuid(value: string): boolean {
  return GUID_PATTERN.test(value.trim());
}
