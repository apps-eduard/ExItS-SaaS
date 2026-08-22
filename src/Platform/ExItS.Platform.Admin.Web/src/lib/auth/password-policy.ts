import { z } from "zod";
import { isLocalValidationToolsEnabled } from "@/lib/env";

export type AuthPasswordValidationMessages = {
  passwordRequired: string;
  passwordMinLength: string;
  passwordUppercase: string;
  passwordLowercase: string;
  passwordDigit: string;
  passwordSpecial: string;
};

export function buildAuthPasswordFieldSchema(messages: AuthPasswordValidationMessages) {
  const required = z.string().min(1, messages.passwordRequired);

  if (isLocalValidationToolsEnabled()) {
    return required;
  }

  return required
    .min(12, messages.passwordMinLength)
    .refine((value) => /[A-Z]/.test(value), { message: messages.passwordUppercase })
    .refine((value) => /[a-z]/.test(value), { message: messages.passwordLowercase })
    .refine((value) => /\d/.test(value), { message: messages.passwordDigit })
    .refine((value) => /[^A-Za-z0-9]/.test(value), { message: messages.passwordSpecial });
}

export function buildAuthNewPasswordSchema(
  messages: AuthPasswordValidationMessages,
  passwordMismatch: string,
  confirmPasswordRequired: string,
) {
  return z
    .object({
      password: buildAuthPasswordFieldSchema(messages),
      confirmPassword: z.string().min(1, confirmPasswordRequired),
    })
    .refine((values) => values.password === values.confirmPassword, {
      message: passwordMismatch,
      path: ["confirmPassword"],
    });
}
