import { createWebHandoff, listWebWorkspaces } from "@/api/auth/auth-client";
import type { AuthSession } from "@/api/auth/auth-types";
import {
  buildSessionEstablishUrl,
  choosePostAdminLoginWorkspace,
  planWorkspaceLaunch,
  replaceBrowserLocation,
  resolveExperienceOrigins,
  type ExperienceOrigins,
} from "@/lib/auth/cutover-routing";
import { resolvePostLoginPath } from "@/lib/auth/safe-return-path";
import { env, isLocalValidationToolsEnabled } from "@/lib/env";

export type AdminLoginContinuation = "platform" | "handoff" | "stay";

export function readConfiguredExperienceOrigins(): ExperienceOrigins {
  const runtime = typeof window === "undefined" ? undefined : window.__EXITS_PLATFORM_ADMIN_WEB__;
  return resolveExperienceOrigins({
    organization: runtime?.organizationWebOrigin,
    personal: runtime?.personalWebOrigin,
    localValidation: isLocalValidationToolsEnabled(),
    pageHost: typeof window === "undefined" ? null : window.location.hostname,
  });
}

export function isProductAccountClass(accountClass: string | null | undefined): boolean {
  const value = (accountClass ?? "").trim().toLowerCase();
  return value === "personal" || value === "organization";
}

/**
 * Platform accounts stay on this admin host.
 * Any other account with an organization opens POS. A personal account with no organization opens Personal.
 * The product host receives a short-lived handoff ticket so the person stays signed in.
 */
export async function continueAfterAdminAuthentication(input: {
  session: Pick<AuthSession, "accountClass" | "selectedOrganizationId">;
  returnQuery: string | null;
  navigate: (path: string) => void;
  assignLocation?: (url: string) => void;
}): Promise<AdminLoginContinuation> {
  if (!isProductAccountClass(input.session.accountClass)) {
    input.navigate(resolvePostLoginPath(input.returnQuery));
    return "platform";
  }

  const listed = await listWebWorkspaces(env.platformApiBaseUrl);
  const target = choosePostAdminLoginWorkspace(
    listed.workspaces,
    input.session.selectedOrganizationId,
  );
  if (!target) {
    return "stay";
  }

  const plan = planWorkspaceLaunch(target, readConfiguredExperienceOrigins());
  if (plan.kind === "platform") {
    input.navigate(plan.path);
    return "platform";
  }
  if (plan.kind === "origin-missing") {
    throw new Error("workspace origin missing");
  }

  const created = await createWebHandoff(
    env.platformApiBaseUrl,
    plan.targetApp,
    plan.organizationId,
    plan.returnPath,
  );
  const url = buildSessionEstablishUrl(
    plan.origin,
    created.ticket,
    created.returnPath || plan.returnPath,
  );
  if (!url) {
    throw new Error("workspace origin missing");
  }
  (input.assignLocation ?? replaceBrowserLocation)(url);
  return "handoff";
}
