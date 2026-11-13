import { useMemo, useState } from "react";
import { SearchField } from "../components/search/SearchField";
import { PALETTE_LABELS, PALETTES } from "../theme/appearance";
import { ColorShadeButton } from "./ColorShadeButton";
import styles from "./colors-page.module.scss";
import page from "./Playground.module.scss";
import {
  REFERENCE_PALETTES,
  countReferenceSwatches,
  filterReferencePalettes,
  referenceShadeLabel,
  referenceShadeName,
} from "./reference-palettes";
import { ReferenceShadeButton, type ReferenceCopyMode } from "./ReferenceShadeButton";

const STEPS = [50, 100, 200, 300, 400, 500, 600, 700, 800, 900] as const;

const SEMANTIC = [
  ["Primary", "--color-primary"],
  ["Secondary", "--color-secondary"],
  ["Accent", "--color-accent"],
  ["Success", "--color-success"],
  ["Info", "--color-info"],
  ["Warning", "--color-warning"],
  ["Help", "--color-help"],
  ["Danger", "--color-danger"],
  ["Contrast", "--color-contrast"],
] as const;

const FOUNDATION = [
  ["Page", "--color-page"],
  ["Surface", "--color-surface"],
  ["Text", "--color-text"],
  ["Border", "--color-border"],
  ["Disabled", "--color-disabled-fill"],
  ["Overlay", "--color-overlay"],
] as const;

function TokenColorFamily({ title, prefix, tokenPrefix }: { title: string; prefix: string; tokenPrefix: string }) {
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

function SwatchGrid({ items }: { items: readonly (readonly [string, string])[] }) {
  return (
    <div className={styles.semanticGrid}>
      {items.map(([name, token]) => (
        <ColorShadeButton key={token} label={name} token={token} variant="semantic" />
      ))}
    </div>
  );
}

export function ColorsSection() {
  const [query, setQuery] = useState("");
  const [copyMode, setCopyMode] = useState<ReferenceCopyMode>("hex");

  const filtered = useMemo(() => filterReferencePalettes(query), [query]);
  const visibleSwatches = useMemo(
    () => filtered.reduce((total, family) => total + family.matchingSteps.length, 0),
    [filtered],
  );
  const totalSwatches = countReferenceSwatches();

  return (
    <div className={styles.page}>
      <header className={`${page.intro} ${styles.intro}`}>
        <h1 className="type-page">Colors</h1>
        <p className="type-secondary">Semantic roles · foundation · theme palettes · reference library. Warning stays #FF5722.</p>
      </header>

      <section className={styles.section} aria-labelledby="semantic-colors">
        <h2 id="semantic-colors" className={styles.sectionTitle}>
          Semantic Colors
        </h2>
        <SwatchGrid items={SEMANTIC} />
      </section>

      <section className={styles.section} aria-labelledby="foundation-colors">
        <h2 id="foundation-colors" className={styles.sectionTitle}>
          Foundation Colors
        </h2>
        <SwatchGrid items={FOUNDATION} />
      </section>

      <section className={styles.section} aria-labelledby="neutral-scale">
        <h2 id="neutral-scale" className={styles.sectionTitle}>
          Neutral Scale
        </h2>
        <div className={styles.neutralWrap}>
          <TokenColorFamily title="Neutral" prefix="neutral" tokenPrefix="--neutral" />
        </div>
      </section>

      <section className={styles.section} aria-labelledby="color-palettes">
        <h2 id="color-palettes" className={styles.sectionTitle}>
          Color Palettes
        </h2>
        <div className={styles.families}>
          {PALETTES.map((palette) => (
            <TokenColorFamily
              key={palette}
              title={palette === "teal" ? `${PALETTE_LABELS[palette]} · default` : PALETTE_LABELS[palette]}
              prefix={palette}
              tokenPrefix={`--palette-${palette}`}
            />
          ))}
        </div>
      </section>

      <section className={styles.section} aria-labelledby="reference-library">
        <div className={styles.referenceHeader}>
          <div>
            <h2 id="reference-library" className={styles.sectionTitle}>
              Reference Color Library
            </h2>
            <p className={styles.referenceHint}>
              {REFERENCE_PALETTES.length} families · {totalSwatches} swatches · reference only
            </p>
          </div>
          <div className={styles.referenceTools}>
            <SearchField
              label="Search reference colors"
              value={query}
              onChange={setQuery}
              placeholder="Name, shade, or HEX"
              appearance="soft"
            />
            <div className={styles.copyMode} role="group" aria-label="Copy mode">
              <button
                type="button"
                className={styles.copyModeButton}
                data-active={copyMode === "hex" ? "true" : undefined}
                aria-pressed={copyMode === "hex"}
                onClick={() => setCopyMode("hex")}
              >
                HEX
              </button>
              <button
                type="button"
                className={styles.copyModeButton}
                data-active={copyMode === "name" ? "true" : undefined}
                aria-pressed={copyMode === "name"}
                onClick={() => setCopyMode("name")}
              >
                Name
              </button>
            </div>
          </div>
        </div>

        <p className={styles.referenceMeta} aria-live="polite">
          Showing {visibleSwatches} of {totalSwatches}
          {query.trim() ? ` · “${query.trim()}”` : ""}
          {` · click copies ${copyMode === "hex" ? "HEX" : "name"}`}
        </p>

        {filtered.length === 0 ? (
          <p className={styles.empty}>No reference shades match that search.</p>
        ) : (
          <div className={styles.families}>
            {filtered.map((family) => (
              <article key={family.id} className={styles.family} data-reference-family={family.id}>
                <h3 className={styles.familyTitle}>{family.label}</h3>
                <div className={styles.stack}>
                  {family.matchingSteps.map((step) => {
                    const name = referenceShadeName(family.id, step);
                    return (
                      <ReferenceShadeButton
                        key={name}
                        name={name}
                        label={referenceShadeLabel(family.label, step)}
                        hex={family.shades[step]}
                        copyMode={copyMode}
                      />
                    );
                  })}
                </div>
              </article>
            ))}
          </div>
        )}
      </section>
    </div>
  );
}
