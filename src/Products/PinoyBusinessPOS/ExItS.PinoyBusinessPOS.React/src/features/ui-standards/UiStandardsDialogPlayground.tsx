import { useState, type CSSProperties, type ReactNode } from "react";
import {
  CheckCircle2,
  Info,
  Lock,
  MessageSquare,
  OctagonAlert,
  PenLine,
  ScrollText,
  Trash2,
  TriangleAlert,
  X,
  type LucideIcon,
} from "lucide-react";

import { ExitsModal, type ExitsModalSize } from "@/components/exits/ExitsModal";
import { Button, type ButtonIntentTone } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Switch } from "@/components/ui/switch";
import { UiStandardsCopyCommand } from "@/features/ui-standards/UiStandardsCopyCommand";
import {
  CompactSelect,
  PlaygroundPreviewCard,
  PlaygroundTextField,
} from "@/features/ui-standards/UiStandardsPlaygrounds";
import { PlaygroundSection } from "@/features/ui-standards/UiStandardsSnippetBlock";

/**
 * Dialog playground and samples. Every dialog is the product `ExitsModal`;
 * the preview panel mirrors its shell so options can be compared without opening it.
 */
type DialogIntent = "primary" | "info" | "success" | "warning" | "danger";
type DialogMobile = "centered" | "full";

const DIALOG_INTENTS: ReadonlyArray<{
  value: DialogIntent;
  label: string;
  icon: LucideIcon;
  tone: string;
  button: ButtonIntentTone;
}> = [
  { value: "primary", label: "Primary", icon: MessageSquare, tone: "var(--exits-primary)", button: "primary" },
  { value: "info", label: "Info", icon: Info, tone: "var(--exits-severity-info, #0ea5e9)", button: "info" },
  {
    value: "success",
    label: "Success",
    icon: CheckCircle2,
    tone: "var(--exits-severity-success, #22c55e)",
    button: "success",
  },
  { value: "warning", label: "Warn", icon: TriangleAlert, tone: "var(--exits-warning, #ff5722)", button: "warning" },
  {
    value: "danger",
    label: "Danger",
    icon: OctagonAlert,
    tone: "var(--exits-severity-danger, #ef4444)",
    button: "danger",
  },
];

const DIALOG_SIZES: ReadonlyArray<{ value: ExitsModalSize; label: string }> = [
  { value: "sm", label: "Small" },
  { value: "md", label: "Medium" },
  { value: "lg", label: "Large" },
];

const DIALOG_MOBILE: ReadonlyArray<{ value: DialogMobile; label: string }> = [
  { value: "centered", label: "Centered" },
  { value: "full", label: "Full height" },
];

const DIALOG_PLAYGROUND_DEFAULTS = {
  intent: "primary" as DialogIntent,
  size: "md" as ExitsModalSize,
  mobile: "centered" as DialogMobile,
  title: "Save changes?",
  confirmLabel: "Continue",
  description: true,
  icon: true,
  footer: true,
  dismissible: true,
};

function intentDef(intent: DialogIntent) {
  return DIALOG_INTENTS.find((item) => item.value === intent) ?? DIALOG_INTENTS[0]!;
}

export function dialogCursorCommand(options: {
  intent: DialogIntent;
  size: ExitsModalSize;
  mobile: DialogMobile;
  icon: boolean;
  footer: boolean;
  dismissible: boolean;
}): string {
  const parts = ["ext dialog"];
  if (options.intent !== "primary") parts.push(options.intent);
  if (options.size !== "md") parts.push(options.size);
  if (options.mobile === "full") parts.push("fullscreen");
  if (!options.icon) parts.push("no-icon");
  if (!options.footer) parts.push("no-footer");
  if (!options.dismissible) parts.push("persistent");
  return parts.join(" ");
}

function DialogMessage({
  intent,
  showIcon,
  children,
}: {
  intent: DialogIntent;
  showIcon: boolean;
  children: ReactNode;
}) {
  const def = intentDef(intent);
  const Icon = def.icon;
  return (
    <div className="ui-standards-dialog-message" style={{ "--dialog-tone": def.tone } as CSSProperties}>
      {showIcon ? (
        <span className="ui-standards-dialog-message__icon" aria-hidden>
          <Icon className="size-5" />
        </span>
      ) : null}
      <div className="min-w-0 text-[length:var(--exits-text-sm)] text-foreground">{children}</div>
    </div>
  );
}

