import { useEffect, useState, type FormEvent } from "react";
import { Eye, EyeOff, Info, LayoutGrid } from "lucide-react";
import { Link, useLocation, useNavigate } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { ErrorState } from "@/components/exits/ErrorState";
import { AuthExperienceLayout, AuthOrDivider } from "@/features/auth/AuthExperienceLayout";
import { cn } from "@/lib/cn";
import {
  persistRememberedUsername,
  readRememberMePreference,
  readRememberedUsername,
} from "@/features/auth/remember-me";
import { TestUserSelector } from "@/features/auth/TestUserSelector";
import { useI18n } from "@/i18n/I18nProvider";
import { registerPersonalAccount } from "@/api/platform/platform-auth-client";
import { evaluateOfflinePinLoginOffer } from "@/offline/offline-pin-login-offer";
import { mapColdStartDenialToMessageKey } from "@/offline/offline-operating-grant";
import { prefetchPlatformAntiforgeryToken } from "@/api/platform/platform-http";
import { looksLikeOrgScopedStaffLogin } from "@/session/account-class";
import { useSession } from "@/session/SessionProvider";
import type { AuthLoginFailureDiagnostic } from "@/diagnostics/auth-login-failure";
import {
  authLoginFailureToPosErrorReport,
  buildAuthLoginFailure,
  resolveAuthLoginFailurePresentation,
} from "@/diagnostics/auth-login-failure";
type AuthTab = "sign-in" | "sign-up";

function AuthInlineFeedback({
  message,
  testId,
  tone = "error",
}: {
  message: string;
  testId: string;
  tone?: "error" | "success";
}) {
  return (
    <p
      role="alert"
      data-testid={testId}
      className={cn(
        "m-0 rounded-[var(--exits-radius-md)] px-3 py-2 text-[length:var(--exits-text-sm)] leading-relaxed",
        tone === "success"
          ? "border border-success/30 bg-success/5 text-success"
          : "border border-destructive/30 bg-destructive/5 text-destructive",
      )}
    >
      {message}
    </p>
  );
}

