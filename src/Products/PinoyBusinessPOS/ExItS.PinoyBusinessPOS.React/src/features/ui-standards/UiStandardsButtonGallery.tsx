import { useState, type ReactNode } from "react";
import {
  Bell,
  Bookmark,
  Check,
  ChevronDown,
  Heart,
  Loader2,
  Moon,
  Search,
  Star,
  Trash2,
  User,
  X,
  type LucideIcon,
} from "lucide-react";
import {
  Button,
  type ButtonAppearance,
  type ButtonIntentTone,
} from "@/components/ui/button";
import { cn } from "@/lib/cn";
import { PlaygroundLabel } from "@/features/ui-standards/UiStandardsSnippetBlock";
import { UiStandardsCopyCommand } from "@/features/ui-standards/UiStandardsCopyCommand";

/**
 * Locked ExItS Button visual standard — Diamond PrimeNG Button Full sample layout.
 * @see https://diamond.primeng.dev/uikit/button
 * Primary uses Preferences brand tokens; other severities use locked `--exits-severity-*`.
 * Cursor shorthand: Diamond “Text” = ExItS **Ghost**
 * Example: `EXITS BUTTON + GHOST + SUCCESS`
 */
type GallerySeverityId =
  | "primary"
  | "secondary"
  | "success"
  | "info"
  | "warn"
  | "help"
  | "danger"
  | "contrast";

type GallerySeverityDef = {
  id: GallerySeverityId;
  label: string;
  intent: ButtonIntentTone;
  Icon: LucideIcon;
  iconLabel: string;
};

const GALLERY_SEVERITIES: ReadonlyArray<GallerySeverityDef> = [
  { id: "primary", label: "Primary", intent: "primary", Icon: Check, iconLabel: "Check" },
  {
    id: "secondary",
    label: "Secondary",
    intent: "neutral",
    Icon: Bookmark,
    iconLabel: "Bookmark",
  },
  { id: "success", label: "Success", intent: "success", Icon: Search, iconLabel: "Search" },
  { id: "info", label: "Info", intent: "info", Icon: User, iconLabel: "User" },
  { id: "warn", label: "Warn", intent: "warning", Icon: Bell, iconLabel: "Bell" },
  { id: "help", label: "Help", intent: "help", Icon: Heart, iconLabel: "Heart" },
  { id: "danger", label: "Danger", intent: "danger", Icon: X, iconLabel: "Close" },
  {
    id: "contrast",
    label: "Contrast",
    intent: "contrast",
    Icon: Moon,
    iconLabel: "Moon",
  },
];

/** Non-primary severities — locked Diamond fills via `--exits-severity-*`. */
const SEVERITY_SOLID_CLASS: Record<Exclude<GallerySeverityId, "primary">, string> = {
  secondary:
    "!border-[var(--exits-severity-secondary)] !bg-[var(--exits-severity-secondary)] !text-[var(--exits-severity-secondary-foreground)] hover:!border-[var(--exits-severity-secondary-hover)] hover:!bg-[var(--exits-severity-secondary-hover)]",
  success:
    "!border-[var(--exits-severity-success)] !bg-[var(--exits-severity-success)] !text-[var(--exits-severity-success-foreground)] hover:!border-[var(--exits-severity-success-hover)] hover:!bg-[var(--exits-severity-success-hover)]",
  info:
    "!border-[var(--exits-severity-info)] !bg-[var(--exits-severity-info)] !text-[var(--exits-severity-info-foreground)] hover:!border-[var(--exits-severity-info-hover)] hover:!bg-[var(--exits-severity-info-hover)]",
  warn:
    "!border-[var(--exits-severity-warn)] !bg-[var(--exits-severity-warn)] !text-[var(--exits-severity-warn-foreground)] hover:!border-[var(--exits-severity-warn-hover)] hover:!bg-[var(--exits-severity-warn-hover)]",
  help:
    "!border-[var(--exits-severity-help)] !bg-[var(--exits-severity-help)] !text-[var(--exits-severity-help-foreground)] hover:!border-[var(--exits-severity-help-hover)] hover:!bg-[var(--exits-severity-help-hover)]",
  danger:
    "!border-[var(--exits-severity-danger)] !bg-[var(--exits-severity-danger)] !text-[var(--exits-severity-danger-foreground)] hover:!border-[var(--exits-severity-danger-hover)] hover:!bg-[var(--exits-severity-danger-hover)]",
  contrast:
    "!border-[var(--exits-severity-contrast)] !bg-[var(--exits-severity-contrast)] !text-[var(--exits-severity-contrast-foreground)] hover:!border-[var(--exits-severity-contrast-hover)] hover:!bg-[var(--exits-severity-contrast-hover)]",
};

