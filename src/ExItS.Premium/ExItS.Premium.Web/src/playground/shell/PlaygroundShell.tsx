import type { ReactNode } from "react";
import styles from "./PlaygroundShell.module.scss";

export type PlaygroundShellProps = {
  title: string;
  description: ReactNode;
  preview: ReactNode;
  properties: ReactNode;
  examples?: ReactNode;
  previewLabel?: string;
  propertiesLabel?: string;
  examplesLabel?: string;
  className?: string;
};

export function PlaygroundShell({
  title,
  description,
  preview,
  properties,
  examples,
  previewLabel = "Preview",
  propertiesLabel = "Properties",
  examplesLabel = "States & examples",
  className,
}: PlaygroundShellProps) {
  const titleId = `${title.toLowerCase().replace(/[^a-z0-9]+/g, "-").replace(/^-|-$/g, "") || "component"}-playground-title`;

  return (
    <section className={[styles.shell, className].filter(Boolean).join(" ")} aria-labelledby={titleId}>
      <header className={styles.intro}>
        <h1 id={titleId} className="type-page">
          {title}
        </h1>
        <p className="type-secondary">{description}</p>
      </header>

      <div className={styles.workspace}>
        <div className={styles.previewPane}>
          <h2 className={styles.paneLabel}>{previewLabel}</h2>
          <div className={styles.previewSurface}>{preview}</div>
        </div>
        <aside className={styles.propertiesPane} aria-label={propertiesLabel}>
          <h2 className={styles.paneLabel}>{propertiesLabel}</h2>
          <div className={styles.propertiesSurface}>{properties}</div>
        </aside>
      </div>

      {examples ? (
        <section className={styles.examples} aria-labelledby={`${titleId}-examples`}>
          <h2 id={`${titleId}-examples`} className={styles.paneLabel}>
            {examplesLabel}
          </h2>
          <div className={styles.examplesSurface}>{examples}</div>
        </section>
      ) : null}
    </section>
  );
}
