import { useState, type KeyboardEventHandler, type ReactNode } from "react";
import {
  AlignCenter,
  AlignLeft,
  AlignRight,
  Bold,
  ChevronDown,
  Copy,
  EllipsisVertical,
  Italic,
  Pencil,
  Save,
  Share2,
  Trash2,
  Underline,
  type LucideIcon,
} from "lucide-react";

import { Button, type ButtonAppearance, type ButtonIntentTone } from "@/components/ui/button";
import { DropdownMenu, MenuItem, MenuSeparator } from "@/components/ui/dropdown-menu";
import { cn } from "@/lib/cn";

function Example({ label, testId, children }: { label: string; testId: string; children: ReactNode }) {
  return (
    <div className="ui-standards-q-example" data-testid={testId}>
      <div className="ui-standards-q-example__label">{label}</div>
      <div className="ui-standards-q-example__row">{children}</div>
    </div>
  );
}

/* ----------------------------------- Button group ----------------------------------- */

type GroupStyle = {
  label: string;
  id: string;
  intent: ButtonIntentTone;
  appearance: ButtonAppearance;
  groupClassName?: string;
};

const GROUP_STYLES: readonly GroupStyle[] = [
  { label: "Solid", id: "solid", intent: "primary", appearance: "solid" },
  { label: "Outline", id: "outline", intent: "primary", appearance: "outline" },
  { label: "Ghost", id: "ghost", intent: "primary", appearance: "ghost" },
  { label: "Raised", id: "raised", intent: "primary", appearance: "elevated", groupClassName: "exits-btn-group--raised" },
  { label: "Rounded", id: "rounded", intent: "primary", appearance: "solid", groupClassName: "exits-btn-group--pill" },
];

function ButtonGroup({
  children,
  className,
  label,
  testId,
}: {
  children: ReactNode;
  className?: string;
  label: string;
  testId: string;
}) {
  return (
    <div role="group" aria-label={label} className={cn("exits-btn-group", className)} data-testid={testId}>
      {children}
    </div>
  );
}

const ALIGN_OPTIONS: ReadonlyArray<{ id: string; label: string; icon: LucideIcon }> = [
  { id: "left", label: "Align left", icon: AlignLeft },
  { id: "center", label: "Align center", icon: AlignCenter },
  { id: "right", label: "Align right", icon: AlignRight },
];

export function UiStandardsButtonGroupGallery(): ReactNode {
  const [align, setAlign] = useState("left");
  const [marks, setMarks] = useState<string[]>(["bold"]);
  const toggleMark = (mark: string) =>
    setMarks((current) => (current.includes(mark) ? current.filter((m) => m !== mark) : [...current, mark]));

  return (
    <div className="flex flex-col gap-4" data-testid="ui-standard-button-group-gallery">
      {GROUP_STYLES.map((style) => (
        <Example key={style.id} label={style.label} testId={`ui-standard-button-group-${style.id}`}>
          <ButtonGroup label={`${style.label} group`} className={style.groupClassName} testId={`ui-standard-button-group-${style.id}-group`}>
            {["One", "Two", "Three"].map((text) => (
              <Button key={text} type="button" intent={style.intent} appearance={style.appearance} emphasis="strong">
                {text}
              </Button>
            ))}
          </ButtonGroup>
        </Example>
      ))}
      <Example label="Spread (full width)" testId="ui-standard-button-group-spread">
        <ButtonGroup label="Spread group" className="exits-btn-group--spread" testId="ui-standard-button-group-spread-group">
          {["Day", "Week", "Month"].map((text) => (
            <Button key={text} type="button" intent="primary" appearance="outline">
              {text}
            </Button>
          ))}
        </ButtonGroup>
      </Example>
      <Example label="Single choice (segmented)" testId="ui-standard-button-group-single">
        <ButtonGroup label="Text alignment" testId="ui-standard-button-group-align">
          {ALIGN_OPTIONS.map((option) => {
            const Icon = option.icon;
            const active = align === option.id;
            return (
              <Button
                key={option.id}
                type="button"
                size="icon"
                intent="primary"
                appearance={active ? "solid" : "outline"}
                emphasis="strong"
                aria-label={option.label}
                aria-pressed={active}
                data-testid={`ui-standard-button-group-align-${option.id}`}
                onClick={() => setAlign(option.id)}
              >
                <Icon className="size-4" aria-hidden />
              </Button>
            );
          })}
        </ButtonGroup>
      </Example>
      <Example label="Multiple choice (toggles)" testId="ui-standard-button-group-multi">
        <ButtonGroup label="Text style" testId="ui-standard-button-group-marks">
          {(
            [
              ["bold", "Bold", Bold],
              ["italic", "Italic", Italic],
              ["underline", "Underline", Underline],
            ] as const
          ).map(([id, label, Icon]) => {
            const active = marks.includes(id);
            return (
              <Button
                key={id}
                type="button"
                size="icon"
                intent="neutral"
                appearance={active ? "solid" : "outline"}
                emphasis="strong"
                aria-label={label}
                aria-pressed={active}
                data-testid={`ui-standard-button-group-mark-${id}`}
                onClick={() => toggleMark(id)}
              >
                <Icon className="size-4" aria-hidden />
              </Button>
            );
          })}
        </ButtonGroup>
      </Example>
    </div>
  );
}

