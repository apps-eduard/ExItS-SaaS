export type PwaUpdateApplyGuard = () => boolean;

/**
 * Future cart/checkout can return false to block a waiting update.
 * No cart exists in POS-REACT-IMPL-02, so the default allows the user-triggered apply.
 */
export function canApplyPwaUpdate(): boolean {
  return true;
}

export function applyPwaUpdateIfAllowed(
  apply: () => void,
  guard: PwaUpdateApplyGuard = canApplyPwaUpdate,
): boolean {
  if (!guard()) {
    return false;
  }
  apply();
  return true;
}
