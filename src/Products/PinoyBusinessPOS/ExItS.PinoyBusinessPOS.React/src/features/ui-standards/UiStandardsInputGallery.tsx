import { useState, type CSSProperties, type ReactNode } from "react";
import { Eye, EyeOff, type LucideIcon } from "lucide-react";
import {
  INTENT_GLOBAL_AMBER,
  INTENT_GLOBAL_BLACK,
  INTENT_GLOBAL_DANGER,
  INTENT_GLOBAL_HELP,
  INTENT_GLOBAL_INFO,
  INTENT_GLOBAL_PRIMARY,
  INTENT_GLOBAL_SECONDARY,
  INTENT_GLOBAL_STANDARD,
  INTENT_GLOBAL_SUCCESS,
  INTENT_GLOBAL_WARN,
  INTENT_GLOBAL_WARN_FOREGROUND,
} from "@/features/ui-standards/intent-color-global-default";

/**
 * Locked ExItS Input samples. Visual authority for `ext input`.
 * Existing product fields stay on shared Input until a field is explicitly migrated.
 * @see Docs/UI/exits-input-standard.md
 */
export type InputSample = {
  id: string;
  label: string;
  bg: string;
  fg: string;
};

export const INPUT_SAMPLES: readonly InputSample[] = [
  { id: "standard", label: "Standard", bg: INTENT_GLOBAL_STANDARD, fg: "#000000" },
  { id: "primary", label: "Primary", bg: INTENT_GLOBAL_PRIMARY, fg: "#ffffff" },
  { id: "secondary", label: "Secondary", bg: INTENT_GLOBAL_SECONDARY, fg: "#ffffff" },
  { id: "success", label: "Success", bg: INTENT_GLOBAL_SUCCESS, fg: "#ffffff" },
  { id: "info", label: "Info", bg: INTENT_GLOBAL_INFO, fg: "#ffffff" },
  { id: "amber", label: "Amber", bg: INTENT_GLOBAL_AMBER, fg: "#000000" },
  { id: "danger", label: "Danger", bg: INTENT_GLOBAL_DANGER, fg: "#ffffff" },
  { id: "warn", label: "Warn", bg: INTENT_GLOBAL_WARN, fg: INTENT_GLOBAL_WARN_FOREGROUND },
  { id: "help", label: "Help", bg: INTENT_GLOBAL_HELP, fg: "#ffffff" },
  { id: "black", label: "Black", bg: INTENT_GLOBAL_BLACK, fg: "#ffffff" },
];

export type InputAppearance = "standard" | "soft" | "soft-outline" | "filled" | "outlined" | "standout";
export type InputShape = "square" | "standard" | "soft" | "pill";

export function inputAccent(sample: Pick<InputSample, "bg">): string {
  return sample.bg.toLowerCase() === "#ffffff" ? "#000000" : sample.bg;
}

function InputExampleCard({
  title,
  testId,
  appearance,
  shape = "standard",
}: {
  title: string;
  testId: string;
  appearance: InputAppearance;
  shape?: InputShape;
}) {
  return (
    <div className="ui-standards-q-example" data-testid={testId}>
      <div className="ui-standards-q-example__label">{title}</div>
      <div className="ui-standards-q-example__row">
        {INPUT_SAMPLES.map((sample) => (
          <UiStandardsQinput
            key={sample.id}
            sampleId={sample.id}
            label={sample.label}
            accent={inputAccent(sample)}
            fill={sample.bg}
            ink={sample.fg}
            appearance={appearance}
            shape={shape}
            testId={`${testId}-${sample.id}`}
          />
        ))}
      </div>
    </div>
  );
}

