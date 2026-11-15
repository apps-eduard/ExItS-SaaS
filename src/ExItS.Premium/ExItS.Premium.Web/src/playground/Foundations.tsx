import { useLayoutEffect, useRef, useState, type CSSProperties, type ReactNode } from "react";
import { motion } from "motion/react";
import { MOTION_SPRING } from "../motion/motion";
import { Badge } from "../components/badge/Badge";
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

const TYPE_ROLES = [
  { className: "type-display", name: "Display", token: "--type-display", sample: "Personal finance, clearly" },
  { className: "type-page", name: "Page title", token: "--type-page", sample: "Payments" },
  { className: "type-section", name: "Section heading", token: "--type-section", sample: "Recent activity" },
  { className: "type-card", name: "Card title", token: "--type-card", sample: "Available balance" },
  { className: "type-body", name: "Body", token: "--type-body", sample: "A payment recorded today for groceries." },
  { className: "type-secondary", name: "Supporting text", token: "--type-secondary", sample: "Today · sample note" },
  { className: "type-label", name: "Label", token: "--type-label", sample: "Account name" },
  { className: "type-caption", name: "Caption", token: "--type-caption", sample: "Sample data · not connected" },
  { className: "type-numeric", name: "Numeric", token: "--type-numeric", sample: "₱24,580.00" },
] as const;

function formatPx(value: string) {
  const parsed = Number.parseFloat(value);
  if (!Number.isFinite(parsed)) return value;
  const rounded = Math.round(parsed * 10) / 10;
  return Number.isInteger(rounded) ? `${rounded}px` : `${rounded.toFixed(1)}px`;
}

function formatLineHeight(style: CSSStyleDeclaration) {
  const size = Number.parseFloat(style.fontSize);
  const line = Number.parseFloat(style.lineHeight);
  if (!Number.isFinite(size) || !Number.isFinite(line) || size <= 0) return style.lineHeight;
  return String(Math.round((line / size) * 100) / 100);
}

function formatTypeFacts(style: CSSStyleDeclaration) {
  return `${formatPx(style.fontSize)} · ${style.fontWeight} · ${formatLineHeight(style)}`;
}

function Typography() {
  return (
    <>
      <Intro title="Typography">
        System font stack and the nine locked roles. Body stays 16px in Compact, Standard, and Large. Density changes control, button, form label, list, and badge type. Display, page title, and numeric sizes follow the viewport.
      </Intro>
      <section className={styles.stack} aria-labelledby="type-scale-heading">
        <h2 id="type-scale-heading" className="type-section">
          Scale
        </h2>
        <p className="type-secondary">Each sample uses its production class. The figures are the size, weight, and line height in this viewport.</p>
        <div className={styles.typeScale}>
          {TYPE_ROLES.map((role) => (
            <TypeRoleRow key={role.className} role={role} />
          ))}
        </div>
      </section>
      <section className={styles.stack} aria-labelledby="type-use-heading">
        <h2 id="type-use-heading" className="type-section">
          In use
        </h2>
        <p className="type-secondary">
          A production card with a heading, label, body, caption, and numeric total. Secondary and caption colors follow Light and Dark. Primary ink follows the selected palette.
        </p>
        <Card eyebrow="Today" title="Available balance">
          <p className="type-numeric">₱24,580.00</p>
          <p className="type-body">A payment recorded today for groceries stays on body type at every density.</p>
          <p className="type-secondary">Today · sample note</p>
          <p className="type-label">Account name</p>
          <p className="type-caption">Sample data · not connected</p>
        </Card>
        <p className={`type-label ${styles.primaryInk}`}>Primary follows the selected palette.</p>
      </section>
      <section className={styles.stack} aria-labelledby="type-density-heading">
        <h2 id="type-density-heading" className="type-section">
          Density
        </h2>
        <p className="type-secondary">
          Page title and body stay on the type scale. Body measures 16px in each column. Control text, button text, the form label, list text, and the badge follow that column’s density.
        </p>
        <div className={`${styles.densityGrid} ${styles.typeDensity}`}>
          <TypeDensityColumn density="compact" title="Compact" />
          <TypeDensityColumn density="standard" title="Standard" />
          <TypeDensityColumn density="large" title="Large" />
        </div>
      </section>
      <NumericComparison />
      <section className={styles.stack} aria-labelledby="type-responsive-heading">
        <h2 id="type-responsive-heading" className="type-section">
          Responsive text
        </h2>
        <p className="type-secondary">
          Display, page title, and numeric sizes use clamp, so the measured size moves between the role minimum and maximum as the viewport changes. Headings and body wrap. Single-line chrome truncates.
        </p>
        <div className={styles.behaviorGrid}>
          <div className={styles.themeSample}>
            <p className="type-label">Wrapping</p>
            <p className={`type-page ${styles.wrapSample}`}>A long page title stays inside the column when the line runs past the available width.</p>
            <p className={`type-body ${styles.wrapSample}`}>Body text wraps onto the next line. It stays 16px instead of shrinking to fit the column.</p>
          </div>
          <div className={styles.themeSample}>
            <p className="type-label">Truncation</p>
            <p className={`type-card ${styles.truncateSample}`}>North market groceries and household account</p>
            <p className="type-caption">Single-line chrome ends with an ellipsis. Headings and body keep wrapping.</p>
          </div>
        </div>
      </section>
    </>
  );
}

