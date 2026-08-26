export type PwaUpdateApplyGuard = () => boolean;

export function applyPwaUpdateIfAllowed(
  apply: () => void,
  guard: PwaUpdateApplyGuard = () => true,
): boolean {
  if (!guard()) {
    return false;
  }
  apply();
  return true;
}