export function UiStandardsQinput({
  sampleId,
  label,
  value,
  onChange,
  accent,
  fill,
  ink,
  appearance,
  shape = "standard",
  disabled = false,
  invalid = false,
  clearable = false,
  required = false,
  onClear,
  icon,
  placeholder,
  type = "text",
  suffix,
  hint,
  errorMessage,
  testId,
  inputTestId,
}: {
  sampleId?: string;
  label: string;
  value?: string;
  onChange?: (value: string) => void;
  accent: string;
  fill?: string;
  ink?: string;
  appearance: InputAppearance;
  shape?: InputShape;
  disabled?: boolean;
  invalid?: boolean;
  clearable?: boolean;
  required?: boolean;
  onClear?: () => void;
  icon?: LucideIcon | null;
  placeholder?: string;
  type?: string;
  suffix?: ReactNode;
  hint?: string;
  errorMessage?: string;
  testId?: string;
  inputTestId?: string;
}) {
  const Icon = icon ?? null;
  const picker = type === "date" || type === "time";
  const area = type === "textarea";
  const darkInk = sampleId === "standard" || sampleId === "black";
  const style: CSSProperties & { "--qinput-accent"?: string; "--qinput-fill"?: string; "--qinput-ink"?: string } = {
    "--qinput-accent": accent,
    "--qinput-fill": fill ?? accent,
    "--qinput-ink": ink ?? "#ffffff",
  };
  const shown = value ?? "";
  return (
    <div
      className={
        "ui-standards-qinput" +
        ` ui-standards-qinput--${appearance}` +
        (shape === "pill" ? " ui-standards-qinput--pill" : "") +
        (shape === "soft" ? " ui-standards-qinput--shape-soft" : "") +
        (shape === "square" ? " ui-standards-qinput--square" : "") +
        (invalid ? " ui-standards-qinput--error" : "") +
        (disabled ? " ui-standards-qinput--disabled" : "") +
        (darkInk ? " ui-standards-qinput--dark-ink" : "") +
        (Icon ? " ui-standards-qinput--has-icon" : "") +
        (clearable ? " ui-standards-qinput--clearable" : "") +
        (required ? " ui-standards-qinput--required" : "") +
        (picker ? " ui-standards-qinput--picker" : "") +
        (area ? " ui-standards-qinput--area" : "")
      }
      style={style}
      data-testid={testId}
      data-appearance={appearance}
      data-shape={shape}
    >
      <span className="ui-standards-qinput__box">
        {Icon ? <Icon className="ui-standards-qinput__icon" aria-hidden strokeWidth={2} /> : null}
        {type === "textarea" ? (
          <textarea
            className="ui-standards-qinput__control ui-standards-qinput__control--area"
            value={onChange ? shown : undefined}
            defaultValue={onChange ? undefined : ""}
            placeholder={placeholder ?? " "}
            disabled={disabled}
            aria-invalid={invalid || undefined}
            aria-required={required || undefined}
            aria-label={label}
            data-testid={inputTestId}
            rows={3}
            onChange={onChange ? (event) => onChange(event.target.value) : undefined}
          />
        ) : (
          <input
            className="ui-standards-qinput__control"
            type={type}
            value={onChange ? shown : undefined}
            defaultValue={onChange ? undefined : ""}
            placeholder={placeholder ?? " "}
            disabled={disabled}
            aria-invalid={invalid || undefined}
            aria-required={required || undefined}
            aria-label={label}
            data-testid={inputTestId}
            onChange={onChange ? (event) => onChange(event.target.value) : undefined}
          />
        )}
        <span className="ui-standards-qinput__label">
          {label}
          {required ? (
            <span className="ui-standards-qinput__required" aria-hidden>
              *
            </span>
          ) : null}
        </span>
        {suffix}
        {clearable && shown.trim() && !disabled ? (
          <button
            type="button"
            className="ui-standards-qinput__clear"
            aria-label={`Clear ${label}`}
            data-testid={testId ? `${testId}-clear` : undefined}
            onClick={(event) => {
              event.stopPropagation();
              onClear?.();
            }}
          >
            ×
          </button>
        ) : null}
      </span>
      {invalid ? (
        <span className="ui-standards-qinput__error">{errorMessage ?? "Enter a valid value."}</span>
      ) : hint ? (
        <span className="ui-standards-qinput__hint">{hint}</span>
      ) : null}
    </div>
  );
}

const INPUT_TYPE_FIELDS = [
  { id: "text", label: "Text", type: "text" },
  { id: "number", label: "Number", type: "number" },
  { id: "search", label: "Search", type: "search" },
  { id: "url", label: "URL", type: "url" },
  { id: "date", label: "Date", type: "date" },
  { id: "time", label: "Time", type: "time" },
  { id: "textarea", label: "Textarea", type: "textarea" },
] as const;

const EMAIL_SAMPLE = "name@email.com";
const LOCAL_MOBILE_HINT = "09XX XXX XXXX";
const INTL_MOBILE_HINT = "+63 9XX XXX XXXX";

function formatLocalMobile(value: string): string {
  let digits = value.replace(/\D/g, "").slice(0, 11);
  if (digits.length > 0 && !digits.startsWith("0")) {
    digits = `0${digits}`.slice(0, 11);
  }
  return [digits.slice(0, 4), digits.slice(4, 7), digits.slice(7, 11)].filter((part) => part.length > 0).join(" ");
}

function formatIntlMobile(value: string): string {
  let digits = value.replace(/\D/g, "");
  if (digits.startsWith("63")) digits = digits.slice(2);
  if (digits.startsWith("0")) digits = digits.slice(1);
  digits = digits.slice(0, 10);
  if (!digits) return "";
  const grouped = [digits.slice(0, 3), digits.slice(3, 6), digits.slice(6, 10)].filter((part) => part.length > 0).join(" ");
  return `+63 ${grouped}`;
}

