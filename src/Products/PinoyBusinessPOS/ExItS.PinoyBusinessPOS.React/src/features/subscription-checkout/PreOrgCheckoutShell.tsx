import { Outlet, useNavigate } from "react-router-dom";
import { AccountMenu } from "@/components/exits/AccountMenu";
import { useI18n } from "@/i18n/I18nProvider";
import { useSession } from "@/session/SessionProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";
import { useState } from "react";

/**
 * Authenticated pre-organization shell for subscription checkout.
 * No organization sidebar, workspace chooser, or POS bottom nav.
 */
export function PreOrgCheckoutShell() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const { signOut } = useSession();
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
    navigate(result.nextRoute, { replace: true });
  }

  return (
    <div
      className="flex min-h-0 min-w-0 flex-1 flex-col bg-[var(--exits-surface,transparent)]"
      data-testid="pre-org-checkout-shell"
    >
      <header className="app-top-bar app-top-bar--personal" data-testid="pre-org-checkout-top-bar">
        <div className="app-top-bar__row">
          <div className="app-top-bar__brand min-w-0 flex-1">
            <div className="app-top-bar__brand-copy">
                  <p className="app-top-bar__workspace-org m-0 truncate">{t("app.name")}</p>
              <p className="app-top-bar__workspace-branch m-0 truncate">
                {t("subscriptionCheckout.preOrgBadge")}
              </p>
            </div>
          </div>
          <div className="app-top-bar__actions">
            <AccountMenu signingOut={signingOut} onSignOut={() => void handleSignOut()} compact />
          </div>
        </div>
      </header>
      <div className="mx-auto flex w-full max-w-lg min-h-0 min-w-0 flex-1 flex-col gap-4 px-4 pb-8 pt-4">
        <Outlet />
      </div>
    </div>
  );
}
