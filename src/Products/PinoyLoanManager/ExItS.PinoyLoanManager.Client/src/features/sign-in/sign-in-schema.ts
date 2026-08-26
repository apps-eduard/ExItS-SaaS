import { z } from "zod";
import type { FieldValues, Resolver } from "react-hook-form";

export const signInSchema = z.object({
  usernameOrEmail: z.string().trim().min(1, "required"),
  password: z.string().min(1, "required"),
});

export type SignInValues = z.infer<typeof signInSchema>;

export const signUpSchema = z.object({
  displayName: z.string().trim().min(1, "required"),
  email: z.string().trim().min(1, "required"),
});

export type SignUpValues = z.infer<typeof signUpSchema>;

export const forgotPasswordSchema = z.object({
  usernameOrEmail: z.string().trim().min(1, "required"),
});

export type ForgotPasswordValues = z.infer<typeof forgotPasswordSchema>;

export const passwordConfirmSchema = z
  .object({
    password: z.string().min(1, "required"),
    confirmPassword: z.string().min(1, "required"),
  })
  .refine((values) => values.password === values.confirmPassword, {
    path: ["confirmPassword"],
    message: "mismatch",
  });

export type PasswordConfirmValues = z.infer<typeof passwordConfirmSchema>;

export function zodResolver<TFieldValues extends FieldValues>(
  schema: z.ZodType<TFieldValues>,
): Resolver<TFieldValues> {
  return async (values) => {
    const parsed = schema.safeParse(values);
    if (parsed.success) {
      return { values: parsed.data, errors: {} };
    }
    const errors: Record<string, { type: string; message: string }> = {};
    for (const issue of parsed.error.issues) {
      const key = String(issue.path[0] ?? "root");
      if (!errors[key]) {
        errors[key] = { type: issue.code, message: issue.message };
      }
    }
    return { values: {}, errors } as Awaited<ReturnType<Resolver<TFieldValues>>>;
  };
}
