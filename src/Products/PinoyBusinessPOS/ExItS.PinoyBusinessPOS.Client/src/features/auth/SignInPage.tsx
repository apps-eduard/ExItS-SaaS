import { useEffect, useState, type FormEvent } from "react";
import { Eye, EyeOff, Info, LayoutGrid } from "lucide-react";
import { Link, useLocation, useNavigate } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { AuthExperienceLayout, AuthOrDivider } from "@/features/auth/AuthExperienceLayout";
import { CircularAuthButton } from "@/features/auth/CircularAuthButton";
import { cn } from "@/lib/cn";
import {
  persistRememberedUsername,
  readRememberMePreference,
  readRememberedUsername,
} from "@/features/auth/remember-me";
import { TestUserSelector } from "@/features/auth/TestUserSelector";
import { useI18n } from "@/i18n/I18nProvider";
import {
  buildExternalAuthChallengeUrl,
  probeExternalAuthProvider,
  registerPersonalAccount,
  type ExternalAuthProviderAvailability,
} from "@/api/platform/platform-auth-client";
import { evaluateOfflinePinLoginOffer } from "@/offline/offline-pin-login-offer";
import { mapColdStartDenialToMessageKey } from "@/offline/offline-operating-grant";
import { prefetchPlatformAntiforgeryToken } from "@/api/platform/platform-http";
import { looksLikeOrgScopedStaffLogin } from "@/session/account-class";
import { useSession } from "@/session/SessionProvider";

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
  const [info, setInfo] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [isOffline, setIsOffline] = useState(
    () => typeof navigator !== "undefined" && navigator.onLine === false,
  );
  const [canUsePin, setCanUsePin] = useState(false);
  const [pinNoEnrollment, setPinNoEnrollment] = useState(false);
  const [pinGrantExpired, setPinGrantExpired] = useState(false);
  const [googleAvailability, setGoogleAvailability] =
    useState<ExternalAuthProviderAvailability>("disabled");
  const [facebookAvailability, setFacebookAvailability] =
    useState<ExternalAuthProviderAvailability>("disabled");

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

  useEffect(() => {
    if (isOffline) {
      setGoogleAvailability("offline");
      setFacebookAvailability("offline");
      return;
    }
    let cancelled = false;
    void Promise.all([probeExternalAuthProvider("google"), probeExternalAuthProvider("facebook")]).then(
      ([google, facebook]) => {
        if (cancelled) {
          return;
        }
        setGoogleAvailability(google);
        setFacebookAvailability(facebook);
      },
    );
    return () => {
      cancelled = true;
    };
  }, [isOffline]);

  async function handleSignInSubmit(event: FormEvent) {
    event.preventDefault();
    if (isOffline) {
      setError(t("auth.offlinePasswordBlocked"));
      return;
    }
    setSubmitting(true);
    setError(null);
    setInfo(null);
    persistRememberedUsername(usernameOrEmail, rememberMe);
    const ok = await signIn(usernameOrEmail.trim(), password);
    setSubmitting(false);
    if (!ok) {
      setError(t("signIn.error"));
      return;
    }
    navigate("/", { replace: true });
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

  function handleExternalProvider(
    provider: "google" | "facebook",
    availability: ExternalAuthProviderAvailability,
  ) {
    if (availability === "offline") {
      setError(t("auth.providerOffline"));
      return;
    }
    if (availability === "disabled") {
      setError(t("auth.providerUnavailable"));
      return;
    }
    window.location.assign(
      buildExternalAuthChallengeUrl(provider, `${window.location.origin}/sign-in`),
    );
  }

  function handlePinLogin() {
    if (pinGrantExpired) {
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
      {error ? <AuthInlineFeedback message={error} testId="auth-error" /> : null}

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

      <div className="flex flex-col items-center gap-3" data-testid="auth-social-row">
        <div className="flex flex-wrap items-center justify-center gap-4">
          <CircularAuthButton
            testId="auth-facebook-button"
            label={t("auth.continueWithFacebook")}
            variant="facebook"
            disabled={submitting || isOffline}
            onClick={() => handleExternalProvider("facebook", facebookAvailability)}
            icon={
              <svg viewBox="0 0 24 24" width="20" height="20" aria-hidden focusable="false">
                <path
                  fill="currentColor"
                  d="M24 12.07C24 5.41 18.63 0 12 0S0 5.41 0 12.07C0 18.1 4.39 23.1 10.13 24v-8.44H7.08v-3.49h3.04V9.41c0-3.02 1.79-4.7 4.54-4.7 1.31 0 2.68.24 2.68.24v2.97h-1.51c-1.49 0-1.95.93-1.95 1.89v2.26h3.32l-.53 3.49h-2.79V24C19.61 23.1 24 18.1 24 12.07z"
                />
              </svg>
            }
          />
          <CircularAuthButton
            testId="auth-google-button"
            label={t("auth.continueWithGoogle")}
            variant="google"
            disabled={submitting || isOffline}
            onClick={() => handleExternalProvider("google", googleAvailability)}
            icon={
              <svg viewBox="0 0 24 24" width="20" height="20" aria-hidden focusable="false">
                <path fill="#4285F4" d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z" />
                <path fill="#34A853" d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z" />
                <path fill="#FBBC05" d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z" />
                <path fill="#EA4335" d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z" />
              </svg>
            }
          />
          <CircularAuthButton
            testId="auth-pin-button"
            label={t("auth.continueWithPin")}
            disabled={submitting || (isOffline && !canUsePin)}
            variant="pin"
            onClick={handlePinLogin}
            icon={<LayoutGrid className="size-5" aria-hidden />}
          />
        </div>
        <p className="m-0 text-center text-[length:var(--exits-text-xs)] leading-relaxed text-muted">
          {isOffline ? t("auth.socialHelperOffline") : t("auth.socialHelperOnline")}
        </p>
      </div>
    </AuthExperienceLayout>
    </div>
  );
}
