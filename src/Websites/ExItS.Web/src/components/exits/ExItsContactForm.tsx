"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useState } from "react";
import { useForm } from "react-hook-form";

import { ExItsButton } from "@/components/exits/ExItsButton";
import { ExItsFormField } from "@/components/exits/ExItsFormField";
import { ExItsInput } from "@/components/exits/ExItsInput";
import {
  contactSubmissionUnavailableMessage,
  generalContactSchema,
  partnershipSchema,
  salesInquirySchema,
  type GeneralContactValues,
  type PartnershipValues,
  type SalesInquiryValues,
} from "@/lib/contact-schemas";
import { cn } from "@/lib/utils";

const fieldClassName =
  "flex h-11 w-full rounded-md border border-borderDefault bg-surface px-4 py-3 text-primary placeholder:text-muted shadow-none focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brandBright disabled:cursor-not-allowed disabled:opacity-50";

const textareaClassName = cn(fieldClassName, "min-h-32 h-auto resize-y");

function GeneralContactForm() {
  const [status, setStatus] = useState<string | null>(null);
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<GeneralContactValues>({
    resolver: zodResolver(generalContactSchema),
    defaultValues: {
      name: "",
      email: "",
      inquiryType: "General",
      message: "",
    },
  });

  return (
    <form
      className="grid gap-5 sm:grid-cols-2"
      noValidate
      onSubmit={handleSubmit(() => {
        setStatus(contactSubmissionUnavailableMessage);
      })}
    >
      <ExItsFormField id="general-name" label="Name" error={errors.name?.message}>
        <ExItsInput
          id="general-name"
          autoComplete="name"
          aria-invalid={Boolean(errors.name)}
          aria-describedby={errors.name ? "general-name-error" : undefined}
          {...register("name")}
        />
      </ExItsFormField>
      <ExItsFormField id="general-email" label="Email" error={errors.email?.message}>
        <ExItsInput
          id="general-email"
          type="email"
          autoComplete="email"
          aria-invalid={Boolean(errors.email)}
          aria-describedby={errors.email ? "general-email-error" : undefined}
          {...register("email")}
        />
      </ExItsFormField>
      <ExItsFormField
        id="general-inquiry-type"
        label="Subject / inquiry type"
        error={errors.inquiryType?.message}
        className="sm:col-span-2"
      >
        <select
          id="general-inquiry-type"
          className={fieldClassName}
          aria-invalid={Boolean(errors.inquiryType)}
          aria-describedby={errors.inquiryType ? "general-inquiry-type-error" : undefined}
          {...register("inquiryType")}
        >
          <option value="General">General</option>
          <option value="Sales">Sales</option>
          <option value="Partnership">Partnership</option>
          <option value="Support">Support</option>
        </select>
      </ExItsFormField>
      <ExItsFormField
        id="general-message"
        label="Message"
        error={errors.message?.message}
        className="sm:col-span-2"
      >
        <textarea
          id="general-message"
          className={textareaClassName}
          aria-invalid={Boolean(errors.message)}
          aria-describedby={errors.message ? "general-message-error" : undefined}
          {...register("message")}
        />
      </ExItsFormField>
      <div className="sm:col-span-2">
        <ExItsButton type="submit" disabled={isSubmitting}>
          Send Message
        </ExItsButton>
        {status ? (
          <p className="mt-3 text-sm leading-relaxed text-muted" role="status">
            {status}
          </p>
        ) : null}
      </div>
    </form>
  );
}