const SEVERITY_OUTLINE_CLASS: Record<Exclude<GallerySeverityId, "primary">, string> = {
  secondary:
    "!border-[color-mix(in_srgb,var(--exits-severity-secondary-foreground)_35%,transparent)] !bg-transparent !text-[var(--exits-severity-secondary-foreground)] hover:!bg-[var(--exits-severity-secondary)]",
  success:
    "!border-[var(--exits-severity-success)] !bg-transparent !text-[var(--exits-severity-success)] hover:!bg-[color-mix(in_srgb,var(--exits-severity-success)_12%,transparent)]",
  info:
    "!border-[var(--exits-severity-info)] !bg-transparent !text-[var(--exits-severity-info)] hover:!bg-[color-mix(in_srgb,var(--exits-severity-info)_12%,transparent)]",
  warn:
    "!border-[var(--exits-severity-warn)] !bg-transparent !text-[var(--exits-severity-warn)] hover:!bg-[color-mix(in_srgb,var(--exits-severity-warn)_12%,transparent)]",
  help:
    "!border-[var(--exits-severity-help)] !bg-transparent !text-[var(--exits-severity-help)] hover:!bg-[color-mix(in_srgb,var(--exits-severity-help)_12%,transparent)]",
  danger:
    "!border-[var(--exits-severity-danger)] !bg-transparent !text-[var(--exits-severity-danger)] hover:!bg-[color-mix(in_srgb,var(--exits-severity-danger)_12%,transparent)]",
  contrast:
    "!border-[var(--exits-severity-contrast)] !bg-transparent !text-[var(--exits-severity-contrast)] hover:!bg-[color-mix(in_srgb,var(--exits-severity-contrast)_8%,transparent)]",
};

const SEVERITY_TEXT_CLASS: Record<Exclude<GallerySeverityId, "primary">, string> = {
  secondary:
    "!border-transparent !bg-transparent !text-[var(--exits-severity-secondary-foreground)] hover:!bg-[var(--exits-severity-secondary)]",
  success:
    "!border-transparent !bg-transparent !text-[var(--exits-severity-success)] hover:!bg-[color-mix(in_srgb,var(--exits-severity-success)_12%,transparent)]",
  info:
    "!border-transparent !bg-transparent !text-[var(--exits-severity-info)] hover:!bg-[color-mix(in_srgb,var(--exits-severity-info)_12%,transparent)]",
  warn:
    "!border-transparent !bg-transparent !text-[var(--exits-severity-warn)] hover:!bg-[color-mix(in_srgb,var(--exits-severity-warn)_12%,transparent)]",
  help:
    "!border-transparent !bg-transparent !text-[var(--exits-severity-help)] hover:!bg-[color-mix(in_srgb,var(--exits-severity-help)_12%,transparent)]",
  danger:
    "!border-transparent !bg-transparent !text-[var(--exits-severity-danger)] hover:!bg-[color-mix(in_srgb,var(--exits-severity-danger)_12%,transparent)]",
  contrast:
    "!border-transparent !bg-transparent !text-[var(--exits-severity-contrast)] hover:!bg-[color-mix(in_srgb,var(--exits-severity-contrast)_8%,transparent)]",
};

