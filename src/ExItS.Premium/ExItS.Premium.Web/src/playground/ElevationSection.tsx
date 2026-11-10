import { useLayoutEffect, useRef, useState } from "react";
import { Button } from "../components/button/Button";
import { Card } from "../components/card/Card";
import { Dialog } from "../components/dialog/Dialog";
import { DropdownMenu } from "../components/menu/DropdownMenu";
import { useAppearance } from "../theme/AppearanceProvider";
import { CopyControl } from "./CopyControl";
import styles from "./catalog.module.scss";
import page from "./Playground.module.scss";

const LEVELS = [
  { level: 0, name: "Flat", usage: "Backgrounds, lists, and inputs", token: "--elevation-0" },
  { level: 1, name: "Resting", usage: "Regular cards and sections", token: "--elevation-1" },
  { level: 2, name: "Raised", usage: "Floating cards and hover surfaces", token: "--elevation-2" },
  { level: 3, name: "Floating", usage: "Dropdowns, menus, and popovers", token: "--elevation-3" },
  { level: 4, name: "Overlay", usage: "Dialogs and prominent overlays", token: "--elevation-4" },
] as const;

function ElevationSurface({ level, name, usage, token }: (typeof LEVELS)[number]) {
  const { resolvedTheme } = useAppearance();
  const ref = useRef<HTMLElement>(null);
  const [shadow, setShadow] = useState("");

  useLayoutEffect(() => {
    const node = ref.current;
    if (!node) return;
    setShadow(getComputedStyle(node).boxShadow);
  }, [resolvedTheme, token]);

  return (
    <article ref={ref} className={styles.level} data-level={level}>
      <p className={styles.name}>Level {level} · {name}</p>
      <p className={styles.usage}>{usage}</p>
      <p className={styles.token}>{token}</p>
      <p className={styles.hex}>{shadow}</p>
      <CopyControl label={`${token} ${name}`} value={token} />
    </article>
  );
}

export function ElevationSection() {
  const [open, setOpen] = useState(false);
  const [choice, setChoice] = useState("None");

  return (
    <>
      <header className={page.intro}>
        <h1 className="type-page">Elevation</h1>
        <p className="type-secondary">
          Five levels, 0 through 4. Shadows stay short. Light and dark use different values. Palette, shape, and density do not change elevation.
        </p>
      </header>

      <section className={styles.section} aria-labelledby="elevation-levels">
        <h2 id="elevation-levels" className="type-section">Five levels</h2>
        <p className={`type-secondary ${styles.lead}`}>
          Each surface below uses the elevation token directly. Level 0 has no shadow. A border separates it from the page.
        </p>
        <div className={styles.grid}>
          {LEVELS.map((item) => (
            <ElevationSurface key={item.level} {...item} />
          ))}
        </div>
      </section>

      <section className={styles.section} aria-labelledby="elevation-compare">
        <h2 id="elevation-compare" className="type-section">Shadow progression</h2>
        <div className={styles.compare}>
          {LEVELS.map((item) => (
            <div key={item.level} className={styles.plate} data-level={item.level}>
              <span className={styles.name}>{item.level} {item.name}</span>
            </div>
          ))}
        </div>
      </section>

      <section className={styles.section} aria-labelledby="elevation-use">
        <h2 id="elevation-use" className="type-section">Component usage</h2>
        <div className={styles.examples}>
          <Card elevation={1} title="Standard card">
            <p className="type-secondary">Level 1. The card shadow is --elevation-1.</p>
          </Card>
          <Card elevation={2} title="Raised card">
            <p className="type-secondary">Level 2. The card shadow is --elevation-2.</p>
          </Card>
          <div className={styles.example}>
            <p className="type-label">Dropdown · level 3</p>
            <DropdownMenu
              label="Open menu"
              items={[
                { id: "edit", label: "Edit note", icon: "edit_note" },
                { id: "remove", label: "Remove", icon: "close", tone: "danger" },
              ]}
              onSelect={setChoice}
            />
            <p className="type-caption">Last choice: {choice}. The menu shadow is --elevation-3.</p>
          </div>
          <div className={styles.example}>
            <p className="type-label">Dialog · level 4</p>
            <Button onClick={() => setOpen(true)}>Open dialog</Button>
            <Dialog
              open={open}
              title="Sample dialog"
              description="This dialog uses elevation level 4."
              onClose={() => setOpen(false)}
              footer={<Button onClick={() => setOpen(false)}>Close</Button>}
            >
              <p className="type-secondary">The dialog shadow is --elevation-4. The scrim is --color-overlay.</p>
            </Dialog>
          </div>
        </div>
      </section>

      <section className={styles.section} aria-labelledby="elevation-themes">
        <h2 id="elevation-themes" className="type-section">Light and dark</h2>
        <div className={styles.pair}>
          {(["light", "dark"] as const).map((theme) => (
            <div key={theme} className={styles.frame} data-theme={theme}>
              <p className="type-label">{theme === "light" ? "Light" : "Dark"}</p>
              <div className={styles.compare}>
                {LEVELS.map((item) => (
                  <div key={item.level} className={styles.plate} data-level={item.level}>
                    <span className={styles.name}>{item.level}</span>
                  </div>
                ))}
              </div>
            </div>
          ))}
        </div>
      </section>

      <section className={styles.section} aria-labelledby="elevation-stack">
        <h2 id="elevation-stack" className="type-section">Elevation and stacking</h2>
        <div className={styles.note}>
          <p className="type-secondary">
            Elevation tokens are shadows. Stacking uses separate tokens: --z-sticky 20, --z-dropdown 40, --z-menu 50, and --z-toast 80. Dialogs and sheets use the browser top layer, so they do not take a z-index token.
          </p>
          <p className="type-secondary">
            Changing the primary palette, corner shape, or density does not change these shadows or stacking values.
          </p>
        </div>
      </section>
    </>
  );
}
