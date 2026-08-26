import { useForm } from "react-hook-form";
import { Link } from "react-router-dom";
import { registerPersonalAccount } from "@/api/platform-auth/platform-auth-client";
import { Button } from "@/components/ui/button";
import { TextField } from "@/components/ui/text-field";
import { AuthLayout } from "@/features/auth/AuthLayout";
import { signUpSchema, zodResolver, type SignUpValues } from "@/features/sign-in/sign-in-schema";
import { useI18n } from "@/i18n/I18nProvider";

export function SignUpPage() {
  const { t } = useI18n();
  const {
    register,
    handleSubmit,
    setFocus,
    formState: { errors, isSubmitting, isSubmitSuccessful },
  } = useForm<SignUpValues>({
    resolver: zodResolver(signUpSchema),
    defaultValues: { displayName: "", email: "" },
  });

  if (isSubmitSuccessful) {
    return (
      <AuthLayout title={t("auth.signUpTitle")} showAccountLinks banner={null}>
        <p className="mt-4 mb-0 text-[length:var(--exits-text-sm)]" role="status">
          {t("auth.checkEmail")}
        </p>
        <p className="mt-6 mb-0 text-[length:var(--exits-text-sm)]">
          <Link className="text-muted underline-offset-4 hover:underline" to="/sign-in">
            {t("auth.backToSignIn")}
          </Link>
        </p>
      </AuthLayout>
    );
  }

  return (
    <AuthLayout title={t("auth.signUpTitle")} showAccountLinks>
      <form
        className="mt-6 flex flex-col gap-4"
        noValidate
        onSubmit={handleSubmit(
          async (values) => {
            await registerPersonalAccount(values.displayName, values.email);
          },
          (formErrors) => {
            const first = Object.keys(formErrors)[0] as keyof SignUpValues | undefined;
            if (first) {
              setFocus(first);
            }
          },
        )}
      >
        <TextField
          label={t("auth.displayName")}
          autoComplete="name"
          error={errors.displayName ? t("auth.fieldRequired") : undefined}
          {...register("displayName")}
        />
        <TextField
          label={t("auth.email")}
          type="email"
          autoComplete="email"
          autoCapitalize="none"
          spellCheck={false}
          error={errors.email ? t("auth.fieldRequired") : undefined}
          {...register("email")}
        />
        <Button type="submit" className="w-full" disabled={isSubmitting}>
          {isSubmitting ? t("auth.creatingAccount") : t("auth.createAccount")}
        </Button>
      </form>
    </AuthLayout>
  );
}
