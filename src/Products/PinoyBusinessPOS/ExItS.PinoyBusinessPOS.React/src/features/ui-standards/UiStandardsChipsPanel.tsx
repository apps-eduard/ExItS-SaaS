import { useMemo, useState, type ReactNode } from "react";
import {
  Ban,
  Building2,
  CheckCircle2,
  CircleAlert,
  CircleCheck,
  CircleX,
  Clock3,
  MapPin,
  Pause,
  Scale,
  Star,
  TriangleAlert,
  Warehouse,
} from "lucide-react";
import { Button } from "@/components/ui/button";
import { CountBadge, CountChip } from "@/components/exits/CountChip";
import { FilterChip } from "@/components/exits/FilterChip";
import { RemovableChip } from "@/components/exits/RemovableChip";
import { StatusChip } from "@/components/exits/StatusChip";
import { TagChip } from "@/components/exits/TagChip";
import { useI18n } from "@/i18n/I18nProvider";
import { UiStandardsSection } from "@/features/ui-standards/UiStandardsSection";

function SampleCard({
  label,
  children,
  testId,
  hint,
}: {
  label: string;
  children: ReactNode;
  testId?: string;
  hint?: string;
}) {
  return (
    <div
      className="flex flex-col gap-1.5 rounded-[var(--exits-radius-md)] border border-border bg-[var(--exits-surface-muted)]/40 p-2"
      data-testid={testId}
    >
      <span className="text-[length:var(--exits-text-xs)] uppercase tracking-wide text-muted">{label}</span>
      <div className="flex flex-wrap items-center justify-start gap-1.5">{children}</div>
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
      data-testid={`ui-standards-chip-group-${slug}`}
    >
      <h3 className="m-0 text-[length:var(--exits-text-sm)] font-semibold text-muted">{title}</h3>
      <div className="grid gap-2 sm:grid-cols-2 lg:grid-cols-3">{children}</div>
    </section>
  );
}

const FILTER_KEYS = ["all", "active", "inactive", "lowStock", "needsAttention"] as const;
type FilterKey = (typeof FILTER_KEYS)[number];

const FILTER_LABELS: Record<FilterKey, string> = {
  all: "All",
  active: "Active",
  inactive: "Inactive",
  lowStock: "Low stock",
  needsAttention: "Needs attention",
};

const MULTI_KEYS = ["active", "pending", "inactive"] as const;
type MultiKey = (typeof MULTI_KEYS)[number];

const MULTI_LABELS: Record<MultiKey, string> = {
  active: "Active",
  pending: "Pending",
  inactive: "Inactive",
};

const DEFAULT_REMOVABLE = [
  { id: "branch", label: "Branch: Main", removeLabel: "Remove Branch: Main filter" },
  { id: "category", label: "Category: Fruits", removeLabel: "Remove Category: Fruits filter" },
  { id: "supplier", label: "Supplier: Mica", removeLabel: "Remove Supplier: Mica filter" },
] as const;

type DisclosureProps = {
  isOpen: (id: string) => boolean;
  setOpen: (id: string, open: boolean) => void;
};

