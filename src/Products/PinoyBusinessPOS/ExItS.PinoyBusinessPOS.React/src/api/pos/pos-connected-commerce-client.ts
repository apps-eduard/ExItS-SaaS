import { z } from "zod";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import { posRequest } from "@/api/pos/pos-http";

const PATH = "/api/v1/pos/connected-commerce";

export const connectedCommerceCategoryRuleSchema = z.object({
  categoryId: z.string().uuid(),
  discountPercent: z.number(),
});

export const organizationConnectedCommerceSettingsSchema = z.object({
  organizationId: z.string().uuid(),
  allowPayBeforeFulfillment: z.boolean(),
  allowPayOnDeliveryOrReceipt: z.boolean(),
  allowSupplierCredit: z.boolean(),
  defaultPaymentTiming: z.string(),
  defaultB2bDiscountPercent: z.number(),
  proposalReservationHoldHours: z.number().int(),
  categoryRules: z.array(connectedCommerceCategoryRuleSchema).default([]),
});

export type OrganizationConnectedCommerceSettingsDto = z.infer<
  typeof organizationConnectedCommerceSettingsSchema
>;

/** Backend overview currently returns settings + customer counts; UI derives status cards. */
export const connectedCommerceOverviewSchema = z.object({
  settings: organizationConnectedCommerceSettingsSchema,
  activeBusinessCustomerCount: z.number().int().nonnegative(),
  pendingBusinessCustomerCount: z.number().int().nonnegative(),
});

export type ConnectedCommerceOverviewDto = z.infer<typeof connectedCommerceOverviewSchema>;

function normalizeSettings(raw: Record<string, unknown>): unknown {
  return {
    organizationId: raw.organizationId ?? raw.OrganizationId,
    allowPayBeforeFulfillment: raw.allowPayBeforeFulfillment ?? raw.AllowPayBeforeFulfillment,
    allowPayOnDeliveryOrReceipt:
      raw.allowPayOnDeliveryOrReceipt ?? raw.AllowPayOnDeliveryOrReceipt,
    allowSupplierCredit: raw.allowSupplierCredit ?? raw.AllowSupplierCredit,
    defaultPaymentTiming: raw.defaultPaymentTiming ?? raw.DefaultPaymentTiming,
    defaultB2bDiscountPercent: raw.defaultB2bDiscountPercent ?? raw.DefaultB2bDiscountPercent,
    proposalReservationHoldHours:
      raw.proposalReservationHoldHours ?? raw.ProposalReservationHoldHours,
    categoryRules: ((raw.categoryRules ?? raw.CategoryRules ?? []) as Record<string, unknown>[]).map(
      (rule) => ({
        categoryId: rule.categoryId ?? rule.CategoryId,
        discountPercent: rule.discountPercent ?? rule.DiscountPercent,
      }),
    ),
  };
}

export async function getConnectedCommerceOverview(
  workspace: PosWorkspaceScope,
  signal?: AbortSignal,
): Promise<ConnectedCommerceOverviewDto> {
  const data = await posRequest<Record<string, unknown>>({
    workspace,
    path: `${PATH}/overview`,
    signal,
  });
  const settingsRaw = (data.settings ?? data.Settings ?? {}) as Record<string, unknown>;
  return connectedCommerceOverviewSchema.parse({
    settings: normalizeSettings(settingsRaw),
    activeBusinessCustomerCount:
      data.activeBusinessCustomerCount ?? data.ActiveBusinessCustomerCount ?? 0,
    pendingBusinessCustomerCount:
      data.pendingBusinessCustomerCount ?? data.PendingBusinessCustomerCount ?? 0,
  });
}

export async function getOrganizationConnectedCommerceSettings(
  workspace: PosWorkspaceScope,
  signal?: AbortSignal,
): Promise<OrganizationConnectedCommerceSettingsDto> {
  const data = await posRequest<Record<string, unknown>>({
    workspace,
    path: `${PATH}/settings`,
    signal,
  });
  return organizationConnectedCommerceSettingsSchema.parse(normalizeSettings(data));
}

