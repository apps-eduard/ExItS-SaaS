import { Check, ChevronDown } from "lucide-react";
import type { ReactNode } from "react";
import { DropdownMenu, MenuItem, useDismissibleOpen } from "@/components/ui/dropdown-menu";
import "@/features/ui-standards/org-enterprise-hybrid-glass/org-enterprise-hybrid-glass.css";
import {
  ORG_EHG_SELECT_GLASS_DEFAULT_APPEARANCE,
  ORG_EHG_SELECT_GLASS_DEFAULT_COMMAND,
  ORG_EHG_SELECT_GLASS_DEFAULT_MENU_TRANSITION,
  ORG_EHG_SELECT_GLASS_DEFAULT_SHAPE,
  ORG_EHG_SELECT_GLASS_DEFAULT_SIZE,
  ORG_EHG_SELECT_GLASS_DEFAULT_TONE,
} from "@/features/ui-standards/org-enterprise-hybrid-glass/org-ehg-select-glass-global-default";
import { cn } from "@/lib/cn";

export type ExitsOrgEhgSelectOption<T extends string = string> = {
  value: T;
  label: string;
};

export type ExitsOrgEhgSelectProps<T extends string = string> = {
  label: ReactNode;
  value: T | "";
  options: ReadonlyArray<ExitsOrgEhgSelectOption<T>>;
  onChange: (value: T | "") => void;
  placeholder?: string;
  disabled?: boolean;
  clearable?: boolean;
  testId?: string;
  className?: string;
};

/**
 * Product Org EHG select glass.
 * Locked: ext org-ehg select-glass qa primary stacked md icon-none shape-standard no-shadow
 */
export function ExitsOrgEhgSelect<T extends string = string>({
  label,
  value,
  options,
  onChange,
  placeholder,
  disabled = false,
  clearable = false,
  testId,
  className,
}: ExitsOrgEhgSelectProps<T>) {
  const menu = useDismissibleOpen(false);
  const selected = options.find((option) => option.value === value) ?? null;
  const hasSelection = Boolean(selected);
  const raised = hasSelection || menu.open;
  const labelText = typeof label === "string" ? label : undefined;

  const fieldShellClass = cn(
    "org-ehg-qa-glass",
    "org-ehg-qa-input-glass__field",
    "org-ehg-qa-select-glass__field",
    `org-ehg-qa-glass--tone-${ORG_EHG_SELECT_GLASS_DEFAULT_TONE}`,
    `org-ehg-qa-glass--size-${ORG_EHG_SELECT_GLASS_DEFAULT_SIZE}`,
    "org-ehg-qa-input-glass--density-balance",
    "org-ehg-qa-glass--no-shadow",
  );

  return (
    <div
      className={cn(
        "org-ehg-qa-input-glass org-ehg-qa-select-glass",
        `org-ehg-qa-glass--tone-${ORG_EHG_SELECT_GLASS_DEFAULT_TONE}`,
        className,
      )}
      data-shell="org-ehg"
      data-org-ehg="product"
      data-appearance={ORG_EHG_SELECT_GLASS_DEFAULT_APPEARANCE}
      data-size={ORG_EHG_SELECT_GLASS_DEFAULT_SIZE}
      data-card-shape={ORG_EHG_SELECT_GLASS_DEFAULT_SHAPE}
      data-density="balance"
      data-raised={raised ? "true" : "false"}
      data-open={menu.open ? "true" : "false"}
      data-has-icon="false"
      data-icon-look="none"
      data-recipe={ORG_EHG_SELECT_GLASS_DEFAULT_COMMAND}
      data-testid={testId ? `${testId}-field` : undefined}
    >
      <DropdownMenu
        open={menu.open}
        onOpenChange={(next) => {
          if (disabled) {
            return;
          }
          menu.setOpen(next);
        }}
        align="start"
        matchTriggerWidth
        menuGapPx={1}
        menuTransition={ORG_EHG_SELECT_GLASS_DEFAULT_MENU_TRANSITION}
        menuClassName={`org-ehg-glass-menu org-ehg-qa-select-glass-menu org-ehg-qa-glass--tone-${ORG_EHG_SELECT_GLASS_DEFAULT_TONE}`}
        menuLabel={labelText}
        className="org-ehg-qa-select-glass__menu-host"
        trigger={({ id, expanded, controls, onClick, onKeyDown }) => (
          <button
            type="button"
            id={id}
            className={fieldShellClass}
            data-card-shape={ORG_EHG_SELECT_GLASS_DEFAULT_SHAPE}
            aria-haspopup="menu"
            aria-expanded={expanded}
            aria-controls={controls}
            aria-label={labelText}
            disabled={disabled}
            data-testid={testId}
            onClick={onClick}
            onKeyDown={onKeyDown}
          >
            <span className="org-ehg-qa-input-glass__label">{label}</span>
            <span
              className={cn(
                "org-ehg-qa-input-glass__control org-ehg-qa-select-glass__value",
                !hasSelection && "org-ehg-qa-select-glass__value--placeholder",
              )}
              data-testid={testId ? `${testId}-value` : undefined}
            >
              {hasSelection ? selected!.label : placeholder?.trim() ? placeholder : "\u00a0"}
            </span>
            {clearable && hasSelection && !disabled ? (
              <span
                role="button"
                tabIndex={0}
                className="org-ehg-qa-input-glass__clear"
                aria-label="Clear"
                data-testid={testId ? `${testId}-clear` : undefined}
                onClick={(event) => {
                  event.preventDefault();
                  event.stopPropagation();
                  onChange("");
                  menu.close();
                }}
                onKeyDown={(event) => {
                  if (event.key === "Enter" || event.key === " ") {
                    event.preventDefault();
                    event.stopPropagation();
                    onChange("");
                    menu.close();
                  }
                }}
              >
                ×
              </span>
            ) : null}
            <ChevronDown
              className="org-ehg-qa-select-glass__chevron"
              aria-hidden
              strokeWidth={2.25}
            />
          </button>
        )}
      >
        <div
          className="org-ehg-glass-menu__list"
          role="listbox"
          data-testid={testId ? `${testId}-menu` : undefined}
        >
          {options.map((option) => {
            const selectedOption = option.value === value;
            return (
              <MenuItem
                key={option.value}
                className={cn(
                  "org-ehg-glass-menu__option org-ehg-qa-select-glass-menu__option",
                  selectedOption && "org-ehg-glass-menu__option--selected",
                )}
                data-selected={selectedOption ? "true" : "false"}
                data-testid={testId ? `${testId}-option-${option.value}` : undefined}
                onSelect={() => {
                  onChange(option.value);
                  menu.close();
                }}
              >
                <span className="org-ehg-qa-select-glass-menu__option-label">{option.label}</span>
                {selectedOption ? (
                  <Check
                    className="org-ehg-qa-select-glass-menu__check"
                    strokeWidth={2.5}
                    aria-hidden
                  />
                ) : (
                  <span className="org-ehg-qa-select-glass-menu__check" aria-hidden />
                )}
              </MenuItem>
            );
          })}
        </div>
      </DropdownMenu>
    </div>
  );
}