/* ---------------------------------- Button dropdown --------------------------------- */

type DropdownAction = { id: string; label: string; icon?: LucideIcon; destructive?: boolean; disabled?: boolean };

const RECORD_ACTIONS: readonly (DropdownAction | "separator")[] = [
  { id: "edit", label: "Edit", icon: Pencil },
  { id: "duplicate", label: "Duplicate", icon: Copy },
  { id: "share", label: "Share", icon: Share2, disabled: true },
  "separator",
  { id: "delete", label: "Delete", icon: Trash2, destructive: true },
];

function ActionMenu({
  actions,
  onSelect,
  close,
}: {
  actions: readonly (DropdownAction | "separator")[];
  onSelect: (label: string) => void;
  close: () => void;
}) {
  return (
    <>
      {actions.map((action, index) =>
        action === "separator" ? (
          <MenuSeparator key={`sep-${index}`} />
        ) : (
          <MenuItem
            key={action.id}
            destructive={action.destructive}
            disabled={action.disabled}
            data-testid={`ui-standard-button-dropdown-item-${action.id}`}
            onSelect={() => {
              onSelect(action.label);
              close();
            }}
          >
            {action.icon ? <action.icon className="size-4 shrink-0" aria-hidden /> : null}
            {action.label}
          </MenuItem>
        ),
      )}
    </>
  );
}

function SampleDropdown({
  testId,
  menuLabel,
  actions,
  onSelect,
  align = "start",
  renderTrigger,
}: {
  testId: string;
  menuLabel: string;
  actions: readonly (DropdownAction | "separator")[];
  onSelect: (label: string) => void;
  align?: "start" | "end";
  renderTrigger: (props: {
    id: string;
    expanded: boolean;
    controls: string;
    onClick: () => void;
    onKeyDown: KeyboardEventHandler<HTMLButtonElement>;
  }) => ReactNode;
}) {
  const [open, setOpen] = useState(false);
  return (
    <DropdownMenu
      open={open}
      onOpenChange={setOpen}
      align={align}
      menuLabel={menuLabel}
      trigger={renderTrigger}
      menuClassName="min-w-[11rem]"
    >
      <div data-testid={`${testId}-menu`}>
        <ActionMenu actions={actions} onSelect={onSelect} close={() => setOpen(false)} />
      </div>
    </DropdownMenu>
  );
}