function isEmail(value: string): boolean {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value);
}

function PasswordTypeField({
  accent,
  fill,
  ink,
}: {
  accent: string;
  fill: string;
  ink: string;
}) {
  const [value, setValue] = useState("");
  const [visible, setVisible] = useState(false);
  return (
    <UiStandardsQinput
      sampleId="primary"
      label="Password"
      accent={accent}
      fill={fill}
      ink={ink}
      appearance="standard"
      type={visible ? "text" : "password"}
      value={value}
      onChange={setValue}
      testId="ui-standard-input-type-password"
      suffix={
        <button
          type="button"
          className="ui-standards-qinput__eye"
          aria-label={visible ? "Hide password" : "Show password"}
          data-testid="ui-standard-input-type-password-toggle"
          onClick={() => setVisible((current) => !current)}
        >
          {visible ? <EyeOff className="size-4" aria-hidden /> : <Eye className="size-4" aria-hidden />}
        </button>
      }
    />
  );
}

function EmailTypeField({
  accent,
  fill,
  ink,
}: {
  accent: string;
  fill: string;
  ink: string;
}) {
  const [value, setValue] = useState("");
  const invalid = value.trim().length > 0 && !isEmail(value.trim());
  return (
    <UiStandardsQinput
      sampleId="primary"
      label="Email"
      accent={accent}
      fill={fill}
      ink={ink}
      appearance="standard"
      type="email"
      value={value}
      onChange={setValue}
      invalid={invalid}
      errorMessage="Enter a valid email address."
      hint={`Sample: ${EMAIL_SAMPLE}`}
      testId="ui-standard-input-type-email"
    />
  );
}

function MobileTypeField({
  kind,
  accent,
  fill,
  ink,
}: {
  kind: "international" | "local";
  accent: string;
  fill: string;
  ink: string;
}) {
  const [value, setValue] = useState("");
  const international = kind === "international";
  return (
    <UiStandardsQinput
      sampleId="primary"
      label={international ? "Mobile international" : "Mobile local"}
      accent={accent}
      fill={fill}
      ink={ink}
      appearance="standard"
      type="tel"
      value={value}
      onChange={(next) => setValue(international ? formatIntlMobile(next) : formatLocalMobile(next))}
      hint={international ? INTL_MOBILE_HINT : LOCAL_MOBILE_HINT}
      testId={international ? "ui-standard-input-type-mobile-international" : "ui-standard-input-type-mobile-local"}
    />
  );
}

export function UiStandardsInputTypes(): ReactNode {
  const accent = inputAccent(INPUT_SAMPLES[1]);
  const fill = INPUT_SAMPLES[1].bg;
  const ink = INPUT_SAMPLES[1].fg;
  return (
    <div className="ui-standards-q-example" data-testid="ui-standard-input-types">
      <div className="ui-standards-q-example__label">Input types</div>
      <div className="ui-standards-q-example__row">
        <UiStandardsQinput
          sampleId="primary"
          label="Text"
          accent={accent}
          fill={fill}
          ink={ink}
          appearance="standard"
          type="text"
          testId="ui-standard-input-type-text"
        />
        <PasswordTypeField accent={accent} fill={fill} ink={ink} />
        <EmailTypeField accent={accent} fill={fill} ink={ink} />
        <MobileTypeField kind="international" accent={accent} fill={fill} ink={ink} />
        <MobileTypeField kind="local" accent={accent} fill={fill} ink={ink} />
        {INPUT_TYPE_FIELDS.filter((field) => field.id !== "text").map((field) => (
          <UiStandardsQinput
            key={field.id}
            sampleId="primary"
            label={field.label}
            accent={accent}
            fill={fill}
            ink={ink}
            appearance="standard"
            type={field.type}
            testId={`ui-standard-input-type-${field.id}`}
          />
        ))}
      </div>
    </div>
  );
}

export function UiStandardsInputGallery(): ReactNode {
  return (
    <div className="flex flex-col gap-4" data-testid="ui-standard-input-gallery">
      <InputExampleCard title="Standard" testId="ui-standard-input-standard" appearance="standard" />
      <InputExampleCard title="Soft" testId="ui-standard-input-soft" appearance="soft" />
      <InputExampleCard title="Soft outline" testId="ui-standard-input-soft-outline" appearance="soft-outline" />
      <InputExampleCard title="Filled" testId="ui-standard-input-filled" appearance="filled" />
      <InputExampleCard title="Outlined" testId="ui-standard-input-outlined" appearance="outlined" />
      <InputExampleCard title="Standout" testId="ui-standard-input-standout" appearance="standout" />
      <InputExampleCard
        title="Pill"
        testId="ui-standard-input-pill"
        appearance="outlined"
        shape="pill"
      />
    </div>
  );
}
