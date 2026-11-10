import { useLayoutEffect, useRef, useState, type CSSProperties, type ReactNode } from "react";
import { motion } from "motion/react";
import { MOTION_SPRING } from "../motion/motion";
import { Button } from "../components/button/Button";
import { Card } from "../components/card/Card";
import { Input } from "../components/input/Input";
import { SearchField } from "../components/search/SearchField";
import { Select } from "../components/select/Select";
import { SegmentedControl } from "../components/segmented/SegmentedControl";
import { Switch } from "../components/switch/Switch";
import { PALETTE_LABELS } from "../theme/appearance";
import { useAppearance } from "../theme/AppearanceProvider";
import type { SectionId } from "./nav";
import { ColorsSection } from "./ColorsSection";
import { ElevationSection } from "./ElevationSection";
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

export function Foundations({ id }: { id: SectionId }) {
  if (id === "overview") return <Overview />;
  if (id === "colors") return <ColorsSection />;
  if (id === "typography") return <Typography />;
  if (id === "spacing") return <Spacing />;
  if (id === "radius") return <Radius />;
  if (id === "elevation") return <ElevationSection />;
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
        The spacing scale stays fixed. Density changes control text, button text, form labels, list text, control height, padding, and the gap between stacked items. Page headings, body copy, and totals stay on the type scale.
      </Intro>
      <h2 className="type-section">Density</h2>
      <div className={styles.densityGrid}>
        <DensityColumn density="compact" title="Compact" />
        <DensityColumn density="standard" title="Standard" />
        <DensityColumn density="large" title="Large" />
      </div>
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

function DensityColumn({ density, title }: { density: "compact" | "standard" | "large"; title: string }) {
  const [query, setQuery] = useState("Market");
  const [category, setCategory] = useState("groceries");
  return (
    <section className={styles.densityColumn} data-density={density} aria-label={title}>
      <h3 className="type-section">{title}</h3>
      <DensityMetrics />
      <Input label="Name" placeholder="Account name" hint="Shown on the payment." />
      <SearchField label="Search" value={query} onChange={setQuery} placeholder="Name or note" />
      <Select label="Category" value={category} onChange={(event) => setCategory(event.target.value)}>
        <option value="groceries">Groceries</option>
        <option value="rent">Rent</option>
        <option value="utilities">Utilities</option>
      </Select>
      <Button>Save</Button>
      <Card title="Today">
        <p className={styles.txn}>
          <span>Groceries</span>
          <span className="type-body">₱240.00</span>
        </p>
      </Card>
    </section>
  );
}

function DensityMetrics() {
  const rootRef = useRef<HTMLDListElement>(null);
  const [rows, setRows] = useState<Array<{ label: string; value: string }>>([]);

  useLayoutEffect(() => {
    const root = rootRef.current;
    const column = root?.closest("section");
    if (!root || !column) return;
    const measure = () => {
      const input = column.querySelector("input");
      const button = [...column.querySelectorAll("button")].find((node) => node.textContent?.trim() === "Save");
      const label = column.querySelector("label");
      const row = column.querySelector("article p");
      if (!input || !button || !label || !row) return;
      const inputStyle = getComputedStyle(input);
      const columnStyle = getComputedStyle(column);
      setRows([
        { label: "Control text", value: inputStyle.fontSize },
        { label: "Button text", value: getComputedStyle(button).fontSize },
        { label: "Label", value: getComputedStyle(label).fontSize },
        { label: "List text", value: getComputedStyle(row).fontSize },
        { label: "Height", value: inputStyle.minHeight },
        { label: "Padding", value: inputStyle.paddingLeft },
        { label: "Spacing", value: columnStyle.rowGap },
      ]);
    };
    measure();
    window.addEventListener("resize", measure);
    return () => window.removeEventListener("resize", measure);
  }, []);

  return (
    <dl ref={rootRef} className={styles.densityMetrics}>
      {rows.map((row) => (
        <div key={row.label}>
          <dt>{row.label}</dt>
          <dd>{row.value}</dd>
        </div>
      ))}
    </dl>
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
