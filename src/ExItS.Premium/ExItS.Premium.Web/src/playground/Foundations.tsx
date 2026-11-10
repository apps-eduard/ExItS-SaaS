import { useState, type CSSProperties, type ReactNode } from "react";
import { motion } from "motion/react";
import { MOTION_SPRING } from "../motion/motion";
import { Button } from "../components/button/Button";
import { Card } from "../components/card/Card";
import { SegmentedControl } from "../components/segmented/SegmentedControl";
import { Switch } from "../components/switch/Switch";
import { PALETTE_LABELS, PALETTES } from "../theme/appearance";
import { useAppearance } from "../theme/AppearanceProvider";
import type { SectionId } from "./nav";
import { IconsSection } from "./IconsSection";
import styles from "./Playground.module.scss";

function Intro({ title, children }: { title: string; children: ReactNode }) {
  return (
    <header className={styles.intro}>
      <h1 className="type-page">{title}</h1>
      <p className="type-secondary">{children}</p>
    </header>
  );
}

function Stage({ label, children }: { label: string; children: ReactNode }) {
  return (
    <section className={styles.stageBlock}>
      <h2 className="type-section">{label}</h2>
      <div className={styles.stage}>{children}</div>
    </section>
  );
}

const COLOR_TOKENS = [
  ["Page", "var(--color-page)"],
  ["Surface", "var(--color-surface)"],
  ["Text", "var(--color-text)"],
  ["Secondary", "var(--color-text-secondary)"],
  ["Primary", "var(--color-primary)"],
  ["Success", "var(--color-success)"],
  ["Warning", "var(--color-warning)"],
  ["Danger", "var(--color-danger)"],
  ["Information", "var(--color-info)"],
] as const;

export function Foundations({ id }: { id: SectionId }) {
  const appearance = useAppearance();
  if (id === "overview") return <Overview />;
  if (id === "colors") return <Colors palette={appearance.palette} />;
  if (id === "typography") return <Typography />;
  if (id === "spacing") return <Spacing />;
  if (id === "radius") return <Radius />;
  if (id === "elevation") return <Elevation />;
  if (id === "motion") return <Motion />;
  if (id === "icons") return <IconsSection />;
  return null;
}

function Overview() {
  const appearance = useAppearance();
  return (
    <>
      <Intro title="ExItS Premium">
        A Modern Solid design system for the Personal module. Surfaces are quiet, type is restrained, and motion stays short. Theme, primary, corner shape, and density combine from one token set.
      </Intro>
      <Stage label="Current settings">
        <Card title="Theme">{appearance.theme}</Card>
        <Card title="Resolved">{appearance.resolvedTheme}</Card>
        <Card title="Primary">{PALETTE_LABELS[appearance.palette]}</Card>
        <Card title="Shape">{appearance.radius}</Card>
        <Card title="Density">{appearance.density}</Card>
      </Stage>
      <Stage label="Actions">
        <Button>Primary</Button>
        <Button variant="secondary">Secondary</Button>
        <Button variant="ghost">Ghost</Button>
      </Stage>
      <p className="type-caption">Tab through the page. The focus ring is shared. Theme changes swap colors immediately, without a fade.</p>
    </>
  );
}

function Colors({ palette }: { palette: (typeof PALETTES)[number] }) {
  return (
    <>
      <Intro title="Colors">
        Semantic colors stay independent from the primary palette. Warning uses #FF5722. Its label is a dark ink because white on that fill does not meet text contrast.
      </Intro>
      <div className={styles.swatchGrid}>
        {COLOR_TOKENS.map(([name, value]) => (
          <div key={name} className={styles.chip}>
            <span className={styles.chipFill} style={{ background: value }} />
            <span className="type-label">{name}</span>
          </div>
        ))}
      </div>
      <Stage label="Primary palettes">
        {PALETTES.map((item) => (
          <span key={item} className="type-label" data-theme="light" data-palette={item} style={{ color: "var(--color-primary)" }}>
            {PALETTE_LABELS[item]}
          </span>
        ))}
      </Stage>
      <div className={styles.pair}>
        <div className={styles.themeFrame} data-theme="light" data-palette={palette}>
          <p className="type-label">Light</p>
          <p className="type-card">Primary text on the page</p>
          <p className="type-secondary">Secondary text stays quiet.</p>
          <div className={styles.frameSurface}>
            <Button>Continue</Button>
          </div>
        </div>
        <div className={styles.themeFrame} data-theme="dark" data-palette={palette}>
          <p className="type-label">Dark</p>
          <p className="type-card">Primary text on the page</p>
          <p className="type-secondary">Secondary text stays quiet.</p>
          <div className={styles.frameSurface}>
            <Button>Continue</Button>
          </div>
        </div>
      </div>
    </>
  );
}

