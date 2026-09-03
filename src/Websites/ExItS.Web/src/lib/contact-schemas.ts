import { z } from "zod";

export const generalContactSchema = z.object({
  name: z.string().trim().min(1, "Name is required."),
  email: z.email("Enter a valid email address."),
  inquiryType: z.enum(["General", "Sales", "Partnership", "Support"], {
    error: "Select an inquiry type.",
  }),
  message: z.string().trim().min(1, "Message is required."),
});

export const salesInquirySchema = z.object({
  name: z.string().trim().min(1, "Name is required."),
  businessName: z.string().trim().optional(),
  email: z.email("Enter a valid email address."),
  phone: z.string().trim().optional(),
  businessSize: z.string().trim().optional(),
  message: z.string().trim().optional(),
});

export const partnershipSchema = z.object({
  name: z.string().trim().min(1, "Name is required."),
  organization: z.string().trim().min(1, "Organization is required."),
  email: z.email("Enter a valid email address."),
  partnershipType: z.enum(["Technology", "Distribution", "Reseller", "Other"], {
    error: "Select a partnership type.",
  }),
  message: z.string().trim().optional(),
});

export type GeneralContactValues = z.infer<typeof generalContactSchema>;
export type SalesInquiryValues = z.infer<typeof salesInquirySchema>;
export type PartnershipValues = z.infer<typeof partnershipSchema>;

export const contactSubmissionUnavailableMessage =
  "Form submission is not connected yet. Your message was not sent. Contact capture will activate when the inquiry endpoint is ready.";
