import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import { posRequest } from "@/api/pos/pos-http";

const REGISTERS_PATH = "/api/v1/pos/registers";

export type PosRegisterSummaryDto = {
  registerId: string;
  registerCode: string;
  name: string;
  status: string;
};

export type PosRegisterDto = {
  registerId: string;
  organizationId: string;
  registerCode: string;
  name: string;
  description?: string | null;
  status: string;
  createdAtUtc: string;
  createdBy: string;
  updatedAtUtc: string;
  updatedBy: string;
  hasOpenShift: boolean;
};

export type PosRegisterPagedResult = {
  items: PosRegisterDto[];
  totalCount: number;
  page: number;
  pageSize: number;
};

function appendQuery(
  path: string,
  params: Record<string, string | number | boolean | undefined>,
): string {
  const query = new URLSearchParams();
  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== "") {
      query.set(key, String(value));
    }
  }
  const serialized = query.toString();
  return serialized ? `${path}?${serialized}` : path;
}

/** Active registers without an open shift — ViewRegisters or ManageShifts. */
export function listRegistersAvailableForShift(
  workspace: PosWorkspaceScope,
  signal?: AbortSignal,
): Promise<PosRegisterSummaryDto[]> {
  return posRequest({
    method: "GET",
    workspace,
    signal,
    path: `${REGISTERS_PATH}/available-for-shift`,
  });
}

export function listRegisters(
  workspace: PosWorkspaceScope,
  options: {
    registerCode?: string;
    name?: string;
    status?: string;
    hasOpenShift?: boolean;
    page?: number;
    pageSize?: number;
  } = {},
  signal?: AbortSignal,
): Promise<PosRegisterPagedResult> {
  return posRequest({
    method: "GET",
    workspace,
    signal,
    path: appendQuery(REGISTERS_PATH, {
      registerCode: options.registerCode,
      name: options.name,
      status: options.status,
      hasOpenShift: options.hasOpenShift,
      page: options.page ?? 1,
      pageSize: options.pageSize ?? 20,
    }),
  });
}

export function getRegister(
  workspace: PosWorkspaceScope,
  registerId: string,
  signal?: AbortSignal,
): Promise<PosRegisterDto> {
  return posRequest({
    method: "GET",
    workspace,
    signal,
    path: `${REGISTERS_PATH}/${registerId}`,
  });
}

export type CreateRegisterBody = {
  name: string;
  description?: string | null;
};

/** Requires ManageRegisters. Server allocates REG-NNNNNN code. */
export function createRegister(
  workspace: PosWorkspaceScope,
  body: CreateRegisterBody,
  signal?: AbortSignal,
): Promise<PosRegisterDto> {
  const idempotencyKey =
    typeof crypto !== "undefined" && "randomUUID" in crypto
      ? crypto.randomUUID()
      : `reg-${Date.now()}-${Math.random().toString(16).slice(2)}`;
  return posRequest({
    method: "POST",
    workspace,
    signal,
    path: REGISTERS_PATH,
    body: {
      name: body.name,
      description: body.description ?? null,
    },
    headers: {
      "Idempotency-Key": idempotencyKey,
    },
  });
}
