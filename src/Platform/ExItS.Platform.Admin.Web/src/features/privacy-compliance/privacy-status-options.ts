/** Blazor PrivacyComplianceRequirementDrawer status options — do not invent values. */
export const PRIVACY_REQUIREMENT_STATUS_OPTIONS = [
  "NotStarted",
  "InProgress",
  "ReadyForReview",
  "Approved",
  "NeedsUpdate",
] as const;

export type PrivacyRequirementStatusOption =
  (typeof PRIVACY_REQUIREMENT_STATUS_OPTIONS)[number];
