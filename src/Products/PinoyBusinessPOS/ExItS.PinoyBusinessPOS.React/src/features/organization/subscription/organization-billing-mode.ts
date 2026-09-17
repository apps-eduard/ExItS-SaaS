/**
 * Canonical billing mode for Organization Admin self-service plan changes.
 * Only the payment adapter is simulated — subscription/entitlement domain stays real.
 */
export const ORGANIZATION_BILLING_MODE = "Simulated" as const;

export type OrganizationBillingMode = typeof ORGANIZATION_BILLING_MODE;

export function isSimulatedOrganizationBilling(): boolean {
  return ORGANIZATION_BILLING_MODE === "Simulated";
}
