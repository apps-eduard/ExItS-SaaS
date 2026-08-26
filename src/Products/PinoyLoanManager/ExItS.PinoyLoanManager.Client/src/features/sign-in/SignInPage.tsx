import { Eye, EyeOff } from "lucide-react";
import { useMemo, useState } from "react";
import { useForm } from "react-hook-form";
import { Link, useLocation } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { IconButton } from "@/components/ui/icon-button";
import { TextField } from "@/components/ui/text-field";
import { AuthLayout } from "@/features/auth/AuthLayout";
import { TestUserSelector } from "@/features/sign-in/TestUserSelector";
import { signInSchema, zodResolver, type SignInValues } from "@/features/sign-in/sign-in-schema";
import { useI18n } from "@/i18n/I18nProvider";
import { useSession } from "@/session/SessionProvider";

type SignInNotice = "activated" | "reset";

export function SignInPage() {
  const { t } = useI18n();
  const { signIn, status } = useSession();
  const location = useLocation();
  const notice = useMemo(() => {
    const state = location.state as { notice?: SignInNotice } | null;
    return state?.notice ?? null;
  }, [location.state]);
  const [showPassword, setShowPassword] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);
  const {
    register,
    handleSubmit,
    setFocus,
    setValue,
    formState: { errors, isSubmitting },
  } = useForm<SignInValues>({
    resolver: zodResolver(signInSchema),
    defaultValues: { usernameOrEmail: "", password: "" },
  });

  const usernameRegister = register("usernameOrEmail");
  const passwordRegister = register("password");

  return (
    <AuthLayout
      title={t("auth.signInTitle")}
      showAccountLinks
      banner={
        <>
          {status === "expired" ? (
            <p className="mt-2 mb-0 text-[length:var(--exits-text-sm)] text-muted" role="status">
              {t("auth.sessionExpired")}
            </p>
          ) : null}
          {notice === "activated" ? (
            <p className="mt-2 mb-0 text-[length:var(--exits-text-sm)]" role="status">
              {t("auth.activatedNotice")}
            </p>
          ) : null}
          {notice === "reset" ? (
            <p className="mt-2 mb-0 text-[length:var(--exits-text-sm)]" role="status">
              {t("auth.resetNotice")}
            </p>
          ) : null}
        </>
      }
    >
      <form
        className="mt-6 flex flex-col gap-4"
        noValidate
        onSubmit={handleSubmit(
          async (values) => {
            setFormError(null);
            const ok = await signIn(values.usernameOrEmail, values.password);
            if (!ok) {
              setFormError(t("auth.invalidCredentials"));
            }
          },
          (formErrors) => {
            const first = Object.keys(formErrors)[0] as keyof SignInValues | undefined;
            if (first) {
              setFocus(first);
            }
          },
        )}
      >
        <TextField
          label={t("auth.usernameOrEmail")}
          autoComplete="username"
          autoCapitalize="none"
          spellCheck={false}
          error={errors.usernameOrEmail ? t("auth.fieldRequired") : undefined}
          {...usernameRegister}
        />
        <TextField
          label={t("auth.password")}
          type={showPassword ? "text" : "password"}
          autoComplete="current-password"
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
        <div className="flex flex-wrap justify-between gap-3 text-[length:var(--exits-text-sm)]">
          <Link className="text-muted underline-offset-4 hover:underline" to="/forgot-password">
            {t("auth.forgotPassword")}
          </Link>
          <Link className="text-muted underline-offset-4 hover:underline" to="/sign-up">
            {t("auth.createAccount")}
          </Link>
        </div>
        {formError ? (
          <p className="m-0 text-[length:var(--exits-text-sm)] text-destructive" role="alert">
            {formError}
          </p>
        ) : null}
        <Button type="submit" className="w-full" disabled={isSubmitting}>
          {isSubmitting ? t("auth.signingIn") : t("auth.signIn")}
        </Button>
      </form>
      <TestUserSelector
        onSelectIdentity={(usernameOrEmail) => {
          setValue("usernameOrEmail", usernameOrEmail, {
            shouldDirty: true,
            shouldValidate: true,
          });
        }}
      />
    </AuthLayout>
  );
}
