import { Eye, EyeOff } from "lucide-react";
import { useEffect, useRef, useState } from "react";
import { useForm } from "react-hook-form";
import { useLocation, useNavigate } from "react-router-dom";
import {
  platformProblemDetail,
  resetPasswordWithToken,
} from "@/api/platform-auth/platform-auth-client";
import { Button } from "@/components/ui/button";
import { IconButton } from "@/components/ui/icon-button";
import { TextField } from "@/components/ui/text-field";
import { AuthLayout } from "@/features/auth/AuthLayout";
import {
  captureEmailCallbackToken,
  scrubTokenFromBrowserLocation,
} from "@/features/auth/callback-token";
import {
  passwordConfirmSchema,
  zodResolver,
  type PasswordConfirmValues,
} from "@/features/sign-in/sign-in-schema";
import { useI18n } from "@/i18n/I18nProvider";

export function ResetPasswordPage() {
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
      scrubTokenFromBrowserLocation("/reset-password");
    }
  }, []);

  const token = tokenRef.current;
  if (!token) {
    return (
      <AuthLayout title={t("auth.resetTitle")}>
        <p className="mt-4 mb-0 text-[length:var(--exits-text-sm)]" role="alert">
          {t("auth.resetLinkInvalid")}
        </p>
      </AuthLayout>
    );
  }

  const passwordRegister = register("password");
  const confirmRegister = register("confirmPassword");

  return (
    <AuthLayout title={t("auth.resetTitle")}>
      <form
        className="mt-6 flex flex-col gap-4"
        noValidate
        onSubmit={handleSubmit(
          async (values) => {
            setFormError(null);
            const result = await resetPasswordWithToken(token, values.password);
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
                    : platformProblemDetail(result.body, t("auth.resetFailed")),
              );
              return;
            }
            scrubTokenFromBrowserLocation("/reset-password");
            await navigate("/sign-in", { replace: true, state: { notice: "reset" } });
          },
          (formErrors) => {
            const first = Object.keys(formErrors)[0] as keyof PasswordConfirmValues | undefined;
            if (first) {
              setFocus(first);
            }
          },
        )}
      >
        <TextField
          label={t("auth.newPassword")}
          type={showPassword ? "text" : "password"}
          autoComplete="new-password"
          error={errors.password ? t("auth.fieldRequired") : undefined}
          trailing={
            <IconButton
              label={showPassword ? t("auth.hidePassword") : t("auth.showPassword")}
              onClick={() => setShowPassword((current) => !current)}
            >
              {showPassword ? <EyeOff size={18} aria-hidden /> : <Eye size={18} aria-hidden />}
            </IconButton>
          }
          {...passwordRegister}
        />
        <TextField
          label={t("auth.confirmPassword")}
          type={showPassword ? "text" : "password"}
          autoComplete="new-password"
          error={
            errors.confirmPassword?.message === "mismatch"
              ? t("auth.passwordsMustMatch")
              : errors.confirmPassword
                ? t("auth.fieldRequired")
                : undefined
          }
          {...confirmRegister}
        />
        {formError ? (
          <p className="m-0 text-[length:var(--exits-text-sm)] text-destructive" role="alert">
            {formError}
          </p>
        ) : null}
        <Button type="submit" className="w-full" disabled={isSubmitting}>
          {isSubmitting ? t("auth.resetting") : t("auth.resetPassword")}
        </Button>
      </form>
    </AuthLayout>
  );
}