const NUMERIC_REVIEW = [
  {
    id: "balanced",
    name: "B · Balanced",
    detail: "26–32px · 600",
    note: "Production numeric role. Locked.",
  },
  {
    id: "previous",
    name: "A · Previous",
    detail: "28–36px · 700",
    note: "Earlier production size. Not used.",
  },
  {
    id: "minimal",
    name: "C · Minimal",
    detail: "26–28px · 500",
    note: "Not chosen.",
  },
] as const;

const NUMERIC_AMOUNT = "₱24,580.00";
const NUMERIC_LONG_AMOUNT = "₱1,234,567,890.50";

function financialBreaks(amount: string) {
  return amount.replaceAll(",", ",\u200b").replace(".", "\u200b.");
}

function NumericComparison() {
  return (
    <section className={styles.stack} aria-labelledby="type-numeric-heading">
      <h2 id="type-numeric-heading" className="type-section">
        Financial numeric
      </h2>
      <p className="type-secondary">
        B is the locked production role: 26–32px, weight 600, line height 1.1, tabular figures, text color. A and C stay here as the comparison that was not chosen. A dashboard balance uses this role. A transaction row stays on list type.
      </p>
      <div className={styles.numericCompare}>
        {NUMERIC_REVIEW.map((style) => (
          <NumericReviewCard key={style.id} style={style} />
        ))}
      </div>
      <div className={styles.themeSample}>
        <p className="type-label">Transaction row</p>
        <p className={styles.txn}>
          <span>Groceries</span>
          <span>{NUMERIC_AMOUNT}</span>
        </p>
        <p className="type-caption">List type. The amount does not use the numeric size.</p>
      </div>
    </section>
  );
}

function NumericReviewCard({ style }: { style: (typeof NUMERIC_REVIEW)[number] }) {
  const amountRef = useRef<HTMLParagraphElement>(null);
  const [facts, setFacts] = useState("Measuring");

  useLayoutEffect(() => {
    const node = amountRef.current;
    if (!node) return;
    const read = () => {
      const computed = getComputedStyle(node);
      if (!computed.fontSize) return;
      setFacts(formatTypeFacts(computed));
    };
    read();
    const observer = typeof ResizeObserver === "undefined" ? null : new ResizeObserver(read);
    observer?.observe(document.documentElement);
    window.addEventListener("resize", read);
    return () => {
      observer?.disconnect();
      window.removeEventListener("resize", read);
    };
  }, []);

  const amountClass =
    style.id === "balanced"
      ? `type-numeric ${styles.numericReview}`
      : `${styles.numericReview} ${styles.numericPreview}`;

  return (
    <Card eyebrow="Available balance" title={style.name}>
      <p className="type-caption">
        {style.detail}. {style.note}
      </p>
      <p ref={amountRef} className={amountClass} data-numeric={style.id} data-amount="primary">
        {NUMERIC_AMOUNT}
      </p>
      <p className={`type-caption ${styles.typeFacts}`}>{facts}</p>
      <p className={amountClass} data-numeric={style.id} data-amount="long">
        {financialBreaks(NUMERIC_LONG_AMOUNT)}
      </p>
      <p className="type-caption">Long amounts wrap. They are not truncated.</p>
    </Card>
  );
}

