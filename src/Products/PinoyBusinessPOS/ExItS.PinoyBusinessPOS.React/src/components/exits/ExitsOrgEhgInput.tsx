import { Search } from "lucide-react";
import type { InputHTMLAttributes, ReactNode } from "react";
import "@/features/ui-standards/org-enterprise-hybrid-glass/org-enterprise-hybrid-glass.css";
import {
  ORG_EHG_INPUT_GLASS_DEFAULT_APPEARANCE,
  ORG_EHG_INPUT_GLASS_DEFAULT_COMMAND,
  ORG_EHG_INPUT_GLASS_DEFAULT_SHAPE,
  ORG_EHG_INPUT_GLASS_DEFAULT_SIZE,
  ORG_EHG_INPUT_GLASS_DEFAULT_TONE,
  ORG_EHG_SEARCH_GLASS_DEFAULT_COMMAND,
  ORG_EHG_SEARCH_GLASS_DEFAULT_ICON,
  ORG_EHG_SEARCH_GLASS_DEFAULT_ICON_ID,
} from "@/features/ui-standards/org-enterprise-hybrid-glass/org-ehg-input-glass-global-default";
import { cn } from "@/lib/cn";

export type ExitsOrgEhgInputVariant = "input" | "search";

export type ExitsOrgEhgInputProps = Omit<
  InputHTMLAttributes<HTMLInputElement>,
  "value" | "onChange" | "size"
> & {
  label: ReactNode;
  value: string;
  onChange: (value: string) => void;
  testId?: string;
  clearable?: boolean;
  /**
   * `input` — ext org-ehg input-glass qa primary stacked md icon-none shape-standard no-shadow
   * `search` — ext org-ehg input-glass qa primary stacked md icon-bare shape-standard no-shadow
   */
  variant?: ExitsOrgEhgInputVariant;
};

/**
 * Product Org EHG input / search glass.
 * Input (default): ext org-ehg input-glass qa primary stacked md icon-none shape-standard no-shadow
 * Search: ext org-ehg input-glass qa primary stacked md icon-bare shape-standard no-shadow
 *
 * Search variant always uses `type="text"` (not native search) so the browser
 * does not paint a second clear beside the locked Org EHG clear mark.
 */
export function ExitsOrgEhgInput({
  label,
  value,
  onChange,
  testId,
  clearable = false,
  variant = "input",
  className,
  id,
  disabled,
  required,
  type,
  inputMode,
  autoComplete,
  ...props
}: ExitsOrgEhgInputProps) {
  const isSearch = variant === "search";
  const raised = value.trim().length > 0;
  const fieldId = id ?? (typeof testId === "string" ? testId : undefined);
  const recipe = isSearch
    ? ORG_EHG_SEARCH_GLASS_DEFAULT_COMMAND
    : ORG_EHG_INPUT_GLASS_DEFAULT_COMMAND;
  const inputType = isSearch ? "text" : (type ?? "text");

  return (
    <label
      className={cn(
        "org-ehg-qa-input-glass",
        `org-ehg-qa-glass--tone-${ORG_EHG_INPUT_GLASS_DEFAULT_TONE}`,
        className,
      )}
      data-shell="org-ehg"
      data-org-ehg="product"
      data-appearance={ORG_EHG_INPUT_GLASS_DEFAULT_APPEARANCE}
      data-size={ORG_EHG_INPUT_GLASS_DEFAULT_SIZE}
      data-card-shape={ORG_EHG_INPUT_GLASS_DEFAULT_SHAPE}
      data-density="balance"
      data-raised={raised ? "true" : "false"}
      data-has-icon={isSearch ? "true" : "false"}
      data-icon-look={isSearch ? "bare" : "none"}
      data-icon-id={isSearch ? ORG_EHG_SEARCH_GLASS_DEFAULT_ICON_ID : undefined}
      data-icon={isSearch ? ORG_EHG_SEARCH_GLASS_DEFAULT_ICON : "icon-none"}
      data-recipe={recipe}
      data-testid={testId ? `${testId}-field` : undefined}
    >
      <span
        className={cn(
          "org-ehg-qa-glass",
          "org-ehg-qa-input-glass__field",
          `org-ehg-qa-glass--tone-${ORG_EHG_INPUT_GLASS_DEFAULT_TONE}`,
          `org-ehg-qa-glass--size-${ORG_EHG_INPUT_GLASS_DEFAULT_SIZE}`,
          "org-ehg-qa-input-glass--density-balance",
          "org-ehg-qa-glass--no-shadow",
        )}
        data-card-shape={ORG_EHG_INPUT_GLASS_DEFAULT_SHAPE}
        data-hover="off"
      >
        {isSearch ? (
          <Search
            className="org-ehg-qa-glass__glyph org-ehg-qa-glass__glyph--bare"
            strokeWidth={2.25}
            aria-hidden
          />
        ) : null}
        <span className="org-ehg-qa-input-glass__label">{label}</span>
        <input
          id={fieldId}
          className="org-ehg-qa-input-glass__control"
          value={value}
          type={inputType}
          inputMode={inputMode ?? (isSearch ? "search" : undefined)}
          autoComplete={autoComplete ?? (isSearch ? "off" : undefined)}
          placeholder=" "
          disabled={disabled}
          required={required}
          aria-label={typeof label === "string" ? label : undefined}
          data-testid={testId}
          onChange={(event) => onChange(event.target.value)}
          {...props}
        />
        {clearable && value && !disabled ? (
          <button
            type="button"
            className="org-ehg-qa-input-glass__clear"
            aria-label="Clear"
            data-testid={testId ? `${testId}-clear` : undefined}
            onClick={() => onChange("")}
          >
            ×
          </button>
        ) : null}
      </span>
    </label>
  );
}
