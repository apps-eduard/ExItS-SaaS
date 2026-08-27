import { z } from "zod";
import type { FieldValues, Resolver } from "react-hook-form";

/** Client-side confirm match only; Platform password policy is enforced server-side. */
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
