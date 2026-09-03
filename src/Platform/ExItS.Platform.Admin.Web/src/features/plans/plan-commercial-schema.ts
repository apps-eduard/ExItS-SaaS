import { z } from "zod";

/** Mirrors the Platform domain Plan.MaxAreas bounds so the editor fails before the API does. */
export const MIN_MAX_AREAS = 1;
export const MAX_MAX_AREAS = 10_000;

export type PlanRenameValues = {
  displayName: string;
};

export type PlanCommercialValues = {
  displayName: string;
  description?: string | null;
  monthlyPrice: number;
  annualPrice: number;
  currencyCode: string;
  maxBranches: number;
  maxActiveStaff: number;
  maxActivePosDevices: number;
  maxActiveBusinessTypes: number;
  maxAreas: number;
  customerCreditEnabled: boolean;
  advancedReportsEnabled: boolean;
  exportEnabled: boolean;
  trialAllowed: boolean;
  defaultTrialDays: number;
  sortOrder: number;
};

export const planRenameSchema = z.object({
  displayName: z.string().trim().min(1, "Display name is required.").max(200),
});

export const planCommercialSchema = z
  .object({
    displayName: z.string().trim().min(1, "Display name is required.").max(200),
    description: z.string().max(2000).optional().nullable(),
    monthlyPrice: z.number().min(0, "Cannot be negative."),
    annualPrice: z.number().min(0, "Cannot be negative."),
    currencyCode: z
      .string()
      .trim()
      .min(3, "Currency code is required.")
      .max(3, "Use a three-letter currency code."),
    maxBranches: z.number().int().min(0, "Cannot be negative."),
    maxActiveStaff: z.number().int().min(0, "Cannot be negative."),
    maxActivePosDevices: z.number().int().min(0, "Cannot be negative."),
    maxActiveBusinessTypes: z.number().int().min(0, "Cannot be negative."),
    maxAreas: z
      .number()
      .int()
      .min(MIN_MAX_AREAS, "Must be at least 1.")
      .max(MAX_MAX_AREAS, "Cannot exceed 10000."),
    customerCreditEnabled: z.boolean(),
    advancedReportsEnabled: z.boolean(),
    exportEnabled: z.boolean(),
    trialAllowed: z.boolean(),
    defaultTrialDays: z.number().int().min(0, "Cannot be negative."),
    sortOrder: z.number().int(),
  })
  .superRefine((values, ctx) => {
    if (values.trialAllowed && values.defaultTrialDays <= 0) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        path: ["defaultTrialDays"],
        message: "Default trial days must be greater than zero when trials are allowed.",
      });
    }
  });

export type DraftFeatureGrantValues = {
  featureCode: string;
  enabled: boolean;
  numericLimit: number | null;
};

export const draftFeatureGrantSchema = z
  .object({
    featureCode: z.string().min(1),
    enabled: z.boolean(),
    numericLimit: z.number().int().min(0).nullable(),
  })
  .superRefine((values, ctx) => {
    if (!values.enabled && values.numericLimit != null) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        path: ["numericLimit"],
        message: "Numeric limits apply only when the grant is enabled.",
      });
    }
  });
