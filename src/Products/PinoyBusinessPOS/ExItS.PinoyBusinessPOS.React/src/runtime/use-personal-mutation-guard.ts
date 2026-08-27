import { useAppOnline, useOptionalConnectivity } from "@/connectivity/ConnectivityProvider";
import { personalWebAllowsOfflineBusinessMutations } from "@/runtime/personal-web-runtime-policy";

/**
 * Central gate for Personal business writes on Web/PWA.
 * Pages should disable primary actions and show contextual feedback when blocked.
 */
export function usePersonalMutationGuard(): {
  online: boolean;
  canMutate: boolean;
  blocksMutations: boolean;
} {
  const online = useAppOnline();
  const connectivity = useOptionalConnectivity();
  const blocksMutations = connectivity?.blocksMutations ?? !online;
  const canMutate =
    !blocksMutations && (online || personalWebAllowsOfflineBusinessMutations());

  return { online, canMutate, blocksMutations };
}
