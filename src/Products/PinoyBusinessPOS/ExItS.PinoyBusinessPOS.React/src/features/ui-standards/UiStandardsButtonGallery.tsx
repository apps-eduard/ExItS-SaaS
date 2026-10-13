import {
  Bell,
  Bookmark,
  Check,
  Heart,
  Info,
  Moon,
  Star,
  Trash2,
  TriangleAlert,
  type LucideIcon,
} from "lucide-react";
import type { CSSProperties } from "react";
import { useQbtnRipple } from "@/features/ui-standards/UiStandardsQbtnRipple";

/**
 * Quasar QBtn samples for the Buttons live card.
 * @see https://quasar.dev/vue-components/button#standard
 */
type QuasarSampleButton = {
  id: string;
  label: string;
  bg: string;
  fg: string;
  glossy?: boolean;
  border?: boolean;
  Icon: LucideIcon;
};

const QUASAR_STANDARD_BUTTONS: readonly QuasarSampleButton[] = [
  { id: "standard", label: "Standard", bg: "#ffffff", fg: "#000000", border: true, Icon: Star },
  { id: "primary", label: "Primary", bg: "#1976d2", fg: "#ffffff", Icon: Check },
  { id: "secondary", label: "Secondary", bg: "#26a69a", fg: "#ffffff", Icon: Bookmark },
  { id: "success", label: "Success", bg: "#22c55e", fg: "#ffffff", Icon: Check },
  { id: "info", label: "Info", bg: "#0ea5e9", fg: "#ffffff", Icon: Info },
  { id: "amber", label: "Amber", bg: "#ffc107", fg: "#000000", Icon: Bell },
  { id: "danger", label: "Danger", bg: "#ef4444", fg: "#ffffff", Icon: Trash2 },
  { id: "warn", label: "Warn", bg: "#ff5722", fg: "#ffffff", Icon: TriangleAlert },
  { id: "help", label: "Help", bg: "#a855f7", fg: "#ffffff", Icon: Heart },
  { id: "black", label: "Black", bg: "#000000", fg: "#ffffff", Icon: Moon },
];

function QuasarExampleCard({
  title,
  testId,
  outline = false,
  raised = false,
  icon = false,
  ghost = false,
  glossy = false,
  gradient = false,
  rounded = false,
  flat = false,
  square = false,
  command,
}: {
  title: string;
  testId: string;
  outline?: boolean;
  raised?: boolean;
  icon?: boolean;
  ghost?: boolean;
  glossy?: boolean;
  gradient?: boolean;
  rounded?: boolean;
  flat?: boolean;
  square?: boolean;
  command?: string;
}) {
  return (
    <div className="ui-standards-q-example" data-testid={testId}>
      <div className="ui-standards-q-example__label">{title}</div>
      <div className="ui-standards-q-example__row">
        {QUASAR_STANDARD_BUTTONS.map((button) => (
          <SampleQbtn
            key={button.id}
            button={button}
            testId={testId}
            outline={outline}
            raised={raised}
            icon={icon}
            ghost={ghost}
            glossy={glossy}
            gradient={gradient}
            rounded={rounded}
            flat={flat}
            square={square}
          />
        ))}
      </div>
      {command ? (
        <code className="ui-standards-q-example__command" data-testid={`${testId}-command`}>
          {command}
        </code>
      ) : null}
    </div>
  );
}

