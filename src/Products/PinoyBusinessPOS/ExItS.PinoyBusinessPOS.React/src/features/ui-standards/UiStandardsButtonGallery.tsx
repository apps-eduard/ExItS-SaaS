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
import { useQbtnRipple } from "@/features/ui-standards/UiStandardsQbtnRipple";

/**
 * Quasar QBtn samples for the Buttons live card.
 * Intent fills come from the locked Intent color global default.
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
  { id: "standard", label: "Standard", bg: INTENT_GLOBAL_STANDARD, fg: "#000000", border: true, Icon: Star },
  { id: "primary", label: "Primary", bg: INTENT_GLOBAL_PRIMARY, fg: "#ffffff", Icon: Check },
  { id: "secondary", label: "Secondary", bg: INTENT_GLOBAL_SECONDARY, fg: "#ffffff", Icon: Bookmark },
  { id: "success", label: "Success", bg: INTENT_GLOBAL_SUCCESS, fg: "#ffffff", Icon: Check },
  { id: "info", label: "Info", bg: INTENT_GLOBAL_INFO, fg: "#ffffff", Icon: Info },
  { id: "amber", label: "Amber", bg: INTENT_GLOBAL_AMBER, fg: "#000000", Icon: Bell },
  { id: "danger", label: "Danger", bg: INTENT_GLOBAL_DANGER, fg: "#ffffff", Icon: Trash2 },
  { id: "warn", label: "Warn", bg: INTENT_GLOBAL_WARN, fg: INTENT_GLOBAL_WARN_FOREGROUND, Icon: TriangleAlert },
  { id: "help", label: "Help", bg: INTENT_GLOBAL_HELP, fg: "#ffffff", Icon: Heart },
  { id: "black", label: "Black", bg: INTENT_GLOBAL_BLACK, fg: "#ffffff", Icon: Moon },
];

function QuasarExampleCard({
  title,
  testId,
  outline = false,
  soft = false,
  softOutline = false,
  softGlass = false,
  raised = false,
  icon = false,
  ghost = false,
  glossy = false,
  glossyGlass = false,
  glossyRaise = false,
  gradient = false,
  gradientGlass = false,
  rounded = false,
  flat = false,
  square = false,
  command,
}: {
  title: string;
  testId: string;
  outline?: boolean;
  soft?: boolean;
  softOutline?: boolean;
  softGlass?: boolean;
  raised?: boolean;
  icon?: boolean;
  ghost?: boolean;
  glossy?: boolean;
  glossyGlass?: boolean;
  glossyRaise?: boolean;
  gradient?: boolean;
  gradientGlass?: boolean;
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
            soft={soft}
            softOutline={softOutline}
            softGlass={softGlass}
            raised={raised}
            icon={icon}
            ghost={ghost}
            glossy={glossy}
            glossyGlass={glossyGlass}
            glossyRaise={glossyRaise}
            gradient={gradient}
            gradientGlass={gradientGlass}
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
  soft,
  softOutline,
  softGlass,
  raised,
  icon,
  ghost,
  glossy,
  glossyGlass,
  glossyRaise,
  gradient,
  gradientGlass,
  rounded,
  flat,
  square,
}: {
  button: QuasarSampleButton;
  testId: string;
  outline: boolean;
  soft: boolean;
  softOutline: boolean;
  softGlass: boolean;
  raised: boolean;
  icon: boolean;
  ghost: boolean;
  glossy: boolean;
  glossyGlass: boolean;
  glossyRaise: boolean;
  gradient: boolean;
  gradientGlass: boolean;
  rounded: boolean;
  flat: boolean;
  square: boolean;
}) {
  const { showRipple, layer } = useQbtnRipple(true);
  const accent = button.bg.toLowerCase() === "#ffffff" ? "#000000" : button.bg;
  const ButtonIcon = button.Icon;
  const transparent = outline || ghost;
  const wash = soft || softOutline || softGlass || glossyGlass || gradientGlass;
  const darkInk =
    (button.id === "standard" || button.id === "black") &&
    ((outline && !icon) || ghost || wash);
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
        (soft ? " ui-standards-qbtn--soft" : "") +
        (softOutline ? " ui-standards-qbtn--soft-outline" : "") +
        (softGlass ? " ui-standards-qbtn--soft-glass" : "") +
        (outline ? " ui-standards-qbtn--outline" : "") +
        (ghost ? " ui-standards-qbtn--ghost" : "") +
        (darkInk ? " ui-standards-qbtn--dark-ink" : "") +
        (flat ? " ui-standards-qbtn--flat" : "") +
        (raised && !glossyRaise ? " ui-standards-qbtn--raised" : "") +
        (!transparent && gradient && !gradientGlass ? " ui-standards-qbtn--gradient" : "") +
        (!transparent && gradientGlass ? " ui-standards-qbtn--gradient-glass" : "") +
        (!transparent && !gradient && !gradientGlass && !glossyGlass && !glossyRaise && (glossy || button.glossy)
          ? " ui-standards-qbtn--glossy"
          : "") +
        (!transparent && !gradient && !gradientGlass && glossyGlass
          ? " ui-standards-qbtn--glossy-glass"
          : "") +
        (!transparent && !gradient && !gradientGlass && glossyRaise
          ? " ui-standards-qbtn--glossy-raise"
          : "") +
        (!transparent && !wash && button.border ? " ui-standards-qbtn--border" : "")
      }
      style={
        softOutline
          ? { color: accent, "--qbtn-bg": accent, borderColor: accent }
          : soft || softGlass || glossyGlass || gradientGlass
            ? { color: accent, "--qbtn-bg": accent }
            : transparent
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
      <QuasarExampleCard title="Button soft" testId="ui-standard-btn-quasar-soft" soft />
      <QuasarExampleCard title="Button soft outline" testId="ui-standard-btn-quasar-soft-outline" softOutline />
      <QuasarExampleCard title="Button soft glass" testId="ui-standard-btn-quasar-soft-glass" softGlass />
      <QuasarExampleCard title="Raised" testId="ui-standard-btn-quasar-raised" raised />
      <QuasarExampleCard title="Button ghost" testId="ui-standard-btn-quasar-ghost" ghost />
      <QuasarExampleCard title="Button glossy" testId="ui-standard-btn-quasar-glossy" glossy />
      <QuasarExampleCard
        title="Button glossy glass"
        testId="ui-standard-btn-quasar-glossy-glass"
        glossyGlass
      />
      <QuasarExampleCard
        title="Button glossy raise"
        testId="ui-standard-btn-quasar-glossy-raise"
        glossyRaise
      />
      <QuasarExampleCard title="Button gradient" testId="ui-standard-btn-quasar-gradient" gradient />
      <QuasarExampleCard
        title="Button gradient glass"
        testId="ui-standard-btn-quasar-gradient-glass"
        gradientGlass
      />
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
      <ButtonIconsSample />
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

/** Medallion icon buttons — same icon set used on Card pennant. */
function ButtonIconsSample() {
  return (
    <div className="ui-standards-q-example" data-testid="ui-standard-btn-quasar-button-icons">
      <div className="ui-standards-q-example__label">Medallion</div>
      <div className="ui-standards-q-example__row ui-standards-qbtn-icons-row">
        {QUASAR_STANDARD_BUTTONS.map((button) => {
          const ButtonIcon = button.Icon;
          return (
            <button
              key={button.id}
              type="button"
              className="ui-standards-qbtn-icon-medallion"
              style={
                {
                  "--qbtn-icon-bg": button.bg,
                  "--qbtn-icon-fg": button.fg,
                } as CSSProperties
              }
              aria-label={button.label}
              data-testid={`ui-standard-btn-quasar-button-icons-${button.id}`}
            >
              <ButtonIcon className="size-4" aria-hidden strokeWidth={2} />
            </button>
          );
        })}
      </div>
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