export function SignInPage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const location = useLocation();
  const { signIn, status, coldStartDenial } = useSession();
  const [activeTab, setActiveTab] = useState<AuthTab>("sign-in");
  const [usernameOrEmail, setUsernameOrEmail] = useState(() => readRememberedUsername());
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [rememberMe, setRememberMe] = useState(() => readRememberMePreference());
  const [showUsernameHint, setShowUsernameHint] = useState(false);
  const [signUpName, setSignUpName] = useState("");
  const [signUpEmail, setSignUpEmail] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [signInFailure, setSignInFailure] = useState<{
    failure: AuthLoginFailureDiagnostic;
    title: string;
    detail: string;
    friendlyMessage: string;
  } | null>(null);
  const [info, setInfo] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [isOffline, setIsOffline] = useState(
    () => typeof navigator !== "undefined" && navigator.onLine === false,
  );
  const [canUsePin, setCanUsePin] = useState(false);
  const [pinNoEnrollment, setPinNoEnrollment] = useState(false);
  const [pinGrantExpired, setPinGrantExpired] = useState(false);
  const expired = Boolean((location.state as { expired?: boolean } | null)?.expired);
  const staffLoginHint = looksLikeOrgScopedStaffLogin(usernameOrEmail);
  const offlineLocked =
    status === "unauthenticated" &&
    coldStartDenial != null &&
    isOffline;
  const offlineLockedDetail = offlineLocked
    ? t(mapColdStartDenialToMessageKey(coldStartDenial))
    : null;

  useEffect(() => {
    const syncOnline = () => setIsOffline(typeof navigator !== "undefined" && navigator.onLine === false);
    window.addEventListener("online", syncOnline);
    window.addEventListener("offline", syncOnline);
    return () => {
      window.removeEventListener("online", syncOnline);
      window.removeEventListener("offline", syncOnline);
    };
  }, []);

  useEffect(() => {
    void prefetchPlatformAntiforgeryToken();
  }, []);

  useEffect(() => {
    let cancelled = false;
    void evaluateOfflinePinLoginOffer().then((offer) => {
      if (cancelled) {
        return;
      }
      setCanUsePin(offer.canOfferPinUnlock);
      setPinNoEnrollment(offer.noEnrollment && !offer.canOfferPinUnlock);
      setPinGrantExpired(offer.grantExpired);
    });
    return () => {
      cancelled = true;
    };
  }, [isOffline, status]);

  async function handleSignInSubmit(event: FormEvent) {
    event.preventDefault();
    if (isOffline) {
      setError(t("auth.offlinePasswordBlocked"));
      return;
    }
    setSubmitting(true);
    setError(null);
    setSignInFailure(null);
    setInfo(null);
    persistRememberedUsername(usernameOrEmail, rememberMe);
    try {
      const result = await signIn(usernameOrEmail.trim(), password);
      if (!result.ok) {
        setSignInFailure({
          failure: result.failure,
          ...resolveAuthLoginFailurePresentation(result.failure, t),
        });
        return;
      }
      navigate("/", { replace: true });
    } catch (caught) {
      const failure = buildAuthLoginFailure(caught);
      setSignInFailure({
        failure,
        ...resolveAuthLoginFailurePresentation(failure, t),
      });
    } finally {
      setSubmitting(false);
    }
  }

  async function handleSignUpSubmit(event: FormEvent) {
    event.preventDefault();
    if (isOffline) {
      setError(t("auth.offlineSignUpBlocked"));
      return;
    }
    setSubmitting(true);
    setError(null);
    setInfo(null);
    const result = await registerPersonalAccount(signUpName.trim(), signUpEmail.trim());
    setSubmitting(false);
    if (!result.ok) {
      setError(result.detail);
      return;
    }
    setInfo(t("auth.signUpAck"));
    setActiveTab("sign-in");
    setUsernameOrEmail(signUpEmail.trim());
    setPassword("");
  }

  function handlePinLogin() {    if (pinGrantExpired) {
      setError(t("auth.offlineGrantExpired"));
      return;
    }
    if (!canUsePin) {
      setError(t("auth.offlinePinUnavailable"));
      return;
    }
    navigate("/offline-pin", { replace: true });
  }

  const offlineBanner =
    isOffline || offlineLocked ? (
      <div
        className="rounded-[var(--exits-radius-md)] border border-border bg-[var(--exits-surface-muted)] px-4 py-3"
        data-testid="sign-in-offline-banner"
      >
        <p className="m-0 text-[length:var(--exits-text-sm)] font-semibold text-foreground">
          {t("auth.offlineBannerTitle")}
        </p>
        <p className="m-0 mt-1 text-[length:var(--exits-text-sm)] leading-relaxed text-muted">
          {offlineLockedDetail ?? t("auth.offlineBannerDetail")}
        </p>
        {pinNoEnrollment ? (
          <p className="m-0 mt-2 text-[length:var(--exits-text-sm)] leading-relaxed text-muted">
            {t("auth.offlinePinUnavailable")}
          </p>
        ) : null}
        {pinGrantExpired ? (
          <p className="m-0 mt-2 text-[length:var(--exits-text-sm)] leading-relaxed text-muted">
            {t("auth.offlineGrantExpired")}
          </p>
        ) : null}
      </div>
    ) : null;

  return (
    <div data-testid="sign-in-page" data-exits-build-mode={import.meta.env.MODE}>
      <AuthExperienceLayout
      activeTab={activeTab}
      onTabChange={(tab) => {
        setActiveTab(tab);
        setError(null);
        setSignInFailure(null);
        setInfo(null);
      }}
      offlineBanner={offlineBanner}
      belowCard={
        import.meta.env.MODE !== "production" ? (
          <TestUserSelector
            onSelectIdentity={(value) => {
              setUsernameOrEmail(value);
              setPassword("");
              setError(null);
              setSignInFailure(null);
              setActiveTab("sign-in");
            }}
          />
        ) : null
      }
    >
      {expired ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{t("signIn.expired")}</p>
      ) : null}
      {info ? <AuthInlineFeedback message={info} testId="auth-info" tone="success" /> : null}
      {signInFailure ? (
        <ErrorState
          title={signInFailure.title}
          detail={signInFailure.detail}
          diagnostic={authLoginFailureToPosErrorReport(
            signInFailure.failure,
            signInFailure.friendlyMessage,
          )}
        />
      ) : error ? (
        <AuthInlineFeedback message={error} testId="auth-error" />
      ) : null}

      {activeTab === "sign-in" ? (
        <form className="flex flex-col gap-4" onSubmit={(event) => void handleSignInSubmit(event)}>
          <Input
            label={t("signIn.usernameLabel")}
            name="usernameOrEmail"
            autoComplete="username"
            value={usernameOrEmail}
            onChange={(event) => setUsernameOrEmail(event.target.value)}
            required
            disabled={submitting || isOffline}
            labelAccessory={
              <button
                type="button"
                className="inline-flex size-8 items-center justify-center rounded-full text-[var(--exits-info)] hover:bg-[var(--exits-surface-muted)]"
                aria-expanded={showUsernameHint}
                aria-controls="sign-in-username-hint"
                aria-label={
                  showUsernameHint ? t("auth.usernameHintHide") : t("auth.usernameHintShow")
                }
                data-testid="sign-in-username-hint-toggle"
                onClick={() => setShowUsernameHint((value) => !value)}
              >
                <Info className="size-4" aria-hidden />
              </button>
            }
          />
          {showUsernameHint ? (
            <p
              id="sign-in-username-hint"
              className="m-0 text-[length:var(--exits-text-sm)] leading-relaxed text-muted"
              data-testid="sign-in-username-hint"
            >
              {t("signIn.usernameHint")}
            </p>
          ) : null}
          {staffLoginHint ? (
            <p className="m-0 text-[length:var(--exits-text-sm)] leading-relaxed text-muted" data-testid="staff-login-hint">
              {t("signIn.staffLoginHint")}
            </p>
          ) : null}

          <div className="relative">
            <Input
              label={t("signIn.passwordLabel")}
              name="password"
              type={showPassword ? "text" : "password"}
              autoComplete="current-password"
              value={password}
              onChange={(event) => setPassword(event.target.value)}
              required
              disabled={submitting || isOffline}
            />
            <button
              type="button"
              className="absolute right-3 top-[2.15rem] inline-flex size-9 items-center justify-center rounded-[var(--exits-radius-sm)] text-muted hover:text-foreground"
              aria-label={showPassword ? t("auth.hidePassword") : t("auth.showPassword")}
              onClick={() => setShowPassword((value) => !value)}
              disabled={submitting}
            >
              {showPassword ? <EyeOff className="size-5" aria-hidden /> : <Eye className="size-5" aria-hidden />}
            </button>
          </div>

          <div className="flex flex-wrap items-center justify-between gap-3">
            <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)] text-foreground">
              <input
                type="checkbox"
                className="auth-remember-checkbox"
                checked={rememberMe}
                disabled={submitting}
                onChange={(event) => setRememberMe(event.target.checked)}
              />
              <span>{t("auth.rememberMe")}</span>
            </label>
            <Link
              to="/forgot-password"
              className="text-[length:var(--exits-text-sm)] font-semibold text-[var(--exits-info)] hover:underline"
              data-testid="auth-forgot-password-link"
            >
              {t("auth.forgotPassword")}
            </Link>
          </div>

          <Button
            type="submit"
            className="auth-submit-button w-full"
            disabled={submitting || isOffline}
            data-testid="sign-in-submit"
          >
            {submitting ? t("signIn.submitting") : t("signIn.submit")}
          </Button>
        </form>
      ) : (
        <form className="flex flex-col gap-4" onSubmit={(event) => void handleSignUpSubmit(event)}>
          <p className="m-0 text-[length:var(--exits-text-sm)] leading-relaxed text-muted">
            {t("auth.signUpLede")}
          </p>
          <Input
            label={t("auth.signUpNameLabel")}
            name="displayName"
            autoComplete="name"
            value={signUpName}
            onChange={(event) => setSignUpName(event.target.value)}
            required
            disabled={submitting || isOffline}
          />
          <Input
            label={t("auth.signUpEmailLabel")}
            name="email"
            type="email"
            autoComplete="email"
            value={signUpEmail}
            onChange={(event) => setSignUpEmail(event.target.value)}
            required
            disabled={submitting || isOffline}
          />
          <Button type="submit" className="auth-submit-button w-full" disabled={submitting || isOffline}>
            {submitting ? t("auth.signUpSubmitting") : t("auth.signUpSubmit")}
          </Button>
        </form>
      )}

      <AuthOrDivider />

      <div className="flex flex-col gap-3" data-testid="auth-alternate-sign-in">
        <Button
          type="button"
          variant="ghost"
          className="auth-submit-button inline-flex w-full items-center justify-center gap-2 border border-border bg-surface hover:bg-[var(--exits-surface-muted)]"
          data-testid="auth-pin-button"
          disabled={submitting || (isOffline && !canUsePin)}
          onClick={handlePinLogin}
        >
          <LayoutGrid className="size-5 shrink-0" aria-hidden />
          {t("auth.continueWithPin")}
        </Button>
        {isOffline ? (
          <p className="m-0 text-center text-[length:var(--exits-text-xs)] leading-relaxed text-muted">
            {t("auth.socialHelperOffline")}
          </p>
        ) : null}
      </div>    </AuthExperienceLayout>
    </div>
  );
}
