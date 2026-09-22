import type { ReceiveDiscrepancyOtherReasonOption } from "@/features/purchasing/ReceiveDiscrepancyDialog";

export const RECEIVE_OTHER_REASON_CODES = [
  "WrongItem",
  "WrongVariant",
  "Expired",
  "PackagingIssue",
  "QualityIssue",
  "Other",
] as const;

export type ReceiveOtherReasonCode = (typeof RECEIVE_OTHER_REASON_CODES)[number];

const REASON_I18N_KEYS: Record<ReceiveOtherReasonCode, string> = {
  WrongItem: "purchasing.otherReasonWrongItem",
  WrongVariant: "purchasing.otherReasonWrongVariant",
  Expired: "purchasing.otherReasonExpired",
  PackagingIssue: "purchasing.otherReasonPackagingIssue",
  QualityIssue: "purchasing.otherReasonQualityIssue",
  Other: "purchasing.otherReasonOther",
};

export function buildReceiveOtherReasonOptions(
  t: (key: string) => string,
): ReceiveDiscrepancyOtherReasonOption[] {
  return RECEIVE_OTHER_REASON_CODES.map((value) => ({
    value,
    label: t(REASON_I18N_KEYS[value]),
  }));
}

export function buildReceiveOtherReasonSummaryLabels(
  t: (key: string) => string,
): Record<string, string> {
  const out: Record<string, string> = {};
  for (const code of RECEIVE_OTHER_REASON_CODES) {
    out[code] = t(REASON_I18N_KEYS[code]);
  }
  return out;
}