export function UiStandardsButtonDropdownGallery(): ReactNode {
  const [last, setLast] = useState<string | null>(null);

  return (
    <div className="flex flex-col gap-4" data-testid="ui-standard-button-dropdown-gallery">
      <Example label="Basic" testId="ui-standard-button-dropdown-basic">
        <SampleDropdown
          testId="ui-standard-button-dropdown-basic"
          menuLabel="Record actions"
          actions={RECORD_ACTIONS}
          onSelect={setLast}
          renderTrigger={(props) => (
            <Button
              type="button"
              intent="primary"
              appearance="solid"
              emphasis="strong"
              id={props.id}
              aria-haspopup="menu"
              aria-expanded={props.expanded}
              aria-controls={props.controls}
              onClick={props.onClick}
              onKeyDown={props.onKeyDown}
              data-testid="ui-standard-button-dropdown-basic-trigger"
            >
              Actions
              <ChevronDown className={cn("size-4 transition-transform", props.expanded && "rotate-180")} aria-hidden />
            </Button>
          )}
        />
        <SampleDropdown
          testId="ui-standard-button-dropdown-outline"
          menuLabel="Record actions"
          actions={RECORD_ACTIONS}
          onSelect={setLast}
          renderTrigger={(props) => (
            <Button
              type="button"
              intent="primary"
              appearance="outline"
              id={props.id}
              aria-haspopup="menu"
              aria-expanded={props.expanded}
              aria-controls={props.controls}
              onClick={props.onClick}
              onKeyDown={props.onKeyDown}
            >
              Outline
              <ChevronDown className={cn("size-4 transition-transform", props.expanded && "rotate-180")} aria-hidden />
            </Button>
          )}
        />
      </Example>
      <Example label="Split button" testId="ui-standard-button-dropdown-split">
        <div className="exits-btn-group" role="group" aria-label="Save options">
          <Button
            type="button"
            intent="primary"
            appearance="solid"
            emphasis="strong"
            data-testid="ui-standard-button-dropdown-split-main"
            onClick={() => setLast("Save")}
          >
            <Save className="size-4" aria-hidden />
            Save
          </Button>
          <SampleDropdown
            testId="ui-standard-button-dropdown-split"
            menuLabel="More save options"
            align="end"
            actions={[
              { id: "save-draft", label: "Save as draft" },
              { id: "save-new", label: "Save and new" },
            ]}
            onSelect={setLast}
            renderTrigger={(props) => (
              <Button
                type="button"
                size="icon"
                intent="primary"
                appearance="solid"
                emphasis="strong"
                aria-label="More save options"
                id={props.id}
                aria-haspopup="menu"
                aria-expanded={props.expanded}
                aria-controls={props.controls}
                onClick={props.onClick}
                onKeyDown={props.onKeyDown}
                data-testid="ui-standard-button-dropdown-split-trigger"
              >
                <ChevronDown className="size-4" aria-hidden />
              </Button>
            )}
          />
        </div>
      </Example>
      <Example label="Icon (more actions)" testId="ui-standard-button-dropdown-icon">
        <SampleDropdown
          testId="ui-standard-button-dropdown-icon"
          menuLabel="More actions"
          actions={RECORD_ACTIONS}
          onSelect={setLast}
          renderTrigger={(props) => (
            <Button
              type="button"
              size="icon"
              intent="neutral"
              appearance="ghost"
              aria-label="More actions"
              id={props.id}
              aria-haspopup="menu"
              aria-expanded={props.expanded}
              aria-controls={props.controls}
              onClick={props.onClick}
              onKeyDown={props.onKeyDown}
            >
              <EllipsisVertical className="size-4" aria-hidden />
            </Button>
          )}
        />
        <Button type="button" intent="primary" appearance="outline" disabled>
          Disabled
          <ChevronDown className="size-4" aria-hidden />
        </Button>
      </Example>
      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted" data-testid="ui-standard-button-dropdown-last">
        {last ? `Last action: ${last}` : "Choose an action from any menu."}
      </p>
    </div>
  );
}