/** Match Diamond p-button sizing (~32.4px / 6px radius / 14px medium). */
const GALLERY_BUTTON_BASE =
  "!h-[32px] !min-h-[32px] !rounded-[6px] !px-[10.5px] !text-[14px] !font-medium !shadow-none";

const GALLERY_ICON_BASE =
  "!size-8 !min-h-8 !min-w-8 !rounded-[6px] !p-0 !shadow-none";

const PRIMENG_LOGO_SRC = "https://primefaces.org/cdn/primeng/images/logo.svg";

/**
 * Primary uses Preferences brand tokens (`--exits-primary`) — user-selectable.
 * Other severities use locked Diamond `--exits-severity-*` fills.
 */
function severityAppearanceClass(id: GallerySeverityId, appearance: ButtonAppearance): string | null {
  if (id === "primary") return null;
  if (appearance === "outline") return SEVERITY_OUTLINE_CLASS[id];
  if (appearance === "ghost") return SEVERITY_TEXT_CLASS[id];
  return SEVERITY_SOLID_CLASS[id];
}

function GallerySection({
  title,
  children,
  testId,
  note,
  command,
  commandContext,
  composeAppearance,
}: {
  title: string;
  children: ReactNode;
  testId: string;
  note?: string;
  /** Base Cursor shorthand for this gallery card. */
  command?: string;
  commandContext?: string;
  /**
   * When set, shows one sample compose command:
   * `EXITS BUTTON + {composeAppearance} + PRIMARY`
   */
  composeAppearance?: string;
}) {
  return (
    <div
      className="flex flex-col gap-2 rounded-[var(--exits-radius-md)] border border-border bg-[var(--exits-surface)] p-3 shadow-[var(--exits-shadow-sm)]"
      data-testid={testId}
    >
      <h3 className="m-0 text-[length:var(--exits-text-sm)] font-bold text-foreground">{title}</h3>
      <div className="flex flex-wrap items-center gap-2">{children}</div>
      {note ? (
        <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">{note}</p>
      ) : null}
      {command ? (
        <UiStandardsCopyCommand standard="Button" command={command} context={commandContext} />
      ) : null}
      {composeAppearance ? (
        <div data-testid={`${testId}-compose-commands`}>
          <UiStandardsCopyCommand
            standard="Button"
            command={`EXITS BUTTON + ${composeAppearance} + PRIMARY`}
            context="Compose with severity: swap PRIMARY for SECONDARY · SUCCESS · INFO · WARN · HELP · DANGER · CONTRAST."
          />
        </div>
      ) : null}
    </div>
  );
}

function SeverityRow({
  appearance,
  shape = "standard",
  iconOnly = false,
  raisedText = false,
  testIdPrefix,
  labelOverride,
}: {
  appearance: ButtonAppearance;
  shape?: "standard" | "pill" | "round";
  iconOnly?: boolean;
  /** Diamond “Rounded Text” = rounded icon + text + raised */
  raisedText?: boolean;
  testIdPrefix: string;
  labelOverride?: (id: GallerySeverityId, label: string) => string;
}) {
  const shapeClass =
    shape === "pill" || shape === "round" ? "!rounded-full" : "!rounded-[6px]";

  return (
    <>
      {GALLERY_SEVERITIES.map(({ id, label, intent, Icon, iconLabel }) => {
        const textLabel = labelOverride?.(id, label) ?? label;
        const severityClass = severityAppearanceClass(id, appearance);
        return (
          <Button
            key={`${testIdPrefix}-${id}`}
            type="button"
            intent={intent}
            appearance={appearance}
            emphasis={appearance === "ghost" || appearance === "outline" ? "soft" : "strong"}
            size={iconOnly ? "icon" : "default"}
            shape={shape === "round" ? "round" : shape === "pill" ? "pill" : "standard"}
            aria-label={iconOnly ? iconLabel : undefined}
            className={cn(
              iconOnly ? GALLERY_ICON_BASE : GALLERY_BUTTON_BASE,
              shapeClass,
              (appearance === "elevated" || raisedText) &&
                "!shadow-[0_1px_3px_color-mix(in_srgb,#0f172a_18%,transparent)] hover:!-translate-y-px",
              severityClass,
              severityClass ? "exits-severity-btn" : null,
            )}
            data-testid={`${testIdPrefix}-${id}`}
            data-gallery-severity={id}
          >
            {iconOnly ? <Icon className="size-4" aria-hidden strokeWidth={2} /> : textLabel}
          </Button>
        );
      })}
    </>
  );
}

