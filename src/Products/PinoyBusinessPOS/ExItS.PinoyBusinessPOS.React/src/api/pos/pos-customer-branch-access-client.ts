import { posRequest, type PosWorkspaceScope } from "@/api/pos/pos-http";

const PARTIES_CUSTOMERS = "/api/v1/pos/parties/customers";

export type CustomerBranchGrantSource =
  | "ExplicitAssign"
  | "CreateAtBranch"
  | "Transaction"
  | "SetupCopy"
  | "MigrationBackfill";

export type CustomerBranchAccessItem = {
  branchId: string;
  grantSource: CustomerBranchGrantSource;
  grantedAtUtc: string;
};

export type CustomerBranchAccessList = {
  customerId: string;
  homeBranchId: string | null;
  items: CustomerBranchAccessItem[];
};

function asRecord(value: unknown): Record<string, unknown> {
  return typeof value === "object" && value !== null ? (value as Record<string, unknown>) : {};
}

function normalizeGrantSource(raw: unknown): CustomerBranchGrantSource {
  const value = String(raw ?? "").trim();
  switch (value) {
    case "ExplicitAssign":
    case "CreateAtBranch":
    case "Transaction":
    case "SetupCopy":
    case "MigrationBackfill":
      return value;
    default:
      return "MigrationBackfill";
  }
}

function normalizeAccessList(raw: unknown): CustomerBranchAccessList {
  const r = asRecord(raw);
  const itemsRaw = r.items ?? r.Items;
  const list = Array.isArray(itemsRaw) ? itemsRaw : [];
  const home = r.homeBranchId ?? r.HomeBranchId;
  return {
    customerId: String(r.customerId ?? r.CustomerId ?? r.connectionId ?? r.ConnectionId ?? ""),
    homeBranchId: home == null || home === "" ? null : String(home),
    items: list.map((item) => {
      const row = asRecord(item);
      return {
        branchId: String(row.branchId ?? row.BranchId ?? ""),
        grantSource: normalizeGrantSource(row.grantSource ?? row.GrantSource),
        grantedAtUtc: String(row.grantedAtUtc ?? row.GrantedAtUtc ?? ""),
      };
    }),
  };
}

export async function listCustomerBranchAccess(
  workspace: PosWorkspaceScope,
  customerId: string,
  signal?: AbortSignal,
): Promise<CustomerBranchAccessList> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    path: `${PARTIES_CUSTOMERS}/${customerId}/branch-access`,
    signal,
  });
  return normalizeAccessList(raw);
}

export async function grantCustomerBranchAccess(
  workspace: PosWorkspaceScope,
  customerId: string,
  branchId: string,
  signal?: AbortSignal,
): Promise<void> {
  await posRequest({
    method: "POST",
    workspace,
    path: `${PARTIES_CUSTOMERS}/${customerId}/branch-access`,
    body: { branchId },
    signal,
  });
}

export async function revokeCustomerBranchAccess(
  workspace: PosWorkspaceScope,
  customerId: string,
  branchId: string,
  signal?: AbortSignal,
): Promise<void> {
  await posRequest({
    method: "DELETE",
    workspace,
    path: `${PARTIES_CUSTOMERS}/${customerId}/branch-access`,
    body: { branchId },
    signal,
  });
}

const BUSINESS_CUSTOMERS = "/api/v1/pos/connected-suppliers/business-customers";

export async function listBusinessCustomerBranchAccess(
  workspace: PosWorkspaceScope,
  connectionId: string,
  signal?: AbortSignal,
): Promise<CustomerBranchAccessList> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    path: `${BUSINESS_CUSTOMERS}/${connectionId}/branch-access`,
    signal,
  });
  return normalizeAccessList(raw);
}

export async function grantBusinessCustomerBranchAccess(
  workspace: PosWorkspaceScope,
  connectionId: string,
  branchId: string,
  signal?: AbortSignal,
): Promise<void> {
  await posRequest({
    method: "POST",
    workspace,
    path: `${BUSINESS_CUSTOMERS}/${connectionId}/branch-access`,
    body: { branchId },
    signal,
  });
}

export async function revokeBusinessCustomerBranchAccess(
  workspace: PosWorkspaceScope,
  connectionId: string,
  branchId: string,
  signal?: AbortSignal,
): Promise<void> {
  await posRequest({
    method: "DELETE",
    workspace,
    path: `${BUSINESS_CUSTOMERS}/${connectionId}/branch-access`,
    body: { branchId },
    signal,
  });
}
