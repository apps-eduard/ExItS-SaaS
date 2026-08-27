export type CheckoutCustomerOption = {
  customerId: string;
  displayName: string;
  mobileNumber?: string | null;
  status: string;
  /** POS correlation only — not Platform CustomerLink Active status. */
  linkedPersonalPublicUserId?: string | null;
  /** Platform BusinessCustomer id — used to overlay Connected/Pending on the list. */
  platformBusinessCustomerId?: string | null;
  /** Personal display name from ExItS ID / QR resolve, when the cashier just looked them up. */
  resolvedPersonalDisplayName?: string | null;
};