const PLAYGROUND_BODY = "Your edits will be saved to this record. You can change them again later.";
const PLAYGROUND_DESCRIPTION = "Review before you continue.";

export function UiStandardsDialogPlayground() {
  const [intent, setIntent] = useState(DIALOG_PLAYGROUND_DEFAULTS.intent);
  const [size, setSize] = useState(DIALOG_PLAYGROUND_DEFAULTS.size);
  const [mobile, setMobile] = useState(DIALOG_PLAYGROUND_DEFAULTS.mobile);
  const [title, setTitle] = useState(DIALOG_PLAYGROUND_DEFAULTS.title);
  const [confirmLabel, setConfirmLabel] = useState(DIALOG_PLAYGROUND_DEFAULTS.confirmLabel);
  const [description, setDescription] = useState(DIALOG_PLAYGROUND_DEFAULTS.description);
  const [icon, setIcon] = useState(DIALOG_PLAYGROUND_DEFAULTS.icon);
  const [footer, setFooter] = useState(DIALOG_PLAYGROUND_DEFAULTS.footer);
  const [dismissible, setDismissible] = useState(DIALOG_PLAYGROUND_DEFAULTS.dismissible);
  const [open, setOpen] = useState(false);
  const def = intentDef(intent);
  const shownTitle = title.trim() || DIALOG_PLAYGROUND_DEFAULTS.title;
  const shownConfirm = confirmLabel.trim() || DIALOG_PLAYGROUND_DEFAULTS.confirmLabel;

  function resetPreview() {
    setIntent(DIALOG_PLAYGROUND_DEFAULTS.intent);
    setSize(DIALOG_PLAYGROUND_DEFAULTS.size);
    setMobile(DIALOG_PLAYGROUND_DEFAULTS.mobile);
    setTitle(DIALOG_PLAYGROUND_DEFAULTS.title);
    setConfirmLabel(DIALOG_PLAYGROUND_DEFAULTS.confirmLabel);
    setDescription(DIALOG_PLAYGROUND_DEFAULTS.description);
    setIcon(DIALOG_PLAYGROUND_DEFAULTS.icon);
    setFooter(DIALOG_PLAYGROUND_DEFAULTS.footer);
    setDismissible(DIALOG_PLAYGROUND_DEFAULTS.dismissible);
  }

  const footerButtons = (onDone: () => void) => (
    <>
      <Button type="button" intent="neutral" appearance="outline" onClick={onDone}>
        Cancel
      </Button>
      <Button type="button" intent={def.button} appearance="solid" onClick={onDone}>
        {shownConfirm}
      </Button>
    </>
  );

  const switches: ReadonlyArray<[string, boolean, (value: boolean) => void, string]> = [
    ["Description", description, setDescription, "ui-standard-dialog-pg-description"],
    ["Icon", icon, setIcon, "ui-standard-dialog-pg-icon"],
    ["Footer", footer, setFooter, "ui-standard-dialog-pg-footer"],
    ["Close on backdrop / Esc", dismissible, setDismissible, "ui-standard-dialog-pg-dismissible"],
  ];

  return (
    <PlaygroundSection testId="ui-standard-dialog-playground" className="">
      <div className="grid min-w-0 gap-2 sm:grid-cols-2 lg:grid-cols-3">
        <CompactSelect
          label="Intent"
          value={intent}
          options={DIALOG_INTENTS.map((item) => ({ label: item.label, value: item.value, color: item.tone }))}
          onChange={setIntent}
          testId="ui-standard-dialog-pg-intent"
        />
        <CompactSelect
          label="Size"
          value={size}
          options={DIALOG_SIZES}
          onChange={setSize}
          testId="ui-standard-dialog-pg-size"
        />
        <CompactSelect
          label="Mobile"
          value={mobile}
          options={DIALOG_MOBILE}
          onChange={setMobile}
          testId="ui-standard-dialog-pg-mobile"
        />
        <PlaygroundTextField label="Title" value={title} onChange={setTitle} testId="ui-standard-dialog-pg-title" />
        <PlaygroundTextField
          label="Confirm label"
          value={confirmLabel}
          onChange={setConfirmLabel}
          testId="ui-standard-dialog-pg-confirm-label"
        />
      </div>
      <div className="flex flex-wrap gap-4">
        {switches.map(([label, checked, onChange, testId]) => (
          <label key={testId} className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
            <Switch checked={checked} onCheckedChange={onChange} aria-label={label} data-testid={testId} />
            {label}
          </label>
        ))}
      </div>
      <PlaygroundPreviewCard
        previewTestId="ui-standard-dialog-pg-preview"
        themeTestId="ui-standard-dialog-pg-theme"
        resetTestId="ui-standard-dialog-pg-reset"
        onReset={resetPreview}
        previewClassName="ui-standards-dialog-stage"
      >
        <div
          className="ui-standards-dialog-panel"
          data-size={size}
          data-intent={intent}
          data-testid="ui-standard-dialog-pg-panel"
        >
          <div className="ui-standards-dialog-panel__header">
            <div className="min-w-0">
              <p className="ui-standards-dialog-panel__title" data-testid="ui-standard-dialog-pg-panel-title">
                {shownTitle}
              </p>
              {description ? <p className="ui-standards-dialog-panel__description">{PLAYGROUND_DESCRIPTION}</p> : null}
            </div>
            <span className="ui-standards-dialog-panel__close" aria-hidden>
              <X className="size-4" />
            </span>
          </div>
          <DialogMessage intent={intent} showIcon={icon}>
            {PLAYGROUND_BODY}
          </DialogMessage>
          {footer ? (
            <div className="ui-standards-dialog-panel__footer" data-testid="ui-standard-dialog-pg-panel-footer">
              {footerButtons(() => undefined)}
            </div>
          ) : null}
        </div>
      </PlaygroundPreviewCard>
      <div className="flex flex-wrap gap-2">
        <Button
          type="button"
          intent="primary"
          appearance="solid"
          data-testid="ui-standard-dialog-pg-open"
          onClick={() => setOpen(true)}
        >
          Open dialog
        </Button>
      </div>
      <UiStandardsCopyCommand
        standard="Dialog"
        standardStatus="pilot"
        command={dialogCursorCommand({ intent, size, mobile, icon, footer, dismissible })}
      />
      <ExitsModal
        open={open}
        onOpenChange={setOpen}
        title={shownTitle}
        description={description ? PLAYGROUND_DESCRIPTION : undefined}
        size={size}
        fullHeightOnCompact={mobile === "full"}
        closeOnOutsideClick={dismissible}
        closeOnEscape={dismissible}
        footer={footer ? footerButtons(() => setOpen(false)) : undefined}
        testId="ui-standard-dialog-pg-modal"
      >
        <DialogMessage intent={intent} showIcon={icon}>
          {PLAYGROUND_BODY}
        </DialogMessage>
      </ExitsModal>
    </PlaygroundSection>
  );
}