function SampleQbtn({
  button,
  testId,
  outline,
  raised,
  icon,
  ghost,
  glossy,
  gradient,
  rounded,
  flat,
  square,
}: {
  button: QuasarSampleButton;
  testId: string;
  outline: boolean;
  raised: boolean;
  icon: boolean;
  ghost: boolean;
  glossy: boolean;
  gradient: boolean;
  rounded: boolean;
  flat: boolean;
  square: boolean;
}) {
  const { showRipple, layer } = useQbtnRipple(true);
  const accent = button.bg.toLowerCase() === "#ffffff" ? "#000000" : button.bg;
  const ButtonIcon = button.Icon;
  const transparent = outline || ghost;
  const darkInk = (button.id === "standard" || button.id === "black") && ((outline && !icon) || ghost);
  const filledStyle: CSSProperties & { "--qbtn-bg"?: string } = {
    backgroundColor: button.bg,
    color: button.fg,
  };
  if (gradient) filledStyle["--qbtn-bg"] = button.bg;
  return (
    <button
      type="button"
      className={
        "ui-standards-qbtn" +
        (icon ? " ui-standards-qbtn--icon" : "") +
        (square ? " ui-standards-qbtn--square" : "") +
        (rounded ? " ui-standards-qbtn--rounded" : "") +
        (outline ? " ui-standards-qbtn--outline" : "") +
        (ghost ? " ui-standards-qbtn--ghost" : "") +
        (darkInk ? " ui-standards-qbtn--dark-ink" : "") +
        (flat ? " ui-standards-qbtn--flat" : "") +
        (raised ? " ui-standards-qbtn--raised" : "") +
        (!transparent && gradient ? " ui-standards-qbtn--gradient" : "") +
        (!transparent && !gradient && (glossy || button.glossy) ? " ui-standards-qbtn--glossy" : "") +
        (!transparent && button.border ? " ui-standards-qbtn--border" : "")
      }
      style={
        transparent
          ? { color: accent, borderColor: outline ? accent : "transparent", backgroundColor: "transparent" }
          : filledStyle
      }
      aria-label={icon ? button.label : undefined}
      data-testid={`${testId}-${button.id}`}
      onClick={showRipple}
    >
      <span className="ui-standards-qbtn-ripple-content">
        {icon ? <ButtonIcon className="size-4" aria-hidden strokeWidth={2} /> : button.label}
      </span>
      {layer}
    </button>
  );
}

function QuasarStandardButtons() {
  return (
    <div className="flex flex-col gap-4">
      <div data-testid="ui-standard-btn-quasar-standard">
        <QuasarExampleCard
          title="Standard buttons"
          testId="ui-standard-btn-quasar-standard-row"
          command="cursor: ext-btn-standard-primary"
        />
        <p className="m-0 mt-2 text-[length:var(--exits-text-xs)] text-muted">
          Quasar QBtn standard example.
        </p>
      </div>
      <QuasarExampleCard title="Button outline" testId="ui-standard-btn-quasar-outline" outline />
      <QuasarExampleCard title="Raised" testId="ui-standard-btn-quasar-raised" raised />
      <QuasarExampleCard title="Button ghost" testId="ui-standard-btn-quasar-ghost" ghost />
      <QuasarExampleCard title="Button glossy" testId="ui-standard-btn-quasar-glossy" glossy />
      <QuasarExampleCard title="Button gradient" testId="ui-standard-btn-quasar-gradient" gradient />
      <QuasarExampleCard title="Button rounded" testId="ui-standard-btn-quasar-rounded" rounded />
      <QuasarExampleCard title="Button flat" testId="ui-standard-btn-quasar-flat" flat />
      <QuasarExampleCard
        title="Rounded icon solid"
        testId="ui-standard-btn-quasar-round-solid"
        icon
      />
      <QuasarExampleCard
        title="Rounded icon outlined"
        testId="ui-standard-btn-quasar-round-outline"
        icon
        outline
      />
      <QuasarExampleCard
        title="Rounded icon ghost"
        testId="ui-standard-btn-quasar-round-ghost"
        icon
        ghost
      />
      <QuasarExampleCard title="Rounded icon" testId="ui-standard-btn-quasar-round-icon" icon />
      <QuasarExampleCard
        title="Square icon"
        testId="ui-standard-btn-quasar-square-icon"
        icon
        square
      />
    </div>
  );
}

export function UiStandardsButtonGallery() {
  return (
    <div className="flex flex-col gap-3" data-testid="ui-standard-button-gallery">
      <QuasarStandardButtons />
    </div>
  );
}
