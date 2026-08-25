import { Link2, Receipt, Store } from "lucide-react";
import { ActionTileGrid } from "@/components/exits/ActionTileGrid";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";

export type PersonalCommerceNavActive = "none" | "stores" | "orders" | "links";

type PersonalCommerceNavProps = {
  active: PersonalCommerceNavActive;
  /** Section card with heading (More page). Toolbar = tiles only (child pages). */
  variant?: "section" | "toolbar";
  className?: string;
};

export function PersonalCommerceNav({
  active,
  variant = "toolbar",
  className,
}: PersonalCommerceNavProps) {
  const { t } = useI18n();

  const tiles = [
    {
      key: "stores",
      label: t("personal.more.stores"),
      icon: Store,
      to: "/personal/linked-merchants",
      testId:
        active === "none"
          ? "more-open-stores"
          : active === "stores"
            ? "linked-merchants-tab-stores"
            : active === "orders"
              ? "my-orders-open-stores"
              : "customer-links-open-stores",
      primary: active === "none",
      current: active === "stores",
    },
    {
      key: "orders",
      label: t("personal.nav.orders"),
      icon: Receipt,
      to: "/personal/orders",
      testId:
        active === "none"
          ? "more-open-orders"
          : active === "stores"
            ? "open-my-orders"
            : active === "orders"
              ? "my-orders-tab-orders"
              : "customer-links-open-orders",
      current: active === "orders",
    },
    {
      key: "links",
      label: t("personal.customerLinks.title"),
      icon: Link2,
      to: "/personal/customer-links",
      testId:
        active === "none"
          ? "more-open-customer-links"
          : active === "stores"
            ? "open-customer-links"
            : active === "orders"
              ? "my-orders-open-customer-links"
              : "customer-links-tab-links",
      current: active === "links",
    },
  ];

  const grid = <ActionTileGrid tiles={tiles} />;

  if (variant === "section") {
    return (
      <section
        className={cn(
          "catalog-form-section exits-animate-panel personal-section gap-3",
          className,
        )}
        data-testid="personal-more-commerce"
      >
        <h2 className="catalog-form-section__title text-muted">
          {t("personal.more.group.commerce")}
        </h2>
        {grid}
      </section>
    );
  }

  return (
    <div
      className={cn("pc-commerce-nav exits-animate-toolbar", className)}
      data-testid={
        active === "stores"
          ? "linked-merchants-toolbar"
          : active === "orders"
            ? "my-orders-toolbar"
            : active === "links"
              ? "customer-links-toolbar"
              : undefined
      }
    >
      <h2 className="pc-commerce-nav__heading">{t("personal.more.group.commerce")}</h2>
      {grid}
    </div>
  );
}
