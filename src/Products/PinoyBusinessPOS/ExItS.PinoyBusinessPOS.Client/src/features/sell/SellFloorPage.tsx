import { useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { PageHeader } from "@/components/exits/PageHeader";
import { useI18n } from "@/i18n/I18nProvider";
import { useSellingMode } from "@/selling/SellingModeProvider";
import { cn } from "@/lib/cn";

const CATEGORY_STUBS = ["stub-a", "stub-b"] as const;

export function SellFloorPage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const { returnRoute, exit } = useSellingMode();
  const [activeCategory, setActiveCategory] = useState<string>("all");
  const [cartSheetOpen, setCartSheetOpen] = useState(false);

  const categoryLabels = useMemo(
    () => ({
      all: t("sell.categoryAll"),
      "stub-a": t("sell.categoryStubA"),
      "stub-b": t("sell.categoryStubB"),
    }),
    [t],
  );

  return (
    <div
      data-testid="sell-floor"
      className="sell-floor-root -mx-[max(var(--exits-page-padding),env(safe-area-inset-left))] flex min-h-[calc(100dvh-12rem)] min-w-0 flex-col px-[max(var(--exits-page-padding),env(safe-area-inset-left))]"
    >
      <div className="mb-4 flex min-w-0 items-start justify-between gap-3">
        <PageHeader title={t("sell.title")} description={t("sell.lede")} />
        <Button
          type="button"
          variant="ghost"
          className="shrink-0"
          onClick={() => {
            exit();
            navigate(returnRoute ?? "/");
          }}
        >
          {t("sell.exitSelling")}
        </Button>
      </div>

      <div className="sell-floor-layout min-h-0 min-w-0 flex-1">
        <section className="sell-floor-browse flex min-h-0 min-w-0 flex-col gap-3">
          <label className="flex min-w-0 flex-col gap-1">
            <span className="sr-only">{t("sell.searchLabel")}</span>
            <input
              data-testid="sell-search"
              type="search"
              autoFocus
              autoComplete="off"
              spellCheck={false}
              placeholder={t("sell.searchPlaceholder")}
              className="h-[var(--exits-control-height)] min-h-[var(--exits-touch-target-min)] w-full rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 text-[length:var(--exits-text-md)] text-foreground outline-none focus-visible:ring-2 focus-visible:ring-ring"
            />
          </label>

          <div
            data-testid="sell-categories"
            className="flex flex-wrap gap-2"
            role="list"
            aria-label={t("sell.categoriesLabel")}
          >
            {(["all", ...CATEGORY_STUBS] as const).map((categoryId) => (
              <button
                key={categoryId}
                type="button"
                role="listitem"
                className={cn(
                  "rounded-full border px-3 py-1.5 text-[length:var(--exits-text-sm)] font-semibold transition-colors",
                  activeCategory === categoryId
                    ? "border-primary bg-primary text-primary-foreground"
                    : "border-border bg-surface text-foreground hover:bg-[var(--exits-surface-muted)]",
                )}
                aria-pressed={activeCategory === categoryId}
                onClick={() => setActiveCategory(categoryId)}
              >
                {categoryLabels[categoryId]}
              </button>
            ))}
          </div>

          <div
            data-testid="sell-products"
            className="grid min-h-[12rem] flex-1 grid-cols-2 gap-3 rounded-[var(--exits-radius-lg)] border border-dashed border-border bg-[var(--exits-surface-muted)] p-4 sm:grid-cols-3 lg:grid-cols-4"
            aria-label={t("sell.productsLabel")}
          >
            {Array.from({ length: 6 }).map((_, index) => (
              <div
                key={index}
                className="animate-pulse rounded-[var(--exits-radius-md)] bg-surface"
                aria-hidden="true"
              />
            ))}
            <p className="col-span-full m-0 text-center text-[length:var(--exits-text-sm)] text-muted">
              {t("sell.catalogPlaceholder")}
            </p>
          </div>
        </section>

        <aside
          data-testid="sell-cart-landscape"
          className="sell-cart-landscape hidden min-h-0 min-w-0 flex-col gap-3 rounded-[var(--exits-radius-lg)] border border-border bg-surface p-4"
          aria-label={t("sell.cartLabel")}
        >
          <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">
            {t("sell.cartLabel")}
          </h2>
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{t("sell.cartEmpty")}</p>
          <Button
            data-testid="sell-pay"
            type="button"
            disabled
            title={t("sell.payDisabledTitle")}
            className="mt-auto w-full"
          >
            {t("sell.pay")}
          </Button>
        </aside>
      </div>

      <button
        type="button"
        data-testid="sell-cart-bar"
        className="sell-cart-bar sticky bottom-[max(0.75rem,env(safe-area-inset-bottom))] z-20 mt-4 flex w-full items-center justify-between gap-3 rounded-[var(--exits-radius-lg)] border border-border bg-surface px-4 py-3 shadow-[0_-4px_24px_rgba(0,0,0,0.08)]"
        onClick={() => setCartSheetOpen(true)}
        aria-expanded={cartSheetOpen}
        aria-controls="sell-cart-sheet-panel"
      >
        <span className="text-[length:var(--exits-text-sm)] font-semibold">
          {t("sell.cartBarSummary")}
        </span>
        <span className="text-[length:var(--exits-text-sm)] text-muted">
          {t("sell.cartBarHint")}
        </span>
      </button>

      {cartSheetOpen ? (
        <div
          className="sell-cart-sheet-backdrop fixed inset-0 z-30 bg-black/40"
          role="presentation"
          onClick={() => setCartSheetOpen(false)}
        />
      ) : null}

      <div
        id="sell-cart-sheet-panel"
        data-testid="sell-cart-sheet"
        className={cn(
          "sell-cart-sheet fixed inset-x-0 bottom-0 z-40 flex max-h-[75dvh] flex-col gap-3 rounded-t-[var(--exits-radius-lg)] border border-border bg-surface p-4 shadow-[0_-8px_32px_rgba(0,0,0,0.12)] transition-transform duration-[var(--exits-motion-normal)]",
          cartSheetOpen ? "translate-y-0" : "translate-y-full pointer-events-none",
        )}
        aria-hidden={!cartSheetOpen}
      >
        <div className="flex items-center justify-between gap-3">
          <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">
            {t("sell.cartSheetTitle")}
          </h2>
          <Button
            type="button"
            variant="ghost"
            aria-label={t("sell.cartSheetClose")}
            onClick={() => setCartSheetOpen(false)}
          >
            {t("sell.cartSheetClose")}
          </Button>
        </div>
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{t("sell.cartEmpty")}</p>
        <Button
          data-testid="sell-pay"
          type="button"
          disabled
          title={t("sell.payDisabledTitle")}
          className="mt-auto w-full"
        >
          {t("sell.pay")}
        </Button>
      </div>
    </div>
  );
}
