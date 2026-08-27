import { useAppOnline, useOptionalConnectivity } from "@/connectivity/ConnectivityProvider";
import { organizationWebAllowsOfflineBusinessMutations } from "@/runtime/organization-web-runtime-policy";

/**
 * Central gate for Organization business writes on Web/PWA.
 * Pages should disable primary actions and show contextual feedback when blocked.
 */
export function useOrganizationMutationGuard(): {
  online: boolean;
  canMutate: boolean;
  blocksMutations: boolean;
} {
  const online = useAppOnline();
  const connectivity = useOptionalConnectivity();
  const blocksMutations = connectivity?.blocksMutations ?? !online;
  const canMutate =
    !blocksMutations && (online || organizationWebAllowsOfflineBusinessMutations());

  return { online, canMutate, blocksMutations };
}
