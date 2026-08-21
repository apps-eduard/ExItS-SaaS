import type { PosDeviceCapacityDto } from "@/api/platform/pos-devices-client";

/**
 * Server-authoritative POS device capacity.
 * Allowed is always a finite MaxActivePosDevices value (Plan domain: 1..10000).
 * Do not invent an "unlimited" sentinel from large Allowed values.
 */
export type FormattedDeviceCapacity = {
  kind: "finite";
  used: number;
  allowed: number;
  available: number;
  atLimit: boolean;
  progressRatio: number;
};

export function formatPosDeviceCapacity(
  capacity: PosDeviceCapacityDto | null | undefined,
): FormattedDeviceCapacity | null {
  if (!capacity) {
    return null;
  }
  const used = Math.max(0, Number(capacity.used) || 0);
  const allowed = Math.max(0, Number(capacity.allowed) || 0);
  const available = Math.max(0, allowed - used);
  const atLimit = used >= allowed && allowed > 0;
  const progressRatio = allowed > 0 ? Math.min(1, used / allowed) : 0;

  return {
    kind: "finite",
    used,
    allowed,
    available,
    atLimit,
    progressRatio,
  };
}
