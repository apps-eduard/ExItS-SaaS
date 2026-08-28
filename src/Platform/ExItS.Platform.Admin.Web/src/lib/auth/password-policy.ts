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
  return z.string().superRefine((value, ctx) => {
    if (!value) {
      ctx.addIssue({ code: "custom", message: messages.passwordRequired });
      return;
    }

    // Read the Local Validation flag at parse time so a stale schema (config.js loaded
    // after first render) cannot keep production 12-character rules.
    if (isLocalValidationToolsEnabled()) {
      return;
    }

    if (value.length < 12) {
      ctx.addIssue({ code: "custom", message: messages.passwordMinLength });
    }
    if (!/[A-Z]/.test(value)) {
      ctx.addIssue({ code: "custom", message: messages.passwordUppercase });
    }
    if (!/[a-z]/.test(value)) {
      ctx.addIssue({ code: "custom", message: messages.passwordLowercase });
    }
    if (!/\d/.test(value)) {
      ctx.addIssue({ code: "custom", message: messages.passwordDigit });
    }
    if (!/[^A-Za-z0-9]/.test(value)) {
      ctx.addIssue({ code: "custom", message: messages.passwordSpecial });
    }
  });
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
