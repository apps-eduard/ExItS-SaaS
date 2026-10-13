import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useQueryClient } from "@tanstack/react-query";
import { Loader2 } from "lucide-react";
import {
  requestWorkplacePasswordReset,
  type PersonalWorkplaceWire,
} from "@/api/platform/personal-workplaces-client";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { BottomSheet } from "@/components/exits/SheetDialog";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { resolveAuthLoginFailurePresentation, isHandledSignInFailure } from "@/diagnostics/auth-login-failure";
import { ACCOUNT_CONTEXT_SWITCH_PATH } from "@/features/account/account-context-switch-route";
import { useI18n } from "@/i18n/I18nProvider";
import { useSession } from "@/session/SessionProvider";

export function WorkplaceSignInDialog({
  workplace,
  open,
  onClose,
  returnPath,
}: {
  workplace: PersonalWorkplaceWire | null;
  open: boolean;
  onClose: () => void;
  returnPath: string;
}) {
  const { t } = useI18n();
  const online = useBrowserOnline();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { signIn } = useSession();
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [signingIn, setSigningIn] = useState(false);
  const [resetting, setResetting] = useState(false);

  useEffect(() => {
    if (!open || !workplace) {
      return;
    }
    setUsername(workplace.staffLogin);
    setPassword("");
    setError(null);
    setNotice(null);
  }, [open, workplace]);

  if (!workplace) {
    return null;
  }

  const resetPending = workplace.passwordResetStatus === "Pending";

  async function onLogin() {
    const login = username.trim();
    const workplacePassword = password.trim();
    if (!online || !login || !workplacePassword || !workplace) {
      setError(t("personal.workplaces.passwordLabel"));
      return;
    }
    setError(null);
    setSigningIn(true);
    navigate(ACCOUNT_CONTEXT_SWITCH_PATH, { replace: true });
    const result = await signIn(login, workplacePassword);
    if (!result.ok) {
      const presentation = resolveAuthLoginFailurePresentation(result.failure, t);
      const message = isHandledSignInFailure(result.failure)
        ? presentation.friendlyMessage
        : presentation.friendlyMessage || t("personal.workplaces.login");
      setSigningIn(false);
      navigate(returnPath, {
        replace: true,
        state: { workplaceSignInError: message },
      });
      return;
    }
    navigate("/", { replace: true });
  }

  async function onForgot() {
    if (!online || !workplace || resetPending) {
      return;
    }
    setError(null);
    setNotice(null);
    setResetting(true);
    const result = await requestWorkplacePasswordReset(workplace.membershipId);
    setResetting(false);
    if (!result.ok) {
      setError(result.body?.detail ?? t("personal.workplaces.resetRequested"));
      return;
    }
    setNotice(t("personal.workplaces.resetRequestSent"));
    await queryClient.invalidateQueries({ queryKey: ["personal", "workplaces"] });
  }

  return (
    <BottomSheet
      open={open}
      onClose={onClose}
      title={workplace.organizationDisplayName}
      panelId="workplace-sign-in-dialog"
      testId="workplace-sign-in-dialog"
      closeLabel={t("personal.install.close")}
      presentation="sheet-mobile-dialog-desktop"
    >
      <form
        className="flex flex-col gap-3"
        onSubmit={(event) => {
          event.preventDefault();
          void onLogin();
        }}
      >
        <Input
          id="workplace-sign-in-username"
          label={t("personal.workplaces.username")}
          autoComplete="username"
          value={username}
          onChange={(event) => setUsername(event.target.value)}
          data-testid="workplace-sign-in-username"
        />
        <Input
          id="workplace-sign-in-password"
          label={t("personal.workplaces.passwordLabel")}
          type="password"
          autoComplete="current-password"
          value={password}
          onChange={(event) => setPassword(event.target.value)}
          data-testid="workplace-sign-in-password"
        />
        {resetPending ? (
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("personal.workplaces.resetRequested")}
          </p>
        ) : null}
        {notice ? (
          <p className="m-0 text-[length:var(--exits-text-sm)]" role="status">
            {notice}
          </p>
        ) : null}
        {error ? (
          <p className="m-0 text-[length:var(--exits-text-sm)] text-danger" role="alert">
            {error}
          </p>
        ) : null}
        <Button
          type="submit"
          disabled={signingIn || !online || !username.trim() || !password.trim()}
          data-testid="workplace-sign-in-submit"
        >
          {signingIn ? <Loader2 className="size-4 animate-spin" aria-hidden /> : null}
          {t("personal.workplaces.login")}
        </Button>
        {resetPending ? null : (
          <Button
            type="button"
            variant="ghost"
            disabled={resetting || !online}
            data-testid="workplace-sign-in-forgot"
            onClick={() => void onForgot()}
          >
            {t("personal.workplaces.forgotPassword")}
          </Button>
        )}
      </form>
    </BottomSheet>
  );
}
