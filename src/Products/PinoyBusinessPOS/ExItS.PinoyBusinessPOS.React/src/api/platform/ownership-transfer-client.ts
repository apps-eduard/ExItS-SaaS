import { z } from "zod";
import { platformRequest } from "@/api/platform/platform-http";

const guidSchema = z
  .string()
  .regex(/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/);

function pick(raw: Record<string, unknown>, camel: string, pascal: string): unknown {
  return raw[camel] ?? raw[pascal];
}

function asNullableString(value: unknown): string | null {
  if (value == null) return null;
  return String(value);
}

export const organizationOwnershipTransferSchema = z.object({
  id: guidSchema,
  organizationId: guidSchema,
  organizationDisplayName: z.string().nullable().optional().default(null),
  publicOrganizationId: z.string().nullable().optional().default(null),
  fromOwnerUserId: guidSchema,
  toUserId: guidSchema,
  toDisplayName: z.string().nullable().optional().default(null),
  toPublicUserId: z.string().nullable().optional().default(null),
  status: z.string(),
  createdAtUtc: z.string(),
  expiresAtUtc: z.string(),
  acceptedAtUtc: z.string().nullable().optional().default(null),
  declinedAtUtc: z.string().nullable().optional().default(null),
  cancelledAtUtc: z.string().nullable().optional().default(null),
  completedAtUtc: z.string().nullable().optional().default(null),
  updatedAtUtc: z.string(),
});

export type OrganizationOwnershipTransferDto = z.infer<
  typeof organizationOwnershipTransferSchema
>;

export const ownershipTransferTargetSchema = z.object({
  publicUserId: z.string(),
  displayName: z.string(),
});

export type OwnershipTransferTargetDto = z.infer<typeof ownershipTransferTargetSchema>;

function normalizeTransfer(raw: unknown): unknown {
  if (!raw || typeof raw !== "object") {
    return raw;
  }
  const r = raw as Record<string, unknown>;
  return {
    id: pick(r, "id", "Id"),
    organizationId: pick(r, "organizationId", "OrganizationId"),
    organizationDisplayName: asNullableString(
      pick(r, "organizationDisplayName", "OrganizationDisplayName"),
    ),
    publicOrganizationId: asNullableString(
      pick(r, "publicOrganizationId", "PublicOrganizationId"),
    ),
    fromOwnerUserId: pick(r, "fromOwnerUserId", "FromOwnerUserId"),
    toUserId: pick(r, "toUserId", "ToUserId"),
    toDisplayName: asNullableString(pick(r, "toDisplayName", "ToDisplayName")),
    toPublicUserId: asNullableString(pick(r, "toPublicUserId", "ToPublicUserId")),
    status: pick(r, "status", "Status"),
    createdAtUtc: pick(r, "createdAtUtc", "CreatedAtUtc"),
    expiresAtUtc: pick(r, "expiresAtUtc", "ExpiresAtUtc"),
    acceptedAtUtc: asNullableString(pick(r, "acceptedAtUtc", "AcceptedAtUtc")),
    declinedAtUtc: asNullableString(pick(r, "declinedAtUtc", "DeclinedAtUtc")),
    cancelledAtUtc: asNullableString(pick(r, "cancelledAtUtc", "CancelledAtUtc")),
    completedAtUtc: asNullableString(pick(r, "completedAtUtc", "CompletedAtUtc")),
    updatedAtUtc: pick(r, "updatedAtUtc", "UpdatedAtUtc"),
  };
}

function normalizeTarget(raw: unknown): unknown {
  if (!raw || typeof raw !== "object") {
    return raw;
  }
  const r = raw as Record<string, unknown>;
  return {
    publicUserId: pick(r, "publicUserId", "PublicUserId"),
    displayName: pick(r, "displayName", "DisplayName"),
  };
}

const BASE = "/api/v1/platform/ownership-transfers";

function orgTransferBase(organizationId: string): string {
  return `/api/v1/platform/organizations/${organizationId}/ownership-transfer`;
}

export async function listMyPendingOwnershipTransfers(
  signal?: AbortSignal,
): Promise<OrganizationOwnershipTransferDto[]> {
  const raw = await platformRequest<unknown>({ path: `${BASE}/my-pending`, signal });
  const list = Array.isArray(raw) ? raw : [];
  return list.map((item) => organizationOwnershipTransferSchema.parse(normalizeTransfer(item)));
}

export async function acceptOwnershipTransfer(
  transferId: string,
): Promise<OrganizationOwnershipTransferDto> {
  const raw = await platformRequest<unknown>({
    method: "POST",
    path: `${BASE}/${transferId}/accept`,
  });
  return organizationOwnershipTransferSchema.parse(normalizeTransfer(raw));
}

export async function declineOwnershipTransfer(
  transferId: string,
): Promise<OrganizationOwnershipTransferDto> {
  const raw = await platformRequest<unknown>({
    method: "POST",
    path: `${BASE}/${transferId}/decline`,
  });
  return organizationOwnershipTransferSchema.parse(normalizeTransfer(raw));
}

export async function resolveOwnershipTransferTarget(
  organizationId: string,
  input: string,
  signal?: AbortSignal,
): Promise<OwnershipTransferTargetDto> {
  const raw = await platformRequest<unknown>({
    method: "POST",
    path: `${orgTransferBase(organizationId)}/resolve-target`,
    body: { input },
    signal,
  });
  return ownershipTransferTargetSchema.parse(normalizeTarget(raw));
}

export async function requestOwnershipTransfer(
  organizationId: string,
  targetInput: string,
): Promise<OrganizationOwnershipTransferDto> {
  const raw = await platformRequest<unknown>({
    method: "POST",
    path: `${orgTransferBase(organizationId)}/request`,
    body: { targetInput },
  });
  return organizationOwnershipTransferSchema.parse(normalizeTransfer(raw));
}

export async function getPendingOwnershipTransferForOrg(
  organizationId: string,
  signal?: AbortSignal,
): Promise<OrganizationOwnershipTransferDto | null> {
  const raw = await platformRequest<unknown>({
    path: `${orgTransferBase(organizationId)}/pending`,
    signal,
  });
  if (raw == null) {
    return null;
  }
  return organizationOwnershipTransferSchema.parse(normalizeTransfer(raw));
}

export async function cancelOwnershipTransfer(
  transferId: string,
): Promise<OrganizationOwnershipTransferDto> {
  const raw = await platformRequest<unknown>({
    method: "POST",
    path: `${BASE}/${transferId}/cancel`,
  });
  return organizationOwnershipTransferSchema.parse(normalizeTransfer(raw));
}
