import { z } from "zod";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import { posRequest } from "@/api/pos/pos-http";

const PATH = "/api/v1/pos/payment-methods";

const paymentMethodSettingSchema = z.object({
  methodCode: z.string(),
  requiredCapability: z.string(),
  integrationMode: z.string(),
  settlementMode: z.string(),
  availability: z.string(),
  entitled: z.boolean(),
  isEnabled: z.boolean(),
  isCheckoutEligible: z.boolean(),
  comingSoon: z.boolean(),
  displayName: z.string().nullable().optional(),
  requireReference: z.boolean(),
  branchScope: z.string(),
  selectedBranchIds: z.array(z.string().uuid()).default([]),
  instructions: z.string().nullable().optional(),
  accountHint: z.string().nullable().optional(),
  canConfigure: z.boolean(),
});

export type PaymentMethodSettingDto = z.infer<typeof paymentMethodSettingSchema>;

const checkoutMethodsSchema = z.object({
  methods: z.array(z.string()),
});

export async function listPaymentMethods(
  workspace: PosWorkspaceScope,
  signal?: AbortSignal,
): Promise<PaymentMethodSettingDto[]> {
  const data = await posRequest<unknown>({
    workspace,
    path: PATH,
    signal,
  });
  return z.array(paymentMethodSettingSchema).parse(data);
}

export async function listCheckoutPaymentMethods(
  workspace: PosWorkspaceScope,
  signal?: AbortSignal,
): Promise<string[]> {
  const data = await posRequest<unknown>({
    workspace,
    path: `${PATH}/checkout`,
    signal,
  });
  return checkoutMethodsSchema.parse(data).methods;
}

export async function upsertPaymentMethod(
  workspace: PosWorkspaceScope,
  methodCode: string,
  body: {
    isEnabled: boolean;
    displayName?: string | null;
    requireReference: boolean;
    branchScope: string;
    selectedBranchIds?: string[];
    instructions?: string | null;
    accountHint?: string | null;
  },
): Promise<PaymentMethodSettingDto> {
  const data = await posRequest<unknown>({
    workspace,
    path: `${PATH}/${encodeURIComponent(methodCode)}`,
    method: "PUT",
    body: {
      methodCode,
      isEnabled: body.isEnabled,
      displayName: body.displayName ?? null,
      requireReference: body.requireReference,
      branchScope: body.branchScope,
      selectedBranchIds: body.selectedBranchIds ?? [],
      instructions: body.instructions ?? null,
      accountHint: body.accountHint ?? null,
    },
  });
  return paymentMethodSettingSchema.parse(data);
}
