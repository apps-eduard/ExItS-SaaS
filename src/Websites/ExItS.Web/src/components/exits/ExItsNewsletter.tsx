"use client";

import { useState } from "react";

import { ExItsButton } from "@/components/exits/ExItsButton";
import { ExItsInput } from "@/components/exits/ExItsInput";

export function ExItsNewsletter() {
  const [message, setMessage] = useState<string | null>(null);

  return (
    <form
      className="mt-8 max-w-xl"
      onSubmit={(event) => {
        event.preventDefault();
        setMessage(
          "Email updates are not connected yet. This form will submit when the waitlist endpoint is ready.",
        );
      }}
    >
      <div className="flex flex-col gap-3 sm:flex-row">
        <label className="sr-only" htmlFor="newsletter-email">
          Email address
        </label>
        <ExItsInput
          id="newsletter-email"
          name="email"
          type="email"
          required
          autoComplete="email"
          placeholder="you@business.ph"
          className="sm:flex-1"
        />
        <ExItsButton type="submit" className="min-h-11 sm:w-auto">
          Subscribe
        </ExItsButton>
      </div>
      {message ? (
        <p className="mt-3 text-sm leading-relaxed text-muted" role="status">
          {message}
        </p>
      ) : null}
    </form>
  );
}