export async function updateOrganizationConnectedCommerceSettings(
  workspace: PosWorkspaceScope,
  body: {
    allowPayBeforeFulfillment: boolean;
    allowPayOnDeliveryOrReceipt: boolean;
    allowSupplierCredit: boolean;
    defaultPaymentTiming: string;
    defaultB2bDiscountPercent: number;
    proposalReservationHoldHours: number;
    categoryRules: Array<{ categoryId: string; discountPercent: number }>;
  },
  signal?: AbortSignal,
): Promise<OrganizationConnectedCommerceSettingsDto> {
  const data = await posRequest<Record<string, unknown>>({
    workspace,
    path: `${PATH}/settings`,
    method: "PUT",
    body: {
      allowPayBeforeFulfillment: body.allowPayBeforeFulfillment,
      allowPayOnDeliveryOrReceipt: body.allowPayOnDeliveryOrReceipt,
      allowSupplierCredit: body.allowSupplierCredit,
      defaultPaymentTiming: body.defaultPaymentTiming,
      defaultB2bDiscountPercent: body.defaultB2bDiscountPercent,
      proposalReservationHoldHours: body.proposalReservationHoldHours,
      categoryRules: body.categoryRules,
    },
    signal,
  });
  return organizationConnectedCommerceSettingsSchema.parse(normalizeSettings(data));
}

export async function updateOrganizationOfferDelivery(
  workspace: PosWorkspaceScope,
  offerDelivery: boolean,
  signal?: AbortSignal,
): Promise<{ organizationId: string; offerDelivery: boolean }> {
  return posRequest({
    workspace,
    path: "/api/v1/pos/connected-suppliers/organization/fulfillment-settings/offer-delivery",
    method: "PUT",
    body: { offerDelivery },
    signal,
  });
}

export async function getOrganizationOfferDelivery(
  workspace: PosWorkspaceScope,
  signal?: AbortSignal,
): Promise<{ organizationId: string; offerDelivery: boolean }> {
  return posRequest({
    workspace,
    path: "/api/v1/pos/connected-suppliers/organization/fulfillment-settings",
    signal,
  });
}

const paymentTimingOverrideSchema = z.object({
  connectionId: z.string().uuid(),
  useOrganizationPaymentTimingDefaults: z.boolean(),
  allowPayBeforeFulfillment: z.boolean(),
  allowPayOnDeliveryOrReceipt: z.boolean(),
  allowSupplierCredit: z.boolean(),
  customerDefaultPaymentTiming: z.string(),
  effectiveAllowPayBeforeFulfillment: z.boolean(),
  effectiveAllowPayOnDeliveryOrReceipt: z.boolean(),
  effectiveAllowSupplierCredit: z.boolean(),
  effectiveDefaultPaymentTiming: z.string(),
});

export type BusinessCustomerPaymentTimingOverrideDto = z.infer<
  typeof paymentTimingOverrideSchema
>;

const pricingOverridesSchema = z.object({
  connectionId: z.string().uuid(),
  customerDiscountPercent: z.number().nullable().optional(),
  categoryOverrides: z.array(connectedCommerceCategoryRuleSchema).default([]),
});

export type BusinessCustomerPricingOverridesDto = z.infer<typeof pricingOverridesSchema>;

