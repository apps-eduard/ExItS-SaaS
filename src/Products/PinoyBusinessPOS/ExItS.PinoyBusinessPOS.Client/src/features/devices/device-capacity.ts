import type { PosDeviceCapacityDto } from "@/api/platform/pos-devices-client";

/** Server sentinel for unlimited device slots. */
export const UNLIMITED_DEVICE_CAPACITY_THRESHOLD = 10_000;

export type FormattedDeviceCapacity =
  | {
      kind: "unlimited";
      used: number;
      progressRatio: null;
    }
  | {
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

  if (allowed >= UNLIMITED_DEVICE_CAPACITY_THRESHOLD) {
    return { kind: "unlimited", used, progressRatio: null };
  }

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
