import type { PosSessionGrantFacts } from "@/access/pos-capabilities";
import {
  isOrganizationAdministratorMembership,
  isOrganizationOwnerMembership,
  resolveEffectivePosRoleCode,
} from "@/access/pos-capabilities";
import { resolveFriendlyPosRole } from "@/lib/user-display";
import {
  sessionAccountClass,
  type AccountClassName,
} from "@/session/account-class";
import type { BrowserSessionSnapshot } from "@/api/platform/browser-session";

/** i18n keys for concise authenticated-role display (workspace-independent). */
export type AuthenticatedRoleLabelKey =
  | "personal.badge"
  | "account.role.owner"
  | "account.role.admin"
  | "account.role.manager"
  | "account.role.cashier";

/**
 * Canonical display role from session + org membership / POS grant.
 * Workspace / route / page title must never feed this — authorization identity only.
 */
export function resolveAuthenticatedRoleLabelKey(
  session: BrowserSessionSnapshot | null | undefined,
  sessionGrant: PosSessionGrantFacts | null | undefined,
): AuthenticatedRoleLabelKey | null {
  const accountClass: AccountClassName | null = sessionAccountClass(session);
  if (accountClass === "Personal") {
    return "personal.badge";
  }

  if (isOrganizationOwnerMembership(sessionGrant)) {
    return "account.role.owner";
  }
  if (isOrganizationAdministratorMembership(sessionGrant)) {
    return "account.role.admin";
  }

  const friendlyRole = resolveFriendlyPosRole(resolveEffectivePosRoleCode(sessionGrant));
  if (friendlyRole === "owner") {
    return "account.role.owner";
  }
  if (friendlyRole === "manager") {
    return "account.role.manager";
  }
  if (friendlyRole === "cashier") {
    return "account.role.cashier";
  }
  return null;
}
