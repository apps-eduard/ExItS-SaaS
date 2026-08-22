import { z } from "zod";
import { CATALOG_TEMPLATE_SELECTION_MODES } from "@/api/global-catalog/global-catalog-types";

const selectionModeSchema = z.enum(CATALOG_TEMPLATE_SELECTION_MODES);

export const createTemplateSchema = z.object({
  name: z.string().trim().min(1, "Enter a template name."),
  slug: z.string().optional(),
  description: z.string().optional(),
  primaryBusinessTypeId: z.string().trim().min(1, "Select a primary business type."),
  iconReference: z.string().optional(),
  defaultBatchSize: z.number().int().min(1),
  selectionMode: selectionModeSchema,
});

export const editTemplateSchema = createTemplateSchema;

export type CreateTemplateFormValues = z.infer<typeof createTemplateSchema>;
export type EditTemplateFormValues = z.infer<typeof editTemplateSchema>;