function TypeRoleRow({ role }: { role: (typeof TYPE_ROLES)[number] }) {
  const sampleRef = useRef<HTMLParagraphElement>(null);
  const [facts, setFacts] = useState("Measuring");

  useLayoutEffect(() => {
    const node = sampleRef.current;
    if (!node) return;
    const read = () => {
      const style = getComputedStyle(node);
      if (!style.fontSize) return;
      setFacts(formatTypeFacts(style));
    };
    read();
    const observer = typeof ResizeObserver === "undefined" ? null : new ResizeObserver(read);
    observer?.observe(document.documentElement);
    window.addEventListener("resize", read);
    return () => {
      observer?.disconnect();
      window.removeEventListener("resize", read);
    };
  }, []);

  return (
    <div className={styles.typeRow}>
      <div className={styles.typeMeta}>
        <p className="type-label">{role.name}</p>
        <p className="type-caption">{`${role.className} · ${role.token}-size`}</p>
        <p className={`type-caption ${styles.typeFacts}`}>{facts}</p>
      </div>
      <p ref={sampleRef} className={`${role.className} ${styles.typeSample}`} data-role={role.className}>
        {role.sample}
      </p>
    </div>
  );
}

function TypeDensityColumn({ density, title }: { density: "compact" | "standard" | "large"; title: string }) {
  return (
    <section className={styles.densityColumn} data-density={density} aria-label={`${title} type`}>
      <h3 className="type-section">{title}</h3>
      <p className="type-page" data-measure="page">
        Payments
      </p>
      <p className="type-body" data-measure="body">
        Body stays 16px.
      </p>
      <Badge className={styles.typeBadge} tone="primary" data-measure="badge">
        Paid
      </Badge>
      <Input label="Account name" placeholder="Name" />
      <Button>Save</Button>
      <p className={styles.txn} data-measure="list">
        <span>Groceries</span>
        <span>₱240.00</span>
      </p>
      <TypeDensityFacts />
    </section>
  );
}

function TypeDensityFacts() {
  const rootRef = useRef<HTMLDListElement>(null);
  const [rows, setRows] = useState<Array<{ label: string; value: string }>>([]);

  useLayoutEffect(() => {
    const root = rootRef.current;
    const column = root?.closest("section");
    if (!root || !column) return;
    const read = () => {
      const page = column.querySelector('[data-measure="page"]');
      const body = column.querySelector('[data-measure="body"]');
      const badge = column.querySelector('[data-measure="badge"]');
      const input = column.querySelector("input");
      const button = column.querySelector("button");
      const label = column.querySelector("label");
      const list = column.querySelector('[data-measure="list"]');
      if (!page || !body || !badge || !input || !button || !label || !list) return;
      const size = (node: Element) => formatPx(getComputedStyle(node).fontSize);
      setRows([
        { label: "Page title", value: size(page) },
        { label: "Body", value: size(body) },
        { label: "Badge", value: size(badge) },
        { label: "Control", value: size(input) },
        { label: "Button", value: size(button) },
        { label: "Form label", value: size(label) },
        { label: "List", value: size(list) },
      ]);
    };
    read();
    const observer = typeof ResizeObserver === "undefined" ? null : new ResizeObserver(read);
    observer?.observe(document.documentElement);
    window.addEventListener("resize", read);
    return () => {
      observer?.disconnect();
      window.removeEventListener("resize", read);
    };
  }, []);

  return (
    <dl ref={rootRef} className={styles.typeMetrics}>
      {rows.map((row) => (
        <div key={row.label}>
          <dt>{row.label}</dt>
          <dd>{row.value}</dd>
        </div>
      ))}
    </dl>
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
        { label: "Button", value: getComputedStyle(button).minHeight },
        { label: "Switch", value: `${columnStyle.getPropertyValue("--switch-width").trim()} × ${columnStyle.getPropertyValue("--switch-height").trim()}` },
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
