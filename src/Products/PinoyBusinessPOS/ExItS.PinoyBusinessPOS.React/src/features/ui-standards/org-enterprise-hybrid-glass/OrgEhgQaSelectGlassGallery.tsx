import { useState, type ReactNode } from "react";
import { ChevronDown, ListFilter, Mail, UserRound, type LucideIcon } from "lucide-react";
import { DropdownMenu, MenuItem, useDismissibleOpen } from "@/components/ui/dropdown-menu";
import { OrgEhgQaSelectGlassPlayground } from "@/features/ui-standards/org-enterprise-hybrid-glass/OrgEhgQaSelectGlassPlayground";
import type {
  OrgEhgQaGlassIconShape,
  OrgEhgQaGlassSize,
  OrgEhgQaGlassTone,
} from "@/features/ui-standards/org-enterprise-hybrid-glass/OrgEhgQuickActionGlassGallery";

type SelectOption = { label: string; value: string };

const SAMPLE_OPTIONS: ReadonlyArray<SelectOption> = [
  { label: "Option A", value: "a" },
  { label: "Option B", value: "b" },
  { label: "Option C", value: "c" },
];

function QaSelect({
  label,
  tone = "primary",
  icon: Icon,
  iconShape = "soft",
  size = "md",
  ghost = false,
  placeholder = "Choose…",
  defaultValue = "",
  caption,
  testId,
  menuTheme,
}: {
  label: string;
  tone?: OrgEhgQaGlassTone;
  icon?: LucideIcon;
  iconShape?: OrgEhgQaGlassIconShape;
  size?: OrgEhgQaGlassSize;
  ghost?: boolean;
  placeholder?: string;
  defaultValue?: string;
  caption?: string;
  testId?: string;
  menuTheme: "light" | "dark";
}) {
  const menu = useDismissibleOpen(false);
  const [value, setValue] = useState(defaultValue);
  const selected = SAMPLE_OPTIONS.find((option) => option.value === value);
  const raised = Boolean(selected) || menu.open;
  const fieldShellClass = [
    "org-ehg-qa-glass",
    "org-ehg-qa-input-glass__field",
    "org-ehg-qa-select-glass__field",
    "org-ehg-qa-input-glass--density-balance",
    `org-ehg-qa-glass--tone-${tone}`,
    `org-ehg-qa-glass--size-${size}`,
    ghost ? "org-ehg-qa-glass--ghost" : "",
  ]
    .filter(Boolean)
    .join(" ");

  return (
    <div className="org-ehg-qa-glass-sample">
      <div
        className={`org-ehg-qa-input-glass org-ehg-qa-select-glass org-ehg-qa-glass--tone-${tone}`}
        data-testid={testId}
        data-raised={raised ? "true" : "false"}
        data-open={menu.open ? "true" : "false"}
        data-has-icon={Icon ? "true" : "false"}
        data-icon-look={Icon ? "chip" : "none"}
        data-appearance="outlined"
      >
        <DropdownMenu
          open={menu.open}
          onOpenChange={menu.setOpen}
          align="start"
          matchTriggerWidth
          menuGapPx={1}
          menuTransition="scale"
          menuClassName={
            `org-ehg-glass-menu org-ehg-qa-select-glass-menu org-ehg-qa-glass--tone-${tone}` +
            (menuTheme === "dark" ? " org-ehg-glass-menu--dark" : "")
          }
          menuLabel={label}
          className="org-ehg-qa-select-glass__menu-host"
          trigger={({ id, expanded, controls, onClick, onKeyDown }) => (
            <button
              type="button"
              id={id}
              className={fieldShellClass}
              aria-haspopup="menu"
              aria-expanded={expanded}
              aria-controls={controls}
              aria-label={label}
              onClick={onClick}
              onKeyDown={onKeyDown}
            >
              {Icon ? (
                <span
                  className={
                    "org-ehg-qa-glass__icon" +
                    (iconShape === "rounded" ? " org-ehg-qa-glass__icon--rounded" : "")
                  }
                  aria-hidden="true"
                >
                  <Icon className="org-ehg-qa-glass__glyph" strokeWidth={2.25} />
                </span>
              ) : null}
              <span className="org-ehg-qa-input-glass__label">{label}</span>
              <span
                className={
                  "org-ehg-qa-input-glass__control org-ehg-qa-select-glass__value" +
                  (selected ? "" : " org-ehg-qa-select-glass__value--placeholder")
                }
              >
                {selected
                  ? selected.label
                  : /* Idle: floating label is the prompt — placeholder only when open. */
                    raised
                    ? placeholder
                    : "\u00a0"}
              </span>
              {value ? (
                <span
                  role="button"
                  tabIndex={0}
                  className="org-ehg-qa-input-glass__clear"
                  aria-label="Clear"
                  onClick={(event) => {
                    event.preventDefault();
                    event.stopPropagation();
                    setValue("");
                    menu.close();
                  }}
                  onKeyDown={(event) => {
                    if (event.key === "Enter" || event.key === " ") {
                      event.preventDefault();
                      event.stopPropagation();
                      setValue("");
                      menu.close();
                    }
                  }}
                >
                  ×
                </span>
              ) : null}
              <ChevronDown className="org-ehg-qa-select-glass__chevron" aria-hidden strokeWidth={2.25} />
            </button>
          )}
        >
          <div className="org-ehg-glass-menu__list" role="group">
            {SAMPLE_OPTIONS.map((option) => {
              const selectedOption = option.value === value;
              return (
                <MenuItem
                  key={option.value}
                  className={
                    "org-ehg-glass-menu__option" +
                    (selectedOption ? " org-ehg-glass-menu__option--selected" : "")
                  }
                  data-selected={selectedOption ? "true" : "false"}
                  onSelect={() => {
                    setValue(option.value);
                    menu.close();
                  }}
                >
                  {option.label}
                </MenuItem>
              );
            })}
          </div>
        </DropdownMenu>
      </div>
      {caption ? <span className="org-ehg-qa-glass-sample__caption">{caption}</span> : null}
    </div>
  );
}