function SalesInquiryForm() {
  const [status, setStatus] = useState<string | null>(null);
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<SalesInquiryValues>({
    resolver: zodResolver(salesInquirySchema),
    defaultValues: {
      name: "",
      businessName: "",
      email: "",
      phone: "",
      businessSize: "",
      message: "",
    },
  });

  return (
    <form
      className="grid gap-5 sm:grid-cols-2"
      noValidate
      onSubmit={handleSubmit(() => {
        setStatus(contactSubmissionUnavailableMessage);
      })}
    >
      <ExItsFormField id="sales-name" label="Name" error={errors.name?.message}>
        <ExItsInput
          id="sales-name"
          autoComplete="name"
          aria-invalid={Boolean(errors.name)}
          aria-describedby={errors.name ? "sales-name-error" : undefined}
          {...register("name")}
        />
      </ExItsFormField>
      <ExItsFormField id="sales-business-name" label="Business name" error={errors.businessName?.message}>
        <ExItsInput
          id="sales-business-name"
          autoComplete="organization"
          {...register("businessName")}
        />
      </ExItsFormField>
      <ExItsFormField id="sales-email" label="Email" error={errors.email?.message}>
        <ExItsInput
          id="sales-email"
          type="email"
          autoComplete="email"
          aria-invalid={Boolean(errors.email)}
          aria-describedby={errors.email ? "sales-email-error" : undefined}
          {...register("email")}
        />
      </ExItsFormField>
      <ExItsFormField id="sales-phone" label="Phone (optional)" error={errors.phone?.message}>
        <ExItsInput id="sales-phone" type="tel" autoComplete="tel" {...register("phone")} />
      </ExItsFormField>
      <ExItsFormField
        id="sales-business-size"
        label="Number of branches / business size"
        error={errors.businessSize?.message}
        className="sm:col-span-2"
      >
        <ExItsInput id="sales-business-size" {...register("businessSize")} />
      </ExItsFormField>
      <ExItsFormField
        id="sales-message"
        label="Message"
        error={errors.message?.message}
        className="sm:col-span-2"
      >
        <textarea id="sales-message" className={textareaClassName} {...register("message")} />
      </ExItsFormField>
      <div className="sm:col-span-2">
        <ExItsButton type="submit" disabled={isSubmitting}>
          Request Demo
        </ExItsButton>
        {status ? (
          <p className="mt-3 text-sm leading-relaxed text-muted" role="status">
            {status}
          </p>
        ) : null}
      </div>
    </form>
  );
}

function PartnershipForm() {
  const [status, setStatus] = useState<string | null>(null);
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<PartnershipValues>({
    resolver: zodResolver(partnershipSchema),
    defaultValues: {
      name: "",
      organization: "",
      email: "",
      partnershipType: "Technology",
      message: "",
    },
  });

  return (
    <form
      className="grid gap-5 sm:grid-cols-2"
      noValidate
      onSubmit={handleSubmit(() => {
        setStatus(contactSubmissionUnavailableMessage);
      })}
    >
      <ExItsFormField id="partner-name" label="Name" error={errors.name?.message}>
        <ExItsInput
          id="partner-name"
          autoComplete="name"
          aria-invalid={Boolean(errors.name)}
          aria-describedby={errors.name ? "partner-name-error" : undefined}
          {...register("name")}
        />
      </ExItsFormField>
      <ExItsFormField
        id="partner-organization"
        label="Organization"
        error={errors.organization?.message}
      >
        <ExItsInput
          id="partner-organization"
          autoComplete="organization"
          aria-invalid={Boolean(errors.organization)}
          aria-describedby={errors.organization ? "partner-organization-error" : undefined}
          {...register("organization")}
        />
      </ExItsFormField>
      <ExItsFormField id="partner-email" label="Email" error={errors.email?.message}>
        <ExItsInput
          id="partner-email"
          type="email"
          autoComplete="email"
          aria-invalid={Boolean(errors.email)}
          aria-describedby={errors.email ? "partner-email-error" : undefined}
          {...register("email")}
        />
      </ExItsFormField>
      <ExItsFormField
        id="partner-type"
        label="Partnership type"
        error={errors.partnershipType?.message}
      >
        <select
          id="partner-type"
          className={fieldClassName}
          aria-invalid={Boolean(errors.partnershipType)}
          aria-describedby={errors.partnershipType ? "partner-type-error" : undefined}
          {...register("partnershipType")}
        >
          <option value="Technology">Technology</option>
          <option value="Distribution">Distribution</option>
          <option value="Reseller">Reseller</option>
          <option value="Other">Other</option>
        </select>
      </ExItsFormField>
      <ExItsFormField
        id="partner-message"
        label="Message"
        error={errors.message?.message}
        className="sm:col-span-2"
      >
        <textarea id="partner-message" className={textareaClassName} {...register("message")} />
      </ExItsFormField>
      <div className="sm:col-span-2">
        <ExItsButton type="submit" disabled={isSubmitting}>
          Submit
        </ExItsButton>
        {status ? (
          <p className="mt-3 text-sm leading-relaxed text-muted" role="status">
            {status}
          </p>
        ) : null}
      </div>
    </form>
  );
}

export function ExItsContactForm({
  variant,
}: {
  variant: "general" | "sales" | "partnership";
}) {
  if (variant === "sales") return <SalesInquiryForm />;
  if (variant === "partnership") return <PartnershipForm />;
  return <GeneralContactForm />;
}
