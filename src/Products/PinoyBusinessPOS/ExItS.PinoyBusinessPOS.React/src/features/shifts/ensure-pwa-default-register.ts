import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import {
  ensureAvailablePwaRegisterForShift,
  type PosRegisterDto,
  type PosRegisterSummaryDto,
} from "@/api/pos/pos-registers-client";

/** First auto cash-register display name used by the pure React PWA. */
export const PWA_DEFAULT_REGISTER_NAME = "PWA-0001";

const PWA_NAME_PATTERN = /^PWA-(\d{4})$/i;

export function isPwaRegisterDisplayName(name: string | null | undefined): boolean {
  return PWA_NAME_PATTERN.test((name ?? "").trim());
}

/**
 * Next PWA display name after the highest existing `PWA-NNNN` ordinal (max + 1).
 * Gaps are not reused. Unrelated manual names are ignored.
 */
export function nextPwaRegisterDisplayName(existingNames: Iterable<string | null | undefined>): string {
  let max = 0;
  for (const name of existingNames) {
    const match = PWA_NAME_PATTERN.exec((name ?? "").trim());
    if (!match) {
      continue;
    }
    const ordinal = Number.parseInt(match[1]!, 10);
    if (Number.isFinite(ordinal) && ordinal > max) {
      max = ordinal;
    }
  }
  return `PWA-${String(max + 1).padStart(4, "0")}`;
}

/**
 * Pure React PWA: ensure an Active register is available for Open Shift.
 * Prefers any free Active register; otherwise server allocates the next PWA-NNNN
 * via ManageShifts (cashiers do not need ManageRegisters).
 *
 * FUTURE CAPACITOR: stop calling when device enforcement is enabled.
 */
export async function ensurePwaDefaultCashRegister(
  workspace: PosWorkspaceScope,
): Promise<PosRegisterDto> {
  return ensureAvailablePwaRegisterForShift(workspace);
}

export function toRegisterSummary(register: PosRegisterDto): PosRegisterSummaryDto {
  return {
    registerId: register.registerId,
    registerCode: register.registerCode,
    name: register.name,
    status: register.status,
  };
}
