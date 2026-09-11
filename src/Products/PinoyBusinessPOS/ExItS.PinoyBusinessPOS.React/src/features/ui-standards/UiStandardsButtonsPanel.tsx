import { useState, type ReactNode } from "react";
import {
  ArrowLeft,
  ArrowRight,
  Ban,
  Check,
  CheckCircle2,
  CircleX,
  CornerUpLeft,
  ExternalLink,
  Eye,
  Loader2,
  MoreHorizontal,
  Pause,
  Pencil,
  Plus,
  Power,
  Printer,
  RefreshCw,
  RotateCcw,
  Save,
  Trash2,
  X,
  XCircle,
} from "lucide-react";
import { Button, buttonIconMotion } from "@/components/ui/button";
import { cn } from "@/lib/cn";
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
      <div className="flex justify-start">{children}</div>
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
      data-testid={`ui-standards-btn-group-${slug}`}
    >
      <h3 className="m-0 text-[length:var(--exits-text-sm)] font-semibold text-muted">{title}</h3>
      <div className="grid gap-2 sm:grid-cols-2 lg:grid-cols-3">{children}</div>
    </section>
  );
}

function RefreshRoundDemo() {
  const [deg, setDeg] = useState(0);
  const [hover, setHover] = useState(false);
  return (
    <Button
      type="button"
      size="icon"
      shape="round"
      variant="secondary"
      title="Refresh"
      aria-label="Refresh"
      onMouseEnter={() => setHover(true)}
      onMouseLeave={() => setHover(false)}
      onClick={() => setDeg((n) => n + 360)}
    >
      <RefreshCw
        className="size-4 motion-reduce:transition-none"
        style={{
          transform: `rotate(${deg + (hover ? 20 : 0)}deg)`,
          transition: "transform var(--exits-motion-fast) var(--exits-ease-standard)",
        }}
        aria-hidden
      />
    </Button>
  );
}

function SaveSuccessDemo() {
  const [phase, setPhase] = useState<"idle" | "saving" | "saved">("idle");

  const onClick = () => {
    if (phase !== "idle") return;
    setPhase("saving");
    window.setTimeout(() => setPhase("saved"), 700);
    window.setTimeout(() => setPhase("idle"), 1800);
  };

  return (
    <Button
      type="button"
      shape="soft"
      disabled={phase === "saving"}
      aria-busy={phase === "saving"}
      onClick={onClick}
      className={cn(phase === "saved" && "border-[var(--exits-success)] text-[var(--exits-success)]")}
      variant={phase === "saved" ? "success" : "default"}
    >
      {phase === "saving" ? (
        <>
          <Loader2 className="size-4 animate-spin motion-reduce:animate-none" aria-hidden />
          Saving...
        </>
      ) : null}
      {phase === "saved" ? (
        <>
          <Check
            className="size-4 scale-100 opacity-100 transition-[opacity,transform] duration-150 ease-[var(--exits-ease-standard)] motion-reduce:transition-none"
            aria-hidden
          />
          Saved
        </>
      ) : null}
      {phase === "idle" ? (
        <>
          <Save className="size-4" aria-hidden />
          Save
        </>
      ) : null}
    </Button>
  );
}

export type UiStandardsButtonsPanelProps = {
  isOpen: (id: string) => boolean;
  setOpen: (id: string, open: boolean) => void;
};

