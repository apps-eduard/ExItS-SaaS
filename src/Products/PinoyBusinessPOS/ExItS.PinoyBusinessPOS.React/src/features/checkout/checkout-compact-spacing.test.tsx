import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { useState } from "react";
import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { Percent, WalletCards } from "lucide-react";
import { CheckoutCollapsibleSection } from "@/features/checkout/CheckoutCollapsibleSection";

const here = dirname(fileURLToPath(import.meta.url));

describe("POS-CHECKOUT-COMPACT-SPACING-V1", () => {
  it("defines density-aware section gap and checkout rhythm tokens", () => {
    const css = readFileSync(resolve(here, "../../styles/globals.css"), "utf8");

    expect(css).toMatch(/--exits-section-gap:\s*0\.625rem/);
    expect(css).toMatch(/\[data-density="compact"\][\s\S]*?--exits-section-gap:\s*0\.5rem/);
    expect(css).toMatch(/\[data-density="comfort"\][\s\S]*?--exits-section-gap:\s*0\.75rem/);
    expect(css).toMatch(
      /\.checkout-cash-page__scroll\s*\{[\s\S]*?gap:\s*var\(--exits-section-gap\)/,
    );
    expect(css).toContain(".checkout-tender-dock");
    expect(css).toMatch(/\.checkout-section-card\s*\{[\s\S]*?overflow:\s*visible/);
    expect(css).toMatch(
      /\.checkout-cash-page__scroll\s*>\s*\.checkout-sale-preview[\s\S]*?padding:\s*var\(--exits-list-card-padding-y\)\s+var\(--exits-list-card-padding-x\)/,
    );
    expect(css).toContain(".checkout-sale-preview__lines");
    expect(css).toMatch(
      /\.checkout-sale-preview__totals\s*\{[\s\S]*?margin-top:\s*0\.5rem;[\s\S]*?padding-top:\s*0\.5rem/,
    );
  });

  it("CheckoutCashPage uses scroll region + tender dock and keeps section order", () => {
    const source = readFileSync(resolve(here, "CheckoutCashPage.tsx"), "utf8");
    expect(source).toContain('data-testid="checkout-cash-page" className="checkout-cash-page"');
    expect(source).toContain('className="checkout-cash-page__scroll"');
    expect(source).toContain('data-testid="checkout-tender-dock"');
    expect(source).toContain('className="checkout-sale-preview__lines"');

    const money = source.indexOf('data-testid="checkout-money-summary"');
    const payment = source.indexOf('data-testid="checkout-payment-method"');
    const discount = source.indexOf('data-testid="checkout-discount-panel"');
    const utang = source.indexOf('data-testid="checkout-utang-panel"');
    const dock = source.indexOf('data-testid="checkout-tender-dock"');
    const cash = source.indexOf('data-testid="checkout-cash-received"');
    const confirm = source.indexOf('data-testid="checkout-confirm"');
    expect(money).toBeGreaterThan(-1);
    expect(payment).toBeGreaterThan(money);
    expect(discount).toBeGreaterThan(payment);
    expect(utang).toBeGreaterThan(discount);
    expect(dock).toBeGreaterThan(utang);
    expect(cash).toBeGreaterThan(dock);
    expect(confirm).toBeGreaterThan(dock);
  });

  it("collapsed payment / discount toggles stay compact and expand on open", async () => {
    const user = userEvent.setup();

    function Harness() {
      const [paymentOpen, setPaymentOpen] = useState(false);
      const [discountOpen, setDiscountOpen] = useState(false);
      return (
        <div className="checkout-cash-page">
          <CheckoutCollapsibleSection
            testId="checkout-payment-collapse"
            title="Payment method"
            expandLabel="Choose payment method"
            summary="Utang"
            open={paymentOpen}
            onOpenChange={setPaymentOpen}
            icon={WalletCards}
          >
            <p data-testid="payment-body">methods</p>
          </CheckoutCollapsibleSection>
          <CheckoutCollapsibleSection
            testId="checkout-discount-collapse"
            title="Discount"
            expandLabel="Add discount"
            open={discountOpen}
            onOpenChange={setDiscountOpen}
            icon={Percent}
          >
            <p data-testid="discount-body">form</p>
          </CheckoutCollapsibleSection>
          <p data-testid="checkout-discount-empty">No discounts added.</p>
        </div>
      );
    }

    render(<Harness />);

    const paymentToggle = screen.getByTestId("checkout-payment-collapse-toggle");
    expect(paymentToggle).toHaveAttribute("aria-expanded", "false");
    expect(paymentToggle.className).not.toMatch(/toggle--cta/);
    expect(screen.getByText("Utang")).toBeInTheDocument();
    expect(screen.getByTestId("checkout-payment-collapse")).toHaveAttribute("data-open", "false");
    expect(screen.getByTestId("checkout-payment-collapse-panel")).toHaveAttribute(
      "aria-hidden",
      "true",
    );

    const discountToggle = screen.getByTestId("checkout-discount-collapse-toggle");
    expect(discountToggle).toHaveAttribute("aria-expanded", "false");
    expect(discountToggle.className).toMatch(/toggle--cta/);
    expect(screen.getByText("Add discount")).toBeInTheDocument();
    expect(screen.getByTestId("checkout-discount-empty")).toBeInTheDocument();
    expect(screen.getByTestId("checkout-discount-collapse-panel")).toHaveAttribute(
      "aria-hidden",
      "true",
    );

    await user.click(paymentToggle);
    expect(screen.getByTestId("checkout-payment-collapse-toggle")).toHaveAttribute(
      "aria-expanded",
      "true",
    );
    expect(screen.getByTestId("checkout-payment-collapse")).toHaveAttribute("data-open", "true");
    expect(screen.getByTestId("checkout-payment-collapse-panel")).toHaveAttribute(
      "aria-hidden",
      "false",
    );
    expect(screen.getByTestId("payment-body")).toBeInTheDocument();

    await user.click(screen.getByTestId("checkout-discount-collapse-toggle"));
    expect(screen.getByTestId("checkout-discount-collapse-toggle")).toHaveAttribute(
      "aria-expanded",
      "true",
    );
    expect(screen.getByTestId("checkout-discount-collapse-panel")).toHaveAttribute(
      "aria-hidden",
      "false",
    );
    expect(screen.getByTestId("discount-body")).toBeInTheDocument();
  });
});
