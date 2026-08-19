import { z } from "zod";
import type { FieldValues, Resolver } from "react-hook-form";

export const signInSchema = z.object({
  usernameOrEmail: z.string().trim().min(1, "required"),
  password: z.string().min(1, "required"),
});

export type SignInValues = z.infer<typeof signInSchema>;

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
