import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import { PosApiError, posRequest } from "@/api/pos/pos-http";

const SHIFTS_PATH = "/api/v1/pos/cashier-shifts";

export type CashCountDenominationLineDto = {
  denominationValue: number;
  quantity: number;
  lineTotal?: number | null;
};

export type PosCashierShiftDto = {
  shiftId: string;
  organizationId: string;
  shiftNumber: string;
  status: string;
  actorId: string;
  registerId?: string | null;
  registerCode?: string | null;
  registerName?: string | null;
  businessDate: string;
  openingCashAmount: number;
  openingCashCounted: boolean;
  effectiveCashCountMode: string;
  openedAtUtc: string;
  openedBy: string;
  closingCashAmount?: number | null;
  expectedCashAmountSnapshot?: number | null;
  cashVarianceAmount?: number | null;
  closingCashCountState?: string | null;
  closingNotes?: string | null;
  closedAtUtc?: string | null;
  closedBy?: string | null;
  cancelledAtUtc?: string | null;
  cancelledBy?: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
  openingDenominationLines?: CashCountDenominationLineDto[] | null;
  closingDenominationLines?: CashCountDenominationLineDto[] | null;
};

export type PosCashierShiftSummaryDto = {
  shiftId: string;
  shiftNumber: string;
  status: string;
  openingCashAmount: number;
  openingCashCounted: boolean;
  effectiveCashCountMode: string;
  netCashSales: number;
  cashSalesTotal: number;
  gCashSalesTotal: number;
  utangSalesTotal: number;
  cashRefundsTotal: number;
  totalCashIn: number;
  totalCashOut: number;
  expectedCashAmount: number;
  closingCashAmount?: number | null;
  expectedCashAmountSnapshot?: number | null;
  cashVarianceAmount?: number | null;
  closingCashCountState?: string | null;
  completedCashCount: number;
  voidedCashCount: number;
  completedGCashCount: number;
  completedUtangCount: number;
};

export type PosCashierShiftPagedResult = {
  items: PosCashierShiftDto[];
  totalCount: number;
  page: number;
  pageSize: number;
};

export type OpenCashierShiftRequest = {
  registerId: string;
  openingCashAmount?: number | null;
  businessDate?: string | null;
  denominationLines?: CashCountDenominationLineDto[] | null;
};

export type CloseCashierShiftRequest = {
  closingCashAmount?: number | null;
  notes?: string | null;
  denominationLines?: CashCountDenominationLineDto[] | null;
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

export function isOpenCashierShift(shift: PosCashierShiftDto | null | undefined): boolean {
  return Boolean(
    shift && shift.status.localeCompare("Open", undefined, { sensitivity: "accent" }) === 0,
  );
}

/**
 * Current open shift for the authenticated actor.
 * Returns null when the server responds 404 (no open shift).
 */
export async function getCurrentCashierShift(
  workspace: PosWorkspaceScope,
  signal?: AbortSignal,
): Promise<PosCashierShiftDto | null> {
  try {
    return await posRequest<PosCashierShiftDto>({
      method: "GET",
      workspace,
      signal,
      path: `${SHIFTS_PATH}/current`,
    });
  } catch (error) {
    if (error instanceof PosApiError && error.status === 404) {
      return null;
    }
    throw error;
  }
}

export function getCashierShift(
  workspace: PosWorkspaceScope,
  shiftId: string,
  signal?: AbortSignal,
): Promise<PosCashierShiftDto> {
  return posRequest({
    method: "GET",
    workspace,
    signal,
    path: `${SHIFTS_PATH}/${shiftId}`,
  });
}

export function getCashierShiftSummary(
  workspace: PosWorkspaceScope,
  shiftId: string,
  signal?: AbortSignal,
): Promise<PosCashierShiftSummaryDto> {
  return posRequest({
    method: "GET",
    workspace,
    signal,
    path: `${SHIFTS_PATH}/${shiftId}/summary`,
  });
}

export function listCashierShifts(
  workspace: PosWorkspaceScope,
  options: {
    status?: string;
    actorId?: string;
    shiftNumber?: string;
    fromBusinessDate?: string;
    toBusinessDate?: string;
    page?: number;
    pageSize?: number;
  } = {},
  signal?: AbortSignal,
): Promise<PosCashierShiftPagedResult> {
  return posRequest({
    method: "GET",
    workspace,
    signal,
    path: appendQuery(SHIFTS_PATH, {
      status: options.status,
      actorId: options.actorId,
      shiftNumber: options.shiftNumber,
      fromBusinessDate: options.fromBusinessDate,
      toBusinessDate: options.toBusinessDate,
      page: options.page ?? 1,
      pageSize: options.pageSize ?? 20,
    }),
  });
}

export function openCashierShift(
  workspace: PosWorkspaceScope,
  body: OpenCashierShiftRequest,
  signal?: AbortSignal,
): Promise<PosCashierShiftDto> {
  return posRequest({
    method: "POST",
    workspace,
    signal,
    path: SHIFTS_PATH,
    body: {
      registerId: body.registerId,
      openingCashAmount: body.openingCashAmount ?? null,
      businessDate: body.businessDate ?? null,
      denominationLines: body.denominationLines ?? null,
    },
  });
}

export function closeCashierShift(
  workspace: PosWorkspaceScope,
  shiftId: string,
  body: CloseCashierShiftRequest = {},
  signal?: AbortSignal,
): Promise<PosCashierShiftDto> {
  return posRequest({
    method: "POST",
    workspace,
    signal,
    path: `${SHIFTS_PATH}/${shiftId}/close`,
    body: {
      closingCashAmount: body.closingCashAmount ?? null,
      notes: body.notes ?? null,
      denominationLines: body.denominationLines ?? null,
    },
  });
}
