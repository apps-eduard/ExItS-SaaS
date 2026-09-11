import { useState, type ReactNode } from "react";
import {
  Building2,
  Circle,
  CircleCheck,
  ClipboardList,
  Coins,
  MoreHorizontal,
  Package,
  PackagePlus,
  RefreshCw,
  ShoppingCart,
  TriangleAlert,
  Warehouse,
} from "lucide-react";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardMedia,
  CardTitle,
} from "@/components/ui/card";
import { CountBadge } from "@/components/exits/CountChip";
import { ExitsTabs } from "@/components/exits/ExitsTabs";
import { StatusChip } from "@/components/exits/StatusChip";
import { TagChip } from "@/components/exits/TagChip";
import { UiStandardsSection } from "@/features/ui-standards/UiStandardsSection";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";
import { formatPeso } from "@/lib/format-money";

function SampleFrame({
  label,
  children,
  hint,
  testId,
  className,
}: {
  label: string;
  children: ReactNode;
  hint?: string;
  testId?: string;
  className?: string;
}) {
  return (
    <div className="flex flex-col gap-1.5" data-testid={testId}>
      <span className="text-[length:var(--exits-text-xs)] uppercase tracking-wide text-muted">
        {label}
      </span>
      <div className={cn("min-w-0", className)}>{children}</div>
      {hint ? (
        <span className="text-[length:var(--exits-text-xs)] text-muted">{hint}</span>
      ) : null}
    </div>
  );
}

function StaticSampleGroup({ title, children }: { title: string; children: ReactNode }) {
  const slug = title
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-|-$/g, "");
  return (
    <section
      className="grid gap-2 border-t border-border pt-3 first:border-t-0 first:pt-0"
      data-testid={`ui-standards-cards-group-${slug}`}
    >
      <h3 className="m-0 text-[length:var(--exits-text-sm)] font-semibold text-muted">{title}</h3>
      <div className="grid gap-2 sm:grid-cols-2 lg:grid-cols-3">{children}</div>
    </section>
  );
}

type DisclosureProps = {
  isOpen: (id: string) => boolean;
  setOpen: (id: string, open: boolean) => void;
};

const WAREHOUSE_OPTIONS = [
  { key: "main", label: "Main Branch", meta: "Iloilo · 124 products", disabled: false },
  { key: "iloilo", label: "Iloilo Warehouse", meta: "Secondary · 86 products", disabled: false },
  { key: "kalibo", label: "Kalibo Warehouse", meta: "Unavailable", disabled: true },
] as const;

function EntityAvatar({ initials }: { initials: string }) {
  return (
    <span
      aria-hidden
      className="flex size-10 shrink-0 items-center justify-center rounded-full bg-[var(--exits-surface-muted)] text-[length:var(--exits-text-sm)] font-semibold text-foreground"
    >
      {initials}
    </span>
  );
}

function ProductImagePlaceholder({
  color,
  label,
  className,
}: {
  color?: string;
  label?: string;
  className?: string;
}) {
  return (
    <div
      className={cn(
        "flex size-full min-h-[4.5rem] min-w-[4.5rem] items-center justify-center bg-[var(--exits-surface-muted)] text-muted",
        className,
      )}
      style={color ? { backgroundColor: color } : undefined}
      aria-hidden={label ? undefined : true}
      aria-label={label}
    >
      {!color ? <Package className="size-5 opacity-60" aria-hidden /> : null}
    </div>
  );
}

