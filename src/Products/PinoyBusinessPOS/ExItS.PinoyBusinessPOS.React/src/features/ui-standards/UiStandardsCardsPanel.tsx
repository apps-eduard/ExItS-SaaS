import { useState, type ReactNode } from "react";
import {
  Building2,
  Check,
  Circle,
  CircleCheck,
  ClipboardList,
  Clock3,
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
  CardReveal,
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
    <div className={cn("flex min-w-0 flex-col gap-1.5", className)} data-testid={testId}>
      <span className="text-[length:var(--exits-text-xs)] uppercase tracking-wide text-muted">
        {label}
      </span>
      <div className="min-w-0">{children}</div>
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
  {
    key: "main",
    label: "Main Branch",
    subtitle: "Iloilo",
    detail: "124 products",
    disabled: false,
  },
  {
    key: "iloilo",
    label: "Iloilo Warehouse",
    subtitle: "Secondary",
    detail: "86 products",
    disabled: false,
  },
  {
    key: "kalibo",
    label: "Kalibo Warehouse",
    subtitle: "Unavailable",
    detail: null,
    disabled: true,
  },
] as const;

type SelectableIndicator = "radio" | "check" | "none";

function SelectableCardBody({
  title,
  subtitle,
  detail,
  selected,
  disabled,
  indicator,
}: {
  title: string;
  subtitle: string;
  detail?: string | null;
  selected?: boolean;
  disabled?: boolean;
  indicator: SelectableIndicator;
}) {
  return (
    <div className="flex min-w-0 items-start justify-between gap-3">
      <div className="min-w-0 flex-1 text-start">
        <CardTitle
          as="h4"
          className={cn(
            "text-[length:var(--exits-text-sm)]",
            disabled && "text-muted",
          )}
        >
          {title}
        </CardTitle>
        <CardDescription className={cn(disabled && "opacity-90")}>{subtitle}</CardDescription>
        {detail ? (
          <p
            className={cn(
              "m-0 mt-1 text-[length:var(--exits-text-xs)] text-muted",
              disabled && "opacity-90",
            )}
          >
            {detail}
          </p>
        ) : null}
      </div>
      {indicator === "none" ? (
        <span className="size-4 shrink-0" aria-hidden />
      ) : indicator === "check" ? (
        <CircleCheck
          className={cn(
            "mt-0.5 size-4 shrink-0",
            selected ? "text-[var(--exits-primary)]" : "text-muted opacity-45",
            disabled && "opacity-50",
          )}
          aria-hidden
        />
      ) : (
        <Circle
          className={cn(
            "mt-0.5 size-4 shrink-0",
            selected ? "text-[var(--exits-primary)]" : "text-muted opacity-55",
            disabled && "opacity-50",
          )}
          aria-hidden
        />
      )}
    </div>
  );
}

function EntityAvatar({ initials }: { initials: string }) {
  return (
    <span
      aria-hidden
      className="flex size-9 shrink-0 items-center justify-center rounded-full bg-[var(--exits-surface-muted)] text-[length:var(--exits-text-sm)] font-semibold text-foreground"
    >
      {initials}
    </span>
  );
}

