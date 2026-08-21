import { useState } from "react";
import { Outlet, useNavigate } from "react-router-dom";
import { AccountMenu } from "@/components/exits/AccountMenu";
import { PersonalBottomNav } from "@/features/personal/PersonalBottomNav";
import { useI18n } from "@/i18n/I18nProvider";
import { useSession } from "@/session/SessionProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

export function PersonalShell() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const { session, signOut } = useSession();
  const { clearBoundWorkspace } = useWorkspace();
  const [signingOut, setSigningOut] = useState(false);

  async function handleSignOut() {
    if (signingOut) {
      return;
    }
    setSigningOut(true);
    const result = await signOut();
    if (!result.ok) {
      setSigningOut(false);
      return;
    }
    clearBoundWorkspace();
    navigate("/sign-in", { replace: true });
  }

  return (
    <div className="flex min-h-0 min-w-0 flex-1 flex-col" data-testid="personal-shell">
      <header
        className="flex min-w-0 items-center justify-between gap-3 border-b border-border py-2"
        data-testid="personal-top-bar"
      >
        <div className="min-w-0">
          <p className="m-0 truncate text-[length:var(--exits-text-sm)] font-semibold text-foreground">
            {session?.displayName || t("personal.badge")}
          </p>
          <p className="m-0 truncate text-[length:var(--exits-text-xs)] text-muted">
            {t("personal.badge")}
          </p>
        </div>
        <AccountMenu signingOut={signingOut} onSignOut={() => void handleSignOut()} compact />
      </header>

      <div className="flex min-h-0 min-w-0 flex-1 flex-col gap-4 pb-20 pt-4">
        <Outlet />
      </div>

      <PersonalBottomNav />
    </div>
  );
}