function Typography() {
  const samples: Array<[string, string, string]> = [
    ["type-display", "Display", "Personal finance, clearly"],
    ["type-page", "Page heading", "Design system"],
    ["type-section", "Section heading", "Recent activity"],
    ["type-card", "Card heading", "Available balance"],
    ["type-body", "Body", "A payment recorded today for groceries."],
    ["type-secondary", "Secondary", "Today · sample note"],
    ["type-label", "Label", "Account name"],
    ["type-caption", "Caption", "Sample data · not connected"],
    ["type-numeric", "Numeric", "₱24,580.00"],
  ];
  return (
    <>
      <Intro title="Typography">
        System fonts, a stable scale, and tabular figures. Sizes use rem so browser zoom and text scaling keep working. Labels stay at 14px or larger.
      </Intro>
      <div className={styles.stack}>
        {samples.map(([className, name, sample]) => (
          <div key={name}>
            <p className="type-caption">{name}</p>
            <p className={className}>{sample}</p>
          </div>
        ))}
      </div>
    </>
  );
}

function Spacing() {
  const steps = [1, 2, 3, 4, 5, 6, 8, 10];
  return (
    <>
      <Intro title="Spacing">
        The scale stays fixed. Density changes control height, page padding, card padding, and the gap between stacked items.
      </Intro>
      <div className={styles.spaceList}>
        {steps.map((step) => (
          <div key={step} className={styles.spaceRow}>
            <span className="type-label">{step}</span>
            <span className={styles.spaceBar} style={{ width: `var(--space-${step})` }} />
          </div>
        ))}
      </div>
    </>
  );
}

function Radius() {
  const items = [
    ["Button", "var(--radius-button)"],
    ["Input", "var(--radius-control)"],
    ["Card", "var(--radius-card)"],
    ["Dialog", "var(--radius-dialog)"],
  ];
  return (
    <>
      <Intro title="Radius">
        Soft is the default: buttons 14px, inputs 12px, cards 18px, dialogs 24px. Pill rounds controls only. Cards, dialogs, and sheets stay practical.
      </Intro>
      <div className={styles.radiusGrid}>
        {items.map(([name, radius]) => (
          <div key={name} className={styles.radiusBox} style={{ borderRadius: radius } as CSSProperties}>
            <span className="type-label">{name}</span>
          </div>
        ))}
      </div>
    </>
  );
}

function Elevation() {
  return (
    <>
      <Intro title="Elevation">
        Three quiet levels. Borders do most of the work. Shadows stay short so surfaces do not look stacked for effect.
      </Intro>
      <div className={styles.pair}>
        <Card elevation={1} title="Level 1">Resting surface</Card>
        <Card elevation={2} title="Level 2">Raised surface</Card>
        <Card elevation={3} title="Level 3">Overlay surface</Card>
      </div>
    </>
  );
}

function Motion() {
  const [play, setPlay] = useState(0);
  const [on, setOn] = useState(true);
  const [range, setRange] = useState<"day" | "week">("week");
  return (
    <>
      <Intro title="Motion">
        Press, selection, and entrance use transform and opacity. Springs settle quickly. Reduced motion shortens the tokens to 1ms and Motion for React follows the system setting.
      </Intro>
      <Stage label="Interactions">
        <Button>Press</Button>
        <Switch label={on ? "On" : "Off"} checked={on} onChange={(event) => setOn(event.target.checked)} />
        <SegmentedControl
          label="Range"
          name="motion-range"
          value={range}
          onChange={setRange}
          options={[
            { value: "day", label: "Day" },
            { value: "week", label: "Week" },
          ]}
        />
      </Stage>
      <Stage label="Card entrance">
        <Button variant="secondary" onClick={() => setPlay((value) => value + 1)}>
          Replay entrance
        </Button>
        <motion.div
          key={play}
          className={styles.motionCard}
          initial={{ opacity: 0, y: 8 }}
          animate={{ opacity: 1, y: 0 }}
          transition={MOTION_SPRING}
        >
          <Card title="Settled">The card moves 8px and fades in.</Card>
        </motion.div>
      </Stage>
    </>
  );
}
