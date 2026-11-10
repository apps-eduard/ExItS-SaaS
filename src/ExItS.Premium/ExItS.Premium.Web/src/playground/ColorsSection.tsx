import { useState, type ReactNode } from "react";
import { Badge } from "../components/badge/Badge";
import { Button } from "../components/button/Button";
import { Card } from "../components/card/Card";
import { IconTile } from "../components/icon-tile/IconTile";
import { Input } from "../components/input/Input";
import { SearchField } from "../components/search/SearchField";
import { Tabs } from "../components/tabs/Tabs";
import { ExIcon } from "../icons/ExIcon";
import { FEATURE_ICONS, FEATURE_TONES, type FeatureTone } from "../icons/registry";
import { PALETTE_LABELS, PALETTES, type Palette } from "../theme/appearance";
import { useAppearance } from "../theme/AppearanceProvider";
import { ColorSwatch } from "./ColorSwatch";
import styles from "./catalog.module.scss";
import page from "./Playground.module.scss";

const STEPS = [50, 100, 200, 300, 400, 500, 600, 700, 800, 900] as const;

const PALETTE_NOTES: Record<Palette, string> = {
  teal: "Light uses 700, 800, and 900. Dark uses 400, 300, and 500.",
  blue: "Light uses 700, 800, and 900. Dark uses 300, 200, and 400.",
  violet: "Light uses 700, 800, and 900. Dark uses 300, 200, and 400.",
  orange: "Light uses 700, 800, and 900. Dark uses 300, 200, and 400.",
  rose: "Light uses 700, 800, and 900. Dark uses 300, 200, and 400.",
};

const ROLES = [
  ["Primary", "--color-primary"],
  ["Foreground", "--color-primary-foreground"],
  ["Hover", "--color-primary-hover"],
  ["Pressed", "--color-primary-pressed"],
  ["Subtle", "--color-primary-soft"],
  ["Selected", "--color-primary-selected"],
  ["Focus", "--color-focus"],
] as const;

const STATUSES = [
  ["Success", "success", "check", "Paid"],
  ["Warning", "warning", "warning", "Due"],
  ["Danger", "danger", "error", "Failed"],
  ["Information", "info", "info", "Note"],
] as const;

const STATUS_PARTS = [
  ["Solid", ""],
  ["Foreground", "-foreground"],
  ["Soft", "-soft"],
  ["Border", "-border"],
  ["Icon", "-ink"],
] as const;

const FEATURE_LABELS: Record<FeatureTone, string> = {
  lent: "Lent",
  owe: "Owe",
  people: "People",
  stores: "Stores",
  reminders: "Reminders",
};

const SURFACES = [
  ["Page", "--color-page"],
  ["Card", "--color-surface"],
  ["Elevated", "--color-surface-elevated"],
  ["Input", "--color-surface"],
  ["Hover", "--color-hover"],
  ["Selected", "--color-primary-selected"],
  ["Overlay", "--color-overlay"],
] as const;

const TEXT = [
  ["Primary", "--color-text"],
  ["Secondary", "--color-text-secondary"],
  ["Muted", "--color-text-caption"],
  ["Disabled", "--color-disabled-text"],
  ["Inverse", "--color-text-inverse"],
] as const;

const BORDERS = [
  ["Default", "--color-border"],
  ["Subtle", "--color-border-subtle"],
  ["Strong", "--color-border-strong"],
  ["Focus", "--color-focus"],
  ["Error", "--color-danger"],
] as const;

function Intro({ title, children }: { title: string; children: string }) {
  return (
    <header className={page.intro}>
      <h1 className="type-page">{title}</h1>
      <p className="type-secondary">{children}</p>
    </header>
  );
}

function ThemePair({ children }: { children: (theme: "light" | "dark") => ReactNode }) {
  const { palette } = useAppearance();
  return (
    <div className={styles.pair}>
      {(["light", "dark"] as const).map((theme) => (
        <div key={theme} className={styles.frame} data-theme={theme} data-palette={palette}>
          <p className="type-label">{theme === "light" ? "Light" : "Dark"}</p>
          {children(theme)}
        </div>
      ))}
    </div>
  );
}