export function UiStandardsCardsPanel({ isOpen, setOpen }: DisclosureProps) {
  const { t } = useI18n();
  const [interactiveClicked, setInteractiveClicked] = useState(false);
  const [selectedWarehouse, setSelectedWarehouse] = useState<string>("main");
  const [accountTab, setAccountTab] = useState("overview");

  return (
    <div className="grid gap-3" data-testid="ui-standards-cards-section">
      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{t("uiStandards.cardsPilotLede")}</p>

      <UiStandardsSection
        id="cards.treatments"
        title={t("uiStandards.cardsTreatmentsTitle")}
        description="Independent treatments, radius, and shadow — type/purpose is separate from appearance. PILOT / NOT LOCKED."
        summary="SURFACE · BORDERED · ELEVATED · INTERACTIVE"
        open={isOpen("cards.treatments")}
        onOpenChange={(open) => setOpen("cards.treatments", open)}
        testId="ui-standards-cards-treatments"
      >
        <div className="grid gap-3">
          <StaticSampleGroup title="NOT EVERYTHING NEEDS A CARD">
            <SampleFrame label="Guidance" className="sm:col-span-2 lg:col-span-3">
              <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                Use Card when grouping related information provides meaningful structure. Avoid card
                inside card, every field in a card, or excessive dashboard boxes — prefer whitespace
                and dividers when enough.
              </p>
            </SampleFrame>
          </StaticSampleGroup>

          <StaticSampleGroup title="DEFAULT RECOMMENDATION CANDIDATES — CANDIDATE / NOT LOCKED">
            <SampleFrame label="CANDIDATE / NOT LOCKED" className="sm:col-span-2 lg:col-span-3">
              <div className="grid gap-1 text-[length:var(--exits-text-xs)] text-muted">
                <div>NORMAL CONTENT → BORDERED / SURFACE</div>
                <div>DASHBOARD METRIC → KPI CARD</div>
                <div>BUSINESS ENTITY → ENTITY CARD</div>
                <div>PRODUCT → PRODUCT CARD</div>
                <div>QUICK ACTION → ACTION CARD</div>
                <div>SELECTION → SELECTABLE CARD</div>
                <div>WARNING / ERROR SUMMARY → STATUS CARD</div>
                <div>DENSE BUSINESS INFO → COMPACT CARD</div>
              </div>
            </SampleFrame>
          </StaticSampleGroup>

          <StaticSampleGroup title="TREATMENTS">
            <SampleFrame label="SURFACE">
              <Card treatment="surface">
                <CardTitle as="h4">Surface</CardTitle>
                <CardDescription>Minimal weight — forms, content sections.</CardDescription>
              </Card>
            </SampleFrame>
            <SampleFrame label="BORDERED" testId="ui-standards-card-treatment-bordered">
              <Card treatment="bordered">
                <CardTitle as="h4">Bordered</CardTitle>
                <CardDescription>Most common POS card — subtle 1px border.</CardDescription>
              </Card>
            </SampleFrame>
            <SampleFrame label="ELEVATED">
              <Card treatment="elevated">
                <CardTitle as="h4">Elevated</CardTitle>
                <CardDescription>Restrained shadow — important summaries.</CardDescription>
              </Card>
            </SampleFrame>
            <SampleFrame
              label="INTERACTIVE"
              testId="ui-standards-card-interactive"
              hint={interactiveClicked ? "Clicked — local demo state" : "Click to toggle demo state"}
            >
              <Card
                treatment="interactive"
                interactive
                onClick={() => setInteractiveClicked((prev) => !prev)}
              >
                <CardTitle as="h4">Interactive</CardTitle>
                <CardDescription>
                  {interactiveClicked
                    ? "Selected — navigates or performs an action."
                    : "Hover / focus / click — pointer + keyboard."}
                </CardDescription>
              </Card>
            </SampleFrame>
            <SampleFrame label="SELECTED">
              <Card treatment="selected" selected>
                <CardTitle as="h4">Selected</CardTitle>
                <CardDescription>Primary border + soft fill — chosen option.</CardDescription>
              </Card>
            </SampleFrame>
            <SampleFrame label="ACCENT · start · warning">
              <Card treatment="accent" accentTone="warning" accentPosition="start">
                <CardTitle as="h4">Accent</CardTitle>
                <CardDescription>Semantic inline-start accent — restrained.</CardDescription>
              </Card>
            </SampleFrame>
          </StaticSampleGroup>

          <StaticSampleGroup title="RADIUS">
            <SampleFrame label="STANDARD">
              <Card treatment="bordered" radius="standard">
                <CardTitle as="h4">Standard radius</CardTitle>
                <CardDescription>Default medium radius token.</CardDescription>
              </Card>
            </SampleFrame>
            <SampleFrame label="SOFT">
              <Card treatment="bordered" radius="soft">
                <CardTitle as="h4">Soft radius</CardTitle>
                <CardDescription>Slightly larger modern radius — candidate.</CardDescription>
              </Card>
            </SampleFrame>
          </StaticSampleGroup>

          <StaticSampleGroup title="SHADOW">
            <SampleFrame label="NO SHADOW · bordered">
              <Card treatment="bordered">
                <CardTitle as="h4">No shadow</CardTitle>
                <CardDescription>Border + surface — preferred default.</CardDescription>
              </Card>
            </SampleFrame>
            <SampleFrame label="SUBTLE SHADOW · elevated">
              <Card treatment="elevated">
                <CardTitle as="h4">Subtle shadow</CardTitle>
                <CardDescription>Elevated treatment only — not floating.</CardDescription>
              </Card>
            </SampleFrame>
          </StaticSampleGroup>
        </div>
      </UiStandardsSection>

      <UiStandardsSection
        id="cards.basic"
        title={t("uiStandards.cardsBasicTitle")}
        description="Content, summary, footers, header actions, chips, and in-card tabs — reuses locked Button / Chip / Tabs standards."
        summary="BASIC · SUMMARY · FOOTERS"
        open={isOpen("cards.basic")}
        onOpenChange={(open) => setOpen("cards.basic", open)}
        testId="ui-standards-cards-basic"
      >
        <div className="grid gap-3">
          <StaticSampleGroup title="BASIC CONTENT">
            <SampleFrame label="STORE INFORMATION">
              <Card treatment="bordered">
                <CardHeader>
                  <div className="min-w-0">
                    <CardTitle>Store information</CardTitle>
                    <CardDescription>Kizy Fruits Main Branch</CardDescription>
                  </div>
                </CardHeader>
                <CardContent>
                  <p className="m-0">
                    <span className="text-muted">Business hours</span>
                    <br />
                    8:00 AM – 8:00 PM
                  </p>
                </CardContent>
                <CardFooter className="justify-end border-t-0 pt-0">
                  <Button type="button" variant="outline" shape="soft">
                    Edit
                  </Button>
                </CardFooter>
              </Card>
            </SampleFrame>
          </StaticSampleGroup>

          <StaticSampleGroup title="SUMMARY">
            <SampleFrame label="ORDER SUMMARY">
              <Card treatment="bordered">
                <CardHeader>
                  <div className="min-w-0">
                    <CardTitle>Order summary</CardTitle>
                    <CardDescription>PO-20260911-000001</CardDescription>
                  </div>
                  <StatusChip tone="warning">Pending</StatusChip>
                </CardHeader>
                <CardContent>
                  <dl className="m-0 grid gap-1">
                    <div className="flex justify-between gap-2">
                      <dt className="text-muted">Subtotal</dt>
                      <dd className="m-0 tabular-nums">{formatPeso(1200)}</dd>
                    </div>
                    <div className="flex justify-between gap-2">
                      <dt className="text-muted">Discount</dt>
                      <dd className="m-0 tabular-nums">{formatPeso(100)}</dd>
                    </div>
                    <div className="flex justify-between gap-2">
                      <dt className="text-muted">Tax</dt>
                      <dd className="m-0 tabular-nums">{formatPeso(0)}</dd>
                    </div>
                    <div className="mt-1 flex justify-between gap-2 border-t border-border pt-2 font-semibold">
                      <dt>Total</dt>
                      <dd className="m-0 tabular-nums">{formatPeso(1100)}</dd>
                    </div>
                  </dl>
                </CardContent>
              </Card>
            </SampleFrame>
          </StaticSampleGroup>

          <StaticSampleGroup title="FOOTERS">
            <SampleFrame label="A · ACTION FOOTER">
              <Card treatment="bordered">
                <CardContent>
                  <p className="m-0 text-muted">Unsaved branch settings.</p>
                </CardContent>
                <CardFooter>
                  <Button type="button" variant="ghost">
                    Cancel
                  </Button>
                  <Button type="button" variant="default">
                    Save
                  </Button>
                </CardFooter>
              </Card>
            </SampleFrame>
            <SampleFrame label="B · METADATA FOOTER">
              <Card treatment="bordered">
                <CardContent>
                  <p className="m-0">Inventory snapshot for Main Warehouse.</p>
                </CardContent>
                <CardFooter className="border-t-0 pt-0 text-[length:var(--exits-text-xs)] text-muted">
                  Updated 5 minutes ago
                </CardFooter>
              </Card>
            </SampleFrame>
            <SampleFrame label="C · SPLIT FOOTER">
              <Card treatment="bordered">
                <CardContent>
                  <p className="m-0">Customer credit limit review.</p>
                </CardContent>
                <CardFooter>
                  <span className="text-[length:var(--exits-text-xs)] text-muted">
                    Updated 5 minutes ago
                  </span>
                  <Button type="button" variant="outline" shape="soft">
                    View
                  </Button>
                </CardFooter>
              </Card>
            </SampleFrame>
          </StaticSampleGroup>

          <StaticSampleGroup title="HEADER ACTIONS">
            <SampleFrame label="ICON ONLY · ROUND · GHOST">
              <Card treatment="bordered">
                <CardHeader>
                  <div className="min-w-0">
                    <CardTitle>Customer details</CardTitle>
                    <CardDescription>Kizy Fruits</CardDescription>
                  </div>
                  <div className="flex shrink-0 items-center gap-1">
                    <Button
                      type="button"
                      variant="ghost"
                      shape="round"
                      size="icon"
                      aria-label="More actions"
                    >
                      <MoreHorizontal aria-hidden />
                    </Button>
                  </div>
                </CardHeader>
              </Card>
            </SampleFrame>
            <SampleFrame label="REFRESH">
              <Card treatment="bordered">
                <CardHeader>
                  <div className="min-w-0">
                    <CardTitle>Inventory</CardTitle>
                    <CardDescription>Main Warehouse</CardDescription>
                  </div>
                  <Button
                    type="button"
                    variant="ghost"
                    shape="round"
                    size="icon"
                    aria-label="Refresh"
                  >
                    <RefreshCw aria-hidden />
                  </Button>
                </CardHeader>
              </Card>
            </SampleFrame>
          </StaticSampleGroup>

          <StaticSampleGroup title="WITH CHIPS">
            <SampleFrame label="ACCOUNT DETAILS">
              <Card treatment="bordered">
                <CardHeader>
                  <div className="min-w-0">
                    <CardTitle>Account details</CardTitle>
                    <CardDescription>Kizy Fruits · Business customer</CardDescription>
                  </div>
                </CardHeader>
                <CardContent>
                  <div className="flex flex-wrap gap-1.5">
                    <TagChip tone="info">B2B</TagChip>
                    <StatusChip tone="success">Active</StatusChip>
                  </div>
                </CardContent>
              </Card>
            </SampleFrame>
          </StaticSampleGroup>

          <StaticSampleGroup title="WITH TABS">
            <SampleFrame label="SOFT TABS IN CARD" className="sm:col-span-2 lg:col-span-3">
              <Card treatment="bordered">
                <CardHeader>
                  <div className="min-w-0">
                    <CardTitle>Customer</CardTitle>
                    <CardDescription>Kizy Fruits</CardDescription>
                  </div>
                </CardHeader>
                <CardContent className="grid gap-3">
                  <ExitsTabs
                    variant="soft"
                    ariaLabel="Customer detail tabs"
                    value={accountTab}
                    onValueChange={setAccountTab}
                    items={[
                      { key: "overview", label: "Overview" },
                      { key: "purchases", label: "Purchases" },
                      { key: "credit", label: "Credit" },
                    ]}
                    panels={{
                      overview: (
                        <p className="m-0 text-muted">
                          Overview — credit limit {formatPeso(50000)}, terms Net 30.
                        </p>
                      ),
                      purchases: (
                        <p className="m-0 text-muted">Purchases — 12 orders this month.</p>
                      ),
                      credit: (
                        <p className="m-0 text-muted">Credit — {formatPeso(12400)} outstanding.</p>
                      ),
                    }}
                  />
                </CardContent>
              </Card>
            </SampleFrame>
          </StaticSampleGroup>
        </div>
      </UiStandardsSection>

      <UiStandardsSection
        id="cards.kpi"
        title={t("uiStandards.cardsKpiTitle")}
        description="Dashboard metrics — neutral surface by default; semantic accents stay small."
        summary="TEXT · ICON · CHIP · SUBMETRIC"
        open={isOpen("cards.kpi")}
        onOpenChange={(open) => setOpen("cards.kpi", open)}
        testId="ui-standards-cards-kpi"
      >
        <div className="grid gap-3">
          <StaticSampleGroup title="KPI VARIANTS">
            <div className="grid gap-2 sm:col-span-2 sm:grid-cols-2 lg:col-span-3 lg:grid-cols-4">
              <SampleFrame label="A · TEXT KPI">
                <Card treatment="bordered" data-testid="ui-standards-card-kpi-sales">
                  <CardDescription className="uppercase tracking-wide">Today&apos;s sales</CardDescription>
                  <p className="m-0 text-[length:var(--exits-text-xl)] font-semibold tabular-nums">
                    {formatPeso(24850)}
                  </p>
                  <p className="m-0 text-[length:var(--exits-text-xs)] text-[var(--exits-success)]">
                    +8.4% vs yesterday
                  </p>
                </Card>
              </SampleFrame>
              <SampleFrame label="B · ICON KPI">
                <Card treatment="bordered">
                  <CardHeader className="items-center gap-2">
                    <Coins className="size-4 shrink-0 text-muted" aria-hidden />
                    <CardDescription className="uppercase tracking-wide">Today&apos;s sales</CardDescription>
                  </CardHeader>
                  <CardContent className="grid gap-0.5 pt-0">
                    <p className="m-0 text-[length:var(--exits-text-xl)] font-semibold tabular-nums">
                      {formatPeso(24850)}
                    </p>
                    <p className="m-0 text-[length:var(--exits-text-xs)] text-[var(--exits-success)]">
                      +8.4% vs yesterday
                    </p>
                  </CardContent>
                </Card>
              </SampleFrame>
              <SampleFrame label="C · KPI + CHIP">
                <Card treatment="bordered">
                  <CardDescription className="uppercase tracking-wide">Low stock</CardDescription>
                  <p className="m-0 text-[length:var(--exits-text-xl)] font-semibold tabular-nums">18</p>
                  <StatusChip tone="warning">Needs attention</StatusChip>
                </Card>
              </SampleFrame>
              <SampleFrame label="D · KPI + SUBMETRIC">
                <Card treatment="bordered">
                  <CardDescription className="uppercase tracking-wide">Orders</CardDescription>
                  <p className="m-0 text-[length:var(--exits-text-xl)] font-semibold tabular-nums">126</p>
                  <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
                    Pending <CountBadge tone="neutral" count={12} />
                  </p>
                </Card>
              </SampleFrame>
            </div>
          </StaticSampleGroup>

          <StaticSampleGroup title="KPI COLOR RULE">
            <SampleFrame label="NEUTRAL DEFAULT" className="sm:col-span-2 lg:col-span-3">
              <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                Normal KPI cards use neutral surface. Primary only for intentionally emphasized
                metrics; success / warning / danger via small accents — not rainbow dashboards.
              </p>
            </SampleFrame>
          </StaticSampleGroup>
        </div>
      </UiStandardsSection>

      <UiStandardsSection
        id="cards.action"
        title={t("uiStandards.cardsActionTitle")}
        description="One clear action per card — locked Button Standard. Card body is not fully clickable."
        summary="NEW SALE · REQUEST · ADD"
        open={isOpen("cards.action")}
        onOpenChange={(open) => setOpen("cards.action", open)}
        testId="ui-standards-cards-action"
      >
        <StaticSampleGroup title="ACTION CARDS">
          <SampleFrame label="NEW SALE">
            <Card treatment="bordered">
              <CardHeader>
                <ShoppingCart className="size-4 text-[var(--exits-primary)]" aria-hidden />
                <div className="min-w-0">
                  <CardTitle>New sale</CardTitle>
                  <CardDescription>Start a new customer transaction.</CardDescription>
                </div>
              </CardHeader>
              <CardFooter className="justify-end border-t-0 pt-0">
                <Button type="button" variant="default">
                  Start sale
                </Button>
              </CardFooter>
            </Card>
          </SampleFrame>
          <SampleFrame label="REQUEST STOCK">
            <Card treatment="bordered">
              <CardHeader>
                <PackagePlus className="size-4 text-muted" aria-hidden />
                <div className="min-w-0">
                  <CardTitle>Request stock</CardTitle>
                  <CardDescription>Request inventory from a warehouse.</CardDescription>
                </div>
              </CardHeader>
              <CardFooter className="justify-end border-t-0 pt-0">
                <Button type="button" variant="outline">
                  Request stock
                </Button>
              </CardFooter>
            </Card>
          </SampleFrame>
          <SampleFrame label="ADD PRODUCT">
            <Card treatment="bordered">
              <CardHeader>
                <Package className="size-4 text-muted" aria-hidden />
                <div className="min-w-0">
                  <CardTitle>Add product</CardTitle>
                  <CardDescription>Create a new catalog item.</CardDescription>
                </div>
              </CardHeader>
              <CardFooter className="justify-end border-t-0 pt-0">
                <Button type="button" variant="outline">
                  Add product
                </Button>
              </CardFooter>
            </Card>
          </SampleFrame>
        </StaticSampleGroup>
      </UiStandardsSection>

      <UiStandardsSection
        id="cards.entity"
        title={t("uiStandards.cardsEntityTitle")}
        description="Customers, suppliers, branches — optional avatar; actions use locked Button Standard."
        summary="WITH AVATAR · WITHOUT"
        open={isOpen("cards.entity")}
        onOpenChange={(open) => setOpen("cards.entity", open)}
        testId="ui-standards-cards-entity"
      >
        <StaticSampleGroup title="ENTITY CARD">
          <SampleFrame label="WITH AVATAR / INITIALS">
            <Card treatment="bordered" layout="horizontal">
              <EntityAvatar initials="KF" />
              <div className="flex min-w-0 flex-1 flex-col gap-2">
                <CardHeader className="flex-col items-start gap-1">
                  <CardTitle>Kizy Fruits</CardTitle>
                  <CardDescription>Main Branch · Iloilo</CardDescription>
                </CardHeader>
                <CardContent className="pt-0">
                  <div className="flex flex-wrap gap-1.5">
                    <TagChip tone="info">B2B</TagChip>
                    <StatusChip tone="success">Active</StatusChip>
                  </div>
                  <p className="m-0 mt-2 text-[length:var(--exits-text-xs)] text-muted">
                    Last order: Sep 11
                  </p>
                </CardContent>
                <CardFooter className="justify-end border-t-0 pt-0">
                  <Button type="button" variant="outline" shape="soft">
                    View details
                  </Button>
                </CardFooter>
              </div>
            </Card>
          </SampleFrame>
          <SampleFrame label="WITHOUT AVATAR">
            <Card treatment="bordered">
              <CardHeader>
                <Building2 className="size-4 shrink-0 text-muted" aria-hidden />
                <div className="min-w-0">
                  <CardTitle>Mica Trading</CardTitle>
                  <CardDescription>Supplier · Cebu</CardDescription>
                </div>
              </CardHeader>
              <CardContent>
                <div className="flex flex-wrap gap-1.5">
                  <StatusChip tone="success">Active</StatusChip>
                  <TagChip tone="primary">Preferred</TagChip>
                </div>
              </CardContent>
              <CardFooter className="justify-end border-t-0 pt-0">
                <Button type="button" variant="outline" shape="soft">
                  View details
                </Button>
              </CardFooter>
            </Card>
          </SampleFrame>
        </StaticSampleGroup>
      </UiStandardsSection>

      <UiStandardsSection
        id="cards.product"
        title={t("uiStandards.cardsProductTitle")}
        description="Vertical and horizontal product layouts — image present vs missing fallback."
        summary="VERTICAL · HORIZONTAL"
        open={isOpen("cards.product")}
        onOpenChange={(open) => setOpen("cards.product", open)}
        testId="ui-standards-cards-product"
      >
        <StaticSampleGroup title="PRODUCT / MEDIA">
          <SampleFrame label="VERTICAL · WITH IMAGE">
            <Card treatment="bordered">
              <CardMedia className="aspect-[4/3] w-full">
                <ProductImagePlaceholder color="color-mix(in srgb, var(--exits-success) 25%, var(--exits-surface-muted))" />
              </CardMedia>
              <CardHeader>
                <CardTitle>Apple</CardTitle>
                <CardDescription>{formatPeso(120)} / kg</CardDescription>
              </CardHeader>
              <CardContent>
                <div className="flex flex-wrap gap-1.5">
                  <TagChip tone="neutral">Weighted</TagChip>
                  <StatusChip tone="warning">Low stock</StatusChip>
                </div>
                <p className="m-0 mt-2 text-[length:var(--exits-text-xs)] text-muted">
                  Stock: 8.5 kg
                </p>
              </CardContent>
            </Card>
          </SampleFrame>
          <SampleFrame label="VERTICAL · MISSING IMAGE">
            <Card treatment="bordered">
              <CardMedia className="aspect-[4/3] w-full">
                <ProductImagePlaceholder label="No product image" />
              </CardMedia>
              <CardHeader>
                <CardTitle>Banana Lakatan</CardTitle>
                <CardDescription>{formatPeso(76)} / kg</CardDescription>
              </CardHeader>
              <CardContent>
                <div className="flex flex-wrap gap-1.5">
                  <TagChip tone="neutral">Weighted</TagChip>
                  <StatusChip tone="success">In stock</StatusChip>
                </div>
              </CardContent>
            </Card>
          </SampleFrame>
          <SampleFrame label="HORIZONTAL" className="sm:col-span-2 lg:col-span-3">
            <Card treatment="bordered" layout="horizontal">
              <CardMedia className="size-20 shrink-0">
                <ProductImagePlaceholder color="color-mix(in srgb, var(--exits-success) 25%, var(--exits-surface-muted))" />
              </CardMedia>
              <div className="flex min-w-0 flex-1 flex-col gap-2">
                <CardHeader className="flex-col items-start gap-0.5">
                  <CardTitle>Apple</CardTitle>
                  <CardDescription>{formatPeso(120)} / kg</CardDescription>
                </CardHeader>
                <CardContent className="pt-0">
                  <div className="flex flex-wrap gap-1.5">
                    <TagChip tone="neutral">Weighted</TagChip>
                    <StatusChip tone="warning">Low stock</StatusChip>
                  </div>
                  <p className="m-0 mt-1 text-[length:var(--exits-text-xs)] text-muted">
                    Stock: 8.5 kg
                  </p>
                </CardContent>
              </div>
            </Card>
          </SampleFrame>
        </StaticSampleGroup>
      </UiStandardsSection>

      <UiStandardsSection
        id="cards.selectable"
        title={t("uiStandards.cardsSelectableTitle")}
        description="Choose supply source / branch — distinct from interactive navigation cards."
        summary="UNSELECTED · SELECTED · DISABLED"
        open={isOpen("cards.selectable")}
        onOpenChange={(open) => setOpen("cards.selectable", open)}
        testId="ui-standards-cards-selectable"
      >
        <div className="grid gap-3">
          <StaticSampleGroup title="WAREHOUSE RADIO GROUP">
            <SampleFrame
              label="SELECT ONE"
              className="sm:col-span-2 lg:col-span-3"
              testId="ui-standards-card-selectable-main"
            >
              <div className="grid gap-2 sm:grid-cols-3" role="radiogroup" aria-label="Warehouse">
                {WAREHOUSE_OPTIONS.map((option) => {
                  const selected = selectedWarehouse === option.key;
                  return (
                    <Card
                      key={option.key}
                      as="button"
                      type="button"
                      role="radio"
                      aria-checked={selected}
                      aria-disabled={option.disabled}
                      disabled={option.disabled}
                      treatment={selected ? "selected" : "bordered"}
                      selected={selected}
                      className="text-start"
                      onClick={() => {
                        if (!option.disabled) setSelectedWarehouse(option.key);
                      }}
                    >
                      <CardHeader className="items-center gap-2">
                        <CircleCheck
                          className={cn(
                            "size-4 shrink-0",
                            selected ? "text-[var(--exits-primary)]" : "text-muted opacity-40",
                          )}
                          aria-hidden
                        />
                        <div className="min-w-0">
                          <CardTitle as="h4">{option.label}</CardTitle>
                          <CardDescription>{option.meta}</CardDescription>
                        </div>
                      </CardHeader>
                    </Card>
                  );
                })}
              </div>
            </SampleFrame>
          </StaticSampleGroup>

          <StaticSampleGroup title="INDICATOR CANDIDATES — CANDIDATE / NOT LOCKED">
            <SampleFrame label="A · RADIO · Circle">
              <Card treatment="bordered" as="div" role="radio" aria-checked={false} tabIndex={0}>
                <CardHeader className="items-center gap-2">
                  <Circle className="size-4 shrink-0 text-muted" aria-hidden />
                  <CardTitle as="h4">Main Branch</CardTitle>
                </CardHeader>
              </Card>
            </SampleFrame>
            <SampleFrame label="B · CircleCheck">
              <Card treatment="selected" selected as="div" role="radio" aria-checked tabIndex={0}>
                <CardHeader className="items-center gap-2">
                  <CircleCheck className="size-4 shrink-0 text-[var(--exits-primary)]" aria-hidden />
                  <CardTitle as="h4">Main Branch</CardTitle>
                </CardHeader>
              </Card>
            </SampleFrame>
            <SampleFrame label="C · BORDER ONLY">
              <Card treatment="selected" selected as="div" role="radio" aria-checked tabIndex={0}>
                <CardTitle as="h4">Main Branch</CardTitle>
                <CardDescription>Selected border + background only</CardDescription>
              </Card>
            </SampleFrame>
          </StaticSampleGroup>

          <StaticSampleGroup title="MULTI SELECT NOTE">
            <SampleFrame label="CHECKBOX WHERE VALID" className="sm:col-span-2 lg:col-span-3">
              <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                Single-select uses radio semantics. Multi-select (e.g. filters) may use checkbox
                patterns — do not mix navigate/interactive with selection semantics on the same
                control.
              </p>
            </SampleFrame>
          </StaticSampleGroup>
        </div>
      </UiStandardsSection>

      <UiStandardsSection
        id="cards.status"
        title={t("uiStandards.cardsStatusTitle")}
        description="Restrained semantic accent on neutral surface — not full-card color fills."
        summary="WARNING · DANGER · SUCCESS"
        open={isOpen("cards.status")}
        onOpenChange={(open) => setOpen("cards.status", open)}
        testId="ui-standards-cards-status"
      >
        <div className="grid gap-3">
          <StaticSampleGroup title="ACCENT · START">
            <SampleFrame label="WARNING">
              <Card treatment="accent" accentTone="warning" accentPosition="start">
                <CardTitle as="h4">Low stock</CardTitle>
                <CardDescription>18 products need attention.</CardDescription>
                <CardFooter className="justify-end border-t-0 pt-0">
                  <Button type="button" variant="outline" shape="soft">
                    Review inventory
                  </Button>
                </CardFooter>
              </Card>
            </SampleFrame>
            <SampleFrame label="DANGER">
              <Card treatment="accent" accentTone="danger" accentPosition="start">
                <CardTitle as="h4">Payment failed</CardTitle>
                <CardDescription>GCash payment requires review.</CardDescription>
                <CardFooter className="justify-end border-t-0 pt-0">
                  <Button type="button" variant="destructive" shape="soft">
                    View transaction
                  </Button>
                </CardFooter>
              </Card>
            </SampleFrame>
            <SampleFrame label="SUCCESS">
              <Card treatment="accent" accentTone="success" accentPosition="start">
                <CardTitle as="h4">Order completed</CardTitle>
                <CardDescription>Order #SO12345 completed.</CardDescription>
              </Card>
            </SampleFrame>
          </StaticSampleGroup>

          <StaticSampleGroup title="ACCENT POSITION CANDIDATES — CANDIDATE / NOT LOCKED">
            <SampleFrame label="TOP · warning">
              <Card treatment="accent" accentTone="warning" accentPosition="top">
                <CardTitle as="h4">Top accent</CardTitle>
                <CardDescription>Block-start border accent.</CardDescription>
              </Card>
            </SampleFrame>
            <SampleFrame label="TINT · warning">
              <Card treatment="accent" accentTone="warning" accentPosition="tint">
                <CardTitle as="h4">Soft tint</CardTitle>
                <CardDescription>Subtle semantic background fill.</CardDescription>
              </Card>
            </SampleFrame>
            <SampleFrame label="START · locked candidate">
              <Card treatment="accent" accentTone="warning" accentPosition="start">
                <CardTitle as="h4">Start accent</CardTitle>
                <CardDescription>Inline-start — default candidate.</CardDescription>
              </Card>
            </SampleFrame>
          </StaticSampleGroup>
        </div>
      </UiStandardsSection>

      <UiStandardsSection
        id="cards.compact"
        title={t("uiStandards.cardsCompactTitle")}
        description="Dense business info — padding=compact, not a separate global density setting."
        summary="COMPACT · WAREHOUSE"
        open={isOpen("cards.compact")}
        onOpenChange={(open) => setOpen("cards.compact", open)}
        testId="ui-standards-cards-compact"
      >
        <StaticSampleGroup title="COMPACT CARD">
          <SampleFrame label="MAIN WAREHOUSE · padding=compact">
            <Card treatment="bordered" padding="compact">
              <CardHeader className="items-center gap-2">
                <Warehouse className="size-4 shrink-0 text-muted" aria-hidden />
                <div className="min-w-0 flex-1">
                  <CardTitle as="h4">Main Warehouse</CardTitle>
                  <div className="mt-1 flex flex-wrap gap-1">
                    <StatusChip tone="success">Active</StatusChip>
                  </div>
                </div>
              </CardHeader>
              <CardContent className="pt-0 text-[length:var(--exits-text-xs)] text-muted">
                124 products · 18 low stock
              </CardContent>
            </Card>
          </SampleFrame>
        </StaticSampleGroup>
      </UiStandardsSection>

      <UiStandardsSection
        id="cards.states"
        title={t("uiStandards.cardsStatesTitle")}
        description="Loading skeleton, empty, and error — restrained; locked Button for actions."
        summary="LOADING · EMPTY · ERROR"
        open={isOpen("cards.states")}
        onOpenChange={(open) => setOpen("cards.states", open)}
        testId="ui-standards-cards-states"
      >
        <StaticSampleGroup title="CARD STATES">
          <SampleFrame label="LOADING">
            <Card treatment="bordered" aria-busy="true" aria-label="Loading">
              <div className="grid gap-2">
                <div className="h-4 w-2/5 animate-pulse rounded-[var(--exits-radius-sm)] bg-[var(--exits-surface-muted)]" />
                <div className="h-3 w-full animate-pulse rounded-[var(--exits-radius-sm)] bg-[var(--exits-surface-muted)]" />
                <div className="h-3 w-4/5 animate-pulse rounded-[var(--exits-radius-sm)] bg-[var(--exits-surface-muted)]" />
                <div className="h-3 w-3/5 animate-pulse rounded-[var(--exits-radius-sm)] bg-[var(--exits-surface-muted)]" />
              </div>
            </Card>
          </SampleFrame>
          <SampleFrame label="EMPTY">
            <Card treatment="bordered">
              <CardContent className="flex flex-col items-center gap-2 py-2 text-center">
                <ClipboardList className="size-6 text-muted" aria-hidden />
                <CardTitle as="h4">No recent orders</CardTitle>
                <CardDescription>Orders from the last 7 days appear here.</CardDescription>
                <Button type="button" variant="outline" shape="soft" className="mt-1">
                  Create order
                </Button>
              </CardContent>
            </Card>
          </SampleFrame>
          <SampleFrame label="ERROR">
            <Card treatment="bordered">
              <CardContent className="grid gap-2">
                <div className="flex items-start gap-2">
                  <TriangleAlert className="mt-0.5 size-4 shrink-0 text-[var(--exits-danger)]" aria-hidden />
                  <div>
                    <CardTitle as="h4">Couldn&apos;t load inventory</CardTitle>
                    <CardDescription>Check your connection and try again.</CardDescription>
                  </div>
                </div>
                <div className="flex justify-end">
                  <Button type="button" variant="destructive" shape="soft">
                    Retry
                  </Button>
                </div>
              </CardContent>
            </Card>
          </SampleFrame>
        </StaticSampleGroup>
      </UiStandardsSection>

      <UiStandardsSection
        id="cards.real-world"
        title={t("uiStandards.cardsRealWorldTitle")}
        description="Polished POS-shaped examples — static demo, no APIs."
        summary="SALES · STOCK · ENTITY · ACTION"
        open={isOpen("cards.real-world")}
        onOpenChange={(open) => setOpen("cards.real-world", open)}
        testId="ui-standards-cards-real-world"
      >
        <div className="grid gap-2 sm:grid-cols-2 lg:grid-cols-3">
          <Card treatment="elevated">
            <CardDescription className="uppercase tracking-wide">Today&apos;s sales</CardDescription>
            <p className="m-0 text-[length:var(--exits-text-xl)] font-semibold tabular-nums">
              {formatPeso(24850)}
            </p>
            <p className="m-0 text-[length:var(--exits-text-xs)] text-[var(--exits-success)]">
              +8.4% from yesterday
            </p>
          </Card>

          <Card treatment="accent" accentTone="warning" accentPosition="start">
            <CardTitle as="h4">Low stock</CardTitle>
            <p className="m-0 text-[length:var(--exits-text-lg)] font-semibold tabular-nums">18 products</p>
            <StatusChip tone="warning">Needs attention</StatusChip>
          </Card>

          <Card treatment="bordered">
            <CardHeader>
              <EntityAvatar initials="KF" />
              <div className="min-w-0">
                <CardTitle>Kizy Fruits</CardTitle>
                <CardDescription>Main Branch</CardDescription>
              </div>
            </CardHeader>
            <CardContent>
              <div className="flex flex-wrap gap-1.5">
                <TagChip tone="info">B2B</TagChip>
                <StatusChip tone="success">Active</StatusChip>
              </div>
            </CardContent>
            <CardFooter className="justify-end border-t-0 pt-0">
              <Button type="button" variant="outline" shape="soft">
                View details
              </Button>
            </CardFooter>
          </Card>

          <Card treatment="bordered" layout="horizontal">
            <CardMedia className="size-16 shrink-0">
              <ProductImagePlaceholder color="color-mix(in srgb, var(--exits-success) 25%, var(--exits-surface-muted))" />
            </CardMedia>
            <div className="flex min-w-0 flex-1 flex-col gap-1">
              <CardTitle as="h4">Apple</CardTitle>
              <CardDescription>{formatPeso(120)}/kg</CardDescription>
              <div className="flex flex-wrap gap-1">
                <TagChip tone="neutral">Weighted</TagChip>
                <StatusChip tone="warning">Low stock</StatusChip>
              </div>
              <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">Stock 8.5 kg</p>
            </div>
          </Card>

          <Card treatment="bordered">
            <CardHeader>
              <Warehouse className="size-4 text-muted" aria-hidden />
              <div className="min-w-0">
                <CardTitle>Main Warehouse</CardTitle>
              </div>
            </CardHeader>
            <CardContent>
              <div className="flex flex-wrap gap-1.5">
                <StatusChip tone="success">Active</StatusChip>
                <TagChip tone="primary">Preferred</TagChip>
              </div>
              <p className="m-0 mt-2 text-[length:var(--exits-text-sm)] text-muted">124 products</p>
            </CardContent>
          </Card>

          <Card treatment="bordered">
            <CardHeader>
              <PackagePlus className="size-4 text-muted" aria-hidden />
              <div className="min-w-0">
                <CardTitle>Request stock</CardTitle>
                <CardDescription>Move inventory from a connected warehouse.</CardDescription>
              </div>
            </CardHeader>
            <CardFooter className="justify-end border-t-0 pt-0">
              <Button type="button" variant="default">
                Request stock
              </Button>
            </CardFooter>
          </Card>
        </div>
      </UiStandardsSection>

      <UiStandardsSection
        id="cards.cheatsheet"
        title={t("uiStandards.cardsCheatTitle")}
        description={t("uiStandards.cardsCheatLede")}
        summary="PILOT · NOT LOCKED"
        open={isOpen("cards.cheatsheet")}
        onOpenChange={(open) => setOpen("cards.cheatsheet", open)}
        testId="ui-standards-cards-cheatsheet"
      >
        <div
          className="rounded-[var(--exits-radius-md)] border border-border bg-[var(--exits-surface-muted)]/30 p-3 font-mono text-[length:var(--exits-text-xs)] leading-relaxed text-foreground"
          data-testid="ui-standards-cards-cheatsheet-body"
        >
          <p className="m-0 mb-2 font-sans text-[length:var(--exits-text-sm)] font-semibold tracking-wide text-muted">
            {t("uiStandards.cardsPilotBadge")}
          </p>
          <pre className="m-0 whitespace-pre-wrap">{`CARD STANDARD
PILOT / NOT LOCKED

TYPES:

BASIC CARD
SUMMARY CARD
KPI CARD
ACTION CARD
ENTITY CARD
PRODUCT CARD
SELECTABLE CARD
STATUS CARD
COMPACT CARD

TREATMENTS:

SURFACE
BORDERED
ELEVATED
INTERACTIVE
SELECTED
ACCENT

LAYOUT:

VERTICAL
HORIZONTAL

OPTIONS:

WITH ICON
WITH CHIP
WITH COUNT
WITH IMAGE
WITH FOOTER
WITH ACTIONS

Examples:

Today's sales
KPI CARD

Customer
ENTITY CARD + WITH CHIP + WITH ACTIONS

Product
PRODUCT CARD + WITH IMAGE + WITH CHIP

Warehouse selector
SELECTABLE CARD

Quick action
ACTION CARD + BORDERED

Low stock warning
STATUS CARD WARNING

Dense warehouse summary
COMPACT CARD

Clickable customer
ENTITY CARD + INTERACTIVE

DEFAULT RECOMMENDATION CANDIDATES — CANDIDATE / NOT LOCKED

NORMAL CONTENT
→ BORDERED / SURFACE

DASHBOARD METRIC
→ KPI CARD

BUSINESS ENTITY
→ ENTITY CARD

PRODUCT
→ PRODUCT CARD

QUICK ACTION
→ ACTION CARD

SELECTION
→ SELECTABLE CARD

WARNING / ERROR SUMMARY
→ STATUS CARD

DENSE BUSINESS INFO
→ COMPACT CARD

STATUS
PILOT / NOT LOCKED`}</pre>
        </div>
      </UiStandardsSection>
    </div>
  );
}
