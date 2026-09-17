import { Eye, EyeOff } from "lucide-react";
import { useEffect, useRef, useState } from "react";
import { useForm } from "react-hook-form";
import { Link, useLocation, useNavigate } from "react-router-dom";
import {
  activatePersonalAccount,
  platformProblemDetail,
} from "@/api/platform/platform-auth-client";
import { isFrontendLocalValidationMode } from "@/api/platform/local-validation-gate";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { AuthExperienceLayout } from "@/features/auth/AuthExperienceLayout";
import {
  captureEmailCallbackToken,
  scrubTokenFromBrowserLocation,
} from "@/features/auth/callback-token";
import {
  passwordConfirmSchema,
  zodResolver,
  type PasswordConfirmValues,
} from "@/features/auth/password-confirm-schema";
import { resolveAuthContinuePath } from "@/features/store/store-acquisition";
import { useI18n } from "@/i18n/I18nProvider";

export function ActivateAccountPage() {
  const { t } = useI18n();
  const location = useLocation();
  const navigate = useNavigate();
  const tokenRef = useRef(captureEmailCallbackToken(location.search));
  const [showPassword, setShowPassword] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);
  const {
    register,
    handleSubmit,
    setFocus,
    formState: { errors, isSubmitting },
  } = useForm<PasswordConfirmValues>({
    resolver: zodResolver(passwordConfirmSchema),
    defaultValues: { password: "", confirmPassword: "" },
  });

  useEffect(() => {
    if (tokenRef.current) {
      scrubTokenFromBrowserLocation("/activate-account");
    }
  }, []);

  const token = tokenRef.current;
  if (!token) {
    return (
      <AuthExperienceLayout
        activeTab="sign-in"
        onTabChange={() => {
          void navigate("/sign-in", { replace: true });
        }}
      >
        <div className="flex flex-col gap-4" data-testid="activate-account-missing-token">
          <h2 className="m-0 text-[length:var(--exits-text-lg)] font-bold text-foreground">
            {t("auth.activateTitle")}
          </h2>
          <p className="m-0 text-[length:var(--exits-text-sm)] text-destructive" role="alert">
            {t("auth.activationLinkInvalid")}
          </p>
          <Link
            to="/sign-in"
            className="text-center text-[length:var(--exits-text-sm)] font-semibold text-primary hover:underline"
          >
            {t("auth.backToSignIn")}
          </Link>
        </div>
      </AuthExperienceLayout>
    );
  }

  const passwordRegister = register("password");
  const confirmRegister = register("confirmPassword");

  return (
    <AuthExperienceLayout
      activeTab="sign-in"
      onTabChange={() => {
        void navigate("/sign-in", { replace: true });
      }}
    >
      <div className="flex flex-col gap-4" data-testid="activate-account-page">
        <div>
          <h2 className="m-0 text-[length:var(--exits-text-lg)] font-bold text-foreground">
            {t("auth.activateTitle")}
          </h2>
          <p className="m-0 mt-2 text-[length:var(--exits-text-sm)] leading-relaxed text-muted">
            {t("auth.createPassword")}
          </p>
          <p className="m-0 mt-1 text-[length:var(--exits-text-xs)] text-muted">
            {t(
              isFrontendLocalValidationMode()
                ? "auth.passwordRequirementsLocalValidation"
                : "auth.passwordRequirements",
            )}
          </p>
        </div>
        <form
          className="flex flex-col gap-4"
          noValidate
          onSubmit={handleSubmit(
            async (values) => {
              setFormError(null);
              const result = await activatePersonalAccount(token, values.password);
              if (!result.ok) {
                const expired =
                  result.body?.errorCode === "application.auth.credential_token_expired";
                const invalid =
                  result.body?.errorCode === "application.auth.credential_token_invalid";
                setFormError(
                  expired
                    ? t("auth.tokenExpired")
                    : invalid
                      ? t("auth.tokenInvalid")
                      : platformProblemDetail(result.body, t("auth.activationFailed")),
                );
                return;
              }
              scrubTokenFromBrowserLocation("/activate-account");
              const continuePath = resolveAuthContinuePath(null);
              await navigate(
                continuePath
                  ? `/sign-in?continue=${encodeURIComponent(continuePath)}`
                  : "/sign-in",
                { replace: true, state: { notice: "activated" } },
              );
            },
            (formErrors) => {
              const first = Object.keys(formErrors)[0] as keyof PasswordConfirmValues | undefined;
              if (first) {
                setFocus(first);
              }
            },
          )}
        >
          <div className="relative flex flex-col gap-1.5">
            <Input
              label={t("auth.newPassword")}
              type={showPassword ? "text" : "password"}
              autoComplete="new-password"
              disabled={isSubmitting}
              {...passwordRegister}
            />
            <button
              type="button"
              className="absolute right-3 top-[2.35rem] text-muted"
              aria-label={showPassword ? t("auth.hidePassword") : t("auth.showPassword")}
              onClick={() => setShowPassword((current) => !current)}
            >
              {showPassword ? <EyeOff size={18} aria-hidden /> : <Eye size={18} aria-hidden />}
            </button>
            {errors.password ? (
              <p className="m-0 text-[length:var(--exits-text-sm)] text-destructive" role="alert">
                {t("auth.fieldRequired")}
              </p>
            ) : null}
          </div>
          <div className="flex flex-col gap-1.5">
            <Input
              label={t("auth.confirmPassword")}
              type={showPassword ? "text" : "password"}
              autoComplete="new-password"
              disabled={isSubmitting}
              {...confirmRegister}
            />
            {errors.confirmPassword ? (
              <p className="m-0 text-[length:var(--exits-text-sm)] text-destructive" role="alert">
                {errors.confirmPassword.message === "mismatch"
                  ? t("auth.passwordsMustMatch")
                  : t("auth.fieldRequired")}
              </p>
            ) : null}
          </div>
          {formError ? (
            <p
              className="m-0 text-[length:var(--exits-text-sm)] text-destructive"
              role="alert"
              data-testid="activate-account-error"
            >
              {formError}
            </p>
          ) : null}
          <Button type="submit" className="w-full" disabled={isSubmitting}>
            {isSubmitting ? t("auth.activating") : t("auth.activateAccount")}
          </Button>
        </form>
        <Link
          to="/sign-in"
          className="text-center text-[length:var(--exits-text-sm)] font-semibold text-primary hover:underline"
        >
          {t("auth.backToSignIn")}
        </Link>
      </div>
    </AuthExperienceLayout>
  );
}
