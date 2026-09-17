import { StatusChip } from "@/components/exits/StatusChip";
import { useI18n } from "@/i18n/I18nProvider";
import { resolveAuthenticatedRoleLabelKey } from "@/lib/authenticated-role-label";
import { useSession } from "@/session/SessionProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

type RoleIdentityChipProps = {
  /** Stable test id for page headers (defaults to shared role-identity chip). */
  testId?: string;
};

/**
 * Informational header metadata: authenticated org/POS role — never workspace.
 * Omits when role data is unavailable (no workspace fallback).
 */
export function RoleIdentityChip({ testId = "role-identity-chip" }: RoleIdentityChipProps) {
  const { t } = useI18n();
  const { session } = useSession();
  const { sessionGrant } = useWorkspace();
  const labelKey = resolveAuthenticatedRoleLabelKey(session, sessionGrant);
  if (!labelKey) {
    return null;
  }

  return (
    <span data-testid={testId}>
      <StatusChip tone="primary" className="role-identity-chip">
        {t(labelKey)}
      </StatusChip>
    </span>
  );
}