function GallerySplitButton({ id }: { id: GallerySeverityId }) {
  const severityClass = severityAppearanceClass(id, "solid");
  return (
    <div
      className="inline-flex overflow-hidden rounded-[6px]"
      data-testid={`ui-standard-btn-gallery-split-${id}`}
      data-gallery-severity={id}
    >
      <Button
        type="button"
        intent={id === "primary" ? "primary" : GALLERY_SEVERITIES.find((s) => s.id === id)!.intent}
        appearance="solid"
        emphasis="strong"
        className={cn(
          GALLERY_BUTTON_BASE,
          "!rounded-none !rounded-l-[6px]",
          severityClass,
          severityClass ? "exits-severity-btn" : null,
        )}
        data-testid={`ui-standard-btn-gallery-split-${id}-main`}
      >
        Save
      </Button>
      <Button
        type="button"
        intent={id === "primary" ? "primary" : GALLERY_SEVERITIES.find((s) => s.id === id)!.intent}
        appearance="solid"
        emphasis="strong"
        size="icon"
        aria-label={`${id} save options`}
        aria-haspopup="menu"
        aria-expanded={false}
        className={cn(
          GALLERY_ICON_BASE,
          "!rounded-none !rounded-r-[6px] !border-l !border-l-[color-mix(in_srgb,#fff_22%,transparent)]",
          severityClass,
          severityClass ? "exits-severity-btn" : null,
        )}
        data-testid={`ui-standard-btn-gallery-split-${id}-menu`}
      >
        <ChevronDown className="size-4" aria-hidden strokeWidth={2} />
      </Button>
    </div>
  );
}

function GalleryLoadingButtons() {
  const [loadingKey, setLoadingKey] = useState<string | null>(null);

  const runLoad = (key: string) => {
    setLoadingKey(key);
    window.setTimeout(() => {
      setLoadingKey((current) => (current === key ? null : current));
    }, 2000);
  };

  const solidPrimary = GALLERY_BUTTON_BASE;
  const solidIcon = GALLERY_ICON_BASE;

  return (
    <>
      <Button
        type="button"
        intent="primary"
        appearance="solid"
        emphasis="strong"
        disabled={loadingKey === "leading"}
        aria-busy={loadingKey === "leading"}
        className={solidPrimary}
        data-testid="ui-standard-btn-gallery-loading-leading"
        onClick={() => runLoad("leading")}
      >
        {loadingKey === "leading" ? (
          <Loader2 className="size-4 animate-spin" aria-hidden strokeWidth={2} />
        ) : (
          <Search className="size-4" aria-hidden strokeWidth={2} />
        )}
        Search
      </Button>
      <Button
        type="button"
        intent="primary"
        appearance="solid"
        emphasis="strong"
        disabled={loadingKey === "trailing"}
        aria-busy={loadingKey === "trailing"}
        className={solidPrimary}
        data-testid="ui-standard-btn-gallery-loading-trailing"
        onClick={() => runLoad("trailing")}
      >
        Search
        {loadingKey === "trailing" ? (
          <Loader2 className="size-4 animate-spin" aria-hidden strokeWidth={2} />
        ) : (
          <Search className="size-4" aria-hidden strokeWidth={2} />
        )}
      </Button>
      <Button
        type="button"
        intent="primary"
        appearance="solid"
        emphasis="strong"
        size="icon"
        disabled={loadingKey === "icon"}
        aria-busy={loadingKey === "icon"}
        aria-label="Search"
        className={solidIcon}
        data-testid="ui-standard-btn-gallery-loading-icon"
        onClick={() => runLoad("icon")}
      >
        {loadingKey === "icon" ? (
          <Loader2 className="size-4 animate-spin" aria-hidden strokeWidth={2} />
        ) : (
          <Search className="size-4" aria-hidden strokeWidth={2} />
        )}
      </Button>
      <Button
        type="button"
        intent="primary"
        appearance="solid"
        emphasis="strong"
        disabled={loadingKey === "plain"}
        aria-busy={loadingKey === "plain"}
        className={solidPrimary}
        data-testid="ui-standard-btn-gallery-loading-plain"
        onClick={() => runLoad("plain")}
      >
        {loadingKey === "plain" ? (
          <Loader2 className="size-4 animate-spin" aria-hidden strokeWidth={2} />
        ) : null}
        Search
      </Button>
    </>
  );
}

