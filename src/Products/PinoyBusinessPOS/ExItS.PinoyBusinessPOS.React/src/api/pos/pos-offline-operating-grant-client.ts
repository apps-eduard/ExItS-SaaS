import { z } from "zod";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import { posRequest } from "@/api/pos/pos-http";

const GRANTS_PATH = "/api/v1/pos/offline-operating-grants";

const guidSchema = z
  .string()
  .regex(/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/);

export const serverSignedOfflineOperatingGrantSchema = z.object({
  grantId: guidSchema,
  schemaVersion: z.number().int(),
  userId: guidSchema,
  scopeKind: z.enum(["Organization", "Personal"]),
  organizationId: guidSchema.nullable(),
  organizationDisplayName: z.string(),
  branchId: guidSchema.nullable(),
  branchName: z.string().nullable(),
  installationDeviceId: z.string().min(1),
  posDeviceId: guidSchema.nullable(),
  roleCode: z.string().nullable(),
  displayName: z.string().nullable(),
  username: z.string().nullable(),
  issuedAtUtc: z.string().min(1),
  lastOnlineValidatedAtUtc: z.string().min(1),
  expiresAtUtc: z.string().min(1),
  signature: z.string().min(1),
});

export type ServerSignedOfflineOperatingGrantDto = z.infer<
  typeof serverSignedOfflineOperatingGrantSchema
>;

const issueOfflineOperatingGrantResponseSchema = z.object({
  grant: serverSignedOfflineOperatingGrantSchema,
});

export type IssueOfflineOperatingGrantRequest = {
  installationDeviceId: string;
  organizationDisplayName?: string | null;
  branchName?: string | null;
  displayName?: string | null;
  username?: string | null;
};

/**
 * POST /api/v1/pos/offline-operating-grants — server-issued offline operating grant.
 * The browser cannot mint or alter authoritative grant fields.
 */
export async function issueOfflineOperatingGrant(
  workspace: PosWorkspaceScope,
  body: IssueOfflineOperatingGrantRequest,
): Promise<ServerSignedOfflineOperatingGrantDto> {
  const response = await posRequest<unknown>({
    method: "POST",
    path: GRANTS_PATH,
    body,
    workspace,
  });
  const parsed = issueOfflineOperatingGrantResponseSchema.safeParse(response);
  if (!parsed.success) {
    throw new Error("Offline operating grant response was malformed.");
  }
  return parsed.data.grant;
}
