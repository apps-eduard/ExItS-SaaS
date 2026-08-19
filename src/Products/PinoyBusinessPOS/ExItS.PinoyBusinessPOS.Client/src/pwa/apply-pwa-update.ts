/**
 * Future cart/form dirty-state can return false to keep the user on the current
 * version. This package has no cart; the default always allows an explicit Refresh.
 */
export type PwaUpdateApplyGuard = () => boolean;

export const allowPwaUpdateApply: PwaUpdateApplyGuard = () => true;

export function applyPwaUpdateIfAllowed(
  apply: () => void,
  guard: PwaUpdateApplyGuard = allowPwaUpdateApply,
): boolean {
  if (!guard()) {
    return false;
  }
  apply();
  return true;
}
