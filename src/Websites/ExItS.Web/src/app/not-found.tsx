import Link from "next/link";

import { ExItsContainer } from "@/components/exits/ExItsContainer";

export default function NotFound() {
  return (
    <div className="py-16">
      <ExItsContainer>
        <div className="max-w-2xl">
          <h1 className="text-4xl font-semibold tracking-tight text-primary md:text-5xl">
            404
          </h1>
          <p className="mt-4 text-base leading-relaxed text-muted">
            The page you’re looking for doesn’t exist. Browse ExItS products or go back
            to the homepage.
          </p>

          <div className="mt-8 flex flex-col gap-3 sm:flex-row">
            <Link href="/" className="w-full sm:w-auto">
              <span
                className="inline-flex h-11 w-full items-center justify-center rounded-md border px-5 text-sm font-semibold transition-colors
                bg-gradient-to-r from-brand to-brandBright text-primary border-borderDefault hover:brightness-110
                focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brandBright ring-offset-base"
              >
                Back to Home
              </span>
            </Link>
            <Link href="/products" className="w-full sm:w-auto">
              <span
                className="inline-flex h-11 w-full items-center justify-center rounded-md border px-5 text-sm font-semibold transition-colors
                bg-transparent text-primary border-borderDefault hover:bg-surface
                focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brandBright ring-offset-base"
              >
                View Products
              </span>
            </Link>
          </div>
        </div>
      </ExItsContainer>
    </div>
  );
}

