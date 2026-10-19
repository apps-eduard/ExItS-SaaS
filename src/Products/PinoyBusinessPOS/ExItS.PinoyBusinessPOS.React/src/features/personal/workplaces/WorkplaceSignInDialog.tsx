import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useQueryClient } from "@tanstack/react-query";
import { Loader2, LogIn } from "lucide-react";
import {
  requestWorkplacePasswordReset,
  type PersonalWorkplaceWire,
} from "@/api/platform/personal-workplaces-client";
import { ExitsOutlineInput } from "@/components/exits/ExitsOutlineField";
import { ExitsModal } from "@/components/exits/ExitsModal";
import { Notice } from "@/components/exits/Notice";
import { Button } from "@/components/ui/button";
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
    <ExitsModal
      open={open}
      onOpenChange={(next) => {
        if (!next) {
          onClose();
        }
      }}
      title={workplace.organizationDisplayName}
      ribbon
      ribbonIcon={LogIn}
      id="workplace-sign-in-dialog"
      testId="workplace-sign-in-dialog"
      closeLabel={t("personal.install.close")}
      busy={signingIn}
      onSubmit={() => {
        void onLogin();
      }}
      footer={
        <div className="flex w-full flex-nowrap items-center justify-end gap-2">
          <Button
            type="submit"
            intent="primary"
            appearance="glossy"
            className="min-w-[8rem]"
            disabled={signingIn || !online || !username.trim() || !password.trim()}
            data-testid="workplace-sign-in-submit"
          >
            {signingIn ? <Loader2 className="size-4 animate-spin" aria-hidden /> : <LogIn className="size-4 shrink-0" aria-hidden />}
            {t("personal.workplaces.login")}
          </Button>
          {resetPending ? null : (
            <Button
              type="button"
              intent="neutral"
              appearance="glossy"
              disabled={resetting || !online}
              data-testid="workplace-sign-in-forgot"
              onClick={() => void onForgot()}
            >
              {t("personal.workplaces.forgotPassword")}
            </Button>
          )}
        </div>
      }
    >
      <div className="flex flex-col gap-3">
        <ExitsOutlineInput
          id="workplace-sign-in-username"
          label={t("personal.workplaces.username")}
          autoComplete="username"
          value={username}
          onChange={setUsername}
          readOnly
          testId="workplace-sign-in-username"
        />
        <ExitsOutlineInput
          id="workplace-sign-in-password"
          label={t("personal.workplaces.passwordLabel")}
          type="password"
          autoComplete="current-password"
          value={password}
          onChange={setPassword}
          testId="workplace-sign-in-password"
          data-exits-modal-autofocus="true"
        />
        {resetPending ? (
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("personal.workplaces.resetRequested")}
          </p>
        ) : null}
        {notice ? <Notice tone="success">{notice}</Notice> : null}
        {error ? <Notice tone="danger">{error}</Notice> : null}
      </div>
    </ExitsModal>
  );
}
