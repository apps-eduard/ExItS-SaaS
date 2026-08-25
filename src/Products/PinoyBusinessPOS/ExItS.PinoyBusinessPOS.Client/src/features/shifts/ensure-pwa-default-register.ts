import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import {
  createRegister,
  listRegisters,
  type PosRegisterDto,
  type PosRegisterSummaryDto,
} from "@/api/pos/pos-registers-client";

/** Display name for the auto cash register used by the pure React PWA. */
export const PWA_DEFAULT_REGISTER_NAME = "PWA-0001";

function isPwaDefaultName(name: string | null | undefined): boolean {
  return (name ?? "").trim().toUpperCase() === PWA_DEFAULT_REGISTER_NAME;
}

/**
 * Pure React PWA: when no cash register is available for Open Shift, ensure a default
 * Active register named PWA-0001 exists (server still allocates REG-NNNNNN code).
 *
 * FUTURE CAPACITOR: stop calling this helper; require an operator-created register again
 * (restore the commented "No register available" UI in ShiftOpenPage).
 */
export async function ensurePwaDefaultCashRegister(
  workspace: PosWorkspaceScope,
): Promise<PosRegisterDto> {
  const listed = await listRegisters(workspace, { page: 1, pageSize: 50 });
  const items = listed.items ?? [];

  const named = items.find((register) => isPwaDefaultName(register.name));
  if (named && named.status.toLowerCase() === "active" && !named.hasOpenShift) {
    return named;
  }

  const freeActive = items.find(
    (register) => register.status.toLowerCase() === "active" && !register.hasOpenShift,
  );
  if (freeActive) {
    return freeActive;
  }

  if (named && named.status.toLowerCase() === "active" && named.hasOpenShift) {
    throw new Error("PWA_DEFAULT_REGISTER_BUSY");
  }

  try {
    return await createRegister(workspace, {
      name: PWA_DEFAULT_REGISTER_NAME,
      description: "Auto-created cash register for web POS (PWA).",
    });
  } catch (error) {
    const again = await listRegisters(workspace, { page: 1, pageSize: 50 });
    const reused =
      again.items.find(
        (register) =>
          isPwaDefaultName(register.name) &&
          register.status.toLowerCase() === "active" &&
          !register.hasOpenShift,
      ) ??
      again.items.find(
        (register) => register.status.toLowerCase() === "active" && !register.hasOpenShift,
      );
    if (reused) {
      return reused;
    }
    throw error;
  }
}

export function toRegisterSummary(register: PosRegisterDto): PosRegisterSummaryDto {
  return {
    registerId: register.registerId,
    registerCode: register.registerCode,
    name: register.name,
    status: register.status,
  };
}