/**
 * Button gallery — locked ExItS Button visual standard (Diamond Full sample layout).
 * Severity fills are showcase-only; product chrome keeps brand tokens.
 * @see https://diamond.primeng.dev/uikit/button
 * @see Docs/UI/exits-button-standard.md
 */
export function UiStandardsButtonGallery() {
  return (
    <div className="flex flex-col gap-3" data-testid="ui-standard-button-gallery">
      <div className="flex flex-col gap-1">
        <PlaygroundLabel>Gallery — locked standard</PlaygroundLabel>
        <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
          ExItS Button standard = Full sample cards from{" "}
          <a
            href="https://diamond.primeng.dev/uikit/button"
            target="_blank"
            rel="noreferrer"
            className="font-medium text-primary underline-offset-2 hover:underline"
          >
            Diamond PrimeNG → Button
          </a>
          . All{" "}
          <strong className="font-medium text-foreground">8 severities</strong> are locked
          (Primary · Secondary · Success · Info · Warn · Help · Danger · Contrast). Product Primary
          keeps ExItS brand tokens; Help / Contrast use severity tokens.
        </p>
      </div>

      <div className="grid min-w-0 gap-3 md:grid-cols-2">
        <GallerySection
          title="Default"
          testId="ui-standard-btn-gallery-default"
          note="Submit / Disabled = Solid Primary. Link = Ghost Primary."
          command="EXITS BUTTON + SOLID + PRIMARY"
          commandContext="Link control: EXITS BUTTON + GHOST + PRIMARY"
        >
          <Button
            type="button"
            intent="primary"
            appearance="solid"
            emphasis="strong"
            className={GALLERY_BUTTON_BASE}
            data-testid="ui-standard-btn-gallery-submit"
            data-gallery-severity="primary"
          >
            Submit
          </Button>
          <Button
            type="button"
            intent="primary"
            appearance="solid"
            emphasis="strong"
            disabled
            className={GALLERY_BUTTON_BASE}
            data-testid="ui-standard-btn-gallery-disabled"
            data-gallery-severity="primary"
          >
            Disabled
          </Button>
          <Button
            type="button"
            intent="primary"
            appearance="ghost"
            className={GALLERY_BUTTON_BASE}
            data-testid="ui-standard-btn-gallery-link"
            data-gallery-severity="primary"
          >
            Link
          </Button>
        </GallerySection>

        <GallerySection
          title="Severities"
          testId="ui-standard-btn-gallery-severities"
          note="Primary follows Preferences primary palette. Secondary–Contrast use locked Diamond severity colors."
          command="EXITS BUTTON + SOLID"
          composeAppearance="SOLID"
        >
          <SeverityRow appearance="solid" testIdPrefix="ui-standard-btn-gallery-severity" />
        </GallerySection>

        <GallerySection
          title="Text (Ghost)"
          testId="ui-standard-btn-gallery-text"
          note="Diamond “Text” = ExItS Ghost appearance."
          command="EXITS BUTTON + GHOST"
          composeAppearance="GHOST"
        >
          <SeverityRow appearance="ghost" testIdPrefix="ui-standard-btn-gallery-text" />
        </GallerySection>

        <GallerySection
          title="Outlined"
          testId="ui-standard-btn-gallery-outlined"
          note="Outline appearance × 8 locked severities."
          command="EXITS BUTTON + OUTLINE"
          composeAppearance="OUTLINE"
        >
          <SeverityRow
            appearance="outline"
            testIdPrefix="ui-standard-btn-gallery-outlined"
            labelOverride={(id, label) => (id === "warn" ? "warn" : label)}
          />
        </GallerySection>

        <GallerySection
          title="Group"
          testId="ui-standard-btn-gallery-group"
          command="EXITS BUTTON + GROUP"
        >
          <div
            className="inline-flex overflow-hidden rounded-[6px]"
            data-testid="ui-standard-btn-gallery-group-bar"
          >
            <Button
              type="button"
              intent="primary"
              appearance="solid"
              emphasis="strong"
              className={cn(GALLERY_BUTTON_BASE, "!rounded-none !rounded-l-[6px]")}
              data-testid="ui-standard-btn-gallery-group-save"
            >
              <Check className="size-4" aria-hidden strokeWidth={2} />
              Save
            </Button>
            <Button
              type="button"
              intent="primary"
              appearance="solid"
              emphasis="strong"
              className={cn(
                GALLERY_BUTTON_BASE,
                "!rounded-none !border-l !border-l-[color-mix(in_srgb,#fff_22%,transparent)]",
              )}
              data-testid="ui-standard-btn-gallery-group-delete"
            >
              <Trash2 className="size-4" aria-hidden strokeWidth={2} />
              Delete
            </Button>
            <Button
              type="button"
              intent="primary"
              appearance="solid"
              emphasis="strong"
              className={cn(
                GALLERY_BUTTON_BASE,
                "!rounded-none !rounded-r-[6px] !border-l !border-l-[color-mix(in_srgb,#fff_22%,transparent)]",
              )}
              data-testid="ui-standard-btn-gallery-group-cancel"
            >
              <X className="size-4" aria-hidden strokeWidth={2} />
              Cancel
            </Button>
          </div>
        </GallerySection>

        <GallerySection
          title="SplitButton"
          testId="ui-standard-btn-gallery-splitbutton"
          command="EXITS BUTTON + SPLIT"
        >
          {GALLERY_SEVERITIES.map(({ id }) => (
            <GallerySplitButton key={id} id={id} />
          ))}
        </GallerySection>

        <GallerySection
          title="Templating"
          testId="ui-standard-btn-gallery-templating"
          command="EXITS BUTTON + TEMPLATE"
        >
          <Button
            type="button"
            intent="primary"
            appearance="solid"
            emphasis="strong"
            size="icon"
            aria-label="PrimeNG logo"
            className="!h-[38px] !min-h-[38px] !min-w-[38px] !rounded-[6px] !p-0 !shadow-none"
            data-testid="ui-standard-btn-gallery-template-logo"
          >
            <img src={PRIMENG_LOGO_SRC} alt="" className="size-6" width={24} height={24} />
          </Button>
          <Button
            type="button"
            intent="success"
            appearance="outline"
            className={cn(
              "!h-[38px] !min-h-[38px] !rounded-[6px] !px-3 !text-[14px] !font-medium !shadow-none",
              SEVERITY_OUTLINE_CLASS.success,
              "exits-severity-btn",
            )}
            data-testid="ui-standard-btn-gallery-template-primeng"
            data-gallery-severity="success"
          >
            <img src={PRIMENG_LOGO_SRC} alt="" className="size-6" width={24} height={24} />
            PrimeNG
          </Button>
        </GallerySection>

        <GallerySection
          title="Icons"
          testId="ui-standard-btn-gallery-icons"
          command="EXITS BUTTON + ICON"
          commandContext="Icon-only: EXITS BUTTON + ICON ONLY. Leading/trailing icon children on labeled buttons."
        >
          <Button
            type="button"
            intent="primary"
            appearance="solid"
            emphasis="strong"
            size="icon"
            aria-label="Favorite"
            className={GALLERY_ICON_BASE}
            data-testid="ui-standard-btn-gallery-icon-only"
            data-gallery-severity="primary"
          >
            <Star className="size-4 fill-current" aria-hidden strokeWidth={2} />
          </Button>
          <Button
            type="button"
            intent="primary"
            appearance="solid"
            emphasis="strong"
            className={GALLERY_BUTTON_BASE}
            data-testid="ui-standard-btn-gallery-icon-leading"
            data-gallery-severity="primary"
          >
            <Bookmark className="size-4" aria-hidden strokeWidth={2} />
            Bookmark
          </Button>
          <Button
            type="button"
            intent="primary"
            appearance="solid"
            emphasis="strong"
            className={GALLERY_BUTTON_BASE}
            data-testid="ui-standard-btn-gallery-icon-trailing"
            data-gallery-severity="primary"
          >
            Bookmark
            <Bookmark className="size-4" aria-hidden strokeWidth={2} />
          </Button>
        </GallerySection>

        <GallerySection
          title="Raised"
          testId="ui-standard-btn-gallery-raised"
          note="Elevated appearance × 8 locked severities."
          command="EXITS BUTTON + ELEVATED"
          composeAppearance="ELEVATED"
        >
          <SeverityRow appearance="elevated" testIdPrefix="ui-standard-btn-gallery-raised" />
        </GallerySection>

        <GallerySection
          title="Rounded"
          testId="ui-standard-btn-gallery-rounded"
          note="Pill shape + Solid."
          command="EXITS BUTTON + SOLID + PILL"
          composeAppearance="SOLID + PILL"
        >
          <SeverityRow
            appearance="solid"
            shape="pill"
            testIdPrefix="ui-standard-btn-gallery-rounded"
          />
        </GallerySection>

        <GallerySection
          title="Rounded Icons"
          testId="ui-standard-btn-gallery-rounded-icons"
          note="Round icon-only + Solid."
          command="EXITS BUTTON + SOLID + ROUND + ICON ONLY"
        >
          <SeverityRow
            appearance="solid"
            shape="round"
            iconOnly
            testIdPrefix="ui-standard-btn-gallery-round-icon"
          />
        </GallerySection>

        <GallerySection
          title="Rounded Text (Ghost)"
          testId="ui-standard-btn-gallery-rounded-text"
          note="Diamond Rounded Text = Ghost + Round icon-only."
          command="EXITS BUTTON + GHOST + ROUND + ICON ONLY"
        >
          <SeverityRow
            appearance="ghost"
            shape="round"
            iconOnly
            raisedText
            testIdPrefix="ui-standard-btn-gallery-round-text"
          />
        </GallerySection>

        <GallerySection
          title="Rounded Outlined"
          testId="ui-standard-btn-gallery-rounded-outlined"
          command="EXITS BUTTON + OUTLINE + ROUND + ICON ONLY"
        >
          <SeverityRow
            appearance="outline"
            shape="round"
            iconOnly
            testIdPrefix="ui-standard-btn-gallery-round-outlined"
          />
        </GallerySection>

        <GallerySection
          title="Loading"
          testId="ui-standard-btn-gallery-loading"
          command="EXITS BUTTON + LOADING"
        >
          <GalleryLoadingButtons />
        </GallerySection>
      </div>
    </div>
  );
}
