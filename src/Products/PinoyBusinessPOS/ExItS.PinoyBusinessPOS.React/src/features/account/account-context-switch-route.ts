/** Neutral route used while Personal ↔ Organization profile / workspace context changes. */
export const ACCOUNT_CONTEXT_SWITCH_PATH = "/switching-context";

export function isAccountContextSwitchPath(pathname: string): boolean {
  return pathname === ACCOUNT_CONTEXT_SWITCH_PATH;
}
