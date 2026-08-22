import { z } from "zod";

export type ProductRenameValues = {
  displayName: string;
};

export const productRenameSchema = z.object({
  displayName: z.string().trim().min(1, "Display name is required.").max(200),
});