export function UiStandardsChipsPanel({ isOpen, setOpen }: DisclosureProps) {
  const { t } = useI18n();
  const [singleFilter, setSingleFilter] = useState<FilterKey>("all");
  const [multiFilters, setMultiFilters] = useState<Set<MultiKey>>(
    () => new Set(["active", "pending"]),
  );
  const [productFilter, setProductFilter] = useState<FilterKey>("all");
  const [removable, setRemovable] = useState(() => [...DEFAULT_REMOVABLE]);
  const [activeFilters, setActiveFilters] = useState(() =>
    DEFAULT_REMOVABLE.filter((x) => x.id === "branch" || x.id === "category"),
  );

  const longTitle = useMemo(() => "Connected supplier relationship", []);

  function toggleMulti(key: MultiKey) {
    setMultiFilters((prev) => {
      const next = new Set(prev);
      if (next.has(key)) next.delete(key);
      else next.add(key);
      return next;
    });
  }

  return (
    <div className="grid gap-3" data-testid="ui-standards-chips-section">
      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{t("uiStandards.chipPilotLede")}</p>

      <UiStandardsSection
        id="chips.shapes"
        title={t("uiStandards.chipShapesTitle")}
        description="Same labels across PILL · SOFT · SQUARE. Shape is independent from tone and family."
        summary="PILL · SOFT · SQUARE"
        open={isOpen("chips.shapes")}
        onOpenChange={(open) => setOpen("chips.shapes", open)}
        testId="ui-standards-chips-shapes"
      >
        <div className="grid gap-3">
          <StaticSampleGroup title="STATUS / PUBLISHED">
            <SampleCard label="PILL">
              <StatusChip tone="success" shape="pill">
                Published
              </StatusChip>
            </SampleCard>
            <SampleCard label="SOFT">
              <StatusChip tone="success" shape="soft">
                Published
              </StatusChip>
            </SampleCard>
            <SampleCard label="SQUARE">
              <StatusChip tone="success" shape="square">
                Published
              </StatusChip>
            </SampleCard>
          </StaticSampleGroup>

          <StaticSampleGroup title="TAG / BETA">
            <SampleCard label="PILL">
              <TagChip tone="info" shape="pill">
                Beta
              </TagChip>
            </SampleCard>
            <SampleCard label="SOFT">
              <TagChip tone="info" shape="soft">
                Beta
              </TagChip>
            </SampleCard>
            <SampleCard label="SQUARE">
              <TagChip tone="info" shape="square">
                Beta
              </TagChip>
            </SampleCard>
          </StaticSampleGroup>

          <StaticSampleGroup title="B2B">
            <SampleCard label="PILL">
              <TagChip tone="info" shape="pill">
                B2B
              </TagChip>
            </SampleCard>
            <SampleCard label="SOFT">
              <TagChip tone="info" shape="soft">
                B2B
              </TagChip>
            </SampleCard>
            <SampleCard label="SQUARE">
              <TagChip tone="info" shape="square">
                B2B
              </TagChip>
            </SampleCard>
          </StaticSampleGroup>

          <StaticSampleGroup title="LOCKED DEFAULT SHAPES">
            <SampleCard label="APPROVED / LOCKED" hint="Explicit shape overrides remain allowed">
              <div className="grid w-full gap-1 text-[length:var(--exits-text-xs)] text-muted">
                <div>STATUS CHIP → PILL</div>
                <div>FILTER CHIP → PILL</div>
                <div>TAG CHIP → SQUARE</div>
                <div>REMOVABLE CHIP → PILL</div>
                <div>COUNT CHIP → SOFT</div>
                <div>COUNT BADGE → PILL / ROUND</div>
              </div>
            </SampleCard>
          </StaticSampleGroup>
        </div>
      </UiStandardsSection>

      <UiStandardsSection
        id="chips.compact-tags"
        title={t("uiStandards.chipCompactTagsTitle")}
        description="Square compact metadata labels — tiny padding, ~4px radius, read-only. Tag default shape."
        summary="SQUARE · locked default"
        open={isOpen("chips.compact-tags")}
        onOpenChange={(open) => setOpen("chips.compact-tags", open)}
        testId="ui-standards-chips-compact-tags"
      >
        <div className="grid gap-3">
          <StaticSampleGroup title="COMPACT TAGS">
            <SampleCard label="Row">
              <div className="flex flex-wrap gap-1">
                <TagChip tone="info" shape="square">
                  Beta
                </TagChip>
                <TagChip tone="primary" shape="square">
                  New
                </TagChip>
                <TagChip tone="info" shape="square">
                  B2B
                </TagChip>
                <TagChip tone="neutral" shape="square">
                  Weighted
                </TagChip>
                <TagChip tone="neutral" shape="square">
                  SKU
                </TagChip>
                <TagChip tone="neutral" shape="square">
                  PO
                </TagChip>
                <TagChip tone="neutral" shape="square">
                  Direct
                </TagChip>
                <TagChip tone="success" shape="square">
                  Synced
                </TagChip>
              </div>
            </SampleCard>
          </StaticSampleGroup>

          <StaticSampleGroup title="SEMANTIC TONES">
            <SampleCard label="NEUTRAL">
              <TagChip tone="neutral" shape="square">
                Draft
              </TagChip>
            </SampleCard>
            <SampleCard label="PRIMARY">
              <TagChip tone="primary" shape="square">
                Preferred
              </TagChip>
            </SampleCard>
            <SampleCard label="INFO">
              <TagChip tone="info" shape="square">
                B2B
              </TagChip>
            </SampleCard>
            <SampleCard label="SUCCESS">
              <TagChip tone="success" shape="square">
                Synced
              </TagChip>
            </SampleCard>
            <SampleCard label="WARNING">
              <TagChip tone="warning" shape="square">
                Review
              </TagChip>
            </SampleCard>
            <SampleCard label="DANGER">
              <TagChip tone="danger" shape="square">
                Failed
              </TagChip>
            </SampleCard>
          </StaticSampleGroup>

          <StaticSampleGroup title="ICONS (optional)">
            <SampleCard label="No icon">
              <TagChip tone="info" shape="square">
                Beta
              </TagChip>
            </SampleCard>
            <SampleCard label="Synced">
              <TagChip tone="success" shape="square" icon={<CheckCircle2 aria-hidden />}>
                Synced
              </TagChip>
            </SampleCard>
            <SampleCard label="Review">
              <TagChip tone="warning" shape="square" icon={<TriangleAlert aria-hidden />}>
                Review
              </TagChip>
            </SampleCard>
          </StaticSampleGroup>
        </div>
      </UiStandardsSection>

      <UiStandardsSection
        id="chips.status"
        title={t("uiStandards.chipStatusTitle")}
        description="Read-only business state. Soft semantic surfaces, pill shape, no pointer/press."
        summary="STATUS · tones"
        open={isOpen("chips.status")}
        onOpenChange={(open) => setOpen("chips.status", open)}
        testId="ui-standards-chips-status"
      >
        <div className="grid gap-3">
          <StaticSampleGroup title="NEUTRAL">
            <SampleCard label="Draft">
              <StatusChip tone="neutral">Draft</StatusChip>
            </SampleCard>
            <SampleCard label="Inactive">
              <StatusChip tone="neutral">Inactive</StatusChip>
            </SampleCard>
            <SampleCard label="Not configured">
              <StatusChip tone="neutral">Not configured</StatusChip>
            </SampleCard>
          </StaticSampleGroup>

          <StaticSampleGroup title="INFO">
            <SampleCard label="B2B">
              <StatusChip tone="info">B2B</StatusChip>
            </SampleCard>
            <SampleCard label="In progress">
              <StatusChip tone="info">In progress</StatusChip>
            </SampleCard>
            <SampleCard label="Linked">
              <StatusChip tone="info">Linked</StatusChip>
            </SampleCard>
          </StaticSampleGroup>

          <StaticSampleGroup title="SUCCESS">
            <SampleCard label="Active">
              <StatusChip tone="success">Active</StatusChip>
            </SampleCard>
            <SampleCard label="Approved">
              <StatusChip tone="success">Approved</StatusChip>
            </SampleCard>
            <SampleCard label="Paid">
              <StatusChip tone="success">Paid</StatusChip>
            </SampleCard>
            <SampleCard label="Completed">
              <StatusChip tone="success">Completed</StatusChip>
            </SampleCard>
          </StaticSampleGroup>

          <StaticSampleGroup title="WARNING">
            <SampleCard label="Pending">
              <StatusChip tone="warning">Pending</StatusChip>
            </SampleCard>
            <SampleCard label="Low stock">
              <StatusChip tone="warning">Low stock</StatusChip>
            </SampleCard>
            <SampleCard label="Paused">
              <StatusChip tone="warning">Paused</StatusChip>
            </SampleCard>
            <SampleCard label="Due soon">
              <StatusChip tone="warning">Due soon</StatusChip>
            </SampleCard>
          </StaticSampleGroup>

          <StaticSampleGroup title="DANGER">
            <SampleCard label="Overdue">
              <StatusChip tone="danger">Overdue</StatusChip>
            </SampleCard>
            <SampleCard label="Declined">
              <StatusChip tone="danger">Declined</StatusChip>
            </SampleCard>
            <SampleCard label="Failed">
              <StatusChip tone="danger">Failed</StatusChip>
            </SampleCard>
            <SampleCard label="Void">
              <StatusChip tone="danger">Void</StatusChip>
            </SampleCard>
          </StaticSampleGroup>

          <StaticSampleGroup title="PRIMARY (brand)">
            <SampleCard label="Preferred" hint="Uses --exits-primary">
              <StatusChip tone="primary">Preferred</StatusChip>
            </SampleCard>
          </StaticSampleGroup>

          <StaticSampleGroup title="LONG LABEL">
            <SampleCard label="Truncation candidate" hint="title on wrapper when truncated">
              <span title={longTitle} className="inline-flex max-w-[12rem]">
                <StatusChip tone="info" className="max-w-full overflow-hidden text-ellipsis">
                  {longTitle}
                </StatusChip>
              </span>
            </SampleCard>
          </StaticSampleGroup>
        </div>
      </UiStandardsSection>

      <UiStandardsSection
        id="chips.status-icons"
        title={t("uiStandards.chipStatusIconsTitle")}
        description="Optional Lucide icons — smaller than Button icons. Not required on every status."
        summary="ICON · compare"
        open={isOpen("chips.status-icons")}
        onOpenChange={(open) => setOpen("chips.status-icons", open)}
        testId="ui-standards-chips-status-icons"
      >
        <div className="grid gap-3">
          <StaticSampleGroup title="TEXT ONLY">
            <SampleCard label="Approved">
              <StatusChip tone="success">Approved</StatusChip>
            </SampleCard>
            <SampleCard label="Active">
              <StatusChip tone="success">Active</StatusChip>
            </SampleCard>
            <SampleCard label="Paid">
              <StatusChip tone="success">Paid</StatusChip>
            </SampleCard>
            <SampleCard label="Pending">
              <StatusChip tone="warning">Pending</StatusChip>
            </SampleCard>
            <SampleCard label="Low stock">
              <StatusChip tone="warning">Low stock</StatusChip>
            </SampleCard>
            <SampleCard label="Paused">
              <StatusChip tone="warning">Paused</StatusChip>
            </SampleCard>
            <SampleCard label="Overdue">
              <StatusChip tone="danger">Overdue</StatusChip>
            </SampleCard>
            <SampleCard label="Failed">
              <StatusChip tone="danger">Failed</StatusChip>
            </SampleCard>
            <SampleCard label="Declined">
              <StatusChip tone="danger">Declined</StatusChip>
            </SampleCard>
            <SampleCard label="Void">
              <StatusChip tone="danger">Void</StatusChip>
            </SampleCard>
          </StaticSampleGroup>

          <StaticSampleGroup title="WITH ICON">
            <SampleCard label="Approved">
              <StatusChip tone="success" icon={<CheckCircle2 aria-hidden />}>
                Approved
              </StatusChip>
            </SampleCard>
            <SampleCard label="Active">
              <StatusChip tone="success" icon={<CheckCircle2 aria-hidden />}>
                Active
              </StatusChip>
            </SampleCard>
            <SampleCard label="Paid">
              <StatusChip tone="success" icon={<CircleCheck aria-hidden />}>
                Paid
              </StatusChip>
            </SampleCard>
            <SampleCard label="Pending">
              <StatusChip tone="warning" icon={<Clock3 aria-hidden />}>
                Pending
              </StatusChip>
            </SampleCard>
            <SampleCard label="Low stock">
              <StatusChip tone="warning" icon={<TriangleAlert aria-hidden />}>
                Low stock
              </StatusChip>
            </SampleCard>
            <SampleCard label="Paused">
              <StatusChip tone="warning" icon={<Pause aria-hidden />}>
                Paused
              </StatusChip>
            </SampleCard>
            <SampleCard label="Overdue">
              <StatusChip tone="danger" icon={<CircleAlert aria-hidden />}>
                Overdue
              </StatusChip>
            </SampleCard>
            <SampleCard label="Failed">
              <StatusChip tone="danger" icon={<CircleX aria-hidden />}>
                Failed
              </StatusChip>
            </SampleCard>
            <SampleCard label="Declined">
              <StatusChip tone="danger" icon={<Ban aria-hidden />}>
                Declined
              </StatusChip>
            </SampleCard>
            <SampleCard label="Void">
              <StatusChip tone="danger" icon={<Ban aria-hidden />}>
                Void
              </StatusChip>
            </SampleCard>
          </StaticSampleGroup>
        </div>
      </UiStandardsSection>

      <UiStandardsSection
        id="chips.filter"
        title={t("uiStandards.chipFilterTitle")}
        description="Interactive filtering. Selected uses PRIMARY tokens — not a full action Button."
        summary="FILTER · select"
        open={isOpen("chips.filter")}
        onOpenChange={(open) => setOpen("chips.filter", open)}
        testId="ui-standards-chips-filter"
      >
        <div className="grid gap-3">
          <StaticSampleGroup title="INTERACTIVE SAMPLE">
            <SampleCard label="Click to select" hint={`Selected: ${FILTER_LABELS[singleFilter]}`}>
              <div className="flex flex-wrap gap-1.5" role="group" aria-label="Filter sample">
                {FILTER_KEYS.map((key) => (
                  <FilterChip
                    key={key}
                    selected={singleFilter === key}
                    onClick={() => setSingleFilter(key)}
                  >
                    {FILTER_LABELS[key]}
                  </FilterChip>
                ))}
              </div>
            </SampleCard>
          </StaticSampleGroup>

          <StaticSampleGroup title="STATES">
            <SampleCard label="Unselected">
              <FilterChip selected={false}>Inactive</FilterChip>
            </SampleCard>
            <SampleCard label="Selected">
              <FilterChip selected>Active</FilterChip>
            </SampleCard>
            <SampleCard label="Disabled">
              <FilterChip disabled>Needs attention</FilterChip>
            </SampleCard>
            <SampleCard label="Disabled selected">
              <FilterChip selected disabled>
                All
              </FilterChip>
            </SampleCard>
          </StaticSampleGroup>
        </div>
      </UiStandardsSection>

      <UiStandardsSection
        id="chips.filter-modes"
        title={t("uiStandards.chipFilterModesTitle")}
        description="Same FilterChip family — single vs multi selection patterns."
        summary="SINGLE · MULTI"
        open={isOpen("chips.filter-modes")}
        onOpenChange={(open) => setOpen("chips.filter-modes", open)}
        testId="ui-standards-chips-filter-modes"
      >
        <div className="grid gap-3">
          <StaticSampleGroup title="SINGLE SELECT">
            <SampleCard label="One selected">
              <div className="flex flex-wrap gap-1.5" role="group" aria-label="Single select filters">
                {(["all", "active", "inactive"] as const).map((key) => (
                  <FilterChip
                    key={key}
                    selected={singleFilter === key}
                    onClick={() => setSingleFilter(key)}
                  >
                    {FILTER_LABELS[key]}
                  </FilterChip>
                ))}
              </div>
            </SampleCard>
          </StaticSampleGroup>

          <StaticSampleGroup title="MULTI SELECT">
            <SampleCard label="Checks when selected" hint="Check only where it clarifies multi-select">
              <div className="flex flex-wrap gap-1.5" role="group" aria-label="Multi select filters">
                {MULTI_KEYS.map((key) => (
                  <FilterChip
                    key={key}
                    selected={multiFilters.has(key)}
                    showCheck
                    onClick={() => toggleMulti(key)}
                  >
                    {MULTI_LABELS[key]}
                  </FilterChip>
                ))}
              </div>
            </SampleCard>
          </StaticSampleGroup>
        </div>
      </UiStandardsSection>

      <UiStandardsSection
        id="chips.tags"
        title={t("uiStandards.chipTagsTitle")}
        description="Descriptive attributes — restrained tones, not automatic status coloring."
        summary="TAG · attributes"
        open={isOpen("chips.tags")}
        onOpenChange={(open) => setOpen("chips.tags", open)}
        testId="ui-standards-chips-tags"
      >
        <div className="grid gap-3">
          <StaticSampleGroup title="TAGS">
            <SampleCard label="B2B">
              <TagChip tone="info">B2B</TagChip>
            </SampleCard>
            <SampleCard label="Weighted">
              <TagChip tone="neutral">Weighted</TagChip>
            </SampleCard>
            <SampleCard label="Tracked">
              <TagChip tone="info">Tracked</TagChip>
            </SampleCard>
            <SampleCard label="Preferred">
              <TagChip tone="primary">Preferred</TagChip>
            </SampleCard>
            <SampleCard label="Warehouse">
              <TagChip tone="neutral">Warehouse</TagChip>
            </SampleCard>
            <SampleCard label="Main branch">
              <TagChip tone="neutral">Main branch</TagChip>
            </SampleCard>
            <SampleCard label="Organization">
              <TagChip tone="info">Organization</TagChip>
            </SampleCard>
            <SampleCard label="Personal">
              <TagChip tone="neutral">Personal</TagChip>
            </SampleCard>
          </StaticSampleGroup>

          <StaticSampleGroup title="WITH ICON (optional)">
            <SampleCard label="B2B">
              <TagChip tone="info" icon={<Building2 aria-hidden />}>
                B2B
              </TagChip>
            </SampleCard>
            <SampleCard label="Warehouse">
              <TagChip tone="neutral" icon={<Warehouse aria-hidden />}>
                Warehouse
              </TagChip>
            </SampleCard>
            <SampleCard label="Main branch">
              <TagChip tone="neutral" icon={<MapPin aria-hidden />}>
                Main branch
              </TagChip>
            </SampleCard>
            <SampleCard label="Weighted">
              <TagChip tone="neutral" icon={<Scale aria-hidden />}>
                Weighted
              </TagChip>
            </SampleCard>
            <SampleCard label="Preferred">
              <TagChip tone="primary" icon={<Star aria-hidden />}>
                Preferred
              </TagChip>
            </SampleCard>
          </StaticSampleGroup>
        </div>
      </UiStandardsSection>

      <UiStandardsSection
        id="chips.removable"
        title={t("uiStandards.chipRemovableTitle")}
        description="Active filters / selected values with quiet trailing X."
        summary="REMOVE · demo"
        open={isOpen("chips.removable")}
        onOpenChange={(open) => setOpen("chips.removable", open)}
        testId="ui-standards-chips-removable"
      >
        <div className="grid gap-3">
          <StaticSampleGroup title="DEMO">
            <SampleCard label="Click × to remove">
              <div className="flex flex-wrap items-center gap-1.5">
                {removable.length === 0 ? (
                  <span className="text-[length:var(--exits-text-sm)] text-muted">No chips — reset sample</span>
                ) : (
                  removable.map((item) => (
                    <RemovableChip
                      key={item.id}
                      removeLabel={item.removeLabel}
                      onRemove={() =>
                        setRemovable((prev) => prev.filter((x) => x.id !== item.id))
                      }
                    >
                      {item.label}
                    </RemovableChip>
                  ))
                )}
              </div>
            </SampleCard>
            <SampleCard label="Reset">
              <Button type="button" variant="secondary" shape="soft" onClick={() => setRemovable([...DEFAULT_REMOVABLE])}>
                Reset sample
              </Button>
            </SampleCard>
          </StaticSampleGroup>
        </div>
      </UiStandardsSection>

      <UiStandardsSection
        id="chips.count"
        title={t("uiStandards.chipCountTitle")}
        description="Integrated label + count is canonical. Tabular nums for counts. Default shape: SOFT."
        summary="COUNT · SOFT"
        open={isOpen("chips.count")}
        onOpenChange={(open) => setOpen("chips.count", open)}
        testId="ui-standards-chips-count"
      >
        <div className="grid gap-3">
          <StaticSampleGroup title="CANONICAL — INLINE [ Pending 3 ]">
            <SampleCard label="NEUTRAL">
              <CountChip layout="inline" tone="neutral" label="Orders" count={24} />
            </SampleCard>
            <SampleCard label="INFO">
              <CountChip layout="inline" tone="info" label="Pending" count={3} />
            </SampleCard>
            <SampleCard label="SUCCESS">
              <CountChip layout="inline" tone="success" label="Ready" count={8} />
            </SampleCard>
            <SampleCard label="WARNING">
              <CountChip layout="inline" tone="warning" label="Low stock" count={12} />
            </SampleCard>
            <SampleCard label="DANGER">
              <CountChip layout="inline" tone="danger" label="Overdue" count={5} />
            </SampleCard>
          </StaticSampleGroup>

          <StaticSampleGroup title="REFERENCE ONLY — SPLIT Pending [ 3 ]">
            <SampleCard label="NEUTRAL">
              <CountChip layout="split" tone="neutral" label="Orders" count={24} />
            </SampleCard>
            <SampleCard label="INFO">
              <CountChip layout="split" tone="info" label="Pending" count={3} />
            </SampleCard>
            <SampleCard label="SUCCESS">
              <CountChip layout="split" tone="success" label="Ready" count={8} />
            </SampleCard>
            <SampleCard label="WARNING">
              <CountChip layout="split" tone="warning" label="Low stock" count={12} />
            </SampleCard>
            <SampleCard label="DANGER">
              <CountChip layout="split" tone="danger" label="Overdue" count={5} />
            </SampleCard>
          </StaticSampleGroup>
        </div>
      </UiStandardsSection>

      <UiStandardsSection
        id="chips.count-badge"
        title={t("uiStandards.chipCountBadgeTitle")}
        description="Tiny count-only indicator for menus, notifications, and tabs. Separate from CountChip."
        summary="BADGE · PILL"
        open={isOpen("chips.count-badge")}
        onOpenChange={(open) => setOpen("chips.count-badge", open)}
        testId="ui-standards-chips-count-badge"
      >
        <StaticSampleGroup title="COUNT BADGE">
          <SampleCard label="Neutral 3">
            <CountBadge tone="neutral" count={3} />
          </SampleCard>
          <SampleCard label="Primary 12">
            <CountBadge tone="primary" count={12} />
          </SampleCard>
          <SampleCard label="Danger 99+">
            <CountBadge tone="danger" count="99+" />
          </SampleCard>
        </StaticSampleGroup>
      </UiStandardsSection>

      <UiStandardsSection
        id="chips.real-world"
        title={t("uiStandards.chipRealWorldTitle")}
        description="Compact context samples — not fake application screens."
        summary="CONTEXT · examples"
        open={isOpen("chips.real-world")}
        onOpenChange={(open) => setOpen("chips.real-world", open)}
        testId="ui-standards-chips-real-world"
      >
        <div className="grid gap-3">
          <StaticSampleGroup title="ENTITY CARDS">
            <SampleCard label="CUSTOMER · Kizy Fruits">
              <TagChip tone="info">B2B</TagChip>
              <StatusChip tone="success">Active</StatusChip>
              <StatusChip tone="success">Approved credit</StatusChip>
            </SampleCard>
            <SampleCard label="PRODUCT · Apple">
              <TagChip tone="neutral">Weighted</TagChip>
              <TagChip tone="info">Tracked</TagChip>
              <StatusChip tone="warning">Low stock</StatusChip>
            </SampleCard>
            <SampleCard label="WAREHOUSE · Main Warehouse">
              <TagChip tone="neutral">Warehouse</TagChip>
              <StatusChip tone="success">Active</StatusChip>
              <TagChip tone="primary">Preferred</TagChip>
            </SampleCard>
            <SampleCard label="ORDER · PO-20260911-000001">
              <StatusChip tone="warning">Pending</StatusChip>
              <TagChip tone="info">B2B</TagChip>
            </SampleCard>
          </StaticSampleGroup>

          <StaticSampleGroup title="PRODUCT FILTERS">
            <SampleCard label="Filter bar">
              <div className="flex flex-wrap gap-1.5" role="group" aria-label="Product filters">
                {(["all", "active", "lowStock", "needsAttention"] as const).map((key) => (
                  <FilterChip
                    key={key}
                    selected={productFilter === key}
                    onClick={() => setProductFilter(key)}
                  >
                    {FILTER_LABELS[key]}
                  </FilterChip>
                ))}
              </div>
            </SampleCard>
            <SampleCard label="ACTIVE FILTERS">
              <div className="flex flex-wrap gap-1.5">
                {activeFilters.length === 0 ? (
                  <span className="text-[length:var(--exits-text-sm)] text-muted">None</span>
                ) : (
                  activeFilters.map((item) => (
                    <RemovableChip
                      key={item.id}
                      removeLabel={item.removeLabel}
                      onRemove={() =>
                        setActiveFilters((prev) => prev.filter((x) => x.id !== item.id))
                      }
                    >
                      {item.label}
                    </RemovableChip>
                  ))
                )}
              </div>
            </SampleCard>
            <SampleCard label="Reset active filters">
              <Button
                type="button"
                variant="secondary"
                shape="soft"
                onClick={() =>
                  setActiveFilters(
                    DEFAULT_REMOVABLE.filter((x) => x.id === "branch" || x.id === "category"),
                  )
                }
              >
                Reset sample
              </Button>
            </SampleCard>
          </StaticSampleGroup>
        </div>
      </UiStandardsSection>

      <UiStandardsSection
        id="chips.cheatsheet"
        title={t("uiStandards.chipCheatTitle")}
        description={t("uiStandards.chipCheatLede")}
        summary="APPROVED · LOCKED"
        open={isOpen("chips.cheatsheet")}
        onOpenChange={(open) => setOpen("chips.cheatsheet", open)}
        testId="ui-standards-chips-cheatsheet"
      >
        <div
          className="rounded-[var(--exits-radius-md)] border border-border bg-[var(--exits-surface-muted)]/30 p-3 font-mono text-[length:var(--exits-text-xs)] leading-relaxed text-foreground"
          data-testid="ui-standards-chip-cheatsheet-body"
        >
          <p className="m-0 mb-2 font-sans text-[length:var(--exits-text-sm)] font-semibold tracking-wide text-muted">
            {t("uiStandards.chipPilotBadge")}
          </p>
          <pre className="m-0 whitespace-pre-wrap">{`FAMILIES
  STATUS CHIP · FILTER CHIP · TAG CHIP · REMOVABLE CHIP · COUNT CHIP · COUNT BADGE

TONES
  NEUTRAL · PRIMARY · INFO · SUCCESS · WARNING · DANGER

SHAPES
  PILL · SOFT · SQUARE

OPTIONS
  WITH ICON · NO ICON · SELECTED · DISABLED

EXAMPLES
  Published         → STATUS CHIP SUCCESS PILL
  Pending           → STATUS CHIP WARNING
  Beta              → TAG CHIP INFO SQUARE
  B2B               → TAG CHIP INFO
  Weighted          → TAG CHIP NEUTRAL
  Selected filter   → FILTER CHIP PRIMARY PILL SELECTED
  Branch: Main      → REMOVABLE CHIP PILL
  Overdue 5         → COUNT CHIP DANGER SOFT
  Notifications 12  → COUNT BADGE PRIMARY

LOCKED DEFAULTS
  STATUS → PILL
  FILTER → PILL
  TAG → SQUARE
  REMOVABLE → PILL
  COUNT → SOFT
  COUNT BADGE → PILL

EQUIVALENCE
  TAG CHIP INFO ≡ TAG CHIP INFO SQUARE
  STATUS CHIP SUCCESS ≡ STATUS CHIP SUCCESS PILL
  COUNT CHIP WARNING ≡ COUNT CHIP WARNING SOFT
  FILTER CHIP SELECTED ≡ FILTER CHIP PRIMARY PILL SELECTED

STATUS
  APPROVED / LOCKED — Docs/UI/exits-chip-standard.md`}</pre>
        </div>
      </UiStandardsSection>
    </div>
  );
}