function Section({ title, children }: { title: string; children: ReactNode }) {
  return (
    <section className="org-ehg-qa-glass-panel__section">
      <h3 className="org-ehg-qa-glass-panel__section-title">{title}</h3>
      <div className="org-ehg-qa-glass-panel__row">{children}</div>
    </section>
  );
}

function ModePanel({
  theme,
  title,
  testId,
}: {
  theme: "light" | "dark";
  title: string;
  testId: string;
}) {
  return (
    <div
      className="org-ehg org-ehg-qa-glass-panel"
      data-org-ehg-theme={theme}
      data-testid={testId}
    >
      <header className="org-ehg-qa-glass-panel__header">
        <h2 className="org-ehg-qa-glass-panel__title">{title} – Select Glass</h2>
      </header>

      <Section title="1. Tones">
        <QaSelect label="Primary" tone="primary" icon={ListFilter} caption="Primary" menuTheme={theme} />
        <QaSelect label="Success" tone="success" icon={Mail} caption="Success" menuTheme={theme} />
        <QaSelect label="Danger" tone="danger" icon={UserRound} caption="Danger" menuTheme={theme} />
        <QaSelect label="Info" tone="info" icon={ListFilter} caption="Info" menuTheme={theme} />
        <QaSelect label="People" tone="people" icon={UserRound} caption="People" menuTheme={theme} />
        <QaSelect label="Neutral" tone="neutral" icon={ListFilter} caption="Neutral" menuTheme={theme} />
      </Section>

      <Section title="2. Sizes">
        <QaSelect
          label="Small"
          tone="primary"
          icon={ListFilter}
          size="sm"
          caption="SM"
          testId={`${testId}-size-sm`}
          menuTheme={theme}
        />
        <QaSelect
          label="Medium"
          tone="primary"
          icon={ListFilter}
          size="md"
          caption="MD"
          testId={`${testId}-size-md`}
          menuTheme={theme}
        />
        <QaSelect
          label="Large"
          tone="primary"
          icon={ListFilter}
          size="lg"
          caption="LG"
          testId={`${testId}-size-lg`}
          menuTheme={theme}
        />
      </Section>

      <Section title="3. Variants">
        <QaSelect label="Default" tone="primary" icon={ListFilter} caption="Default" menuTheme={theme} />
        <QaSelect label="Ghost" tone="primary" icon={ListFilter} ghost caption="Ghost" menuTheme={theme} />
        <QaSelect label="No icon" tone="secondary" caption="Text only" menuTheme={theme} />
        <QaSelect
          label="Rounded chip"
          tone="cyan"
          icon={Mail}
          iconShape="rounded"
          caption="Rounded icon"
          menuTheme={theme}
        />
        <QaSelect
          label="Filled"
          tone="primary"
          icon={ListFilter}
          defaultValue="a"
          caption="With value"
          menuTheme={theme}
        />
      </Section>
    </div>
  );
}

/**
 * Select glass gallery — same Enterprise Hybrid Glass recipe as Input glass.
 * Personal Glass components sidenav. Organization intensity select stays under Input glass.
 */
export function OrgEhgQaSelectGlassGallery() {
  return (
    <div className="org-ehg-qa-glass-gallery" data-testid="ui-standard-org-ehg-qa-select-glass">
      <p className="org-ehg__banner m-0">
        <strong>Enterprise Hybrid Glass · Select Glass</strong>
        {" — "}
        Same glass recipe as Input glass (tones, soft icon chip, card shape, floating label, light/dark)
        with a dropdown trigger and glass menu.
      </p>
      <OrgEhgQaSelectGlassPlayground />
      <div className="org-ehg-qa-glass-gallery__columns">
        <ModePanel theme="light" title="Light Mode" testId="org-ehg-qa-select-glass-light" />
        <ModePanel theme="dark" title="Dark Mode" testId="org-ehg-qa-select-glass-dark" />
      </div>
    </div>
  );
}