export function ColorsSection() {
  const [query, setQuery] = useState("");
  const [tab, setTab] = useState<"paid" | "due">("paid");

  return (
    <>
      <Intro title="Colors">
        One token set colors the playground. The primary palette changes interactive color only. Success, warning, danger, information, and the Personal feature colors stay fixed. Warning remains #FF5722 with dark text.
      </Intro>

      <section className={styles.section} aria-labelledby="color-overview">
        <h2 id="color-overview" className="type-section">Semantic overview</h2>
        <p className={`type-secondary ${styles.lead}`}>
          Neutrals carry pages and type. A palette supplies primary, hover, pressed, and focus. Status and feature colors do not follow the palette setting.
        </p>
        <div className={styles.grid}>
          <ColorSwatch name="Page" token="--color-page" />
          <ColorSwatch name="Text" token="--color-text" />
          <ColorSwatch name="Primary" token="--color-primary" />
          <ColorSwatch name="Success" token="--color-success" />
          <ColorSwatch name="Warning" token="--color-warning" />
          <ColorSwatch name="Danger" token="--color-danger" />
          <ColorSwatch name="Information" token="--color-info" />
        </div>
      </section>

      <section className={styles.section} aria-labelledby="color-neutral">
        <h2 id="color-neutral" className="type-section">Neutral scale</h2>
        <p className={`type-secondary ${styles.lead}`}>
          Ten steps taken from the existing page, border, and text colors. The dark page is slightly deeper than 900 and stays on --color-page.
        </p>
        <div className={styles.grid}>
          {STEPS.map((step) => (
            <ColorSwatch key={step} name={`Neutral ${step}`} token={`--neutral-${step}`} />
          ))}
        </div>
      </section>

      <section className={styles.section} aria-labelledby="color-palettes">
        <h2 id="color-palettes" className="type-section">Primary palettes</h2>
        <p className={`type-secondary ${styles.lead}`}>
          Teal is the default. Each scale is defined once. Light and dark primary, hover, and pressed point at those stops. Neighbor stops are for reference and are not component fills.
        </p>
        {PALETTES.map((palette) => (
          <div key={palette} className={styles.section}>
            <h3 className="type-card">{PALETTE_LABELS[palette]}{palette === "teal" ? " · default" : ""}</h3>
            <p className="type-caption">{PALETTE_NOTES[palette]} Foreground and focus stay outside the scale when the readable ink is white or a dark page color.</p>
            <div className={styles.grid}>
              {STEPS.map((step) => (
                <ColorSwatch key={step} name={`${PALETTE_LABELS[palette]} ${step}`} token={`--palette-${palette}-${step}`} />
              ))}
            </div>
            <div className={styles.pair}>
              {(["light", "dark"] as const).map((theme) => (
                <div key={theme} className={styles.frame} data-theme={theme} data-palette={palette}>
                  <p className="type-label">{theme === "light" ? "Light" : "Dark"}</p>
                  <div className={styles.grid}>
                    {ROLES.map(([name, token]) => (
                      <ColorSwatch key={token} name={name} token={token} />
                    ))}
                  </div>
                </div>
              ))}
            </div>
          </div>
        ))}
      </section>

      <section className={styles.section} aria-labelledby="color-status">
        <h2 id="color-status" className="type-section">Semantic colors</h2>
        <p className={`type-secondary ${styles.lead}`}>
          Status meaning does not change when the primary palette changes. The badge is the real Badge component.
        </p>
        <ThemePair>
          {(theme) => (
            <div className={styles.grid}>
              {STATUSES.map(([name, token, icon, badge]) => (
                <article key={`${theme}-${token}`} className={styles.swatch}>
                  <p className={styles.name}>{name}</p>
                  <div className={styles.grid}>
                    {STATUS_PARTS.map(([part, suffix]) => (
                      <ColorSwatch key={suffix || "solid"} name={part} token={`--color-${token}${suffix}`} />
                    ))}
                  </div>
                  <div className={styles.iconSample}>
                    <ExIcon name={icon} color={`var(--color-${token}-ink)`} label={`${name} icon`} />
                    <Badge tone={token}>{badge}</Badge>
                  </div>
                </article>
              ))}
            </div>
          )}
        </ThemePair>
      </section>

      <section className={styles.section} aria-labelledby="color-personal">
        <h2 id="color-personal" className="type-section">Personal feature colors</h2>
        <p className={`type-secondary ${styles.lead}`}>
          Lent, owe, people, stores, and reminders keep their own colors in light and dark. They do not follow the selected primary.
        </p>
        <ThemePair>
          {(theme) => (
            <div className={styles.grid}>
              {FEATURE_TONES.map((tone) => (
                <article key={`${theme}-${tone}`} className={styles.swatch}>
                  <p className={styles.name}>{FEATURE_LABELS[tone]}</p>
                  <IconTile name={FEATURE_ICONS[tone]} variant="feature" tone={tone} label={FEATURE_LABELS[tone]} />
                  <div className={styles.grid}>
                    <ColorSwatch name="Feature" token={`--color-feature-${tone}`} />
                    <ColorSwatch name="Tinted surface" token={`--color-feature-${tone}-soft`} />
                    <ColorSwatch name="Foreground" token={`--color-feature-${tone}-foreground`} />
                  </div>
                </article>
              ))}
            </div>
          )}
        </ThemePair>
      </section>

      <section className={styles.section} aria-labelledby="color-chrome">
        <h2 id="color-chrome" className="type-section">Surface, text, and border</h2>
        <p className={`type-secondary ${styles.lead}`}>
          Inputs use the card surface. Muted text is the caption token. The error border is the danger color. Overlay is the dialog scrim.
        </p>
        <ThemePair>
          {() => (
            <>
              <h3 className="type-card">Surfaces</h3>
              <div className={styles.grid}>
                {SURFACES.map(([name, token]) => (
                  <ColorSwatch key={name} name={name} token={token} />
                ))}
              </div>
              <h3 className="type-card">Text</h3>
              <div className={styles.grid}>
                {TEXT.map(([name, token]) => (
                  <ColorSwatch key={name} name={name} token={token} />
                ))}
              </div>
              <h3 className="type-card">Borders</h3>
              <div className={styles.grid}>
                {BORDERS.map(([name, token]) => (
                  <ColorSwatch key={name} name={name} token={token} />
                ))}
              </div>
            </>
          )}
        </ThemePair>
      </section>

      <section className={styles.section} aria-labelledby="color-live">
        <h2 id="color-live" className="type-section">Live usage</h2>
        <p className={`type-secondary ${styles.lead}`}>
          These components use the current theme, palette, shape, and density. Change them in Appearance and this group updates.
        </p>
        <div className={styles.examples}>
          <div className={styles.example}>
            <p className="type-label">Buttons</p>
            <div className={styles.row}>
              <Button>Primary</Button>
              <Button variant="secondary">Secondary</Button>
            </div>
          </div>
          <div className={styles.example}>
            <SearchField label="Search" value={query} onChange={setQuery} placeholder="Search people" />
          </div>
          <Card title="Card">
            <p className="type-secondary">Resting surface on the current page color.</p>
          </Card>
          <div className={styles.example}>
            <p className="type-label">Status badges</p>
            <div className={styles.row}>
              <Badge tone="success">Paid</Badge>
              <Badge tone="warning">Due</Badge>
              <Badge tone="danger">Failed</Badge>
              <Badge tone="info">Note</Badge>
            </div>
          </div>
          <div className={styles.example}>
            <p className="type-label">Feature tiles</p>
            <div className={styles.row}>
              {FEATURE_TONES.map((tone) => (
                <IconTile key={tone} name={FEATURE_ICONS[tone]} variant="feature" tone={tone} label={FEATURE_LABELS[tone]} />
              ))}
            </div>
          </div>
          <Tabs
            label="Sample tabs"
            value={tab}
            onChange={setTab}
            tabs={[
              { value: "paid", label: "Paid", content: <p className="type-secondary">Selected tab uses the primary color.</p> },
              { value: "due", label: "Due", content: <p className="type-secondary">Due is the second tab.</p> },
            ]}
          />
          <Input label="Focused field" placeholder="Click to see the focus ring" />
        </div>
      </section>
    </>
  );
}
