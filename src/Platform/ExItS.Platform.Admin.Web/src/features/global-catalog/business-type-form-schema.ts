import { z } from "zod";

export const createBusinessTypeSchema = z.object({
  code: z.string().trim().min(1, "Enter a business type code."),
  name: z.string().trim().min(1, "Enter a business type name."),
  description: z.string().optional(),
  sortOrder: z.number().int().min(0),
  iconReference: z.string().optional(),
});

export const editBusinessTypeSchema = z.object({
  name: z.string().trim().min(1, "Enter a business type name."),
  description: z.string().optional(),
  sortOrder: z.number().int().min(0),
  iconReference: z.string().optional(),
});

export type CreateBusinessTypeFormValues = z.infer<typeof createBusinessTypeSchema>;
export type EditBusinessTypeFormValues = z.infer<typeof editBusinessTypeSchema>;