function normalizeTiming(raw: Record<string, unknown>): unknown {
  return {
    connectionId: raw.connectionId ?? raw.ConnectionId,
    useOrganizationPaymentTimingDefaults:
      raw.useOrganizationPaymentTimingDefaults ?? raw.UseOrganizationPaymentTimingDefaults,
    allowPayBeforeFulfillment: raw.allowPayBeforeFulfillment ?? raw.AllowPayBeforeFulfillment,
    allowPayOnDeliveryOrReceipt:
      raw.allowPayOnDeliveryOrReceipt ?? raw.AllowPayOnDeliveryOrReceipt,
    allowSupplierCredit: raw.allowSupplierCredit ?? raw.AllowSupplierCredit,
    customerDefaultPaymentTiming:
      raw.customerDefaultPaymentTiming ?? raw.CustomerDefaultPaymentTiming,
    effectiveAllowPayBeforeFulfillment:
      raw.effectiveAllowPayBeforeFulfillment ?? raw.EffectiveAllowPayBeforeFulfillment,
    effectiveAllowPayOnDeliveryOrReceipt:
      raw.effectiveAllowPayOnDeliveryOrReceipt ?? raw.EffectiveAllowPayOnDeliveryOrReceipt,
    effectiveAllowSupplierCredit:
      raw.effectiveAllowSupplierCredit ?? raw.EffectiveAllowSupplierCredit,
    effectiveDefaultPaymentTiming:
      raw.effectiveDefaultPaymentTiming ?? raw.EffectiveDefaultPaymentTiming,
  };
}

function normalizePricing(raw: Record<string, unknown>): unknown {
  return {
    connectionId: raw.connectionId ?? raw.ConnectionId,
    customerDiscountPercent: raw.customerDiscountPercent ?? raw.CustomerDiscountPercent ?? null,
    categoryOverrides: ((raw.categoryOverrides ?? raw.CategoryOverrides ?? []) as Record<
      string,
      unknown
    >[]).map((rule) => ({
      categoryId: rule.categoryId ?? rule.CategoryId,
      discountPercent: rule.discountPercent ?? rule.DiscountPercent,
    })),
  };
}

export async function getBusinessCustomerPaymentTimingOverride(
  workspace: PosWorkspaceScope,
  connectionId: string,
  signal?: AbortSignal,
): Promise<BusinessCustomerPaymentTimingOverrideDto> {
  const data = await posRequest<Record<string, unknown>>({
    workspace,
    path: `${PATH}/business-customers/${connectionId}/payment-timing`,
    signal,
  });
  return paymentTimingOverrideSchema.parse(normalizeTiming(data));
}

export async function updateBusinessCustomerPaymentTimingOverride(
  workspace: PosWorkspaceScope,
  connectionId: string,
  body: {
    useOrganizationPaymentTimingDefaults: boolean;
    allowPayBeforeFulfillment: boolean;
    allowPayOnDeliveryOrReceipt: boolean;
    allowSupplierCredit: boolean;
    customerDefaultPaymentTiming: string;
  },
  signal?: AbortSignal,
): Promise<BusinessCustomerPaymentTimingOverrideDto> {
  const data = await posRequest<Record<string, unknown>>({
    workspace,
    path: `${PATH}/business-customers/${connectionId}/payment-timing`,
    method: "PUT",
    body,
    signal,
  });
  return paymentTimingOverrideSchema.parse(normalizeTiming(data));
}

export async function getBusinessCustomerPricingOverrides(
  workspace: PosWorkspaceScope,
  connectionId: string,
  signal?: AbortSignal,
): Promise<BusinessCustomerPricingOverridesDto> {
  const data = await posRequest<Record<string, unknown>>({
    workspace,
    path: `${PATH}/business-customers/${connectionId}/pricing`,
    signal,
  });
  return pricingOverridesSchema.parse(normalizePricing(data));
}

export async function updateBusinessCustomerPricingOverrides(
  workspace: PosWorkspaceScope,
  connectionId: string,
  body: {
    customerDiscountPercent: number | null;
    categoryOverrides: Array<{ categoryId: string; discountPercent: number }>;
  },
  signal?: AbortSignal,
): Promise<BusinessCustomerPricingOverridesDto> {
  const data = await posRequest<Record<string, unknown>>({
    workspace,
    path: `${PATH}/business-customers/${connectionId}/pricing`,
    method: "PUT",
    body,
    signal,
  });
  return pricingOverridesSchema.parse(normalizePricing(data));
}
