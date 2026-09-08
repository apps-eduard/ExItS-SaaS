import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import { posRequest } from "@/api/pos/pos-http";
import { buildPosMutationIdempotencyHeaders } from "@/api/pos/pos-mutation-idempotency";

const REGISTERS_PATH = "/api/v1/pos/registers";
const CREATE_REGISTER_OPERATION = "pos.register.create";

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
  /** Actor who owns the current Open shift on this register, when present. */
  openShiftActorId?: string | null;
  /** Open shift id for this register, when present. */
  openShiftId?: string | null;
  /** When the open shift started (UTC ISO), when present. */
  openShiftOpenedAtUtc?: string | null;
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

export type PosRegisterActivityDto = {
  registerId: string;
  registerCode: string;
  name: string;
  status: string;
  openShiftCount: number;
  closedShiftCount: number;
  completedSaleCount: number;
  grossSalesTotal: number;
  activityFromUtc?: string | null;
  activityToUtc?: string | null;
  cashSalesTotal?: number;
  manualGCashSalesTotal?: number;
  utangSalesTotal?: number;
};

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

/** Register activity totals for a UTC window (ViewRegisters). */
export function getRegisterActivity(
  workspace: PosWorkspaceScope,
  registerId: string,
  options: {
    fromUtc?: string;
    toUtc?: string;
  } = {},
  signal?: AbortSignal,
): Promise<PosRegisterActivityDto> {
  return posRequest({
    method: "GET",
    workspace,
    signal,
    path: appendQuery(`${REGISTERS_PATH}/${registerId}/activity`, {
      fromUtc: options.fromUtc,
      toUtc: options.toUtc,
    }),
  });
}

export type CreateRegisterBody = {
  name: string;
  description?: string | null;
};

const ENSURE_PWA_REGISTER_OPERATION = "pos.register.ensure_available_for_pwa_shift";

/**
 * Reuse any free Active register, or auto-create the next PWA-NNNN.
 * Requires ManageShifts (cashiers allowed). Device enforcement must be disabled.
 */
export async function ensureAvailablePwaRegisterForShift(
  workspace: PosWorkspaceScope,
  signal?: AbortSignal,
): Promise<PosRegisterDto> {
  const operationId =
    typeof crypto !== "undefined" && "randomUUID" in crypto
      ? crypto.randomUUID()
      : `00000000-0000-4000-8000-${Date.now().toString(16).padStart(12, "0").slice(-12)}`;
  const payload = {};
  const payloadJson = JSON.stringify(payload);
  const headers = await buildPosMutationIdempotencyHeaders(
    operationId,
    payloadJson,
    ENSURE_PWA_REGISTER_OPERATION,
  );
  return posRequest({
    method: "POST",
    workspace,
    signal,
    path: `${REGISTERS_PATH}/ensure-available-for-pwa-shift`,
    body: payload,
    headers,
  });
}

/** Requires ManageRegisters. Server allocates REG-NNNNNN code. */
export async function createRegister(
  workspace: PosWorkspaceScope,
  body: CreateRegisterBody,
  signal?: AbortSignal,
): Promise<PosRegisterDto> {
  const operationId =
    typeof crypto !== "undefined" && "randomUUID" in crypto
      ? crypto.randomUUID()
      : `00000000-0000-4000-8000-${Date.now().toString(16).padStart(12, "0").slice(-12)}`;
  const payload = {
    name: body.name,
    description: body.description ?? null,
  };
  const payloadJson = JSON.stringify(payload);
  const headers = await buildPosMutationIdempotencyHeaders(
    operationId,
    payloadJson,
    CREATE_REGISTER_OPERATION,
  );
  return posRequest({
    method: "POST",
    workspace,
    signal,
    path: REGISTERS_PATH,
    body: payload,
    headers,
  });
}
