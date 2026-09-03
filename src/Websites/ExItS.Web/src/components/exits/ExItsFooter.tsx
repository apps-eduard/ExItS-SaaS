import Link from "next/link";

import { ExItsContainer } from "./ExItsContainer";

export function ExItsFooter() {
  const year = new Date().getFullYear();

  return (
    <footer className="border-t border-borderDefault bg-base">
      <ExItsContainer className="py-14">
        <div className="grid grid-cols-1 gap-10 sm:grid-cols-2 lg:grid-cols-4">
          <div className="space-y-3">
            <h3 className="text-sm font-semibold text-primary">Products</h3>
            <ul className="space-y-2 text-sm">
              <li>
                <Link href="/pos" className="text-muted hover:text-primary">
                  Pinoy Business POS
                </Link>
              </li>
              <li>
                <Link href="/products" className="text-muted hover:text-primary">
                  All products
                </Link>
              </li>
              <li>
                <Link href="/service-pro" className="text-muted hover:text-primary">
                  Pinoy Service Pro
                </Link>
              </li>
            </ul>
          </div>

          <div className="space-y-3">
            <h3 className="text-sm font-semibold text-primary">Solutions</h3>
            <ul className="space-y-2 text-sm">
              <li>
                <Link href="/pos#personal-sellers" className="text-muted hover:text-primary">
                  Personal Sellers
                </Link>
              </li>
              <li>
                <Link href="/pos#small-businesses" className="text-muted hover:text-primary">
                  Small Businesses
                </Link>
              </li>
              <li>
                <Link
                  href="/pos#multi-branch-businesses"
                  className="text-muted hover:text-primary"
                >
                  Multi-Branch Businesses
                </Link>
              </li>
            </ul>
          </div>

          <div className="space-y-3">
            <h3 className="text-sm font-semibold text-primary">Company</h3>
            <ul className="space-y-2 text-sm">
              <li>
                <Link href="/about" className="text-muted hover:text-primary">
                  About
                </Link>
              </li>
              <li>
                <Link href="/contact" className="text-muted hover:text-primary">
                  Contact
                </Link>
              </li>
            </ul>
          </div>

          <div className="space-y-3">
            <h3 className="text-sm font-semibold text-primary">Legal</h3>
            <ul className="space-y-2 text-sm">
              <li>
                <Link href="/privacy" className="text-muted hover:text-primary">
                  Privacy
                </Link>
              </li>
              <li>
                <Link href="/terms" className="text-muted hover:text-primary">
                  Terms
                </Link>
              </li>
            </ul>
          </div>
        </div>

        <div className="mt-12 flex flex-col gap-3 border-t border-borderDefault pt-8 sm:flex-row sm:items-center sm:justify-between">
          <div className="text-sm text-muted">Social: TBD — handles not confirmed yet.</div>
          <div className="text-sm text-muted">© {year} ExItS. All rights reserved.</div>
        </div>
      </ExItsContainer>
    </footer>
  );
}