export function UiStandardsButtonsPanel({ isOpen, setOpen }: UiStandardsButtonsPanelProps) {
  const { t } = useI18n();

  return (
    <div className="grid gap-3" data-testid="ui-standards-buttons-section">
      <UiStandardsSection
        id="buttons.shapes"
        title={t("uiStandards.buttonShapesTitle")}
        description={t("uiStandards.buttonShapesLede")}
        summary="5 intents · standard / soft / pill"
        open={isOpen("buttons.shapes")}
        onOpenChange={(open) => setOpen("buttons.shapes", open)}
        testId="ui-standards-button-shapes"
      >
        {(
          [
            {
              label: "PRIMARY / Save",
              render: (shape: "standard" | "soft" | "pill") => (
                <Button type="button" shape={shape}>
                  <Save className="size-4" aria-hidden />
                  Save
                </Button>
              ),
            },
            {
              label: "SUCCESS / Approve",
              render: (shape: "standard" | "soft" | "pill") => (
                <Button type="button" variant="success" shape={shape}>
                  <Check className="size-4" aria-hidden />
                  Approve
                </Button>
              ),
            },
            {
              label: "WARNING / Deactivate",
              render: (shape: "standard" | "soft" | "pill") => (
                <Button type="button" variant="warning" shape={shape}>
                  <Power className="size-4" aria-hidden />
                  Deactivate
                </Button>
              ),
            },
            {
              label: "DANGER / Delete",
              render: (shape: "standard" | "soft" | "pill") => (
                <Button type="button" variant="destructive" shape={shape}>
                  <Trash2 className="size-4" aria-hidden />
                  Delete
                </Button>
              ),
            },
          ] as const
        ).map((row) => (
          <div key={row.label} className="grid gap-2 border-t border-border pt-3 first:border-t-0 first:pt-0">
            <p className="m-0 text-[length:var(--exits-text-sm)] font-medium text-muted">{row.label}</p>
            <div className="grid gap-2 sm:grid-cols-3">
              {(["standard", "soft", "pill"] as const).map((shape) => (
                <div
                  key={shape}
                  className="flex flex-col gap-1.5 rounded-[var(--exits-radius-md)] border border-border bg-[var(--exits-surface-muted)]/40 p-2"
                  data-testid={`ui-standards-shape-${row.label.split(" / ")[0]?.toLowerCase()}-${shape}`}
                >
                  <span className="text-[length:var(--exits-text-xs)] uppercase tracking-wide text-muted">
                    {shape}
                  </span>
                  {row.render(shape)}
                </div>
              ))}
            </div>
          </div>
        ))}

        <div
          className="grid gap-2 border-t border-border pt-3"
          data-testid="ui-standards-cancel-icon-shape-row"
        >
          <p className="m-0 text-[length:var(--exits-text-sm)] font-medium text-muted">
            MUTED / Cancel · icon options (PILOT)
          </p>
          <div className="grid gap-2 sm:grid-cols-3">
            {(
              [
                { id: "circlex", label: "CircleX", Icon: CircleX },
                { id: "corner-up-left", label: "CornerUpLeft", Icon: CornerUpLeft },
                { id: "x", label: "X", Icon: X },
              ] as const
            ).map((opt) => (
              <div
                key={opt.id}
                className="flex flex-col gap-1.5 rounded-[var(--exits-radius-md)] border border-border bg-[var(--exits-surface-muted)]/40 p-2"
                data-testid={`ui-standards-shape-cancel-${opt.id}`}
              >
                <span className="text-[length:var(--exits-text-xs)] uppercase tracking-wide text-muted">
                  {opt.label}
                </span>
                <Button type="button" variant="secondary" shape="soft">
                  <opt.Icon className="size-4" aria-hidden />
                  Cancel
                </Button>
              </div>
            ))}
          </div>
          <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
            {t("uiStandards.cancelIconPilotNote")}
          </p>
        </div>
      </UiStandardsSection>

      <UiStandardsSection
        id="buttons.treatments"
        title={t("uiStandards.buttonTreatmentsTitle")}
        description={t("uiStandards.buttonTreatmentsLede")}
        summary="Flat · elevated · gradient"
        open={isOpen("buttons.treatments")}
        onOpenChange={(open) => setOpen("buttons.treatments", open)}
        testId="ui-standards-button-treatments"
      >
        <div className="grid gap-2 sm:grid-cols-3">
          {(["flat", "elevated", "gradient"] as const).map((treatment) => (
            <div
              key={treatment}
              className="flex flex-col gap-1.5 rounded-[var(--exits-radius-md)] border border-border bg-[var(--exits-surface-muted)]/40 p-2"
              data-testid={`ui-standards-treatment-primary-${treatment}`}
            >
              <span className="text-[length:var(--exits-text-xs)] uppercase tracking-wide text-muted">
                {treatment}
              </span>
              <Button type="button" shape="soft" treatment={treatment}>
                <Save className="size-4" aria-hidden />
                Save
              </Button>
            </div>
          ))}
        </div>
        <div className="grid gap-2 border-t border-border pt-3 sm:grid-cols-3">
          {(["flat", "elevated", "gradient"] as const).map((treatment) => (
            <div
              key={treatment}
              className="flex flex-col gap-1.5 rounded-[var(--exits-radius-md)] border border-border bg-[var(--exits-surface-muted)]/40 p-2"
              data-testid={`ui-standards-treatment-danger-strong-${treatment}`}
            >
              <span className="text-[length:var(--exits-text-xs)] uppercase tracking-wide text-muted">
                DANGER STRONG · {treatment}
              </span>
              <Button type="button" variant="dangerStrong" shape="soft" treatment={treatment}>
                <Trash2 className="size-4" aria-hidden />
                Delete permanently
              </Button>
            </div>
          ))}
        </div>
      </UiStandardsSection>

      <UiStandardsSection
        id="buttons.samples"
        title={t("uiStandards.buttonPilotTitle")}
        description={t("uiStandards.buttonPilotLede")}
        summary="9 intents · semantic samples"
        open={isOpen("buttons.samples")}
        onOpenChange={(open) => setOpen("buttons.samples", open)}
        testId="ui-standards-button-showcase"
      >
        {(
          [
            {
              id: "buttons.samples.primary",
              title: "PRIMARY",
              summary: "3 examples",
              body: (
                <>
                  <SampleCard label="Save">
                    <Button type="button" shape="soft">
                      <Save className="size-4" aria-hidden />
                      Save
                    </Button>
                  </SampleCard>
                  <SampleCard label="Add product">
                    <Button type="button" shape="soft">
                      <Plus className="size-4" aria-hidden />
                      Add product
                    </Button>
                  </SampleCard>
                  <SampleCard label="Continue">
                    <Button type="button" shape="soft">
                      Continue
                      <ArrowRight className="size-4" aria-hidden />
                    </Button>
                  </SampleCard>
                </>
              ),
            },
            {
              id: "buttons.samples.success",
              title: "SUCCESS",
              summary: "3 examples",
              body: (
                <>
                  <SampleCard label="Approve">
                    <Button type="button" variant="success" shape="soft">
                      <Check className="size-4" aria-hidden />
                      Approve
                    </Button>
                  </SampleCard>
                  <SampleCard label="Accept order">
                    <Button type="button" variant="success" shape="soft">
                      <Check className="size-4" aria-hidden />
                      Accept order
                    </Button>
                  </SampleCard>
                  <SampleCard label="Mark paid">
                    <Button type="button" variant="success" shape="soft">
                      <CheckCircle2 className="size-4" aria-hidden />
                      Mark paid
                    </Button>
                  </SampleCard>
                </>
              ),
            },
            {
              id: "buttons.samples.muted",
              title: "MUTED",
              summary: "Cancel icon comparison · PILOT",
              body: (
                <>
                  <div className="grid gap-2 sm:col-span-2 lg:col-span-3" data-testid="ui-standards-cancel-icon-comparison">
                    <p className="m-0 text-[length:var(--exits-text-sm)] font-medium text-muted">CANCEL</p>
                    <div className="grid gap-2 sm:grid-cols-3">
                      <SampleCard label="CircleX" testId="ui-standards-cancel-circlex">
                        <Button type="button" variant="secondary" shape="soft">
                          <CircleX className="size-4" aria-hidden />
                          Cancel
                        </Button>
                      </SampleCard>
                      <SampleCard label="Corner up left" testId="ui-standards-cancel-corner-up-left">
                        <Button type="button" variant="secondary" shape="soft">
                          <CornerUpLeft className="size-4" aria-hidden />
                          Cancel
                        </Button>
                      </SampleCard>
                      <SampleCard label="X" testId="ui-standards-cancel-x">
                        <Button type="button" variant="secondary" shape="soft">
                          <X className="size-4" aria-hidden />
                          Cancel
                        </Button>
                      </SampleCard>
                    </div>
                    <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
                      {t("uiStandards.cancelIconPilotNote")}
                    </p>
                  </div>
                  <SampleCard label="Close (X)">
                    <Button type="button" variant="secondary" shape="soft">
                      <X className="size-4" aria-hidden />
                      Close
                    </Button>
                  </SampleCard>
                </>
              ),
            },
            {
              id: "buttons.samples.outline",
              title: "OUTLINE",
              summary: "2 examples",
              body: (
                <>
                  <SampleCard label="Change branch">
                    <Button type="button" variant="outline">
                      Change branch
                    </Button>
                  </SampleCard>
                  <SampleCard label="Download">
                    <Button type="button" variant="outline">
                      Download
                    </Button>
                  </SampleCard>
                </>
              ),
            },
            {
              id: "buttons.samples.ghost",
              title: "GHOST",
              summary: "2 examples",
              body: (
                <>
                  <SampleCard label="Back">
                    <Button type="button" variant="ghost">
                      <ArrowLeft className="size-4" aria-hidden />
                      Back
                    </Button>
                  </SampleCard>
                  <SampleCard label="More">
                    <Button type="button" variant="ghost">
                      <MoreHorizontal className="size-4" aria-hidden />
                      More
                    </Button>
                  </SampleCard>
                </>
              ),
            },
            {
              id: "buttons.samples.info",
              title: "INFO",
              summary: "2 examples",
              body: (
                <>
                  <SampleCard label="View details">
                    <Button type="button" variant="info">
                      <Eye className="size-4" aria-hidden />
                      View details
                    </Button>
                  </SampleCard>
                  <SampleCard label="Preview">
                    <Button type="button" variant="info">
                      Preview
                      <ExternalLink className="size-4" aria-hidden />
                    </Button>
                  </SampleCard>
                </>
              ),
            },
            {
              id: "buttons.samples.warning",
              title: "WARNING",
              summary: "3 examples",
              body: (
                <>
                  <SampleCard label="Deactivate">
                    <Button type="button" variant="warning">
                      <Power className="size-4" aria-hidden />
                      Deactivate
                    </Button>
                  </SampleCard>
                  <SampleCard label="Pause">
                    <Button type="button" variant="warning">
                      <Pause className="size-4" aria-hidden />
                      Pause
                    </Button>
                  </SampleCard>
                  <SampleCard label="Reset">
                    <Button type="button" variant="warning">
                      <RotateCcw className="size-4" aria-hidden />
                      Reset
                    </Button>
                  </SampleCard>
                </>
              ),
            },
            {
              id: "buttons.samples.danger",
              title: "DANGER",
              summary: "3 examples",
              body: (
                <>
                  <SampleCard label="Decline">
                    <Button type="button" variant="destructive">
                      <XCircle className="size-4" aria-hidden />
                      Decline
                    </Button>
                  </SampleCard>
                  <SampleCard label="Delete">
                    <Button type="button" variant="destructive">
                      <Trash2 className="size-4" aria-hidden />
                      Delete
                    </Button>
                  </SampleCard>
                  <SampleCard label="Void">
                    <Button type="button" variant="destructive">
                      <Ban className="size-4" aria-hidden />
                      Void
                    </Button>
                  </SampleCard>
                </>
              ),
            },
            {
              id: "buttons.samples.danger-strong",
              title: "DANGER STRONG",
              summary: "1 example",
              body: (
                <SampleCard label="Delete permanently">
                  <Button type="button" variant="dangerStrong" shape="soft">
                    <Trash2 className="size-4" aria-hidden />
                    Delete permanently
                  </Button>
                </SampleCard>
              ),
            },
          ] as const
        ).map((group) => {
          const slug = group.title
            .toLowerCase()
            .replace(/[^a-z0-9]+/g, "-")
            .replace(/^-|-$/g, "");
          return (
            <UiStandardsSection
              key={group.id}
              id={group.id}
              title={group.title}
              summary={group.summary}
              open={isOpen(group.id)}
              onOpenChange={(open) => setOpen(group.id, open)}
              level={2}
              testId={`ui-standards-btn-group-${slug}`}
            >
              <div className="grid gap-2 sm:grid-cols-2 lg:grid-cols-3">{group.body}</div>
            </UiStandardsSection>
          );
        })}
      </UiStandardsSection>

      <UiStandardsSection
        id="buttons.icon-only"
        title={t("uiStandards.buttonIconOnlyTitle")}
        description={t("uiStandards.buttonIconOnlyLede")}
        summary="Shape matrix · round intents"
        open={isOpen("buttons.icon-only")}
        onOpenChange={(open) => setOpen("buttons.icon-only", open)}
        testId="ui-standards-button-icon-only"
      >
        <StaticSampleGroup title="ICON ONLY">
          {(
            [
              { action: "Edit", variant: "outline" as const, icon: Pencil },
              { action: "Refresh", variant: "outline" as const, icon: RefreshCw },
              { action: "Print", variant: "outline" as const, icon: Printer },
              { action: "Delete", variant: "destructive" as const, icon: Trash2 },
              { action: "More", variant: "ghost" as const, icon: MoreHorizontal },
            ] as const
          ).map((row) => (
            <div
              key={row.action}
              className="grid gap-2 sm:col-span-2 lg:col-span-3"
              data-testid={`ui-standards-icon-matrix-${row.action.toLowerCase()}`}
            >
              <p className="m-0 text-[length:var(--exits-text-sm)] font-medium text-muted">
                {row.action.toUpperCase()}
              </p>
              <div className="grid gap-2 sm:grid-cols-3">
                {(["standard", "soft", "round"] as const).map((shape) => (
                  <SampleCard
                    key={shape}
                    label={shape}
                    testId={`ui-standards-icon-${row.action.toLowerCase()}-${shape}`}
                  >
                    <Button
                      type="button"
                      variant={row.variant}
                      size="icon"
                      shape={shape}
                      title={row.action}
                      aria-label={row.action}
                    >
                      <row.icon className="size-4" aria-hidden />
                    </Button>
                  </SampleCard>
                ))}
              </div>
            </div>
          ))}
        </StaticSampleGroup>

        <StaticSampleGroup title="ICON ONLY ROUND · INTENTS">
          <SampleCard label="ROUND GHOST · More" testId="ui-standards-round-ghost">
            <Button type="button" variant="ghost" size="icon" shape="round" title="More" aria-label="More">
              <MoreHorizontal className="size-4" aria-hidden />
            </Button>
          </SampleCard>
          <SampleCard label="ROUND MUTED · Refresh" testId="ui-standards-round-muted">
            <Button
              type="button"
              variant="secondary"
              size="icon"
              shape="round"
              title="Refresh"
              aria-label="Refresh"
            >
              <RefreshCw className="size-4" aria-hidden />
            </Button>
          </SampleCard>
          <SampleCard label="ROUND PRIMARY · Add" testId="ui-standards-round-primary">
            <Button type="button" size="icon" shape="round" title="Add" aria-label="Add">
              <Plus className="size-4" aria-hidden />
            </Button>
          </SampleCard>
          <SampleCard label="ROUND INFO · View" testId="ui-standards-round-info">
            <Button type="button" variant="info" size="icon" shape="round" title="View" aria-label="View">
              <Eye className="size-4" aria-hidden />
            </Button>
          </SampleCard>
          <SampleCard label="ROUND WARNING · Pause" testId="ui-standards-round-warning">
            <Button type="button" variant="warning" size="icon" shape="round" title="Pause" aria-label="Pause">
              <Pause className="size-4" aria-hidden />
            </Button>
          </SampleCard>
          <SampleCard label="ROUND DANGER · Delete" testId="ui-standards-round-danger">
            <Button
              type="button"
              variant="destructive"
              size="icon"
              shape="round"
              title="Delete"
              aria-label="Delete"
            >
              <Trash2 className="size-4" aria-hidden />
            </Button>
          </SampleCard>
        </StaticSampleGroup>
      </UiStandardsSection>

      <UiStandardsSection
        id="buttons.states"
        title={t("uiStandards.buttonStatesTitle")}
        description={t("uiStandards.buttonStatesHint")}
        summary="Normal · lift · pressed · disabled · loading"
        open={isOpen("buttons.states")}
        onOpenChange={(open) => setOpen("buttons.states", open)}
        testId="ui-standards-button-states"
      >
        <div className="grid gap-2 sm:grid-cols-2 lg:grid-cols-3" data-testid="ui-standards-btn-group-states">
          <SampleCard label="Normal">
            <Button type="button" shape="soft">
              <Save className="size-4" aria-hidden />
              Normal
            </Button>
          </SampleCard>
          <SampleCard label="Hover / lift">
            <Button type="button" shape="soft" treatment="elevated">
              <Save className="size-4" aria-hidden />
              Hover / lift
            </Button>
          </SampleCard>
          <SampleCard label="Pressed (try)">
            <Button type="button" shape="soft" treatment="elevated">
              Pressed (try)
            </Button>
          </SampleCard>
          <SampleCard label="Disabled">
            <Button type="button" shape="soft" disabled>
              Disabled
            </Button>
          </SampleCard>
          <SampleCard label="Loading">
            <Button type="button" shape="soft" disabled aria-busy="true">
              <Loader2 className="size-4 animate-spin motion-reduce:animate-none" aria-hidden />
              Saving...
            </Button>
          </SampleCard>
        </div>
      </UiStandardsSection>

      <UiStandardsSection
        id="buttons.motion"
        title={t("uiStandards.buttonMotionTitle")}
        description={t("uiStandards.buttonMotionLede")}
        summary="6 interaction patterns"
        open={isOpen("buttons.motion")}
        onOpenChange={(open) => setOpen("buttons.motion", open)}
        testId="ui-standards-button-motion"
      >
        <StaticSampleGroup title="STANDARD MOTION">
          <SampleCard label="Primary" hint="Color + press">
            <Button type="button" shape="soft">
              <Save className="size-4" aria-hidden />
              Save
            </Button>
          </SampleCard>
          <SampleCard label="Muted" hint="Quieter surface">
            <Button type="button" variant="secondary" shape="soft">
              <CircleX className="size-4" aria-hidden />
              Cancel
            </Button>
          </SampleCard>
          <SampleCard label="Ghost" hint="Background fade only">
            <Button type="button" variant="ghost">
              <MoreHorizontal className="size-4" aria-hidden />
              More
            </Button>
          </SampleCard>
        </StaticSampleGroup>

        <StaticSampleGroup title="PRESS FEEDBACK">
          <SampleCard label="Save" hint="Click to preview">
            <Button type="button" shape="soft">
              <Save className="size-4" aria-hidden />
              Save
            </Button>
          </SampleCard>
          <SampleCard label="Delete" hint="Calm press — no shake">
            <Button type="button" variant="destructive" shape="soft">
              <Trash2 className={cn("size-4", buttonIconMotion.delete)} aria-hidden />
              Delete
            </Button>
          </SampleCard>
        </StaticSampleGroup>

        <StaticSampleGroup title="ELEVATED LIFT">
          <SampleCard label="Add" hint="Hover to preview">
            <Button type="button" shape="soft" treatment="elevated">
              <Plus className={cn("size-4", buttonIconMotion.add)} aria-hidden />
              Add
            </Button>
          </SampleCard>
          <SampleCard label="Main CTA" hint="Elevated + soft">
            <Button type="button" shape="soft" treatment="elevated">
              <Save className="size-4" aria-hidden />
              Save
            </Button>
          </SampleCard>
        </StaticSampleGroup>

        <StaticSampleGroup title="ICON MOTION">
          <SampleCard label="Add" hint="Hover: slight scale">
            <Button type="button" shape="soft">
              <Plus className={cn("size-4", buttonIconMotion.add)} aria-hidden />
              Add
            </Button>
          </SampleCard>
          <SampleCard label="View" hint="Hover: slight scale">
            <Button type="button" variant="info" shape="soft">
              <Eye className={cn("size-4", buttonIconMotion.view)} aria-hidden />
              View
            </Button>
          </SampleCard>
          <SampleCard label="Delete" hint="Subtle lift only">
            <Button type="button" variant="destructive" shape="soft">
              <Trash2 className={cn("size-4", buttonIconMotion.delete)} aria-hidden />
              Delete
            </Button>
          </SampleCard>
          <SampleCard label="Warning" hint="Subtle only">
            <Button type="button" variant="warning" shape="soft">
              <Power className={cn("size-4", buttonIconMotion.warning)} aria-hidden />
              Deactivate
            </Button>
          </SampleCard>
        </StaticSampleGroup>

        <StaticSampleGroup title="DIRECTIONAL MOTION">
          <SampleCard label="Continue" hint="Hover to preview">
            <Button type="button" shape="soft">
              Continue
              <ArrowRight className={cn("size-4", buttonIconMotion.continue)} aria-hidden />
            </Button>
          </SampleCard>
          <SampleCard label="Back" hint="Hover to preview">
            <Button type="button" variant="ghost">
              <ArrowLeft className={cn("size-4", buttonIconMotion.back)} aria-hidden />
              Back
            </Button>
          </SampleCard>
          <SampleCard label="Open" hint="Hover to preview">
            <Button type="button" variant="info" shape="soft">
              Open
              <ExternalLink className={cn("size-4", buttonIconMotion.open)} aria-hidden />
            </Button>
          </SampleCard>
        </StaticSampleGroup>

        <StaticSampleGroup title="ROUND ICON MOTION">
          <SampleCard label="Refresh" hint="Hover / click once" testId="ui-standards-motion-round-refresh">
            <RefreshRoundDemo />
          </SampleCard>
          <SampleCard label="Add" hint="Hover: slight scale">
            <Button type="button" size="icon" shape="round" title="Add" aria-label="Add">
              <Plus className={cn("size-4", buttonIconMotion.add)} aria-hidden />
            </Button>
          </SampleCard>
          <SampleCard label="Next" hint="Hover: directional">
            <Button type="button" size="icon" shape="round" title="Next" aria-label="Next">
              <ArrowRight className={cn("size-4", buttonIconMotion.continue)} aria-hidden />
            </Button>
          </SampleCard>
          <SampleCard label="Delete" hint="Subtle lift only">
            <Button
              type="button"
              variant="destructive"
              size="icon"
              shape="round"
              title="Delete"
              aria-label="Delete"
            >
              <Trash2 className={cn("size-4", buttonIconMotion.delete)} aria-hidden />
            </Button>
          </SampleCard>
          <SampleCard label="More" hint="Very subtle">
            <Button type="button" variant="ghost" size="icon" shape="round" title="More" aria-label="More">
              <MoreHorizontal className={cn("size-4", buttonIconMotion.more)} aria-hidden />
            </Button>
          </SampleCard>
        </StaticSampleGroup>

        <StaticSampleGroup title="LOADING MOTION">
          <SampleCard label="PRIMARY loading">
            <Button type="button" shape="soft" disabled aria-busy="true" className="min-w-[8.5rem]">
              <Loader2 className="size-4 animate-spin motion-reduce:animate-none" aria-hidden />
              Saving...
            </Button>
          </SampleCard>
          <SampleCard label="SUCCESS loading">
            <Button
              type="button"
              variant="success"
              shape="soft"
              disabled
              aria-busy="true"
              className="min-w-[9.5rem]"
            >
              <Loader2 className="size-4 animate-spin motion-reduce:animate-none" aria-hidden />
              Processing...
            </Button>
          </SampleCard>
          <SampleCard label="ICON ONLY ROUND loading">
            <Button
              type="button"
              size="icon"
              shape="round"
              disabled
              aria-busy="true"
              title="Loading"
              aria-label="Loading"
            >
              <Loader2 className="size-4 animate-spin motion-reduce:animate-none" aria-hidden />
            </Button>
          </SampleCard>
        </StaticSampleGroup>

        <StaticSampleGroup title="SUCCESS FEEDBACK">
          <SampleCard
            label="Save → Saved"
            hint="Showcase only — click once"
            testId="ui-standards-motion-success-demo"
          >
            <SaveSuccessDemo />
          </SampleCard>
        </StaticSampleGroup>
      </UiStandardsSection>

      <UiStandardsSection
        id="buttons.cheatsheet"
        title={t("uiStandards.buttonCheatTitle")}
        description={t("uiStandards.buttonPilotBadge")}
        summary="PILOT / NOT LOCKED"
        open={isOpen("buttons.cheatsheet")}
        onOpenChange={(open) => setOpen("buttons.cheatsheet", open)}
        testId="ui-standards-button-cheatsheet"
      >
        <pre className="m-0 overflow-x-auto rounded-[var(--exits-radius-md)] border border-border bg-[var(--exits-surface-muted)] p-3 text-[length:var(--exits-text-xs)] leading-relaxed">
{`BUTTON STANDARD — PILOT / NOT LOCKED

INTENT:
PRIMARY
SUCCESS
MUTED
OUTLINE
GHOST
INFO
WARNING
DANGER
DANGER STRONG

SHAPE:
STANDARD
SOFT
PILL
ROUND (icon-only circle)

TREATMENT:
FLAT
ELEVATED
GRADIENT

MOTION (conceptual — not Button API variants):
NONE
STANDARD (= transition + press)
CONTEXTUAL ICON (= specific icons only)

OTHER:
WITH ICON
NO ICON
ICON ONLY
ICON ONLY ROUND

CANCEL ICONS (PILOT / NOT LOCKED):
Cancel → CircleX (candidate)
Cancel & return → CornerUpLeft (candidate)
Close → X
Back → ArrowLeft

Examples:
Edit: ICON ONLY ROUND GHOST
Refresh: ICON ONLY ROUND MUTED
Add: ICON ONLY ROUND PRIMARY
Delete: ICON ONLY ROUND DANGER
Continue: PRIMARY + SOFT + WITH ICON + DIRECTIONAL ICON
Main CTA: PRIMARY + SOFT + ELEVATED + WITH ICON
Save: PRIMARY + SOFT + ELEVATED + WITH ICON
Cancel: MUTED + CircleX (pilot)
Cancel & return: MUTED + CornerUpLeft (pilot)
Approve: SUCCESS + SOFT + WITH ICON
Deactivate: WARNING + STANDARD + WITH ICON
Delete (text): DANGER + STANDARD + WITH ICON
Delete permanently: DANGER STRONG + SOFT + WITH ICON
Main CTA gradient: PRIMARY + SOFT + GRADIENT + WITH ICON`}
        </pre>
      </UiStandardsSection>
    </div>
  );
}
