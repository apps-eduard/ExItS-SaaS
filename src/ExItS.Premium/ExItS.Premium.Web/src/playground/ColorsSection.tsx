import { PALETTE_LABELS, PALETTES } from "../theme/appearance";
import { ColorShadeButton } from "./ColorShadeButton";
import styles from "./colors-page.module.scss";
import page from "./Playground.module.scss";

const STEPS = [50, 100, 200, 300, 400, 500, 600, 700, 800, 900] as const;

const SEMANTIC = [
  ["Page", "--color-page"],
  ["Text", "--color-text"],
  ["Primary", "--color-primary"],
  ["Success", "--color-success"],
  ["Warning", "--color-warning"],
  ["Danger", "--color-danger"],
  ["Information", "--color-info"],
] as const;

function ColorFamily({ title, prefix, tokenPrefix }: { title: string; prefix: string; tokenPrefix: string }) {
  return (
    <article className={styles.family}>
      <h3 className={styles.familyTitle}>{title}</h3>
      <div className={styles.stack}>
        {STEPS.map((step) => (
          <ColorShadeButton key={step} label={`${prefix}-${step}`} token={`${tokenPrefix}-${step}`} />
        ))}
      </div>
    </article>
  );
}

export function ColorsSection() {
  return (
    <div className={styles.page}>
      <header className={`${page.intro} ${styles.intro}`}>
        <h1 className="type-page">Colors</h1>
        <p className="type-secondary">Click any shade to copy its HEX. Warning stays #FF5722.</p>
      </header>

      <section className={styles.section} aria-labelledby="semantic-colors">
        <h2 id="semantic-colors" className={styles.sectionTitle}>
          Semantic Colors
        </h2>
        <div className={styles.semanticGrid}>
          {SEMANTIC.map(([name, token]) => (
            <ColorShadeButton key={token} label={name} token={token} variant="semantic" />
          ))}
        </div>
      </section>

      <section className={styles.section} aria-labelledby="neutral-scale">
        <h2 id="neutral-scale" className={styles.sectionTitle}>
          Neutral Scale
        </h2>
        <div className={styles.neutralWrap}>
          <ColorFamily title="Neutral" prefix="neutral" tokenPrefix="--neutral" />
        </div>
      </section>

      <section className={styles.section} aria-labelledby="color-palettes">
        <h2 id="color-palettes" className={styles.sectionTitle}>
          Color Palettes
        </h2>
        <div className={styles.families}>
          {PALETTES.map((palette) => (
            <ColorFamily
              key={palette}
              title={palette === "teal" ? `${PALETTE_LABELS[palette]} · default` : PALETTE_LABELS[palette]}
              prefix={palette}
              tokenPrefix={`--palette-${palette}`}
            />
          ))}
        </div>
      </section>
    </div>
  );
}
