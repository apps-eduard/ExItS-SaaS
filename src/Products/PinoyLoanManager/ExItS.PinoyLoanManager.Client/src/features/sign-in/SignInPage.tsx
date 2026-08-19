import { Eye, EyeOff } from "lucide-react";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { LanguageControl } from "@/components/exits/LanguageControl";
import { ThemeControl } from "@/components/exits/ThemeControl";
import { ExItsMark } from "@/components/exits/ExItsMark";
import { Button } from "@/components/ui/button";
import { IconButton } from "@/components/ui/icon-button";
import { TextField } from "@/components/ui/text-field";
import { TestUserSelector } from "@/features/sign-in/TestUserSelector";
import { signInSchema, zodResolver, type SignInValues } from "@/features/sign-in/sign-in-schema";
import { useI18n } from "@/i18n/I18nProvider";
import { useSession } from "@/session/SessionProvider";

export function SignInPage() {
  const { t } = useI18n();
  const { signIn, status } = useSession();
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
    <div className="auth-stage flex min-h-dvh flex-col md:items-center md:justify-center md:px-4 md:py-10">
      <div className="flex flex-col px-5 pb-6 pt-[max(2.5rem,env(safe-area-inset-top))] text-white md:hidden">
        <ExItsMark size="lg" className="bg-white text-[#166534]" />
        <p className="mb-0 mt-4 text-[length:var(--exits-text-sm)] font-semibold">
          {t("app.name")}
        </p>
      </div>
      <section className="auth-sheet flex min-h-0 flex-1 flex-col px-5 pb-[max(1.5rem,env(safe-area-inset-bottom))] pt-6 md:h-auto md:w-[420px] md:flex-none md:px-7 md:py-8">
        <div className="mb-6 hidden items-center gap-3 md:flex">
          <ExItsMark size="md" />
          <p className="m-0 font-semibold text-foreground">{t("app.name")}</p>
        </div>
        <h1 className="m-0 text-[length:var(--exits-text-xl)] font-bold tracking-tight">
          {t("auth.signInTitle")}
        </h1>
        {status === "expired" ? (
          <p className="mt-2 mb-0 text-[length:var(--exits-text-sm)] text-muted" role="status">
            {t("auth.sessionExpired")}
          </p>
        ) : null}
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
          <p className="m-0 self-end text-[length:var(--exits-text-sm)] text-muted">
            {t("auth.signInTrouble")}
          </p>
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
        <div className="mt-8 flex flex-col gap-4">
          <LanguageControl />
          <ThemeControl />
        </div>
      </section>
    </div>
  );
}