function EntityLeadingIcon() {
  return (
    <span
      aria-hidden
      className="flex size-9 shrink-0 items-center justify-center rounded-[var(--exits-radius-sm)] bg-[var(--exits-surface-muted)] text-muted"
    >
      <Building2 className="size-4" aria-hidden />
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
        description="Independent treatments, radius, and shadow — type/purpose is separate from appearance. APPROVED / LOCKED."
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

          <StaticSampleGroup title="DEFAULT USAGE RECOMMENDATIONS — APPROVED / LOCKED">
            <SampleFrame label="APPROVED / LOCKED" className="sm:col-span-2 lg:col-span-3">
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
            <Card treatment="bordered" className="flex h-full min-h-[11.5rem] flex-col gap-3">
              <div className="flex min-w-0 items-start gap-2.5">
                <EntityAvatar initials="KF" />
                <div className="min-w-0 flex-1">
                  <CardTitle>Kizy Fruits</CardTitle>
                  <CardDescription>Main Branch · Iloilo</CardDescription>
                </div>
              </div>
              <CardContent className="flex flex-1 flex-col gap-2 pt-0">
                <div className="flex flex-wrap gap-1.5">
                  <TagChip tone="info">B2B</TagChip>
                  <StatusChip tone="success">Active</StatusChip>
                </div>
                <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
                  Last order: Sep 11
                </p>
              </CardContent>
              <CardFooter className="mt-auto justify-end border-t-0 pt-0">
                <Button type="button" variant="outline" shape="soft">
                  View details
                </Button>
              </CardFooter>
            </Card>
          </SampleFrame>
          <SampleFrame label="WITHOUT AVATAR">
            <Card treatment="bordered" className="flex h-full min-h-[11.5rem] flex-col gap-3">
              <div className="flex min-w-0 items-start gap-2.5">
                <EntityLeadingIcon />
                <div className="min-w-0 flex-1">
                  <CardTitle>Mica Trading</CardTitle>
                  <CardDescription>Supplier · Cebu</CardDescription>
                </div>
              </div>
              <CardContent className="flex flex-1 flex-col gap-2 pt-0">
                <div className="flex flex-wrap gap-1.5">
                  <StatusChip tone="success">Active</StatusChip>
                  <TagChip tone="primary">Preferred</TagChip>
                </div>
              </CardContent>
              <CardFooter className="mt-auto justify-end border-t-0 pt-0">
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
              <div
                className="grid grid-cols-1 gap-2 [grid-template-columns:repeat(auto-fit,minmax(min(100%,13.75rem),1fr))]"
                role="radiogroup"
                aria-label="Warehouse"
              >
                {WAREHOUSE_OPTIONS.map((option) => {
                  const selected = selectedWarehouse === option.key;
                  return (
                    <Card
                      key={option.key}
                      as="button"
                      type="button"
                      role="radio"
                      aria-checked={selected}
                      aria-disabled={option.disabled || undefined}
                      disabled={option.disabled}
                      treatment={selected ? "selected" : "bordered"}
                      selected={selected}
                      className={cn(
                        "h-full min-h-[5.75rem] text-start disabled:cursor-not-allowed disabled:opacity-60",
                        option.disabled && "hover:translate-y-0 hover:shadow-none",
                      )}
                      onClick={() => {
                        if (!option.disabled) setSelectedWarehouse(option.key);
                      }}
                    >
                      <SelectableCardBody
                        title={option.label}
                        subtitle={option.subtitle}
                        detail={option.detail}
                        selected={selected}
                        disabled={option.disabled}
                        indicator="check"
                      />
                    </Card>
                  );
                })}
              </div>
            </SampleFrame>
          </StaticSampleGroup>

          <StaticSampleGroup title="SELECTABLE INDICATORS — APPROVED / LOCKED">
            <SampleFrame label="A · RADIO / CIRCLE">
              <Card treatment="bordered" className="min-h-[5.75rem]" aria-hidden>
                <SelectableCardBody
                  title="Main Branch"
                  subtitle="Selected warehouse"
                  selected={false}
                  indicator="radio"
                />
              </Card>
            </SampleFrame>
            <SampleFrame label="B · CIRCLECHECK">
              <Card treatment="selected" selected className="min-h-[5.75rem]" aria-hidden>
                <SelectableCardBody
                  title="Main Branch"
                  subtitle="Selected warehouse"
                  selected
                  indicator="check"
                />
              </Card>
            </SampleFrame>
            <SampleFrame label="C · BORDER ONLY">
              <Card treatment="selected" selected className="min-h-[5.75rem]" aria-hidden>
                <SelectableCardBody
                  title="Main Branch"
                  subtitle="Selected warehouse"
                  selected
                  indicator="none"
                />
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

          <StaticSampleGroup title="ACCENT POSITIONS — APPROVED / LOCKED (default START)">
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
            <SampleFrame label="START · canonical default">
              <Card treatment="accent" accentTone="warning" accentPosition="start">
                <CardTitle as="h4">Start accent</CardTitle>
                <CardDescription>Inline-start — canonical default.</CardDescription>
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

          <Card treatment="bordered" className="flex h-full flex-col gap-3">
            <div className="flex min-w-0 items-start gap-2.5">
              <EntityAvatar initials="KF" />
              <div className="min-w-0 flex-1">
                <CardTitle>Kizy Fruits</CardTitle>
                <CardDescription>Main Branch</CardDescription>
              </div>
            </div>
            <CardContent className="flex flex-1 flex-col gap-2 pt-0">
              <div className="flex flex-wrap gap-1.5">
                <TagChip tone="info">B2B</TagChip>
                <StatusChip tone="success">Active</StatusChip>
              </div>
            </CardContent>
            <CardFooter className="mt-auto justify-end border-t-0 pt-0">
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

          <Card treatment="bordered" className="flex h-full flex-col gap-3">
            <div className="flex min-w-0 items-start gap-2.5">
              <span
                aria-hidden
                className="flex size-9 shrink-0 items-center justify-center rounded-[var(--exits-radius-sm)] bg-[var(--exits-surface-muted)] text-muted"
              >
                <Warehouse className="size-4" aria-hidden />
              </span>
              <div className="min-w-0 flex-1">
                <CardTitle>Main Warehouse</CardTitle>
                <CardDescription>124 products</CardDescription>
              </div>
            </div>
            <CardContent className="pt-0">
              <div className="flex flex-wrap gap-1.5">
                <StatusChip tone="success">Active</StatusChip>
                <TagChip tone="primary">Preferred</TagChip>
              </div>
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
        id="cards.motion"
        title={t("uiStandards.cardsMotionTitle")}
        description="Hover micro-interactions use transform/opacity only — no layout shift. APPROVED / LOCKED."
        summary="STATIC · LIFT · EXPAND · ACCENT"
        open={isOpen("cards.motion")}
        onOpenChange={(open) => setOpen("cards.motion", open)}
        testId="ui-standards-cards-motion"
      >
        <div className="grid gap-3">
          <StaticSampleGroup title="MOTION RECOMMENDATIONS — APPROVED / LOCKED">
            <SampleFrame label="APPROVED / LOCKED" className="sm:col-span-2 lg:col-span-3">
              <div className="grid gap-1 text-[length:var(--exits-text-xs)] text-muted">
                <div>NORMAL INFORMATION CARD → STATIC</div>
                <div>CLICKABLE ENTITY → LIFT</div>
                <div>PROMINENT CLICKABLE CARD → EXPAND</div>
                <div>FEATURED / RECOMMENDED → ACCENT + EXPAND</div>
                <div>PRODUCT / MEDIA → MEDIA ZOOM · optional LIFT</div>
                <div>SELECTABLE → selection transition only (no strong expand)</div>
                <div>KPI → STATIC · LIFT only if clickable</div>
              </div>
            </SampleFrame>
          </StaticSampleGroup>

          <StaticSampleGroup title="CARD MOTION — HOVER TO COMPARE">
            <SampleFrame label="STATIC" className="sm:col-span-2 lg:col-span-3">
              <div className="grid gap-2 [grid-template-columns:repeat(auto-fit,minmax(min(100%,12rem),1fr))]">
                {(
                  [
                    { label: "STATIC", motion: "none" as const, extraLift: false },
                    { label: "LIFT", motion: "lift" as const, extraLift: false },
                    { label: "EXPAND", motion: "expand" as const, extraLift: false },
                    { label: "ACCENT", motion: "accent" as const, extraLift: false },
                    { label: "LIFT + ACCENT", motion: "accent" as const, extraLift: true },
                  ] as const
                ).map((item) => (
                  <Card
                    key={item.label}
                    treatment="bordered"
                    interactive
                    motion={item.extraLift ? "lift" : item.motion}
                    className={cn(
                      item.extraLift &&
                        "hover:border-[var(--exits-primary)] hover:shadow-[0_0_0_1px_color-mix(in_srgb,var(--exits-primary)_22%,transparent),var(--exits-shadow-md)]",
                    )}
                    data-testid={
                      item.motion === "expand"
                        ? "ui-standards-card-motion-expand"
                        : item.motion === "lift" && !item.extraLift
                          ? "ui-standards-card-motion-lift"
                          : undefined
                    }
                  >
                    <CardTitle as="h4">Kizy Fruits</CardTitle>
                    <CardDescription>Main Branch · Iloilo</CardDescription>
                    <div className="mt-2 flex flex-wrap gap-1.5">
                      <TagChip tone="info">B2B</TagChip>
                      <StatusChip tone="success">Active</StatusChip>
                    </div>
                    <p className="m-0 mt-2 text-[length:var(--exits-text-xs)] text-muted">
                      {item.label}
                    </p>
                  </Card>
                ))}
              </div>
            </SampleFrame>
          </StaticSampleGroup>

          <StaticSampleGroup title="EXPAND INTENSITY — APPROVED / LOCKED (default STANDARD ~1.02)">
            {(
              [
                { label: "SUBTLE · ~1.01", scale: "subtle" as const },
                { label: "STANDARD · ~1.02", scale: "standard" as const },
                { label: "STRONG · ~1.03", scale: "strong" as const },
              ] as const
            ).map((item) => (
              <SampleFrame key={item.scale} label={item.label}>
                <Card
                  treatment="bordered"
                  interactive
                  motion="expand"
                  expandScale={item.scale}
                  data-testid={`ui-standards-card-expand-${item.scale}`}
                >
                  <CardTitle as="h4">Warehouse</CardTitle>
                  <CardDescription>Hover to compare expand intensity.</CardDescription>
                </Card>
              </SampleFrame>
            ))}
          </StaticSampleGroup>

          <StaticSampleGroup title="ENTITY · LIFT VS EXPAND">
            <SampleFrame label="LIFT">
              <Card treatment="bordered" interactive motion="lift">
                <div className="flex min-w-0 items-start gap-2.5">
                  <EntityAvatar initials="KF" />
                  <div className="min-w-0">
                    <CardTitle as="h4">Kizy Fruits</CardTitle>
                    <CardDescription>Main Branch</CardDescription>
                  </div>
                </div>
                <div className="mt-2 flex flex-wrap gap-1.5">
                  <TagChip tone="info">B2B</TagChip>
                  <StatusChip tone="success">Active</StatusChip>
                </div>
              </Card>
            </SampleFrame>
            <SampleFrame label="EXPAND">
              <Card treatment="bordered" interactive motion="expand">
                <div className="flex min-w-0 items-start gap-2.5">
                  <EntityAvatar initials="KF" />
                  <div className="min-w-0">
                    <CardTitle as="h4">Kizy Fruits</CardTitle>
                    <CardDescription>Main Branch</CardDescription>
                  </div>
                </div>
                <div className="mt-2 flex flex-wrap gap-1.5">
                  <TagChip tone="info">B2B</TagChip>
                  <StatusChip tone="success">Active</StatusChip>
                </div>
              </Card>
            </SampleFrame>
          </StaticSampleGroup>
        </div>
      </UiStandardsSection>

      <UiStandardsSection
        id="cards.featured-effects"
        title={t("uiStandards.cardsFeaturedEffectsTitle")}
        description="Special-use treatments — not default business cards. SPECIAL USE · APPROVED / LOCKED."
        summary="FEATURED · MEDIA ZOOM · PRICING · REVEAL"
        open={isOpen("cards.featured-effects")}
        onOpenChange={(open) => setOpen("cards.featured-effects", open)}
        testId="ui-standards-cards-featured-effects"
      >
        <div className="grid gap-3">
          <StaticSampleGroup title="PRICING / PLAN CARD — SPECIAL USE">
            <SampleFrame label="PLANS" className="sm:col-span-2 lg:col-span-3">
              <div
                className="grid items-stretch gap-3 [grid-template-columns:repeat(auto-fit,minmax(min(100%,14rem),1fr))]"
                data-testid="ui-standards-card-pricing"
              >
                <Card treatment="bordered" motion="lift" className="h-full">
                  <CardTitle as="h4">Starter</CardTitle>
                  <CardDescription>For a single branch.</CardDescription>
                  <p className="m-0 mt-2 text-[length:var(--exits-text-xl)] font-semibold tabular-nums">
                    {formatPeso(499)}
                    <span className="text-[length:var(--exits-text-sm)] font-normal text-muted">
                      {" "}
                      / month
                    </span>
                  </p>
                  <ul className="m-0 mt-3 list-none space-y-1.5 p-0 text-[length:var(--exits-text-sm)] text-muted">
                    <li className="flex gap-2">
                      <Check className="mt-0.5 size-3.5 shrink-0 text-[var(--exits-primary)]" aria-hidden />
                      1 branch
                    </li>
                    <li className="flex gap-2">
                      <Check className="mt-0.5 size-3.5 shrink-0 text-[var(--exits-primary)]" aria-hidden />
                      Basic reports
                    </li>
                  </ul>
                  <CardFooter className="mt-4 justify-stretch border-t-0 pt-0">
                    <Button type="button" variant="outline" shape="soft" className="w-full">
                      Choose plan
                    </Button>
                  </CardFooter>
                </Card>

                <Card
                  treatment="featured"
                  motion="featured"
                  className="relative h-full"
                  data-testid="ui-standards-card-featured"
                >
                  <div className="absolute -top-2 end-3">
                    <StatusChip tone="primary">Recommended</StatusChip>
                  </div>
                  <CardTitle as="h4">Growth</CardTitle>
                  <CardDescription>Most shops start here.</CardDescription>
                  <p className="m-0 mt-2 text-[length:var(--exits-text-xl)] font-semibold tabular-nums">
                    {formatPeso(999)}
                    <span className="text-[length:var(--exits-text-sm)] font-normal text-muted">
                      {" "}
                      / month
                    </span>
                  </p>
                  <p className="m-0 text-[length:var(--exits-text-xs)] text-muted line-through">
                    {formatPeso(1299)}
                  </p>
                  <ul className="m-0 mt-3 list-none space-y-1.5 p-0 text-[length:var(--exits-text-sm)] text-muted">
                    <li className="flex gap-2">
                      <Check className="mt-0.5 size-3.5 shrink-0 text-[var(--exits-primary)]" aria-hidden />
                      Multi-branch
                    </li>
                    <li className="flex gap-2">
                      <Check className="mt-0.5 size-3.5 shrink-0 text-[var(--exits-primary)]" aria-hidden />
                      Inventory alerts
                    </li>
                    <li className="flex gap-2">
                      <Check className="mt-0.5 size-3.5 shrink-0 text-[var(--exits-primary)]" aria-hidden />
                      Supplier links
                    </li>
                  </ul>
                  <CardFooter className="mt-4 justify-stretch border-t-0 pt-0">
                    <Button type="button" variant="default" shape="soft" className="w-full">
                      Choose plan
                    </Button>
                  </CardFooter>
                </Card>

                <Card treatment="bordered" motion="lift" className="h-full">
                  <CardTitle as="h4">Pro Plus</CardTitle>
                  <CardDescription>For multi-org operations.</CardDescription>
                  <p className="m-0 mt-2 text-[length:var(--exits-text-xl)] font-semibold tabular-nums">
                    {formatPeso(1999)}
                    <span className="text-[length:var(--exits-text-sm)] font-normal text-muted">
                      {" "}
                      / month
                    </span>
                  </p>
                  <ul className="m-0 mt-3 list-none space-y-1.5 p-0 text-[length:var(--exits-text-sm)] text-muted">
                    <li className="flex gap-2">
                      <Check className="mt-0.5 size-3.5 shrink-0 text-[var(--exits-primary)]" aria-hidden />
                      Everything in Growth
                    </li>
                    <li className="flex gap-2">
                      <Check className="mt-0.5 size-3.5 shrink-0 text-[var(--exits-primary)]" aria-hidden />
                      Priority support
                    </li>
                  </ul>
                  <CardFooter className="mt-4 justify-stretch border-t-0 pt-0">
                    <Button type="button" variant="outline" shape="soft" className="w-full">
                      Choose plan
                    </Button>
                  </CardFooter>
                </Card>
              </div>
            </SampleFrame>
          </StaticSampleGroup>

          <StaticSampleGroup title="GRADIENT BORDER — SPECIAL / FEATURED USE">
            <SampleFrame label="SHOWCASE ONLY" className="sm:col-span-2 lg:col-span-3">
              <div
                className="rounded-[calc(var(--exits-radius-md)+1px)] bg-gradient-to-br from-[var(--exits-primary)] to-[var(--exits-info)] p-px"
                data-testid="ui-standards-card-gradient-border"
              >
                <Card treatment="surface" className="border-0 shadow-none">
                  <CardTitle as="h4">Featured media slot</CardTitle>
                  <CardDescription>
                    Thin Primary → Info edge only. Not a default treatment — showcase candidate.
                  </CardDescription>
                </Card>
              </div>
            </SampleFrame>
          </StaticSampleGroup>

          <StaticSampleGroup title="MEDIA ZOOM">
            <SampleFrame label="ZOOM ONLY">
              <Card treatment="bordered" interactive motion="none" data-testid="ui-standards-card-media-zoom">
                <CardMedia zoom className="aspect-[4/3] w-full">
                  <ProductImagePlaceholder color="color-mix(in srgb, var(--exits-success) 25%, var(--exits-surface-muted))" />
                </CardMedia>
                <CardTitle as="h4" className="mt-2">
                  Apple
                </CardTitle>
                <CardDescription>{formatPeso(120)} / kg</CardDescription>
              </Card>
            </SampleFrame>
            <SampleFrame label="LIFT + MEDIA ZOOM">
              <Card treatment="bordered" interactive motion="lift">
                <CardMedia zoom className="aspect-[4/3] w-full">
                  <ProductImagePlaceholder color="color-mix(in srgb, var(--exits-success) 25%, var(--exits-surface-muted))" />
                </CardMedia>
                <CardTitle as="h4" className="mt-2">
                  Apple
                </CardTitle>
                <CardDescription>{formatPeso(120)} / kg</CardDescription>
                <div className="mt-2 flex flex-wrap gap-1.5">
                  <TagChip tone="neutral">Weighted</TagChip>
                  <StatusChip tone="warning">Low stock</StatusChip>
                </div>
              </Card>
            </SampleFrame>
            <SampleFrame label="NO IMAGE · NO ZOOM">
              <Card treatment="bordered" interactive motion="lift">
                <CardMedia className="aspect-[4/3] w-full">
                  <ProductImagePlaceholder label="No product image" />
                </CardMedia>
                <CardTitle as="h4" className="mt-2">
                  Banana
                </CardTitle>
                <CardDescription>Fallback stays calm — no fake zoom.</CardDescription>
              </Card>
            </SampleFrame>
          </StaticSampleGroup>

          <StaticSampleGroup title="HOVER REVEAL — SPECIAL USE">
            <SampleFrame label="ACTIONS ON HOVER / FOCUS" className="sm:col-span-2">
              <Card
                treatment="bordered"
                motion="lift"
                reveal
                className="relative"
                data-testid="ui-standards-card-hover-reveal"
              >
                <CardTitle as="h4">Kizy Fruits</CardTitle>
                <CardDescription>Customer</CardDescription>
                <div className="mt-2 flex flex-wrap gap-1.5">
                  <TagChip tone="info">B2B</TagChip>
                  <StatusChip tone="success">Active</StatusChip>
                </div>
                <CardReveal>
                  <Button type="button" variant="outline" shape="soft">
                    View details
                  </Button>
                </CardReveal>
              </Card>
            </SampleFrame>
          </StaticSampleGroup>

          <StaticSampleGroup title="COUNTDOWN / INFO STRIP">
            <SampleFrame label="COMPACT HORIZONTAL · ACCENT" className="sm:col-span-2 lg:col-span-3">
              <Card
                treatment="accent"
                accentTone="primary"
                accentPosition="start"
                layout="horizontal"
                padding="compact"
                className="items-center"
                data-testid="ui-standards-card-countdown"
              >
                <Clock3 className="size-5 shrink-0 text-[var(--exits-primary)]" aria-hidden />
                <div className="min-w-0 flex-1">
                  <p className="m-0 text-[length:var(--exits-text-xs)] font-medium uppercase tracking-wide text-muted">
                    Promo ends in
                  </p>
                  <div className="mt-1 flex flex-wrap gap-3 text-[length:var(--exits-text-sm)] font-semibold tabular-nums">
                    <span>2 Days</span>
                    <span>04 Hours</span>
                    <span>18 Minutes</span>
                  </div>
                </div>
              </Card>
            </SampleFrame>
          </StaticSampleGroup>
        </div>
      </UiStandardsSection>

      <UiStandardsSection
        id="cards.cheatsheet"
        title={t("uiStandards.cardsCheatTitle")}
        description={t("uiStandards.cardsCheatLede")}
        summary="APPROVED · LOCKED"
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
APPROVED / LOCKED

TYPES
  BASIC · SUMMARY · KPI · ACTION · ENTITY · PRODUCT · SELECTABLE · STATUS · COMPACT · FEATURED

TREATMENTS
  SURFACE · BORDERED · ELEVATED · INTERACTIVE · SELECTED · ACCENT · FEATURED

MOTION
  STATIC · LIFT · EXPAND · ACCENT · MEDIA ZOOM · HOVER REVEAL

EXPAND SCALE (LOCKED DEFAULT STANDARD)
  SUBTLE ~1.01 · STANDARD ~1.02 · STRONG ~1.03

LAYOUT
  VERTICAL · HORIZONTAL

OPTIONS
  WITH ICON · WITH CHIP · WITH COUNT · WITH IMAGE · WITH FOOTER · WITH ACTIONS

EXAMPLES
  Customer → ENTITY CARD + INTERACTIVE + LIFT
  Featured customer → ENTITY CARD + ACCENT + EXPAND
  Product → PRODUCT CARD + MEDIA ZOOM
  Interactive product → PRODUCT CARD + LIFT + MEDIA ZOOM
  Quick action → ACTION CARD + EXPAND
  Recommended plan → FEATURED CARD + ACCENT + EXPAND
  Selectable warehouse → SELECTABLE CARD (no strong expand)
  Promo strip → COMPACT CARD + ACCENT

MOTION DEFAULTS (APPROVED / LOCKED)
  NORMAL INFORMATION → STATIC
  CLICKABLE ENTITY → LIFT
  PROMINENT CLICKABLE → EXPAND
  FEATURED / RECOMMENDED → ACCENT + EXPAND
  PRODUCT / MEDIA → MEDIA ZOOM · optional LIFT
  SELECTABLE → selection only
  KPI → STATIC · LIFT if clickable

BOUNDARY
  Cards own presentation / motion
  Pages own routes / APIs / permissions

STATUS
  APPROVED / LOCKED — Docs/UI/exits-card-standard.md`}</pre>
        </div>
      </UiStandardsSection>
    </div>
  );
}