type SampleId = "alert" | "confirm" | "delete" | "form" | "scroll" | "persistent";

const SAMPLES: ReadonlyArray<{ id: SampleId; label: string; caption: string; icon: LucideIcon; tone: string }> = [
  { id: "alert", label: "Alert", caption: "One action to acknowledge", icon: Info, tone: "var(--exits-severity-info, #0ea5e9)" },
  { id: "confirm", label: "Confirm", caption: "Cancel | Continue", icon: MessageSquare, tone: "var(--exits-primary)" },
  { id: "delete", label: "Delete", caption: "Danger confirm", icon: Trash2, tone: "var(--exits-severity-danger, #ef4444)" },
  { id: "form", label: "Form", caption: "Short form with submit", icon: PenLine, tone: "var(--exits-primary)" },
  { id: "scroll", label: "Long content", caption: "Full height on mobile", icon: ScrollText, tone: "var(--exits-severity-info, #0ea5e9)" },
  { id: "persistent", label: "Persistent", caption: "No backdrop / Esc close", icon: Lock, tone: "var(--exits-warning, #ff5722)" },
];

export function UiStandardsDialogGallery() {
  const [openId, setOpenId] = useState<SampleId | null>(null);
  const [name, setName] = useState("");
  const close = () => setOpenId(null);
  const isOpen = (id: SampleId) => openId === id;

  return (
    <div className="flex flex-col gap-3" data-testid="ui-standard-dialog-gallery">
      <div className="ui-standards-dialog-samples">
        {SAMPLES.map((sample) => {
          const Icon = sample.icon;
          return (
            <button
              key={sample.id}
              type="button"
              className="ui-standards-dialog-sample"
              style={{ "--dialog-tone": sample.tone } as CSSProperties}
              data-testid={`ui-standard-dialog-sample-${sample.id}`}
              onClick={() => setOpenId(sample.id)}
            >
              <span className="ui-standards-dialog-message__icon" aria-hidden>
                <Icon className="size-5" />
              </span>
              <span className="flex min-w-0 flex-col">
                <span className="ui-standards-dialog-sample__label">{sample.label}</span>
                <span className="ui-standards-dialog-sample__caption">{sample.caption}</span>
              </span>
            </button>
          );
        })}
      </div>

      <ExitsModal
        open={isOpen("alert")}
        onOpenChange={(next) => !next && close()}
        title="Backup complete"
        size="sm"
        testId="ui-standard-dialog-alert"
        footer={
          <Button type="button" intent="info" appearance="solid" onClick={close}>
            Got it
          </Button>
        }
      >
        <DialogMessage intent="info" showIcon>
          Your records were saved to this device.
        </DialogMessage>
      </ExitsModal>

      <ExitsModal
        open={isOpen("confirm")}
        onOpenChange={(next) => !next && close()}
        title="Send reminder?"
        description="Juan will get a notification."
        testId="ui-standard-dialog-confirm"
        footer={
          <>
            <Button type="button" intent="neutral" appearance="outline" onClick={close}>
              Cancel
            </Button>
            <Button type="button" intent="primary" appearance="solid" onClick={close}>
              Continue
            </Button>
          </>
        }
      >
        <DialogMessage intent="primary" showIcon>
          A reminder about the ₱1,250.00 balance will be sent now.
        </DialogMessage>
      </ExitsModal>

      <ExitsModal
        open={isOpen("delete")}
        onOpenChange={(next) => !next && close()}
        title="Delete this record?"
        testId="ui-standard-dialog-delete"
        footer={
          <>
            <Button type="button" intent="neutral" appearance="outline" onClick={close}>
              Cancel
            </Button>
            <Button type="button" intent="danger" appearance="solid" onClick={close}>
              <Trash2 className="size-4" aria-hidden />
              Delete
            </Button>
          </>
        }
      >
        <DialogMessage intent="danger" showIcon>
          This cannot be undone.
        </DialogMessage>
      </ExitsModal>

      <ExitsModal
        open={isOpen("form")}
        onOpenChange={(next) => !next && close()}
        title="Rename person"
        testId="ui-standard-dialog-form"
        onSubmit={close}
        footer={
          <>
            <Button type="button" intent="neutral" appearance="outline" onClick={close}>
              Cancel
            </Button>
            <Button type="submit" intent="primary" appearance="solid">
              Save
            </Button>
          </>
        }
      >
        <Input
          id="ui-standard-dialog-form-name"
          label="Name"
          value={name}
          onChange={(event) => setName(event.target.value)}
          data-exits-modal-autofocus="true"
          data-testid="ui-standard-dialog-form-name"
        />
      </ExitsModal>

      <ExitsModal
        open={isOpen("scroll")}
        onOpenChange={(next) => !next && close()}
        title="Terms of use"
        fullHeightOnCompact
        size="lg"
        testId="ui-standard-dialog-scroll"
        footer={
          <Button type="button" intent="primary" appearance="solid" onClick={close}>
            I agree
          </Button>
        }
      >
        <div className="flex flex-col gap-3 text-[length:var(--exits-text-sm)] text-foreground">
          {Array.from({ length: 12 }, (_, index) => (
            <p key={index} className="m-0">
              {index + 1}. Records stay on your account. Shared records are visible to the person you share them
              with, and either of you can review changes before they count.
            </p>
          ))}
        </div>
      </ExitsModal>

      <ExitsModal
        open={isOpen("persistent")}
        onOpenChange={(next) => !next && close()}
        title="Finish setup"
        closeOnOutsideClick={false}
        closeOnEscape={false}
        testId="ui-standard-dialog-persistent"
        footer={
          <Button type="button" intent="warning" appearance="solid" onClick={close}>
            Done
          </Button>
        }
      >
        <DialogMessage intent="warning" showIcon>
          Clicking outside or pressing Esc does not close this dialog. Use Done or the close button.
        </DialogMessage>
      </ExitsModal>
    </div>
  );
}
